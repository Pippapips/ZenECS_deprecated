// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: MainThreadGate.cs
// Purpose: Minimal facade placeholder for main-thread assertions.
// Key concepts:
//   • Extended by concrete gates (InlineMainThreadGate, DefaultMainThreadGate).
//   • Keep small to avoid dependencies in consumers that only need Ensure().
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding.Util
{
    public static class MainThreadGate
    {
        public static void Ensure()
        {
        }
    }
}