// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core
// File: SystemRunner.cs
// Purpose: Executes ECS systems in grouped order with configurable flush policy.
// Key concepts:
//   • Integrates with Unity’s Update / FixedUpdate / LateUpdate style flow.
//   • Supports deferred structural changes via policy (EndOfSimulation, NextFrame, Manual).
//   • Guards write access during presentation to enforce read-only phase.
//   • Coordinates world, systems, and message bus lifecycle.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Configuration for <see cref="SystemRunner"/> behavior.
    /// </summary>
    public sealed class SystemRunnerOptions
    {
        /// <summary>Whether to enable write guards during the Presentation stage (read-only enforcement).</summary>
        public bool GuardWritesInPresentation { get; set; } = true;

        /// <summary>Default constructor using standard behavior.</summary>
        public SystemRunnerOptions() { }

        /// <summary>
        /// Initializes the options directly with custom parameters.
        /// </summary>
        public SystemRunnerOptions(bool guardWritesInPresentation = true)
        {
            GuardWritesInPresentation = guardWritesInPresentation;
        }

        /// <summary>Convenient default instance.</summary>
        public static readonly SystemRunnerOptions Default = new();
    }

    /// <summary>
    /// Coordinates system execution per phase (FrameSetup, Simulation, Presentation)
    /// and manages lifecycle (Initialize / Shutdown).
    /// </summary>
    internal sealed class SystemRunner
    {
        /// <summary>
        /// Configuration for <see cref="SystemRunner"/> behavior.
        /// </summary>
        public SystemRunnerOptions Options { get; }

        private readonly World _w;
        private readonly IMessageBus _bus;
        private readonly SystemPlanner.Plan? _plan;

        private bool _started;
        private bool _stopped;

        public SystemRunner(
            World w,
            IMessageBus? bus = null,
            IEnumerable<ISystem>? systems = null,
            SystemRunnerOptions? opt = null,
            Action<string>? warn = null)
        {
            _w = w ?? throw new ArgumentNullException(nameof(w));
            _bus = bus ?? new MessageBus();
            _plan = SystemPlanner.Build(systems, warn);
            Options = opt ?? new SystemRunnerOptions();
        }

        /// <summary>
        /// Initializes all systems once before the first frame.
        /// </summary>
        public void InitializeSystems()
        {
            if (_started) return;
            _started = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleInitializeOrder)
                    s.Initialize(_w);
            }

            _w.RunScheduledJobs();
        }

        /// <summary>
        /// Shuts down all systems in reverse order (Presentation → Simulation → Setup).
        /// </summary>
        public void ShutdownSystems()
        {
            if (!_started || _stopped) return;
            _stopped = true;

            if (_plan != null)
            {
                foreach (ISystemLifecycle s in _plan.LifecycleShutdownOrder)
                    s.Shutdown(_w);
            }
        }

        /// <summary>
        /// Corresponds to Unity's FixedUpdate phase (fixed timestep).
        /// Structural changes are queued, not applied immediately.
        /// </summary>
        public void FixedStep(float fixedDelta)
        {
            // Optional pre-step setup (no DeltaTime use)
            RunFixedGroup(SystemGroup.FrameSetup);

            // Inject fixed delta
            _w.DeltaTime = fixedDelta;

            RunFixedGroup(SystemGroup.Simulation);
            // NOTE: Do not flush jobs here; handled at frame barrier (BeginFrame).
        }

        /// <summary>
        /// Corresponds to Unity's Update phase (variable timestep + barrier management).
        /// </summary>
        public void BeginFrame(float deltaTime)
        {
            // Consume all queued messages
            _bus.PumpAll();

            // Frame setup phase (no DeltaTime use)
            RunGroup(SystemGroup.FrameSetup);

            _w.RunScheduledJobs();

            // Inject variable timestep
            _w.DeltaTime = deltaTime;

            RunGroup(SystemGroup.Simulation);

            // Barrier handling based on flush policy
            _w.RunScheduledJobs();
        }

        /// <summary>
        /// Corresponds to Unity's LateUpdate phase (Presentation stage, read-only).
        /// </summary>
        public void LateFrame(float interpolationAlpha = 1f)
        {
            _w.BindingRouter?.RunApply();
            
            using IDisposable? guard = Options.GuardWritesInPresentation ? DenyWrites(_w) : null;
            RunLateGroup(SystemGroup.Presentation);

            _w.FrameCount++;
        }

        /// <summary>
        /// Temporarily disables write operations during presentation (Add/Replace/Remove).
        /// </summary>
        private static IDisposable DenyWrites(World w)
        {
            Func<World, Entity, Type, bool> token = static (_, _, __) => false;
            w.AddWritePermission(token);
            return new DisposableAction(() => w.RemoveWritePermission(token));
        }

        private sealed class DisposableAction : IDisposable
        {
            private readonly Action _onDispose;
            public DisposableAction(Action onDispose) => _onDispose = onDispose;
            public void Dispose() => _onDispose();
        }

        private void RunFixedGroup(SystemGroup g)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameSetup:
                    foreach (IFixedSetupSystem s in _plan.FrameSetup.OfType<IFixedSetupSystem>())
                        s.Run(_w);
                    break;

                case SystemGroup.Simulation:
                    foreach (IFixedRunSystem s in _plan.Simulation.OfType<IFixedRunSystem>())
                        s.Run(_w);
                    break;
            }
        }

        private void RunGroup(SystemGroup g)
        {
            if (_plan == null) return;

            switch (g)
            {
                case SystemGroup.FrameSetup:
                    foreach (IFrameSetupSystem s in _plan.FrameSetup.OfType<IFrameSetupSystem>())
                        s.Run(_w);
                    break;

                case SystemGroup.Simulation:
                    foreach (IVariableRunSystem s in _plan.Simulation.OfType<IVariableRunSystem>())
                        s.Run(_w);
                    break;
            }
        }

        private void RunLateGroup(SystemGroup g, float interpolationAlpha = 1.0f)
        {
            if (_plan == null) return;

            if (g == SystemGroup.Presentation)
            {
                foreach (IPresentationSystem s in _plan.Presentation.OfType<IPresentationSystem>())
                    s.Run(_w, interpolationAlpha);
            }
        }
    }
}
