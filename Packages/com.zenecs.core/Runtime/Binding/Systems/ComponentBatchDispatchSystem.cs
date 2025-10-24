// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentBatchDispatchSystem.cs
// Purpose: Dequeues batched component-change records and invokes ViewBindingSystem callbacks.
// Key concepts:
//   • Subscribes to IComponentChangeFeed and buffers records on a local queue.
//   • Uses GenericInvokerCache to call generic handlers without repeated reflection.
//   • Presentation-stage system; ordered after ComponentBindingHubSystem, before ViewBindingSystem.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    [OrderAfter(typeof(ComponentBindingHubSystem))]
    [OrderBefore(typeof(ViewBindingSystem))]
    internal sealed class ComponentBatchDispatchSystem : ISystemLifecycle, IPresentationSystem
    {
        private IDisposable? _subscriptionForFeed;

        private readonly IComponentChangeFeed _feed;
        private readonly ViewBindingSystem _host;
        private readonly Queue<ComponentChangeRecord> _queue = new();

        public ComponentBatchDispatchSystem(IComponentChangeFeed feed, ViewBindingSystem host)
        {
            _feed = feed;
            _host = host;
        }

        public void Initialize(World w)
        {
            _subscriptionForFeed = _feed.SubscribeRaw(OnBatch);
        }

        public void Shutdown(World w)
        {
            _subscriptionForFeed?.Dispose();
            _subscriptionForFeed = null;
            _queue.Clear();
        }

        public void Run(World w)
        {
            while (_queue.Count > 0)
            {
                var rec = _queue.Dequeue();
                var t = rec.ComponentType;
                if (t is null) continue;

                // ✅ Notify the ViewBindingSystem that this entity was processed via batch dispatch.
                _host.NotifyChangedViaFeed(rec.Entity);
                
                // Removal: call OnComponentRemoved<T>(...)
                if ((rec.Mask & ComponentChangeMask.Removed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentRemoved),
                        genericArg: t,
                        w, rec.Entity);
                    continue;
                }

                // Read current value if still present
                if (!TryGetCurrentValue(w, rec.Entity, t, out var boxed)) continue;

                // Added: call OnComponentAdded<T>(..., in T)
                if ((rec.Mask & ComponentChangeMask.Added) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentAdded),
                        genericArg: t,
                        w, rec.Entity, boxed);
                }

                // Changed: call OnComponentChanged<T>(..., in T)
                if ((rec.Mask & ComponentChangeMask.Changed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentChanged),
                        genericArg: t,
                        w, rec.Entity, boxed);
                }
            }
        }

        // Compatibility overload (alpha unused by this system)
        public void Run(World w, float alpha = 1) { Run(w); }

        private void OnBatch(IReadOnlyList<ComponentChangeRecord> recs)
        {
            foreach (var t in recs) _queue.Enqueue(t);
        }

        private static bool TryGetCurrentValue(World w, Entity e, Type t, out object? value)
            => w.TryGetBoxed(e, t, out value);
    }
}
