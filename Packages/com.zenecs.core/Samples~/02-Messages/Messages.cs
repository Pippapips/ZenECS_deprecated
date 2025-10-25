// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 02 Messages
// File: Messages.cs
// Purpose: Demonstrates one-way data flow: View publishes messages, Simulation
//          systems mutate ECS data, Presentation reads only.
// Key concepts:
//   • View never mutates World directly (messages only)
//   • Simulation systems subscribe to messages and update components
//   • Presentation runs in Late (read-only)
//   • Uses EcsKernel.Start(...) to register systems (no manual InitializeSystems)
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS;      // Kernel
using ZenECS.Core;
using ZenECS.Core.Infrastructure; // World, WorldConfig
using ZenECS.Core.Messaging;      // IMessage, MessageBus
using ZenECS.Core.Systems;        // IInitializeSystem, IVariableRunSystem, IPresentationSystem

namespace ZenEcsCoreSamples.Messages
{
    // ──────────────────────────────────────────────────────────────────────────
    // DATA COMPONENTS
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct Health
    {
        public readonly int Value;
        public Health(int value) => Value = value;
        public override string ToString() => $"HP={Value}";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MESSAGES (View → Logic)
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct DamageRequest : IMessage
    {
        public readonly Entity Entity;
        public readonly int Amount;
        public DamageRequest(Entity entity, int amount)
        {
            Entity = entity;
            Amount = amount;
        }
        public override string ToString() => $"DamageRequest(e:{Entity}, amt:{Amount})";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SYSTEMS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to DamageRequest messages and updates entity Health.
    /// Runs in Simulation (writes are allowed here).
    /// </summary>
    [SimulationGroup]
    public sealed class DamageSystem : ISystemLifecycle
    {
        private MessageBus? _bus;
        private IDisposable? _sub;

        public void Initialize(World w)
        {
            _bus = EcsKernel.Bus;

            // Subscribe to View-originated messages
            _sub = _bus.Subscribe<DamageRequest>(m =>
            {
                if (!w.IsAlive(m.Entity)) return;
                if (!w.Has<Health>(m.Entity)) return;

                var current = w.Read<Health>(m.Entity);
                var updated = new Health(Math.Max(0, current.Value - m.Amount));
                w.Replace(m.Entity, updated);

                Console.WriteLine($"[Logic] e:{m.Entity} took {m.Amount} → {updated}");
            });
        }
        public void Shutdown(World w)
        {
            _sub?.Dispose();
        }

        public void Run(World w)
        {
        }
    }

    /// <summary>
    /// Read-only presentation that prints Health each Late frame.
    /// </summary>
    [PresentationGroup]
    public sealed class PrintHealthSystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            Console.WriteLine($"-- Frame {w.FrameCount} (alpha={alpha:0.00}) --");
            foreach (var e in w.Query<Health>())
            {
                var hp = w.Read<Health>(e);
                Console.WriteLine($"Entity {e.Id,2}: {hp}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PROGRAM ENTRY
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        private static World? _w;
        private static Entity _e1;
        private static Entity _e2;
        
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - View→Data via MessageBus (Kernel) ===");

            // Boot: pass systems directly to EcsKernel.Start (as in Basic.cs)
            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 32),
                new ISystem[]
                {
                    new DamageSystem(),      // Simulation (writes via messages)
                    new PrintHealthSystem(), // Presentation (read-only)
                },
                options: null,
                mainThreadGate: null,
                systemRunnerLog: Console.WriteLine,
                configure: (world, bus) =>
                {
                    // Seed entities with Health data
                    var e1 = world.CreateEntity();
                    var e2 = world.CreateEntity();
                    world.Add(e1, new Health(100));
                    world.Add(e2, new Health(75));

                    _w = world;
                    _e1 = e1;
                    _e2 = e2;
                }
            );

            var bus = EcsKernel.Bus;

            // Main loop mirrors Basic.cs: Pump variable step + fixed step + Late
            const float fixedDelta = 1f / 60f; // 60Hz simulation
            const int   maxSubStepsPerFrame = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press [1]/[2] to deal damage, [ESC] to quit.");

            bool running = true;
            var rand = new Random();

            while (running)
            {
                // View layer: publish messages only (never mutates World directly)
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            bus.Publish(new DamageRequest(_e1, rand.Next(5, 15)));
                            Console.WriteLine("[View] Sent DamageRequest → e:1");
                            break;
                        case ConsoleKey.D2:
                            bus.Publish(new DamageRequest(_e2, rand.Next(5, 15)));
                            Console.WriteLine("[View] Sent DamageRequest → e:2");
                            break;
                        case ConsoleKey.Escape:
                            running = false;
                            break;
                    }
                }

                // Timing (same pattern as Basic.cs)
                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
                EcsKernel.LateFrame(alpha);

                Thread.Sleep(50); // be gentle to CPU in console
            }

            Console.WriteLine("Shutting down...");
            EcsKernel.Shutdown();
            Console.WriteLine("Done.");
        }
    }
}
