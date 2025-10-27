// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsKernel.cs
// Purpose: Static global entry point for managing the default ECS host.
// Key concepts:
//   • Wraps IEcsHost to provide simplified Start/Run/Shutdown access.
//   • Used by standalone builds, tests, and console-based ECS loops.
//   • Thread-safe singleton-style management.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Infrastructure.Hosting;
using ZenECS.Core.ViewBinding;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Static façade that manages a single, process-wide default <see cref="IEcsHost"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>EcsKernel</b> offers a minimal global API for bootstrapping and driving ZenECS in
    /// apps where dependency injection or explicit host management is unnecessary (e.g., tests,
    /// console demos, simple tools). It is <b>thread-safe</b> for top-level operations via
    /// an internal lock.
    /// </para>
    /// <para>
    /// Typical loop:
    /// <code>
    /// EcsKernel.Start(config, systems);
    /// while (running)
    /// {
    ///     EcsKernel.BeginFrame(dt);
    ///     // Optionally do fixed steps
    ///     float alpha;
    ///     EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out alpha);
    ///     EcsKernel.LateFrame(alpha);
    /// }
    /// EcsKernel.Shutdown();
    /// </code>
    /// </para>
    /// </remarks>
    public static class EcsKernel
    {
        private static readonly object _gate = new();
        private static IEcsHost? _defaultHost;

        /// <summary>
        /// Gets a value indicating whether the default host is currently started and running.
        /// </summary>
        public static bool IsRunning
        {
            get { lock (_gate) return _defaultHost?.IsRunning == true; }
        }

        /// <summary>
        /// Starts the default host if it is not already running.
        /// </summary>
        /// <param name="config">Optional world configuration (a default instance is created if <see langword="null"/>).</param>
        /// <param name="systems">Optional collection of systems to register (empty if <see langword="null"/>).</param>
        /// <param name="options">Optional <see cref="SystemRunnerOptions"/> for the runner.</param>
        /// <param name="mainThreadGate">
        /// Optional main-thread gate implementation. If <see langword="null"/>, <see cref="DefaultMainThreadGate"/> is used.
        /// </param>
        /// <param name="systemRunnerLog">Optional logger callback for runner messages.</param>
        /// <param name="configure">
        /// Optional callback invoked after the host constructs the <see cref="World"/> and <see cref="MessageBus"/>,
        /// allowing additional configuration.
        /// </param>
        /// <param name="throwIfRunning">
        /// If <see langword="true"/> and the kernel is already started, an <see cref="InvalidOperationException"/> is thrown.
        /// If <see langword="false"/>, the call becomes a no-op when already running.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="throwIfRunning"/> is <see langword="true"/> and the kernel is already running.</exception>
        public static void Start(
            WorldConfig? config = null,
            IEnumerable<ISystem>? systems = null,
            SystemRunnerOptions? options = null,
            IComponentDeltaDispatcher? componentDeltaDispatcher = null,
            Action<string>? systemRunnerLog = null,
            Action<World, MessageBus>? configure = null,
            bool throwIfRunning = false)
        {
            lock (_gate)
            {
                _defaultHost ??= new EcsHost();
                if (_defaultHost.IsRunning)
                {
                    if (throwIfRunning) throw new InvalidOperationException("Kernel already started.");
                    return;
                }
                _defaultHost.Start(
                    config ?? new WorldConfig(),
                    systems ?? Array.Empty<ISystem>(),
                    options,
                    componentDeltaDispatcher,
                    systemRunnerLog,
                    configure);
            }
        }

        /// <summary>
        /// Gets the active <see cref="SystemRunnerOptions"/> from the underlying host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        public static Core.Systems.SystemRunnerOptions RunnerOptions
            => SnapshotHostOrThrow().RunnerOptions;

        /// <summary>
        /// Begins a new frame and runs Update-phase systems with the provided delta time.
        /// </summary>
        /// <param name="dt">Elapsed time (seconds) since the previous frame.</param>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BeginFrame(float dt) => SnapshotHostOrThrow().BeginFrame(dt);

        /// <summary>
        /// Executes a fixed update step across FixedUpdate-phase systems.
        /// </summary>
        /// <param name="fixedDelta">Fixed time step (seconds).</param>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FixedStep(float fixedDelta) => SnapshotHostOrThrow().FixedStep(fixedDelta);

        /// <summary>
        /// Executes LateUpdate-phase systems using an interpolation factor.
        /// </summary>
        /// <param name="alpha">Interpolation factor between the last and next fixed steps (0–1).</param>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LateFrame(float alpha) => SnapshotHostOrThrow().LateFrame(alpha);

        /// <summary>
        /// High-level helper that accumulates <paramref name="dt"/>, performs as many fixed substeps
        /// as allowed by <paramref name="maxSubSteps"/>, and computes <paramref name="alpha"/>.
        /// </summary>
        /// <param name="dt">Frame delta time (seconds).</param>
        /// <param name="fixedDelta">Fixed update time step (seconds).</param>
        /// <param name="maxSubSteps">Maximum number of fixed substeps to perform this frame.</param>
        /// <param name="alpha">Outputs interpolation factor for rendering (0–1).</param>
        /// <returns>The number of fixed substeps performed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        public static int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha)
            => SnapshotHostOrThrow().Pump(dt, fixedDelta, maxSubSteps, out alpha);

        /// <summary>
        /// Shuts down the underlying host and clears the default instance.
        /// </summary>
        /// <remarks>
        /// Safe to call multiple times. After shutdown, <see cref="World"/> and <see cref="Bus"/> are inaccessible
        /// until <see cref="Start"/> is invoked again.
        /// </remarks>
        public static void Shutdown()
        {
            lock (_gate)
            {
                _defaultHost?.Shutdown();
                _defaultHost = null;
            }
        }

        /// <summary>
        /// Gets the active <see cref="World"/> from the underlying host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        public static World World => SnapshotHostOrThrow().World;

        /// <summary>
        /// Gets the global <see cref="MessageBus"/> from the underlying host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the kernel has not been started.</exception>
        public static MessageBus Bus => SnapshotHostOrThrow().Bus;

        /// <summary>
        /// Replaces the current default host implementation.
        /// </summary>
        /// <param name="host">The host instance to use.</param>
        /// <param name="takeover">
        /// If <see langword="true"/> and an existing host is running, it will be shut down before replacement.
        /// </param>
        public static void UseHost(IEcsHost host, bool takeover = true)
        {
            lock (_gate)
            {
                if (takeover && _defaultHost?.IsRunning == true) _defaultHost.Shutdown();
                _defaultHost = host;
            }
        }

        /// <summary>
        /// Returns the current default host or throws if not started.
        /// </summary>
        /// <returns>The active <see cref="IEcsHost"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no host exists or it is not running.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEcsHost SnapshotHostOrThrow()
        {
            IEcsHost? host;
            lock (_gate) host = _defaultHost;
            if (host is null || !host.IsRunning)
                throw new InvalidOperationException("Kernel not started.");
            return host;
        }
    }
}
