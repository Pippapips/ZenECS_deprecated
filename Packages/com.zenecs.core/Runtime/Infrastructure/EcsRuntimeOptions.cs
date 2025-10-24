// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsRuntimeOptions.cs
// Purpose: Tunables for runtime diagnostics, logging, and write-permission policy.
// Key concepts:
//   • Centralizes knobs used by EcsActions and systems.
//   • Pluggable logger with minimal interface to keep dependencies light.
//   • WriteFailurePolicy controls behavior on denied structural writes.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Collection of runtime options and diagnostics hooks used across the core.
    /// </summary>
    public static class EcsRuntimeOptions
    {
        // ---- Logging ------------------------------------------------------------------

        /// <summary>
        /// Minimal logging surface used by the core. Provide your own adapter to engine logs.
        /// </summary>
        public interface ILogger
        {
            void Info(string message);
            void Warn(string message);
            void Error(string message);
        }

        /// <summary>
        /// Current logger instance. Defaults to <see cref="NullLogger"/> (no-op).
        /// </summary>
        public static ILogger Log { get; set; } = new NullLogger();

        private sealed class NullLogger : ILogger
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string message) { }
        }

        // ---- Structural write policy --------------------------------------------------

        /// <summary>
        /// Behavior when a structural write (Add/Replace/Remove) is denied by permissions/validation.
        /// </summary>
        public enum WriteFailurePolicy
        {
            /// <summary>Throw an exception immediately.</summary>
            Throw,

            /// <summary>Log a warning and ignore the operation.</summary>
            Log,

            /// <summary>Silently ignore the operation.</summary>
            Ignore
        }

        /// <summary>
        /// Global policy used by <see cref="EcsActions"/> when write is denied.
        /// </summary>
        public static WriteFailurePolicy WritePolicy { get; set; } = WriteFailurePolicy.Throw;

        // ---- Misc / hooks -------------------------------------------------------------

        /// <summary>   
        /// Optional global callback invoked whenever a critical runtime error occurs.
        /// </summary>
        public static Action<Exception>? OnUnhandledError { get; set; }

        /// <summary>
        /// Helper to report non-fatal errors via the configured logger and optional callback.
        /// </summary>
        public static void Report(Exception ex, string context = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(context))
                    Log.Error($"[{context}] {ex}");
                else
                    Log.Error(ex.ToString());
            }
            catch { /* swallow logging failures */ }

            try { OnUnhandledError?.Invoke(ex); } catch { /* swallow callback errors */ }
        }
    }
}
