// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IMainThreadGate.cs
// Purpose: Utilities for asserting main-thread access and marshaling actions.
// Key concepts:
//   • Ensure() throws or asserts if not on the main thread.
//   • Post/Send marshal work to the main thread (async vs sync).
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
    /// Abstraction to guard main-thread-only APIs and marshal actions to the main thread.
    /// </summary>
    public interface IMainThreadGate
    {
        /// <summary>Indicates whether the current thread is the main thread.</summary>
        bool IsMainThread { get; }

        /// <summary>Ensures main-thread context; may throw or assert if violated.</summary>
        void Ensure();

        /// <summary>Posts an action to be executed on the main thread asynchronously.</summary>
        void Post(Action action);

        /// <summary>Sends an action to the main thread and blocks until completion.</summary>
        void Send(Action action);
    }
}