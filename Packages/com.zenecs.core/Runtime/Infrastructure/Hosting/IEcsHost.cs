// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IEcsHost.cs
// Purpose: Defines the core ECS host interface controlling World, Bus, and Runner.
// Key concepts:
//   • Provides ECS lifecycle: Start → InitializeSystems → Frame Loop → Shutdown.
//   • Allows safe multithreaded management via EcsKernel static wrapper.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core;
using ZenECS.Core.Messaging;
using System;
using System.Collections.Generic;
using ZenECS.Core.Systems;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Infrastructure.Hosting
{
    /// <summary>
    /// Primary host interface for ZenECS that owns the <c>World</c>, the message <c>Bus</c>,
    /// and the system runner lifecycle.
    /// </summary>
    /// <remarks>
    /// Provides the high-level ECS lifecycle:
    /// <list type="number">
    /// <item><description><c>Start(...)</c> — construct/configure the world, register systems, and prepare runners.</description></item>
    /// <item><description>Per-frame loop — call <c>BeginFrame</c>, optionally <c>FixedStep</c> for fixed updates, then <c>LateFrame</c>.</description></item>
    /// <item><description><c>Shutdown()</c> — tear down systems and release resources.</description></item>
    /// </list>
    /// Implementations should be <see cref="IDisposable"/> and coordinate thread-affinity via an <c>IMainThreadGate</c> when needed.
    /// </remarks>
    public interface IEcsHost : IDisposable
    {
        World World { get; }
        IMessageBus Bus { get; }
        IBindingRouter BindingRouter { get; }
        IContextRegistry ContextRegistry { get; }
        bool IsRunning { get; }

        void Start(WorldConfig config,
            IEnumerable<ISystem> systems,
            SystemRunnerOptions? options = null,
            Action<string>? systemRunnerLog = null,
            Action? onComplete = null);
        void Shutdown();

        // ---- Runner lifecycle / forwarding ----
        SystemRunnerOptions RunnerOptions { get; }
        void BeginFrame(float dt);
        void FixedStep(float fixedDelta);
        void LateFrame(float alpha = 1.0f);
        int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha);
    }
}