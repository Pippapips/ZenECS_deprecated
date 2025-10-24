// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Timing.cs
// Purpose: Frame counters and delta-time tracking for simulation stepping.
// Key concepts:
//   • FrameCount increment is runner-driven.
//   • DeltaTime is updated by the host loop per frame.
//   • ResetTimingCounters() clears both fields during world reset.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>Current frame index (incremented externally by the runner).</summary>
        public int FrameCount { get; set; }

        /// <summary>Time delta in seconds between the previous and current frame.</summary>
        public float DeltaTime { get; set; }

        /// <summary>
        /// Resets timing counters to zero.
        /// Called during <see cref="World.ResetSubsystems"/> or when starting a new simulation.
        /// </summary>
        private void ResetTimingCounters()
        {
            FrameCount = 0;
            DeltaTime = 0;
        }
    }
}