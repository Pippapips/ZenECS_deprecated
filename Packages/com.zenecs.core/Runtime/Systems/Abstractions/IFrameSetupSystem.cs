// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFrameSetupSystem.cs
// Purpose: Defines systems executed before Simulation every frame.
// Key concepts:
//   • Used for input polling, buffer swapping, and frame preparation.
//   • Runs once per frame before Simulation systems (no DeltaTime usage).
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Interface for systems executed before <b>Simulation</b> each frame.
    /// <para>Common use cases: input snapshot, buffer swap, and world setup.</para>
    /// <para>DeltaTime is not available in this phase.</para>
    /// </summary>
    public interface IFrameSetupSystem : ISystem { }
}