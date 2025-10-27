    // ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EntityEvents.cs
// Purpose: Global event hub for entity lifecycle notifications.
// Key concepts:
//   • Used internally by World to signal creation and destruction events.
//   • Reset() clears all listeners to avoid leaks during reloads.
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
    internal static class EntityEvents
    {
        internal static event Action<World, Entity>? EntityCreated;
        internal static event Action<World, Entity>? EntityDestroyRequested;
        internal static event Action<World, Entity>? EntityDestroyed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseCreated(World w, Entity e) => EntityCreated?.Invoke(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroyRequested(World w, Entity e) => EntityDestroyRequested?.Invoke(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroyed(World w, Entity e) => EntityDestroyed?.Invoke(w, e);

        /// <summary>
        /// Clears all subscribers to prevent leaks during domain reloads or runtime restarts.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Reset()
        {
            EntityCreated = null;
            EntityDestroyRequested = null;
            EntityDestroyed = null;
        }
    }
}