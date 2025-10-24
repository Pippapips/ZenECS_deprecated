// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IPresentationSystem.cs
// Purpose: Defines systems responsible for post-simulation rendering or view updates.
// Key concepts:
//   • Used for rendering, UI, or data→view synchronization.
//   • Read-only: modifying ECS state here is discouraged.
//   • Interpolation alpha is provided for smooth rendering between frames.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Interface for systems executed during the <b>Presentation</b> phase.
    /// <para>Used for rendering, UI updates, or data→view synchronization.</para>
    /// <para>The provided <paramref name="alpha"/> value can be used for interpolation.</para>
    /// </summary>
    public interface IPresentationSystem : ISystem
    {
        /// <summary>
        /// Executes the presentation logic with interpolation.
        /// </summary>
        /// <param name="w">The ECS world.</param>
        /// <param name="alpha">Interpolation factor (1 = current frame, 0 = previous frame).</param>
        void Run(World w, float alpha);

        /// <summary>
        /// Default implementation for compatibility with <see cref="ISystem"/>.
        /// Calls <see cref="Run(World, float)"/> with <c>alpha = 1f</c>.
        /// </summary>
        void ISystem.Run(World w) => Run(w, 1f);
    }
}