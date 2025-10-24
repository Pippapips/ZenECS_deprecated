// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Component.API.cs
// Purpose: Component-centric API on the World (add, read, remove, and bulk clear).
// Key concepts:
//   • Type→pool indirection and generic non-boxed fast paths.
//   • Boxed accessors for tooling/snapshot compatibility.
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
        /// Removes all components of the specified entity while keeping the entity itself alive.
        /// </summary>
        /// <param name="entityId">The ID of the entity whose components will be removed.</param>
        public void ClearComponentsOf(int entityId)
        {
            if (entityId <= 0 || entityId >= _alive.Length || !_alive.Get(entityId))
                return;

            foreach (var kv in GetAllPools())
            {
                kv.Value.Remove(entityId);
            }
        }

        /// <summary>
        /// Removes all components from all entities, but keeps the entities themselves alive.
        /// </summary>
        /// <remarks>
        /// Used in tooling or scene transitions to keep the world structure intact while clearing all data.
        /// </remarks>
        public void ClearAllComponents()
        {
            var types = new List<Type>(_pools.Keys);
            foreach (var t in types)
            {
                _pools[t] = CreateEmptyPoolForType(t, _alive.Length);
            }
        }

        /// <summary>
        /// Enumerates all components of the given entity as boxed objects.
        /// </summary>
        public IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e)
        {
            foreach (var kv in _pools)
                if (kv.Value.Has(e.Id))
                    yield return (kv.Key, kv.Value.GetBoxed(e.Id));
        }

        /// <summary>
        /// Adds a component by boxed value.
        /// Creates the corresponding component pool if it does not exist.
        /// </summary>
        public void AddBoxed(Entity e, Type t, object boxed) => GetOrCreatePoolByType(t).SetBoxed(e.Id, boxed);

        /// <summary>
        /// Gets a boxed component value (returns null if not present).
        /// </summary>
        public object? GetBoxed(Entity e, Type t) => GetOrCreatePoolByType(t).GetBoxed(e.Id);

        /// <summary>
        /// Tries to get a boxed component value for the entity.
        /// </summary>
        /// <param name="e">The entity to read from.</param>
        /// <param name="t">The component type.</param>
        /// <param name="boxed">The boxed value if found; otherwise null.</param>
        /// <returns>True if found; otherwise false.</returns>
        public bool TryGetBoxed(Entity e, Type t, out object? boxed)
        {
            var obj = GetBoxed(e, t);
            boxed = obj;
            return obj != null;
        }

        /// <summary>
        /// Checks if an entity currently has a component of type <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasComponentInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            return pool != null && pool.Has(e.Id);
        }

        /// <summary>
        /// Attempts to get a component value of type <typeparamref name="T"/> from the specified entity.
        /// Returns true if the component exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetComponentInternal<T>(Entity e, out T value) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool != null && pool.Has(e.Id))
            {
                value = ((ComponentPool<T>)pool).Get(e.Id);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Adds a component to the specified entity.
        /// Throws an exception if the entity is not alive.
        /// Used internally by systems and the command buffer.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddComponentInternal<T>(Entity e, in T value) where T : struct
        {
            if (!IsAlive(e)) throw new InvalidOperationException($"Add<{typeof(T).Name}>: Entity {e.Id} dead.");
            var pool = GetPool<T>();
            pool.EnsureCapacity(e.Id);
            ref var r = ref ((ComponentPool<T>)pool).Ref(e.Id);
            r = value;
        }

        /// <summary>
        /// Removes the specified component from an entity.
        /// Returns true if the component existed.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool RemoveComponentInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null) return false;
            var had = pool.Has(e.Id);
            pool.Remove(e.Id);
            return had;
        }

        /// <summary>
        /// Retrieves a reference to a component (creating it if necessary).
        /// High-performance path for writing.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T RefComponentInternal<T>(Entity e) where T : struct
        {
            var pool = (ComponentPool<T>)GetPool<T>();
            return ref pool.Ref(e.Id);
        }

        /// <summary>
        /// Retrieves a reference to an existing component of type <typeparamref name="T"/>.
        /// Throws an exception if the component does not exist.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref T RefComponentExistingInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }
    }
}
