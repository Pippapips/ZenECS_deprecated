﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Pool.cs
// Purpose: Pool creation/get helpers and internal wiring of component storage.
// Key concepts:
//   • Ensures pool existence and capacity before operations.
//   • Centralizes pool management details.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// Mapping of component <see cref="Type"/> to its corresponding <see cref="IComponentPool"/>.
        /// </summary>
        private readonly Dictionary<Type, IComponentPool> _pools;

        /// <summary>
        /// Cache of factory delegates that create new component pools by type.
        /// Used to avoid repeated reflection calls.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<IComponentPool>> _poolFactories = new();

        /// <summary>
        /// Retrieves an existing factory for a given component type, or builds a new one if missing.
        /// </summary>
        /// <param name="compType">The component type to create a pool for.</param>
        /// <returns>A delegate that constructs an <see cref="IComponentPool"/> for the given type.</returns>
        /// <remarks>
        /// - Uses <c>ComponentPool&lt;T&gt;</c> with a parameterless constructor for AOT/IL2CPP safety.<br/>
        /// - The factory is cached in a concurrent dictionary and reused across all worlds.<br/>
        /// - When multiple threads race to add a factory, the first inserted instance wins.
        /// </remarks>
        private static Func<IComponentPool> GetOrBuildPoolFactory(Type compType)
        {
            if (_poolFactories.TryGetValue(compType, out var existing))
                return existing;

            // Build a closed generic type for ComponentPool<T>.
            var closed = typeof(ComponentPool<>).MakeGenericType(compType);

            // ComponentPool<T> exposes a parameterless constructor,
            // which is safe for use under AOT/IL2CPP environments.
            Func<IComponentPool> factory = () => (IComponentPool)Activator.CreateInstance(closed)!;

            // If multiple threads attempt insertion, the first registered factory is used.
            return _poolFactories.GetOrAdd(compType, factory);
        }
    }
}
