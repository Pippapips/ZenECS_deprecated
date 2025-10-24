// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Pool.API.cs
// Purpose: Public-facing API variants around pool management and diagnostics.
// Key concepts:
//   • Pool lookup and safe creation surface.
//   • Optional diagnostics/metrics entry points.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public partial class World
    {
        /// <summary>
        /// Enumerates all component pools currently registered in this world.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<KeyValuePair<Type, IComponentPool>> GetAllPools() => _pools;

        /// <summary>
        /// Retrieves a component pool for <typeparamref name="T"/>.
        /// If the pool does not exist, it is created and registered automatically.
        /// </summary>
        /// <typeparam name="T">Component struct type.</typeparam>
        /// <returns>The corresponding <see cref="IComponentPool"/> for <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IComponentPool GetPool<T>() where T : struct
        {
            var t = typeof(T);
            if (!_pools.TryGetValue(t, out var pool))
            {
                pool = new ComponentPool<T>();
                _pools.Add(t, pool);
            }

            return pool;
        }

        /// <summary>
        /// Attempts to get a component pool for <typeparamref name="T"/>.
        /// Returns <c>null</c> if the pool does not exist.
        /// </summary>
        /// <typeparam name="T">Component struct type.</typeparam>
        /// <returns>The pool if found; otherwise <c>null</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentPool<T>? TryGetPoolInternal<T>() where T : struct
            => _pools.TryGetValue(typeof(T), out var p) ? (ComponentPool<T>)p : null;

        /// <summary>
        /// Retrieves or creates a component pool by <see cref="Type"/> at runtime.
        /// </summary>
        /// <param name="t">The component type.</param>
        /// <returns>The corresponding <see cref="IComponentPool"/> instance.</returns>
        /// <remarks>
        /// - Uses a pre-built thread-safe factory delegate from <see cref="World.GetOrBuildPoolFactory"/>.<br/>
        /// - Ensures the pool is initialized with a minimal capacity (0).<br/>
        /// - Safe for AOT/IL2CPP environments and concurrent access.
        /// </remarks>
        private IComponentPool GetOrCreatePoolByType(Type t)
        {
            if (!_pools.TryGetValue(t, out var pool))
            {
                // ✅ Safe factory creation through GetOrBuildPoolFactory
                var factory = GetOrBuildPoolFactory(t);
                pool = factory();

                // Ensure minimal capacity so the pool is valid for immediate operations.
                pool.EnsureCapacity(0);

                _pools.Add(t, pool);
            }

            return pool;
        }
    }
}
