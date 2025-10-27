// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 06 World Hooks (Kernel style)
// File: WorldHook.cs
// Purpose: Demonstrates per-world read/write permissions and validator hooks.
// Key concepts:
//   • Write permission (even entity IDs only)
//   • Validator (Mana >= 0)
//   • Read permission (allow reads only for Mana type)
//   • Hooks can be removed/cleared at runtime
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS;                    // Kernel
using ZenECS.Core;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.WorldHooks
{
    // Component used in this sample
    public readonly struct Mana
    {
        public readonly int Value;
        public Mana(int v) => Value = v;
        public override string ToString() => Value.ToString();
    }

    // Simulation: installs hooks, performs writes/reads, then removes hooks. Runs once.
    [SimulationGroup]
    public sealed class WorldHooksDemoSystem : IVariableRunSystem
    {
        private bool _done;

        // Keep references to remove later
        private Func<World, Entity, Type, bool>? _writePerm;
        private Func<Mana, bool>? _validator;

        public void Run(World w)
        {
            if (_done) return;

            Console.WriteLine("=== World Hooks demo (read/write permissions, validator) ===");

            // Configure logging for write failures
            EcsRuntimeOptions.Log = new ConsoleEcsLogger();
            EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;

            // Write permission: only even entity IDs
            _writePerm = (world, e, t) => (e.Id & 1) == 0;
            w.AddWritePermission(_writePerm);

            // Validator: Mana must be >= 0
            _validator = (m) => m.Value >= 0;
            w.AddValidator(_validator);

            // Create entities
            var e1 = w.CreateEntity(); // odd id
            var e2 = w.CreateEntity(); // even id

            // Try to add/replace with various values
            TryAdd(w, e1, new Mana(10));  // should be rejected by write permission
            TryAdd(w, e2, new Mana(-10)); // rejected by validator
            TryAdd(w, e2, new Mana(5));   // OK

            // Read permission: allow reads only for Mana type
            w.AddReadPermission((world, e, t) => t == typeof(Mana));

            // Read attempts
            if (w.TryRead<Mana>(e2, out var mana))
                Console.WriteLine($"Read OK (e:{e2.Id}) -> Mana={mana.Value}");
            else
                Console.WriteLine("Read denied");

            // Remove hooks to restore default behavior
            w.RemoveWritePermission(_writePerm);
            w.RemoveValidator(_validator);
            w.ClearReadPermissions();

            Console.WriteLine("All hooks removed.");

            _done = true;
        }

        private static void TryAdd(World w, Entity e, in Mana v)
        {
            try { w.Add(e, v); Console.WriteLine($"Add<Mana> OK on e:{e.Id} -> {v}"); }
            catch (Exception ex) { Console.WriteLine($"Add<Mana> FAIL on e:{e.Id} :: {ex.Message}"); }
        }
    }

    // Presentation: read-only summary in Late
    [PresentationGroup]
    public sealed class PrintSummarySystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            Console.WriteLine($"[Late] Frame {w.FrameCount}, alive={w.AliveCount}");
            foreach (var e in w.Query<Mana>())
                Console.WriteLine($"Entity {e.Id,2}: Mana={w.Read<Mana>(e).Value}");
        }
    }

    // Program entry — Basic.cs style kernel loop
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - World Hooks (Kernel) ===");

            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 8),
                new ISystem[]
                {
                    new WorldHooksDemoSystem(), // Simulation (install & test hooks)
                    new PrintSummarySystem(),   // Presentation (read-only)
                },
                options: null,
                componentDeltaDispatcher: null,
                systemRunnerLog: Console.WriteLine,
                configure: (world, bus) => { }
            );

            const float fixedDelta = 1f / 60f;
            const int   maxSubSteps = 4;

            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");
            bool running = true;

            while (running)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    running = false;
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                EcsKernel.Pump(dt, fixedDelta, maxSubSteps, out var alpha);
                EcsKernel.LateFrame(alpha);

                Thread.Sleep(10);
            }

            Console.WriteLine("Shutting down...");
            EcsKernel.Shutdown();
            Console.WriteLine("Done.");
        }
    }

    sealed class ConsoleEcsLogger : EcsRuntimeOptions.ILogger
    {
        public void Info(string msg)  => Console.WriteLine(msg);
        public void Warn(string msg)  => Console.WriteLine("WARN: "  + msg);
        public void Error(string msg) => Console.WriteLine("ERROR: " + msg);
    }
}
