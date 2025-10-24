// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentEvents.cs
// Purpose: Global hub for broadcasting component add/change/remove events.
// Key concepts:
//   • Lightweight design — no allocation or invocation overhead when no listeners exist.
//   • Called internally by EcsActions after structural writes.
//   • Reset() should be invoked during Runner/World reset to avoid event leaks in editor reloads.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Events
{
    internal static class ComponentEvents
    {
        internal static event Action<World, Entity, Type>? ComponentChanged;
        internal static event Action<World, Entity, Type>? ComponentAdded;
        internal static event Action<World, Entity, Type>? ComponentRemoved;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseChanged(World w, Entity e, Type t) => ComponentChanged?.Invoke(w, e, t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseAdded(World w, Entity e, Type t) => ComponentAdded?.Invoke(w, e, t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseRemoved(World w, Entity e, Type t) => ComponentRemoved?.Invoke(w, e, t);

        /// <summary>
        /// Clears all event subscriptions to prevent leaks after editor domain reloads
        /// or runtime restarts. Should be called during Runner or World reset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Reset()
        {
            ComponentChanged = null;
            ComponentAdded = null;
            ComponentRemoved = null;
        }
    }
}