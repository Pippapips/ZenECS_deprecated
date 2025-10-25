// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: InlineMatinThreadGate.cs
// Purpose: Test/console main-thread gate that executes actions inline on the same thread.
// Key concepts:
//   • Always reports "main thread".
//   • Post/Send simply invoke the action immediately.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Binding.Util
{
    /// <summary>
    /// Console/tests variant: Post/Send are executed inline on the same thread.
    /// </summary>
    internal sealed class InlineMainThreadGate : IMainThreadGate
    {
        public bool IsMainThread => true;
        public void Ensure() { /* always main in this model */ }
        public void Post(Action action) { action?.Invoke(); }
        public void Send(Action action) { action?.Invoke(); }
    }
}