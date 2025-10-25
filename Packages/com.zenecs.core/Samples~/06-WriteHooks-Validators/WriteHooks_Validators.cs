// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 06 Write Hooks & Validators (Kernel style)
// File: WriteHooks_Validators.cs
// Purpose: Demonstrates per-world validators and write permissions:
//          • Per-type validator: forbid negative Mana values
//          • Write permission: allow writes only to entities that pass a policy
// Key concepts:
//   • Simulation system installs hooks and performs write attempts
//   • Presentation system remains read-only (Late)
//   • EcsRuntimeOptions.WritePolicy configured for logging failures
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS;                    // Kernel
using ZenECS.Core;               // World, WorldConfig
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Systems;       // IVariableRunSystem, IPresentationSystem

namespace ZenEcsCoreSamples.WriteHooks
{
    // ──────────────────────────────────────────────────────────────────────────
    // Component
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct Mana
    {
        public readonly int Value;
        public Mana(int v) => Value = v;
        public override string ToString() => $"Mana={Value}";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Installs validators and write permissions, then attempts several writes
    /// to demonstrate accept/reject behavior. Runs once.
    /// </summary>
    [SimulationGroup]
    public sealed class WriteHooksDemoSystem : IVariableRunSystem
    {
        private bool _done;

        public void Run(World w)
        {
            if (_done) return;

            Console.WriteLine("=== Write Hooks & Validators demo ===");

            // Configure runtime logging policy for failed writes
            EcsRuntimeOptions.Log = new ConsoleEcsLogger();
            EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;

            // 1) Per-type validator: forbid negative Mana
            w.AddValidator<Mana>(m => m.Value >= 0);

            // 2) Write permission: disallow writes to odd entity IDs
            //    (Only even IDs allowed)
            Func<World, Entity, Type, bool> writePerm = (world, e, t) => (e.Id & 1) == 0;
            w.AddWritePermission(writePerm);

            // Create two entities; ids start from 1 and increment
            var e1 = w.CreateEntity(); // likely id=1 (odd)
            var e2 = w.CreateEntity(); // likely id=2 (even)

            // Helper to see outcomes
            void TryAdd(Entity e, Mana v)
            {
                try
                {
                    w.Add(e, v);
                    Console.WriteLine($"Add<Mana> OK on e:{e.Id} -> {v}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Add<Mana> FAIL on e:{e.Id} :: {ex.Message}");
                }
            }

            // Attempts
            TryAdd(e1, new Mana(10));   // rejected by write permission (odd id)
            TryAdd(e2, new Mana(-5));   // rejected by validator (negative)
            TryAdd(e2, new Mana(25));   // accepted (even id + valid value)

            // Replace / Remove also honor the same hooks
            try { w.Replace(e1, new Mana(3)); } catch (Exception ex) { Console.WriteLine($"Replace FAIL e:{e1.Id} :: {ex.Message}"); }
            try { w.Remove<Mana>(e1);   } catch (Exception ex) { Console.WriteLine($"Remove FAIL  e:{e1.Id} :: {ex.Message}"); }

            // Clean up hook for demo completeness (optional)
            w.RemoveWritePermission(writePerm);

            _done = true;
        }
    }

    /// <summary>
    /// Read-only presentation: prints current Mana holders each Late.
    /// </summary>
    [PresentationGroup]
    public sealed class PrintManaSystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            Console.WriteLine($"-- Frame {w.FrameCount} (alpha={alpha:0.00}) --");
            foreach (var e in w.Query<Mana>())
            {
                var mana = w.Read<Mana>(e);
                Console.WriteLine($"Entity {e.Id,2}: {mana.Value}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry — Basic.cs style Kernel loop
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - WriteHooks & Validators (Kernel) ===");

            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 8),
                new ISystem[]
                {
                    new WriteHooksDemoSystem(), // Simulation (writes + hooks)
                    new PrintManaSystem(),      // Presentation (read-only)
                },
                options: null,
                mainThreadGate: null,
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

    // ──────────────────────────────────────────────────────────────────────────
    // Logger
    // ──────────────────────────────────────────────────────────────────────────
    sealed class ConsoleEcsLogger : EcsRuntimeOptions.ILogger
    {
        public void Info(string msg)  => Console.WriteLine(msg);
        public void Warn(string msg)  => Console.WriteLine("WARN: "  + msg);
        public void Error(string msg) => Console.WriteLine("ERROR: " + msg);
    }
}
