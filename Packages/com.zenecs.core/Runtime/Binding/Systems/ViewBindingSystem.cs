// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ViewBindingSystem.cs
// Purpose: Binds/unbinds components to views and applies updates during presentation.
// Key concepts:
//   • Rebinds on demand via IViewBinderRegistry.Registered event and internal queue.
//   • Uses fast/boxed invokers depending on World.TryGet<T>(...) availability.
//   • Tracks last applied hashes to skip redundant applies during reconciliation.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    internal sealed class ViewBindingSystem : ISystemLifecycle, IPresentationSystem
    {
        /// <summary>
        /// Applies a component value using either a zero-boxing fast path or the boxed fallback:
        /// - Fast path: if World exposes TryGet&lt;T&gt;(Entity, out T) / TryGetComponentInternal&lt;T&gt;(...).
        /// - Fallback: boxed Apply via BinderInvokerCache.
        /// </summary>
        private void ApplyWithCaches(World w, Entity e, Type componentType, IViewBinder vb, IComponentBinder binder, object? boxedValueIfNeeded)
        {
            var hasGenericTryGet =
                typeof(World)
                    .GetMethods(System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic)
                    .Any(m =>
                        (m.Name == "TryGet" || m.Name == "TryGetComponentInternal") &&
                        m.IsGenericMethodDefinition &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(Entity) &&
                        m.GetParameters()[1].ParameterType.IsByRef);

            if (hasGenericTryGet)
                BinderFastInvokerCache.GetApplyNoBox(componentType)(w, e, vb, binder);
            else
                BinderInvokerCache.GetApply(componentType)(w, e, boxedValueIfNeeded!, vb, binder);
        }

        private readonly Dictionary<(Entity, Type), IComponentBinder> _activeComponentBinders = new();

        // Rebind queue (duplicates suppressed)
        private readonly HashSet<Entity> _rebindPending = new();
        private readonly Dictionary<(Entity, Type), int> _lastHashes = new();

        private readonly IViewBinderRegistry _viewBinderRegistry;
        private readonly IComponentBinderResolver _resolver;
        private readonly IComponentBinderRegistry _componentBinderRegistry;

        public ViewBindingSystem(IViewBinderRegistry viewBinderRegistry,
                                 IComponentBinderRegistry componentBinderRegistry,
                                 IComponentBinderResolver resolver)
        {
            _componentBinderRegistry = componentBinderRegistry;
            _viewBinderRegistry = viewBinderRegistry;
            _resolver = resolver;
        }

        public void Initialize(World w)
        {
            _viewBinderRegistry.Registered += OnViewBinderRegistered;
        }

        public void Shutdown(World w)
        {
            _viewBinderRegistry.Registered -= OnViewBinderRegistered;
            _rebindPending.Clear();
            _activeComponentBinders.Clear();
            _lastHashes.Clear();
        }

        public void Run(World w)
        {
            if (_rebindPending.Count == 0) return;

            foreach (var e in _rebindPending)
            {
                var vb = _viewBinderRegistry.Resolve(e);
                if (vb is null) continue;
                ReconcileEntity(w, e, vb);
            }

            _rebindPending.Clear();
        }

        // Compatibility overload (alpha unused by this system)
        public void Run(World w, float alpha = 1) { Run(w); }

        /// <summary>
        /// Ignore the provided binder in event payload and resolve it at the time of handling.
        /// (Allows deferred handling.)
        /// </summary>
        private void OnViewBinderRegistered(Entity e, IViewBinder _) => RequestReconcile(e);

        internal void NotifyChangedViaFeed(Entity e)
        {
            // Entities processed via batch dispatch within the same frame
            // are removed from the pending Reconcile queue to prevent duplicate Apply calls.
            _rebindPending.Remove(e);
        }
        
        /// <summary>
        /// Can be called externally (scene load, save/load, LOD switch, etc.).
        /// </summary>
        public void RequestReconcile(Entity e)
        {
            // Mark in the pending queue without going through the batch feed.
            _rebindPending.Add(e);
        } 

        public void OnComponentAdded<T>(World w, Entity e, in T v)
        {
            if (v == null) return;

            var key = (e, typeof(T));

            // 1) Resolve view binder; if absent, skip presentation.
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is null) return;

            // 2) Resolve component binder; if unregistered, skip.
            var componentBinder = _componentBinderRegistry.Resolve<T>();
            if (componentBinder is null) return;

            // 3) If already active (defense against duplicate "Added"), just Apply and return.
            if (_activeComponentBinders.TryGetValue(key, out var existing))
            {
                existing.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnApply();
#endif
                return;
            }

            // 4) First-time binding: Bind → Apply → mark as active
            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _activeComponentBinders[key] = componentBinder;
        }

        public void OnComponentChanged<T>(World w, Entity e, in T v)
        {
            if (v == null) return;

            var key = (e, typeof(T));

            // 1) Skip if view binder is missing
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is null) return;

            // 2) If an active component binder exists, Apply only
            if (_activeComponentBinders.TryGetValue(key, out var h) && h is IComponentBinder typedExisting)
            {
                typedExisting.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnApply();
#endif
                return;
            }

            // 3) If none active (missed Added or prior rebind), bind then apply
            var componentBinder = _componentBinderRegistry.Resolve<T>();
            if (componentBinder is null) return;

            componentBinder.Bind(w, e, viewBinder);
            componentBinder.Apply(w, e, v, viewBinder);
#if ZENECS_TRACE
            _traceCenter?.ViewBinding.OnBind();
            _traceCenter?.ViewBinding.OnApply();
#endif
            _activeComponentBinders[key] = componentBinder;
        }

        public void OnComponentRemoved<T>(World w, Entity e)
        {
            var key = (e, typeof(T));

            if (!_activeComponentBinders.TryGetValue(key, out var h))
                return; // already inactive (duplicate Removed, etc.)

            // Obtain the view binder, if any, and unbind safely
            var viewBinder = _viewBinderRegistry.Resolve(e);
            if (viewBinder is not null && h is IComponentBinder typed)
            {
                typed.Unbind(w, e, viewBinder);
#if ZENECS_TRACE
                _traceCenter?.ViewBinding.OnUnbind();
#endif
            }

            // If no view binder exists (already destroyed), just drop the handler
            _activeComponentBinders.Remove(key);
        }

        private void ReconcileEntity(World w, Entity e, IViewBinder vb)
        {
            foreach (var t in _resolver.RegisteredComponentTypes)
            {
                if (w.TryGetBoxed(e, t, out var value) && value is not null)
                {
                    var key = (e, t);
                    var h = value.GetHashCode();
                    if (!_lastHashes.TryGetValue(key, out var prev) || prev != h)
                    {
                        if (!_activeComponentBinders.TryGetValue(key, out var binder))
                        {
                            if (!_resolver.TryResolve(t, out binder) || binder is null) continue;
                            BinderInvokerCache.GetBind(t)(w, e, vb, binder);
                            _activeComponentBinders[key] = binder;
                        }
                        ApplyWithCaches(w, e, t, vb, binder, value);
                        _lastHashes[key] = h;
                    }
                }
                else
                {
                    var key = (e, t);
                    if (_activeComponentBinders.TryGetValue(key, out var binder))
                    {
                        BinderInvokerCache.GetUnbind(t)(w, e, vb, binder);
                        _activeComponentBinders.Remove(key);
                        _lastHashes.Remove(key);
                    }
                }
            }
        }
    }
}
