// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystem.cs
// Purpose: Base system interface and group enumeration.
// Key concepts:
//   • All systems implement ISystem and belong to one of three groups.
//   • Each system receives a World instance on execution.
//   • Extended interfaces specialize execution phases (Setup, Simulation, Presentation).
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Defines the high-level system group categories used by the scheduler.
    /// </summary>
    public enum SystemGroup
    {
        /// <summary>Executed before simulation — input polling, prep, and buffering.</summary>
        FrameSetup,

        /// <summary>Main game logic phase — physics, AI, gameplay updates.</summary>
        Simulation,

        /// <summary>Executed after simulation — rendering, UI, and presentation.</summary>
        Presentation
    }

    /// <summary>
    /// Base interface for all ECS systems. Each system defines a single entry point: <see cref="Run"/>.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Executes the system logic against the given <see cref="World"/>.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        void Run(World w);
    }
}