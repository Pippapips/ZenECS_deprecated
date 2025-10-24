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
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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

namespace ZenECS.Core.Hosting
{
    public sealed class EcsHost : IEcsHost
    {
        private readonly object _gate = new();

        private World? _world;
        private MessageBus? _bus;
        private SystemRunner? _runner;
        private float _accumulator;

        public World World => _world ?? throw new InvalidOperationException("ECS host not started.");
        public MessageBus Bus => _bus ?? throw new InvalidOperationException("ECS host not started.");

        public bool IsRunning { get; private set; }

        public void Start(WorldConfig config, Action<World, MessageBus>? configure = null)
        {
            lock (_gate)
            {
                if (IsRunning) return;
                _world = new World(config);
                _bus = new MessageBus();
                configure?.Invoke(_world, _bus);
                IsRunning = true;
            }
        }

        /// <summary>
        /// Initializes the SystemRunner and registers/initializes systems.
        /// </summary>
        public void InitializeSystems(IEnumerable<ISystem> systems,
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
                var resolver       = new ComponentBinderResolver(binderRegistry);
                var viewRegistry = new ViewBinderRegistry();
                
                var hub = new ComponentBindingHubSystem(changeFeed);
                var binding = new ViewBindingSystem(viewRegistry, binderRegistry, resolver);
                var dispatch = new ComponentBatchDispatchSystem(changeFeed, binding);
            
                list.Add(hub);
                list.Add(binding);
                list.Add(dispatch);

                _runner = new SystemRunner(World, null, list, options ?? new SystemRunnerOptions(), log);
                _runner.InitializeSystems();
            }
        }
        
        // --- 헬퍼: 리스트에 동일 타입 시스템이 없으면 생성/추가 ---
        private static void EnsureCoreSystem<T>(List<ISystem> list) where T : ISystem, new()
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetType() == typeof(T))
                    return; // 이미 존재
            }
            list.Add(new T()); // 사용자 시스템 뒤에 배치 → 프레임 말단 반영
        }        

        public SystemRunner Runner
            => _runner ?? throw new InvalidOperationException("Runner not initialized. Call InitializeSystems(...) first.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginFrame(float dt)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.BeginFrame(dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedStep(float fixedDelta)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.FixedStep(fixedDelta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateFrame(float alpha)
        {
            if (_runner is null) throw new InvalidOperationException("Runner not initialized.");
            _runner.LateFrame(alpha);
        }

        /// <summary>
        /// Accumulates deltaTime and performs as many fixed steps as possible.
        /// Returns the number of performed substeps and computes interpolation alpha (0..1).
        /// Simplifies external main loop integration.
        /// </summary>
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

        public void ShutdownSystems()
        {
            lock (_gate)
            {
                _runner?.ShutdownSystems();
                _runner = null;
                _accumulator = 0f;
            }
        }

        public void Shutdown()
        {
            lock (_gate)
            {
                if (!IsRunning) return;

                _runner?.ShutdownSystems();
                _runner = null;
                _accumulator = 0f;

                _bus?.Clear();
                ComponentEvents.Reset();
                EntityEvents.Reset();
                _world?.Reset(false);
                _bus = null;
                _world = null;
                IsRunning = false;
            }
        }

        public void Dispose() => Shutdown();
    }
}
