// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFixedRunSystem.cs
// Purpose: Defines systems executed on fixed time steps (physics-equivalent updates).
// Key concepts:
//   • Used for deterministic updates such as physics or fixed-timestep logic.
//   • Runs during the Simulation phase with a constant delta time.
//   • Invoked via SystemRunner.FixedStep().
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Interface for systems executed on a <b>fixed timestep</b>, typically physics or deterministic logic.
    /// </summary>
    public interface IFixedRunSystem : ISystem { }
}