﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsRuntimeOptions.cs
// Purpose: Tunables for runtime diagnostics, logging, and write-permission policy.
// Key concepts:
//   • Centralizes knobs used by EcsActions and systems.
//   • Pluggable logger with minimal interface to keep dependencies light.
//   • WriteFailurePolicy controls behavior on denied structural writes.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Centralized collection of runtime options and diagnostic hooks that influence core behavior.
    /// </summary>
    /// <remarks>
    /// Configure these options during application bootstrap (before systems start). They are static,
    /// process-wide settings and are intended to be read frequently but written infrequently.
    /// </remarks>
    public static class EcsRuntimeOptions
    {
        // ---- Logging ------------------------------------------------------------------

        /// <summary>
        /// Minimal logging surface used by the core. Implement this interface to bridge
        /// ZenECS logging into your engine/framework logs (e.g., Unity, Serilog, NLog).
        /// </summary>
        public interface ILogger
        {
            /// <summary>Writes an informational message.</summary>
            /// <param name="message">The message to log.</param>
            void Info(string message);

            /// <summary>Writes a warning message.</summary>
            /// <param name="message">The message to log.</param>
            void Warn(string message);

            /// <summary>Writes an error message.</summary>
            /// <param name="message">The message to log.</param>
            void Error(string message);
        }

        /// <summary>
        /// Gets or sets the current logger instance used by the core.
        /// </summary>
        /// <value>
        /// Defaults to a no-op logger (<see cref="NullLogger"/>). Replace this with your own
        /// implementation during startup to surface core diagnostics.
        /// </value>
        public static ILogger Log { get; set; } = new NullLogger();

        /// <summary>
        /// No-op logger used as a safe default when no logger is configured.
        /// </summary>
        private sealed class NullLogger : ILogger
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string message) { }
        }

        // ---- Structural write policy --------------------------------------------------

        /// <summary>
        /// Defines how the core reacts when a structural write (Add/Replace/Remove) is denied
        /// by permissions or validation policies.
        /// </summary>
        public enum WriteFailurePolicy
        {
            /// <summary>
            /// Throw an exception immediately. Best for development and strict correctness.
            /// </summary>
            Throw,

            /// <summary>
            /// Log a warning and ignore the operation. Useful when you want visibility without halting.
            /// </summary>
            Log,

            /// <summary>
            /// Silently ignore the operation. Use with caution in production when performance
            /// and resilience outweigh strict feedback.
            /// </summary>
            Ignore
        }

        /// <summary>
        /// Gets or sets the global policy the core uses when a write is denied.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="WriteFailurePolicy.Throw"/> to surface issues early.
        /// Consider <see cref="WriteFailurePolicy.Log"/> for friendlier behavior in non-dev environments.
        /// </remarks>
        public static WriteFailurePolicy WritePolicy { get; set; } = WriteFailurePolicy.Throw;

        // ---- Misc / hooks -------------------------------------------------------------

        /// <summary>
        /// Optional global callback invoked whenever a critical runtime error is reported via <see cref="Report"/>.
        /// </summary>
        /// <remarks>
        /// Use this to route exceptions to crash reporters or UI toasts. The callback is wrapped in a try/catch
        /// to avoid cascading failures; any exception thrown here is swallowed.
        /// </remarks>
        public static Action<Exception>? OnUnhandledError { get; set; }

        /// <summary>
        /// Reports a non-fatal exception through the configured logger and invokes
        /// <see cref="OnUnhandledError"/> if it is set.
        /// </summary>
        /// <param name="ex">The exception to report.</param>
        /// <param name="context">
        /// Optional context string appended to the log (e.g., system name, operation).
        /// </param>
        /// <remarks>
        /// Both logging and callback invocations are individually guarded with try/catch to ensure
        /// reporting never throws.
        /// </remarks>
        public static void Report(Exception ex, string context = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(context))
                    Log.Error($"[{context}] {ex}");
                else
                    Log.Error(ex.ToString());
            }
            catch
            {
                // Swallow logging failures to keep reporting non-throwing.
            }

            try
            {
                OnUnhandledError?.Invoke(ex);
            }
            catch
            {
                // Swallow callback failures for the same reason.
            }
        }
    }
}
