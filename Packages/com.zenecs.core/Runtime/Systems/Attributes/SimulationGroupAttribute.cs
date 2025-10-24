// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: SimulationGroupAttribute.cs
// Purpose: Defines the system group responsible for main game logic.
// Key concepts:
//   • Covers gameplay, physics, AI, and state update systems.
//   • Executes between FrameSetup and Presentation groups.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Marks a system as part of the <b>Simulation</b> group.
    /// <para>Typically includes core gameplay logic, physics updates, and AI systems.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SimulationGroupAttribute : Attribute { }
}