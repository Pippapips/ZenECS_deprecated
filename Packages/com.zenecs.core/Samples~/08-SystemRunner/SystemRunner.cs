using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.SystemRunner
{
    // Components
    public struct Position { public float X, Y; public Position(float x,float y){X=x;Y=y;} }
    public struct Velocity { public float X, Y; public Velocity(float x,float y){X=x;Y=y;} }

    // Systems
    [SimulationGroup]
    public sealed class MoveSystem : IVariableRunSystem
    {
        public void Run(World w)
        {
            foreach (var e in w.Query<Position, Velocity>())
            {
                var p = w.Read<Position>(e);
                var v = w.Read<Velocity>(e);
                p.X += v.X * w.DeltaTime;
                p.Y += v.Y * w.DeltaTime;
                w.Replace(e, p);
            }
        }
    }

    [PresentationGroup]
    public sealed class PrintSystem : IPresentationSystem
    {
        public void Run(World w, float interpolationAlpha)
        {
            foreach (var e in w.Query<Position>())
            {
                var p = w.Read<Position>(e);
                Console.WriteLine($"[Present] e:{e.Id} pos=({p.X:0.00},{p.Y:0.00})");
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var w = new World(new WorldConfig(initialEntityCapacity: 16));
            var bus = new MessageBus();
            var systems = new ISystem[] { new MoveSystem(), new PrintSystem() };

            var runner = new ZenECS.Core.Systems.SystemRunner(w, bus, systems, new SystemRunnerOptions{
                GuardWritesInPresentation = true,
                FlushPolicy = StructuralFlushPolicy.EndOfSimulation
            });

            runner.InitializeSystems();

            var e = w.CreateEntity();
            w.Add(e, new Position(0, 0));
            w.Add(e, new Velocity(1, 0.5f));

            // Simple realtime-ish loop (fixed+variable hybrid)
            float accumulator = 0f;
            const float fixedDt = 0.02f; // 50 Hz
            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalSeconds < 0.2) // keep short for console sample
            {
                float delta = 0.016f; // pretend frame time

                // Variable-step (optional)
                runner.BeginFrame(delta);
                
                accumulator += delta;
                // Fixed-step simulation
                while (accumulator >= fixedDt)
                {
                    runner.FixedStep(fixedDt);
                    accumulator -= fixedDt;
                }

                // Present (read-only)
                runner.LateFrame(interpolationAlpha: 1f);
            }

            runner.ShutdownSystems();
        }
    }
}
