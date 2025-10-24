// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Config.cs
// Purpose: Defines WorldConfig: initial capacities and growth policies for pools and entities.
// Key concepts:
//   • InitialEntityCapacity, GrowthPolicy, and related tuning knobs.
//   • Affects memory layout and resize behavior.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Defines how arrays and pools expand when capacity is exceeded.
    /// </summary>
    public enum GrowthPolicy
    {
        /// <summary>
        /// Doubles the capacity when expanding (guaranteeing at least +256).
        /// Minimizes reallocation frequency and is suitable for dynamic growth.
        /// </summary>
        Doubling,

        /// <summary>
        /// Expands capacity by a fixed number of slots (GrowthStep) each time.
        /// Provides more predictable memory usage.
        /// </summary>
        Step
    }

    /// <summary>
    /// Configuration struct for <see cref="World"/>.
    /// Controls initial memory capacities and growth behavior of entity arrays and component pools.
    /// </summary>
    public readonly struct WorldConfig
    {
        /// <summary>
        /// Initial entity slot count (used to size arrays like Alive/Generation at startup).
        /// </summary>
        public readonly int InitialEntityCapacity;

        /// <summary>
        /// Initial bucket count of the component pool dictionary.
        /// Higher values reduce hash collisions and rehash frequency.
        /// </summary>
        public readonly int InitialPoolBuckets;

        /// <summary>
        /// Initial capacity of the free-id stack used for recycling entity IDs.
        /// Increase this if entity creation/destruction is frequent.
        /// </summary>
        public readonly int InitialFreeIdCapacity;

        /// <summary>
        /// Array/pool expansion policy when capacity is exceeded (Doubling vs Step).
        /// </summary>
        public readonly GrowthPolicy GrowthPolicy;

        /// <summary>
        /// Number of slots added per expansion when using the Step growth policy (e.g., 256, 512, 1024).
        /// </summary>
        public readonly int GrowthStep;

        /// <summary>
        /// Initializes a new <see cref="WorldConfig"/> with specified or default tuning parameters.
        /// </summary>
        /// <param name="initialEntityCapacity">Initial entity slot count.</param>
        /// <param name="initialPoolBuckets">Initial hash bucket count for component pools.</param>
        /// <param name="initialFreeIdCapacity">Initial capacity for the free-id recycle stack.</param>
        /// <param name="growthPolicy">Expansion policy (Doubling or Step).</param>
        /// <param name="growthStep">Number of slots to add per expansion in Step mode.</param>
        public WorldConfig(
            int initialEntityCapacity = 256,
            int initialPoolBuckets = 256,
            int initialFreeIdCapacity = 128,
            GrowthPolicy growthPolicy = GrowthPolicy.Doubling,
            int growthStep = 256)
        {
            InitialEntityCapacity = Math.Max(16, initialEntityCapacity);
            InitialPoolBuckets = Math.Max(16, initialPoolBuckets);
            InitialFreeIdCapacity = Math.Max(16, initialFreeIdCapacity);
            GrowthPolicy = growthPolicy;
            GrowthStep = Math.Max(32, growthStep);
        }
    }
}
