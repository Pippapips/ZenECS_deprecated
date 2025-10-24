// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Reset.cs
// Purpose: Reset logic (keep capacity vs hard reset with fresh allocations).
// Key concepts:
//   • Reset(true) keeps arrays; Reset(false) performs full reallocation.
//   • Cleans entities, pools, jobs, and subsystem states.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// Called before Reset — allows subsystems to perform pre-reset cleanup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnBeforeWorldReset(bool keepCapacity);

        /// <summary>
        /// Called after Reset — allows subsystems to rebuild or reinitialize state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void OnAfterWorldReset(bool keepCapacity);

        /// <summary>
        /// Performs subsystem-level resets (command buffers, jobs, hooks, timing, queries, etc.).
        /// </summary>
        private void ResetSubsystems(bool keepCapacity)
        {
            OnBeforeWorldReset(keepCapacity);

            // 1) Command buffers
            ClearAllCommandBuffers();

            // 2) Job scheduler
            ClearAllScheduledJobs();

            // 3) Hooks / event queues
            ClearAllHookQueues();
            
            // 4) Timing counters
            ResetTimingCounters();

            // 5) Query / filter caches
            ResetQueryCaches();

            OnAfterWorldReset(keepCapacity);
        }

        /// <summary>
        /// Entry point for resetting the world.
        /// </summary>
        /// <param name="keepCapacity">
        /// If true, keeps current array capacities; otherwise performs a full rebuild.
        /// </param>
        public void Reset(bool keepCapacity)
        {
            if (keepCapacity) ResetButKeepCapacity();
            else HardReset();
        }

        /// <summary>
        /// Resets the world while retaining current capacities — fastest reset path.
        /// </summary>
        private void ResetButKeepCapacity()
        {
            int entityCap = Math.Max(_alive.Length, _generation?.Length ?? 0);

            // Reinitialize alive / generation arrays but keep their size
            _alive = new BitSet(entityCap);
            if (_generation == null || _generation.Length != entityCap)
                _generation = new int[entityCap];
            else
                Array.Clear(_generation, 0, _generation.Length);

            // Reset ID allocation state
            _nextId = 1; // 0 is reserved
            if (_freeIds == null)
                _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity);
            else
                _freeIds.Clear();

            // Recreate each component pool in an empty state (capacity retained)
            var types = new List<Type>(_pools.Keys);
            foreach (var t in types)
            {
                _pools[t] = CreateEmptyPoolForType(t, entityCap);
            }

            ResetSubsystems(keepCapacity: true);
        }

        /// <summary>
        /// Performs a full reinitialization — discards all data and rebuilds internal structures.
        /// </summary>
        private void HardReset()
        {
            _alive = new BitSet(_cfg.InitialEntityCapacity);
            _generation = new int[_cfg.InitialEntityCapacity];

            _nextId = 1;
            _freeIds = new Stack<int>(_cfg.InitialFreeIdCapacity);

            _pools.Clear();

            ResetSubsystems(keepCapacity: false);
        }

        /// <summary>
        /// Creates a new empty component pool for the given type,
        /// preallocating internal arrays up to the specified capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IComponentPool CreateEmptyPoolForType(Type compType, int cap)
        {
            var closed = typeof(ComponentPool<>).MakeGenericType(compType);
            var ctorWithCap = closed.GetConstructor(new[] { typeof(int) });
            if (ctorWithCap != null)
            {
                return (IComponentPool)Activator.CreateInstance(closed, cap)!;
            }

            // Fallback: use default factory and expand manually.
            var factory = GetOrBuildPoolFactory(compType);
            var pool = factory();
            if (cap > 0) pool.EnsureCapacity(cap - 1);
            return pool;
        }
    }
}
