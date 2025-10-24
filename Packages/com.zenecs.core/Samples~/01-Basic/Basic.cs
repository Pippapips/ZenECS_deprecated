﻿/*
    ZenECS.Core - Samples
    Basic world + MoveSystem (Simulation) + PrintPositions (Presentation).
    Position += Velocity * dt in Simulation; read-only print in Presentation.
    MIT | © 2025 Pippapips Limited
*/

using System.Diagnostics;
using ZenECS;                        // Kernel
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Basic
{
    public readonly struct Position
    {
        public readonly float X, Y;
        public Position(float x, float y) { X = x; Y = y; }
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y) { X = x; Y = y; }
    }

    /// <summary>Integrates: Position += Velocity * dt (Simulation)</summary>
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

    /// <summary>Read-only presentation: prints positions each frame</summary>
    [PresentationGroup]
    public sealed class PrintPositionsSystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            Console.WriteLine($"-- FrameCount: {w.FrameCount} (alpha={alpha:0.00}) --");
            foreach (var e in w.Query<Position>())
            {
                var p = w.Read<Position>(e); // ✅ read-only
                Console.WriteLine($"Entity {e.Id,3}: pos={p}");
            }
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== ZenECS Core Console Sample 01 Basic (Kernel) ===");

            // 부팅: 월드 구성 콜백에서 로거와 엔티티를 구성
            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 256),
                (world, bus) =>
                {
                    var ecsLogger = new EcsLogger();
                    EcsRuntimeOptions.Log = ecsLogger;

                    var e1 = world.CreateEntity();
                    world.Add(e1, new Position(0, 0));
                    world.Add(e1, new Velocity(1, 0));      // +X / sec

                    var e2 = world.CreateEntity();
                    world.Add(e2, new Position(2, 1));
                    world.Add(e2, new Velocity(0, -0.5f));  // -Y / sec
                }
            );

            // 시스템 초기화
            EcsKernel.InitializeSystems(new ISystem[]
            {
                new MoveSystem(),          // Simulation
                new PrintPositionsSystem() // Presentation (read-only)
            }, new SystemRunnerOptions(), null, Console.WriteLine);

            const float fixedDelta = 1f / 60f;   // 60Hz
            var sw   = Stopwatch.StartNew();
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
                float  dt  = (float)(now - prev);
                prev = now;

                // 가변 스텝 Begin + 고정 스텝 N회 + alpha 계산을 한 번에
                const int maxSubStepsPerFrame = 4;
                EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
                EcsKernel.LateFrame(alpha);

                Thread.Sleep(1); // be gentle to CPU
            }

            Console.WriteLine("Shutting down...");
            EcsKernel.Shutdown();
            Console.WriteLine("Done.");
        }

        class EcsLogger : EcsRuntimeOptions.ILogger
        {
            public void Info(string msg)  => Console.WriteLine(msg);
            public void Warn(string msg)  => Console.Error.WriteLine(msg);
            public void Error(string msg) => Console.Error.Write(msg);
        }
    }
}
