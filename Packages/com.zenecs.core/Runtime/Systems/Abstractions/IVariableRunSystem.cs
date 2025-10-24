// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IVariableRunSystem.cs
// Purpose: Defines systems that execute every frame with a variable timestep.
// Key concepts:
//   • Equivalent to Unity's Update() phase (variable delta time).
//   • Used for non-deterministic updates such as input, UI, or dynamic logic.
//   • Executed during the Simulation group each frame.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Interface for systems executed once per frame with a variable timestep.
    /// <para>Equivalent to Unity's <c>Update()</c> method.</para>
    /// </summary>
    public interface IVariableRunSystem : ISystem { }
}