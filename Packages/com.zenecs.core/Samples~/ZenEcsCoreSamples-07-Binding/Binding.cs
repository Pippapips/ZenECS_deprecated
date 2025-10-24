// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 07 Binding
// File: Binding.cs
// Purpose: Console demo showcasing the ZenECS Core binding pipeline (Unity-free)
// Key concepts:
//   • Demonstrates Entity creation, component binding, and view binding flow
//   • Uses Position component with a console-based view binder
//   • Simulates ECS runtime loop with Begin/Fixed/Late steps
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Binding.Systems;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Extensions;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging;
using ZenECS.Core.Sync;
using ZenECS.Core.Systems;

namespace ZenECS.Binding.ConsoleSample
{
    /// <summary>
    /// Simple position component for demonstration.
    /// </summary>
    public readonly struct Position : IEquatable<Position>
    {
        public readonly float X, Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
        public bool Equals(Position other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>
    /// A simple console binder for Position component.
    /// Prints bind/apply/unbind events to the console.
    /// </summary>
    public sealed class PositionBinder : IComponentBinder<Position>, IComponentBinder
    {
        public Type ComponentType => typeof(Position);

        public void Bind(World w, Entity e, IViewBinder v)
            => Console.WriteLine($"[Bind]   e={e} Position");

        public void Apply(World w, Entity e, in Position value, IViewBinder v)
            => Console.WriteLine($"[Apply]  e={e} Position={value}");

        public void Unbind(World w, Entity e, IViewBinder v)
            => Console.WriteLine($"[Unbind] e={e} Position");

        // Explicit non-generic interface implementations
        void IComponentBinder.Bind(World w, Entity e, IViewBinder t) => Bind(w, e, t);
        void IComponentBinder.Apply(World w, Entity e, object value, IViewBinder t) => Apply(w, e, (Position)value, t);
        void IComponentBinder.Unbind(World w, Entity e, IViewBinder t) => Unbind(w, e, t);
    }

    /// <summary>
    /// Simplified view binder for console output.
    /// </summary>
    public sealed class ConsoleViewBinder : IViewBinder
    {
        public string Name { get; }
        public ConsoleViewBinder(string name) => Name = name;
        public override string ToString() => Name;

        public Entity Entity { get; }
        public int HandleId { get; }

        public void SetEntity(Entity e)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Entry point demonstrating the ECS binding pipeline in a console environment.
    /// </summary>
    internal static class Program
    {
        private static World? _w;
        private static IMessageBus? _bus; 
        
        static void Main()
        {
            // --- Booting ECS ---
            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 256),
                (world, bus) =>
                {
                    var ecsLogger = new EcsLogger();
                    EcsRuntimeOptions.Log = ecsLogger;
                    _w = world;
                    _bus = bus;
                }
            );
            
            // --- Initialize ECS systems (none required for this simple test) ---
            EcsKernel.InitializeSystems(Array.Empty<ISystem>(), new SystemRunnerOptions(), null, Console.WriteLine);

            // Register the Position binder globally
            EcsRuntimeDirectory.ComponentBinderRegistry?.RegisterSingleton<Position>(new PositionBinder());

            if (_w == null)
            {
                Console.Error.WriteLine("World initialization failed.");
                Console.ReadLine();
                return;
            }

            // Create entity and associate with a console view
            var e = _w.CreateEntity();
            var view = new ConsoleViewBinder("Player-View");
            EcsRuntimeDirectory.ViewBinderRegistry?.Register(e, view);

            // Add and modify Position component
            _w.Add(e, new Position(1, 1));
            _w.Replace(e, new Position(2.5f, 4));
            
            const float fixedDelta = 1f / 60f;   // 60Hz
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            bool loop = true;
            while (loop)
            {
                if (Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    _w.Remove<Position>(e);                    
                    loop = false;
                }

                double now = sw.Elapsed.TotalSeconds;
                float dt = (float)(now - prev);
                prev = now;

                // Performs variable-step Begin + multiple Fixed steps + alpha calculation in one call
                const int maxSubStepsPerFrame = 4;
                EcsKernel.Pump(dt, fixedDelta, maxSubStepsPerFrame, out var alpha);
                EcsKernel.LateFrame(alpha);

                if (!loop)
                    break;
                
                Thread.Sleep(1); // CPU-friendly wait
            }

            Console.WriteLine("Shutting down...");
            EcsKernel.Shutdown();
            Console.WriteLine("Done.");
        }
        
        /// <summary>
        /// Simple logger implementation forwarding ECS messages to console.
        /// </summary>
        class EcsLogger : EcsRuntimeOptions.ILogger
        {
            public void Info(string msg)  => Console.WriteLine(msg);
            public void Warn(string msg)  => Console.Error.WriteLine(msg);
            public void Error(string msg) => Console.Error.Write(msg);
        }
    }
}
