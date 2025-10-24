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
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using ZenECS.Core;
using ZenECS.Core.Hosting;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Binding.Util;

namespace ZenECS
{
    public static class EcsKernel
    {
        private static readonly object _gate = new();
        private static IEcsHost? _defaultHost;

        public static bool IsRunning
        {
            get { lock (_gate) return _defaultHost?.IsRunning == true; }
        }

        public static void Start(WorldConfig? config = null, Action<World, MessageBus>? configure = null, bool throwIfRunning = false)
        {
            lock (_gate)
            {
                _defaultHost ??= new EcsHost();
                if (_defaultHost.IsRunning)
                {
                    if (throwIfRunning) throw new InvalidOperationException("Kernel already started.");
                    return;
                }
                _defaultHost.Start(config ?? new WorldConfig(), configure);
            }
        }

        // Forwarding to underlying host
        public static void InitializeSystems(IEnumerable<ISystem> systems,
            SystemRunnerOptions? options = null,
            IMainThreadGate? mainThreadGate = null,
            Action<string>? log = null)
        {
            var host = SnapshotHostOrThrow();
            host.InitializeSystems(systems, options, mainThreadGate, log);
        }

        public static Core.Systems.SystemRunner Runner
            => SnapshotHostOrThrow().Runner;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BeginFrame(float dt) => SnapshotHostOrThrow().BeginFrame(dt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FixedStep(float fixedDelta) => SnapshotHostOrThrow().FixedStep(fixedDelta);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LateFrame(float alpha) => SnapshotHostOrThrow().LateFrame(alpha);

        public static int Pump(float dt, float fixedDelta, int maxSubSteps, out float alpha)
            => SnapshotHostOrThrow().Pump(dt, fixedDelta, maxSubSteps, out alpha);

        public static void Shutdown()
        {
            lock (_gate)
            {
                _defaultHost?.Shutdown();
                _defaultHost = null;
            }
        }

        public static World World => SnapshotHostOrThrow().World;
        public static MessageBus Bus => SnapshotHostOrThrow().Bus;

        public static void UseHost(IEcsHost host, bool takeover = true)
        {
            lock (_gate)
            {
                if (takeover && _defaultHost?.IsRunning == true) _defaultHost.Shutdown();
                _defaultHost = host;
            }
        }

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
