// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.cs
// Purpose: Defines the core World container and central state backing Entity/Component management.
// Key concepts:
//   • Holds pools, entity lifecycle data, and configuration.
//   • Entry point for all ECS operations within a single simulation context.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal;
using ZenECS.Core.Serialization;

namespace ZenECS.Core
{
    /// <summary>
    /// World is the central ECS container. It owns entity lifecycles, component pools,
    /// configuration, and (optionally) hooks/snapshotting. All game/system operations
    /// happen against a single World instance (simulation context).
    /// </summary>
    public sealed partial class World
    {
        private readonly WorldConfig _cfg;

        /// <summary>
        /// Bitset of occupied entity slots (alive flags).
        /// </summary>
        private BitSet _alive;

        /// <summary>
        /// Next id to issue for newly created entities.
        /// Starts at 1 to reserve 0 for "null"/invalid semantics.
        /// </summary>
        private int _nextId = 1;

        /// <summary>
        /// Recycled IDs of destroyed entities (LIFO). When creating a new entity,
        /// the world prefers reusing a freed id from this stack before growing.
        /// </summary>
        private Stack<int> _freeIds;

        /// <summary>
        /// Generation array: per-slot generation counter used to prevent zombie handles.
        /// Increments when an id is destroyed and reused, so stale handles no longer match.
        /// </summary>
        private int[] _generation; // 세대(Generation) 배열: slot별 현재 세대 카운터 → Generation array: per-slot current generation

        /// <summary>
        /// Constructs a World with the given configuration (or defaults).
        /// Initializes liveness bitset, generation array, free-id stack, and the type→pool map.
        /// </summary>
        /// <param name="config">Optional world configuration; if null, defaults are used.</param>
        public World(WorldConfig? config = null)
        {
            _cfg        = config ?? new WorldConfig();

            _alive      = new BitSet(_cfg.InitialEntityCapacity);                         // Bitmap of occupied entity slots
            _generation = new int[_cfg.InitialEntityCapacity];                            // Per-slot generation counters (start at 0)
            _freeIds    = new Stack<int>(_cfg.InitialFreeIdCapacity);                     // Recycled IDs storage for destroyed entities
            _pools       = new Dictionary<Type, IComponentPool>(_cfg.InitialPoolBuckets); // Type→pool map
            _nextId     = 1;                                                              // New entities start from 1
        }
    }
}
