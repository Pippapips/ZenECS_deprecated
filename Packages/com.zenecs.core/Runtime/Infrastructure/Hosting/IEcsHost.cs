// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IEcsHost.cs
// Purpose: Defines the core ECS host interface controlling World, Bus, and Runner.
// Key concepts:
//   • Provides ECS lifecycle: Start → InitializeSystems → Frame Loop → Shutdown.
//   • Allows safe multithreaded management via EcsKernel static wrapper.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core;
using ZenECS.Core.Messaging;
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Hosting
{
    public interface IEcsHost : IDisposable
    {
        World World { get; }
        MessageBus Bus { get; }
        bool IsRunning { get; }

        void Start(WorldConfig config, Action<World, MessageBus>? configure = null);
        void Shutdown();

        // ---- Runner lifecycle / forwarding ----
        void InitializeSystems(IEnumerable<ISystem> systems,
            SystemRunnerOptions? options = null,
            IMainThreadGate? mainThreadGate = null,
            Action<string>? log = null);
        SystemRunner Runner { get; }
        void BeginFrame(float dt);
        void FixedStep(float fixedDelta);
        void LateFrame(float alpha);
        int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha);
    }
}