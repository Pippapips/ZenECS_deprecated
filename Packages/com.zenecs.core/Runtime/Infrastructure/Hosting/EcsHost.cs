// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsHost.cs
// Purpose: Core runtime host that manages World, MessageBus, and SystemRunner lifecycle.
// Key concepts:
//   • Provides safe Start/Shutdown and thread-synchronized Runner management.
//   • Simplifies ECS loop via Pump(dt, fixedDelta, maxSubSteps, out alpha).
//   • Resets runtime state on shutdown (ComponentEvents, EntityEvents, etc.).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Events;
using ZenECS.Core.Systems;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Binding;
using ZenECS.Core.Binding.Systems;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Sync;

namespace ZenECS.Core.Infrastructure.Hosting
{
    /// <summary>
    /// Default implementation of <see cref="IEcsHost"/> providing a managed runtime environment
    /// for the ECS <see cref="World"/>, <see cref="MessageBus"/>, and <see cref="SystemRunner"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>EcsHost</b> coordinates the entire ZenECS lifecycle including initialization,
    /// system registration, frame execution, and graceful shutdown.
    /// </para>
    /// <para>
    /// It is thread-safe for top-level control operations (<see cref="Start"/>, <see cref="Shutdown"/>, etc.)
    /// via internal locking. Systems themselves should remain single-threaded unless managed
    /// through explicit parallel runners.
    /// </para>
    /// </remarks>
    public sealed class EcsHost : IEcsHost
    {
        private readonly object _gate = new();

        private World? _world;
        private MessageBus? _bus;
        private SystemRunner? _runner;
        private float _accumulator;

        /// <summary>
        /// Gets the active <see cref="World"/> instance owned by this host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the host has not been started.</exception>
        public World World => _world ?? throw new InvalidOperationException("ECS host not started.");

        /// <summary>
        /// Gets the global <see cref="MessageBus"/> instance owned by this host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the host has not been started.</exception>
        public MessageBus Bus => _bus ?? throw new InvalidOperationException("ECS host not started.");

        /// <summary>
        /// Indicates whether the ECS host is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the ECS host, constructing a new <see cref="World"/> and <see cref="MessageBus"/>,
        /// and registering all provided systems into a <see cref="SystemRunner"/>.
        /// </summary>
        /// <param name="config">Configuration for the world.</param>
        /// <param name="systems">Collection of systems to register and initialize.</param>
        /// <param name="options">Optional runner configuration.</param>
        /// <param name="mainThreadGate">
        /// Optional synchronization gate for ensuring main-thread operations.
        /// If <see langword="null"/>, a <see cref="DefaultMainThreadGate"/> is used.
        /// </param>
        /// <param name="systemRunnerLog">Optional callback for system runner log messages.</param>
        /// <param name="configure">
        /// Optional callback invoked after systems are initialized, allowing world/bus customization.
        /// </param>
        public void Start(
            WorldConfig config,
            IEnumerable<ISystem> systems,
            SystemRunnerOptions? options = null,
            IMainThreadGate? mainThreadGate = null,
            Action<string>? systemRunnerLog = null,
            Action<World, MessageBus>? configure = null)
        {
            lock (_gate)
            {
                if (IsRunning) return;
                _world = new World(config);
                _bus = new MessageBus();
                IsRunning = true;
                initializeSystems(systems, options, mainThreadGate, systemRunnerLog);
                configure?.Invoke(_world, _bus);
            }
        }

        /// <summary>
        /// Initializes the <see cref="SystemRunner"/> and registers built-in binding and dispatch systems.
        /// </summary>
        private void initializeSystems(
            IEnumerable<ISystem> systems,
            SystemRunnerOptions? options = null,
            IMainThreadGate? mainThreadGate = null,
            Action<string>? log = null)
        {
            lock (_gate)
            {
                if (!IsRunning) throw new InvalidOperationException("Host not started.");
                if (_runner != null) return;

                var list = systems is List<ISystem> l ? new List<ISystem>(l) : new List<ISystem>(systems);

                var gate = mainThreadGate ?? new DefaultMainThreadGate();
                var changeFeed = new ComponentChangeFeed(gate);
                var binderRegistry = new ComponentBinderRegistry();
                var resolver = new ComponentBinderResolver(binderRegistry);
                var viewRegistry = new ViewBinderRegistry();

                var hub = new ComponentBindingHubSystem(changeFeed);
                var binding = new ViewBindingSystem(viewRegistry, binderRegistry, resolver);
                var dispatch = new ComponentBatchDispatchSystem(changeFeed, binding);

                list.Add(hub);
                list.Add(binding);
                list.Add(dispatch);

                _runner = new SystemRunner(World, Bus, list, options ?? new SystemRunnerOptions(), log);
                _runner.InitializeSystems();
            }
        }

        /// <summary>
        /// Gets the current <see cref="SystemRunnerOptions"/> used by this host.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the runner has not been initialized.</exception>
        public SystemRunnerOptions RunnerOptions =>
            _runner == null
                ? throw new InvalidOperationException("Runner not initialized. Call Start() first.")
                : _runner.Options;

        /// <summary>
        /// Begins a new frame and advances all systems scheduled for the Update phase.
        /// </summary>
        /// <param name="dt">Elapsed delta time (in seconds) since the previous frame.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginFrame(float dt)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.BeginFrame(dt);
        }

        /// <summary>
        /// Performs a fixed-timestep update across systems scheduled for FixedUpdate.
        /// </summary>
        /// <param name="fixedDelta">The fixed time-step value in seconds.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedStep(float fixedDelta)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.FixedStep(fixedDelta);
        }

        /// <summary>
        /// Executes LateUpdate-phase systems using the provided interpolation factor.
        /// </summary>
        /// <param name="alpha">
        /// Interpolation ratio between the last and next fixed updates (0–1).
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateFrame(float alpha)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.LateFrame(alpha);
        }

        /// <summary>
        /// Accumulates delta time and performs as many fixed steps as possible
        /// (up to <paramref name="maxSubSteps"/>), calculating interpolation alpha (0–1).
        /// </summary>
        /// <param name="dt">Frame delta time (in seconds).</param>
        /// <param name="fixedDelta">Fixed update time step (in seconds).</param>
        /// <param name="maxSubSteps">Maximum number of substeps allowed in one frame.</param>
        /// <param name="alpha">Output interpolation ratio for rendering.</param>
        /// <returns>The number of performed fixed substeps.</returns>
        /// <remarks>
        /// This method simplifies integration with custom main loops (Unity, custom engines, etc.).
        /// </remarks>
        public int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            BeginFrame(dt);
            _accumulator += dt;
            int sub = 0;
            while (_accumulator >= fixedDelta && sub < maxSubSteps)
            {
                FixedStep(fixedDelta);
                _accumulator -= fixedDelta;
                sub++;
            }
            alpha = fixedDelta > 0f ? Math.Clamp(_accumulator / fixedDelta, 0f, 1f) : 1f;
            return sub;
        }

        /// <summary>
        /// Shuts down all currently running systems without disposing the world or bus.
        /// </summary>
        internal void shutdownSystems()
        {
            lock (_gate)
            {
                _runner?.ShutdownSystems();
                _runner = null;
                _accumulator = 0f;
            }
        }

        /// <summary>
        /// Shuts down the ECS host, releasing all systems, world, and bus instances,
        /// and resetting all static event systems.
        /// </summary>
        public void Shutdown()
        {
            lock (_gate)
            {
                if (!IsRunning) return;

                shutdownSystems();

                _bus?.Clear();
                ComponentEvents.Reset();
                EntityEvents.Reset();
                _world?.Reset(false);
                _bus = null;
                _world = null;
                IsRunning = false;
            }
        }

        /// <summary>
        /// Disposes the ECS host and shuts down all internal subsystems.
        /// </summary>
        public void Dispose() => Shutdown();
    }
}
