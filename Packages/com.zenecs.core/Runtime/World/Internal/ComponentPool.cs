// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: Internal/ComponentPool.cs
// Purpose: Generic component pool (T[]) + presence bitset implementing IComponentPool.
// Key concepts:
//   • O(1) id-indexed access; auto-growth policy.
//   • Boxed accessors for external tooling and snapshots.
//   • Designed for maximum performance in runtime systems and safe use in AOT/IL2CPP.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Internal
{
    /// <summary>
    /// Common interface for all component pools.
    /// Keeps the minimal set of APIs required for snapshot save/load and tooling reflection.
    /// </summary>
    internal interface IComponentPool
    {
        /// <summary>
        /// Ensures that the internal storage is large enough to access the given entity ID.
        /// If necessary, expands the underlying arrays.
        /// </summary>
        void EnsureCapacity(int entityId);

        /// <summary>
        /// Returns whether the entity currently holds this component type.
        /// </summary>
        bool Has(int entityId);

        /// <summary>
        /// Removes the component from the given entity.
        /// Optionally clears the stored data to default.
        /// </summary>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>
        /// Retrieves the component as a boxed value (returns null if not present).
        /// </summary>
        object? GetBoxed(int entityId);

        /// <summary>
        /// Sets the component using a boxed value.
        /// Adds a new component or overwrites an existing one.
        /// </summary>
        void SetBoxed(int entityId, object value);

        /// <summary>
        /// Enumerates all active components in the pool as (entityId, boxed value) pairs.
        /// </summary>
        IEnumerable<(int id, object boxed)> EnumerateAll();

        /// <summary>
        /// Returns the number of active components stored in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Clears all data and resets bit flags — typically used before loading a new snapshot.
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// A strongly-typed pool for value-type components.
    /// Backed by an array for O(1) access and a BitSet for tracking which entity IDs are occupied.
    /// Designed for maximum performance with minimal memory overhead.
    /// </summary> 
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        private const int DefaultInitialCapacity = 256;

        // Core data storage: maps entityId directly to component instance
        private T[] _data;

        // BitSet indicating which entity indices are currently active
        private BitSet _present;

        // Number of components currently stored
        private int _count;

        /// <summary>
        /// Parameterless constructor provided to support reflection and AOT-safe instantiation
        /// through Activator.CreateInstance on closed generic types.
        /// </summary> 
        public ComponentPool() : this(DefaultInitialCapacity) {}

        /// <summary>
        /// Creates a new pool with a specific initial capacity.
        /// </summary>
        public ComponentPool(int initialCapacity /*= DefaultInitialCapacity*/)        
        {
            int cap = Math.Max(1, initialCapacity);
            _data = new T[cap];
            _present = new BitSet(cap);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            // Ensure that internal arrays exist and are ready to use
            if (_data == null || _data.Length == 0)
                _data = new T[DefaultInitialCapacity];

            // The BitSet should never be replaced — only expanded to preserve bit flags
            if (_present == null)
                _present = new BitSet(Math.Max(1, _data.Length));
            else if (_present.Length < _data.Length)
                _present.EnsureCapacity(_data.Length); // Preserve existing bits
        }

        /// <summary>
        /// Gets the number of currently active components.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Ensures that this pool can store a component for the given entityId.
        /// Expands the internal arrays exponentially (×2) to avoid frequent allocations.
        /// </summary>
        public void EnsureCapacity(int entityId)
        {
            EnsureInitialized();
            if (entityId < _data.Length) return;

            int cap = _data.Length == 0 ? 1 : _data.Length;
            while (cap <= entityId) cap <<= 1;

            Array.Resize(ref _data, cap);
            _present.EnsureCapacity(cap); // Preserve bits when expanding
        }

        /// <summary>
        /// Checks whether the specified entity currently has a <typeparamref name="T"/> component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            if (entityId < 0) return false;
            if (_data == null || entityId >= _data.Length) return false;
            if (_present == null) return false;
            var has = _present.Get(entityId);
            return has;
        }

        /// <summary>
        /// Retrieves a reference to the component for writing.
        /// If the component does not exist, it is automatically created and marked as present.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Ref(int entityId)
        {
            EnsureCapacity(entityId);
            if (!_present.Get(entityId))
            {
                _present.Set(entityId, true);
                _count++;
            }
            return ref _data[entityId];
        }

        /// <summary>
        /// Retrieves a reference to an existing component.
        /// Throws an exception if the component is missing.
        /// This is the fastest path for known existing components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T RefExisting(int entityId)
        {
            if (!Has(entityId))
                throw new InvalidOperationException($"Component '{typeof(T).Name}' not present on entity {entityId}.");
            return ref _data[entityId];
        }

        /// <summary>
        /// Returns a copy of the component value (default if not found).
        /// This is slower than Ref() but safe for read-only access.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int entityId)
            => Has(entityId) ? _data[entityId] : default;

        /// <summary>
        /// Attempts to retrieve a component using an out parameter.
        /// Returns true if present, otherwise returns false with default value.
        /// </summary>
        public bool TryGet(int entityId, out T value)
        {
            if (Has(entityId))
            {
                value = _data[entityId];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Removes a component from the given entity.
        /// Optionally clears its memory slot to default value.
        /// </summary>
        public void Remove(int entityId, bool dataClear = true)
        {
            if (!Has(entityId)) return;
            _present.Set(entityId, false);
            _count--;
            if (dataClear)
                _data[entityId] = default;
        }

        /// <summary>
        /// Enumerates all active components with their corresponding entity IDs.
        /// Uses a simple for-loop with bit checks (optimized for JIT).
        /// </summary>
        public IEnumerable<(int id, object boxed)> EnumerateAll()
        {
            var data = _data;
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                if (_present.Get(i))
                    yield return (i, (object)data[i]);
            }
        }

        /// <summary>
        /// Returns the component as a boxed object (null if missing).
        /// Used primarily by reflection or snapshot tools.
        /// </summary>
        public object? GetBoxed(int entityId)
            => Has(entityId) ? (object)_data[entityId] : null;

        /// <summary>
        /// Assigns a boxed component value.
        /// Performs a runtime type check to ensure correctness.
        /// </summary>
        public void SetBoxed(int entityId, object value)
        {
            EnsureInitialized();
            if (value is not T v)
                throw new InvalidCastException(
                    $"SetBoxed type mismatch: value is '{value?.GetType().FullName ?? "null"}' " +
                    $"but pool expects '{typeof(T).FullName}'");

            ref var r = ref Ref(entityId); // Shared for add/update
            r = v;
        }

        /// <summary>
        /// Clears all data and presence flags.
        /// Used when reloading snapshots or resetting the World.
        /// </summary>
        public void ClearAll()
        {
            EnsureInitialized();
            _present.ClearAll();
            _count = 0;
        }
    }
}
