// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ViewBinderRegistry.cs
// Purpose: Tracks active view binders by entity and exposes registration events.
// Key concepts:
//   • Assumes main-thread context (Unity-style usage).
//   • Replace-if-exists semantics on Register(..., replaceIfExists: true).
//   • Lightweight dictionary-backed resolver with a Registered event.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Sync
{
    /// <summary>
    /// Default implementation of <see cref="IViewBinderRegistry"/> that stores and resolves
    /// active view binders by <see cref="Entity"/>. Designed for main-thread usage.
    /// </summary>
    public sealed class ViewBinderRegistry : IViewBinderRegistry
    {
        /// <summary>
        /// Mapping from entity to its currently registered view binder.
        /// </summary>
        private readonly Dictionary<Entity, IViewBinder> _registries = new();

        /// <summary>
        /// Creates a new registry and attaches it to the runtime directory for discovery.
        /// </summary>
        public ViewBinderRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachViewBinderRegistry(this);
        }

        /// <summary>
        /// Raised when a binder is registered for an entity.
        /// </summary>
        public event Action<Entity, IViewBinder>? Registered;

        /// <summary>
        /// Registers a binder for <paramref name="e"/>. When <paramref name="replaceIfExists"/> is false
        /// and a binder is already present, the call returns <c>false</c> without modification.
        /// </summary>
        /// <param name="e">Target entity.</param>
        /// <param name="v">Binder to associate.</param>
        /// <param name="replaceIfExists">If true (default), replaces any existing binder.</param>
        /// <returns><c>true</c> if a binder was stored; otherwise <c>false</c>.</returns>
        public bool Register(Entity e, IViewBinder v, bool replaceIfExists = true)
        {
            if (_registries.ContainsKey(e) && !replaceIfExists) return false;
            _registries[e] = v;
            Registered?.Invoke(e, v);
            return true;
        }

        /// <summary>
        /// Unregisters the binder for <paramref name="e"/> if and only if it matches <paramref name="target"/>.
        /// </summary>
        /// <returns><c>true</c> if the target was removed; otherwise <c>false</c>.</returns>
        public bool Unregister(Entity e, IViewBinder target)
            => _registries.TryGetValue(e, out IViewBinder? cur) && ReferenceEquals(cur, target) && _registries.Remove(e);

        /// <summary>
        /// Resolves the current binder for <paramref name="e"/>, or <c>null</c> when none is registered.
        /// </summary>
        public IViewBinder? Resolve(Entity e)
            => _registries.GetValueOrDefault(e);

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public void Clear() => _registries.Clear();
    }
}
