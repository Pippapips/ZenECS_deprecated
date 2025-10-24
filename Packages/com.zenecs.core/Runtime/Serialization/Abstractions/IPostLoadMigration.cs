// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IPostLoadMigration.cs
// Purpose: Post-load world migration step executed after all pools are restored.
// Key concepts:
//   • Ordered execution by ascending Order value.
//   • Must be idempotent (running multiple times yields the same final state).
//   • Suitable for re-binding, index rebuilds, or data corrections across the world.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Serialization;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Runs after a full snapshot load (after component pools are populated) to perform
    /// global fixes or migrations. Implementations must be <b>idempotent</b>.
    /// Lower <see cref="Order"/> runs first.
    /// </summary>
    public interface IPostLoadMigration
    {
        /// <summary>Execution order (lower values run earlier).</summary>
        int Order { get; }

        /// <summary>
        /// Executes the migration over the entire world (e.g., re-binding, index rebuild, data corrections).
        /// </summary>
        void Run(World world);
    }
}