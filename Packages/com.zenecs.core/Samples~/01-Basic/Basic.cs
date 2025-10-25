// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 01 Basic
// File: Basic.cs
// Purpose: Minimal ECS sample demonstrating Kernel usage with a simulation and
//          presentation system.
// Key concepts:
//   • Demonstrates Position/Velocity component pattern
//   • MoveSystem integrates Position += Velocity * dt (SimulationGroup)
//   • PrintPositionsSystem reads and prints Position each frame (PresentationGroup)
//   • Shows typical Pump + LateFrame loop structure
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System.Diagnostics;
using ZenECS; // Kernel
using ZenECS.Core;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Basic
{
    /// <summary>
    /// Position component used for storing entity coordinates.
    /// </summary>
    public readonly struct Position
    {
        public readonly float X, Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    /// <summary>
    /// Velocity component representing delta per second.
    /// </summary>
    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Integrates: Position += Velocity * dt (Simulation phase)
    /// </summary>
    [SimulationGroup]
    public sealed class MoveSystem : IVariableRunSystem
    {
        public void Run(World w)
        {
            var dt = w.DeltaTime;
            foreach (var e in w.Query<Position, Velocity>())
            {
                var p = w.Read<Position>(e);
                var v = w.Read<Velocity>(e);
                w.Replace(e, new Position(p.X + v.X * dt, p.Y + v.Y * dt));
            }
        }
    }

    /// <summary>
    /// Read-only presentation: prints positions each frame (Late phase)
    /// </summary>
    [PresentationGroup]
    public sealed class PrintPositionsSystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            Console.WriteLine($"-- FrameCount: {w.FrameCount} (alpha={alpha:0.00}) --");
            foreach (var e in w.Query<Position>())
            {
                var p = w.Read<Position>(e); // read-only access
                Console.WriteLine($"Entity {e.Id,3}: pos={p}");
            }
        }
    }

    /// <summary>
    /// Entry point demonstrating ZenECS kernel-driven loop.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - Basic (Kernel) ===");

            // Boot: configure world and entities in setup callback
            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 256),
                new ISystem[]
                {
                    new MoveSystem(),          // Simulation
                    new PrintPositionsSystem() // Presentation (read-only)
                },
                options: null,
                mainThreadGate: null,
                systemRunnerLog: Console.WriteLine,
                configure: (world, bus) =>
                {
                    var ecsLogger = new EcsLogger();
                    EcsRuntimeOptions.Log = ecsLogger;

                    // Create sample entities with Position and Velocity
                    var e1 = world.CreateEntity();
                    world.Add(e1, new Position(0, 0));
                    world.Add(e1, new Velocity(1, 0)); // moves +X / sec

                    var e2 = world.CreateEntity();
                    world.Add(e2, new Position(2, 1));
                    world.Add(e2, new Velocity(0, -0.5f)); // moves -Y / sec
                }
            );

            const float fixedDelta = 1f / 60f; // 60Hz simulation
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    break;
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                // Perform variable-step Begin + multiple Fixed steps + alpha calculation
                const int maxSubStepsPerFrame = 4;
                EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
                EcsKernel.LateFrame(alpha);

                Thread.Sleep(1); // Reduce CPU load
            }

            Console.WriteLine("Shutting down...");
            EcsKernel.Shutdown();
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Simple logger implementation that routes ECS messages to the console.
        /// </summary>
        class EcsLogger : EcsRuntimeOptions.ILogger
        {
            public void Info(string msg) => Console.WriteLine(msg);
            public void Warn(string msg) => Console.Error.WriteLine(msg);
            public void Error(string msg) => Console.Error.Write(msg);
        }
    }
}
