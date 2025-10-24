// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Entity.API.cs
// Purpose: Entity lifecycle API — creation, destruction, and liveness checks.
// Key concepts:
//   • 64-bit handle: upper 32 bits for generation, lower 32 bits for id.
//   • freeIds stack for recycling destroyed entity IDs.
//   • Prevents zombie handles by generation mismatch.
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

namespace ZenECS.Core
{
    public partial class World
    {
        /// <summary>
        /// Creates a new entity.
        /// <para>
        /// - If <paramref name="fixedId"/> is provided, ensures capacity and activates that slot.<br/>
        /// - Otherwise, reuses an id from <c>_freeIds</c> if available; if not, allocates a new id from <c>_nextId</c>.<br/>
        /// - Marks the entity as alive, wraps id + generation into an <see cref="Entity"/>, and fires creation events.
        /// </para>
        /// </summary>
        /// <param name="fixedId">Optional fixed id to activate directly.</param>
        /// <returns>The created <see cref="Entity"/> handle.</returns>
        public Entity CreateEntity(int? fixedId = null)
        {
            int id;
            if (fixedId.HasValue)
            {
                id = fixedId.Value;
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }
            else if (_freeIds.Count > 0)
            {
                id = _freeIds.Pop();
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }
            else
            {
                id = _nextId++;
                EnsureEntityCapacity(id);
                _alive.Set(id, true);
            }

            // The current slot's generation is embedded into the handle.
            var e = new Entity(id, _generation[id]);
            EntityEvents.RaiseCreated(this, e);
            return e;
        }

        /// <summary>
        /// Destroys an entity after validating its liveness.
        /// <para>
        /// - Raises a "DestroyRequested" event.<br/>
        /// - Removes components from all pools.<br/>
        /// - Marks the slot as not alive.<br/>
        /// - Increments the generation counter.<br/>
        /// - Returns the id to <c>_freeIds</c>.<br/>
        /// - Finally raises a "Destroyed" event.
        /// </para>
        /// </summary>
        /// <param name="e">The entity to destroy.</param>
        public void DestroyEntity(Entity e)
        {
            if (!IsAlive(e)) return;

            EntityEvents.RaiseDestroyRequested(this, e);

            foreach (var kv in _pools)
                kv.Value.Remove(e.Id);

            _alive.Set(e.Id, false);

            // Increment generation: ensures that even if the same id is reused, the handle differs.
            _generation[e.Id]++;
            _freeIds.Push(e.Id);

            EntityEvents.RaiseDestroyed(this, e);
        }

        /// <summary>
        /// Destroys all entities currently alive.
        /// <para>
        /// When <paramref name="fireEvents"/> is true, individual Destroy events are fired (slower).<br/>
        /// For fast resets, use the Reset family of methods instead.
        /// </para>
        /// </summary>
        /// <param name="fireEvents">Whether to emit Destroy events for each entity.</param>
        public void DestroyAllEntities(bool fireEvents = false)
        {
            if (!fireEvents)
            {
                // Fast path without firing events. Equivalent to ResetButKeepCapacity.
                ResetButKeepCapacity();
                return;
            }

            // If events are required, call DestroyEntity() for all alive entities.
            // Scan BitSet to find active slots.
            for (int id = 1; id < _alive.Length; id++)
            {
                if (_alive.Get(id))
                {
                    // The standard DestroyEntity path includes events, pool removal, and generation increment.
                    DestroyEntity(new Entity(id, _generation[id]));
                }
            }
        }

        /// <summary>
        /// Returns a list of all currently alive entities.
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var list = new List<Entity>(_nextId);
            for (int id = 1; id < _nextId; id++)
                if (_alive.Get(id))
                    list.Add(new Entity(id, _generation[id]));
            return list;
        }

        /// <summary>
        /// Checks whether an entity is currently alive (both alive bit set and generation matches).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity e) => _alive.Get(e.Id) && _generation[e.Id] == e.Gen;

        /// <summary>
        /// Returns the number of currently alive entities.
        /// </summary>
        /// <remarks>
        /// ⚠️ This method scans the entire BitSet and is therefore O(N).<br/>
        /// Avoid calling it in performance-critical loops. Use only for tooling or diagnostics.
        /// </remarks>
        public int AliveCount => GetAllEntities().Count;

        /// <summary>
        /// Ensures that entity-related arrays and BitSets can address the given id.
        /// Automatically expands generation and alive structures if necessary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEntityCapacity(int id)
        {
            // BitSet expansion and preservation are handled internally by Set().
            if (!_alive.Get(id)) _alive.Set(id, false);

            // Expand the generation array based on the configured growth policy.
            if (id >= _generation.Length)
            {
                int required = id + 1;
                int newLen = ComputeNewCapacity(_generation.Length, required);
                Array.Resize(ref _generation, newLen);
            }
        }

        /// <summary>
        /// Computes a new capacity value based on the current length, required index, and the world's growth policy.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ComputeNewCapacity(int current, int required)
        {
            if (_cfg.GrowthPolicy == GrowthPolicy.Step)
            {
                int step = _cfg.GrowthStep;
                // Round up to the nearest multiple of step.
                int blocks = (required + step - 1) / step;
                return Math.Max(required, blocks * step);
            }
            else // Doubling
            {
                int cap = Math.Max(16, current);
                while (cap < required)
                {
                    int next = cap * 2;
                    // Guarantee at least +256 to avoid too small incremental growth.
                    if (next - cap < 256) next = cap + 256;
                    cap = next;
                }
                return cap;
            }
        }
    }
}
