// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: FrameSetupGroupAttribute.cs
// Purpose: Defines the system group responsible for frame preparation.
// Key concepts:
//   • Used for pre-simulation tasks such as input polling, time updates, and queue swaps.
//   • Executed before SimulationGroup systems each frame.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Marks a system as belonging to the <b>FrameSetup</b> group.
    /// <para>Typical usage: input snapshot, delta time calculation, or queue swapping.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FrameSetupGroupAttribute : Attribute { }
}