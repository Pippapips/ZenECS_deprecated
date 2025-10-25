// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: IFixedSetupSystem.cs
// Purpose: Defines setup systems that run before fixed-timestep simulations.
// Key concepts:
//   • Used for snapshot preparation, queue swapping, or pre-physics buffering.
//   • Must not depend on DeltaTime (executed with dt = 0).
//   • Runs during the FrameSetup phase of FixedStep.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Interface for systems performing fixed-step preparation such as
    /// input snapshotting or queue swapping. 
    /// <para>Do not rely on DeltaTime; it is always 0 in this phase.</para>
    /// </summary>
    public interface IFixedSetupSystem : ISystem { }
}