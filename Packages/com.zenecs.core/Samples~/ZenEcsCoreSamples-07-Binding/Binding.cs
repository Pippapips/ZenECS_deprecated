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
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Diagnostics;
using System.Threading;
using ZenECS.Core;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;
using ZenECS.Core.Binding;

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
    /// Simple position component for demonstration.
    /// </summary>
    public readonly struct Health : IEquatable<Health>
    {
        public readonly int Hp, MaxHp;
        public Health(int hp, int maxHp)
        {
            Hp = hp;
            MaxHp = maxHp;
        }
        public bool Equals(Health other) => Hp == other.Hp && MaxHp == other.MaxHp;
        public override bool Equals(object? obj) => obj is Health other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Hp, MaxHp);
        public override string ToString() => $"({Hp}, {MaxHp})";
    }

    /// <summary>
    /// A simple console binder for Position component.
    /// Prints bind/apply/unbind events to the console.
    /// </summary>
    public sealed class ConsoleViewBinder : BaseBinder,
        // IAlwaysApply,
        IBinds<Position>, IBinds<Health>
    {
        public override int Priority => 10;
        private Position _p;
        private Health _h;

        public void OnDelta(in ComponentDelta<Position> delta)
        {
            Console.WriteLine($"Position {delta.Kind}");
            if (delta.Kind == ComponentDeltaKind.Removed)
            {
                _p = new Position(0, 0);
            }
            else
            {
                _p = delta.Value;                
            }
        }
        
        public void OnDelta(in ComponentDelta<Health> delta)
        {
            Console.WriteLine($"Health {delta.Kind}");
            if (delta.Kind == ComponentDeltaKind.Removed)
            {
                _h = new Health(0, 0);
            }
            else
            {
                _h = delta.Value;
            }
        }

        public override void Apply()
        {
            Console.WriteLine($"[Apply]     e={Entity} Position={_p}");
            Console.WriteLine($"[Apply]     e={Entity} Health={_h}");
        }
        
        protected override void OnBind(World w, Entity e)
        {
            Console.WriteLine($"[Bind]      e={e}");
        }

        protected override void OnUnbind()
        {
            Console.WriteLine($"[Unbind]    e={Entity}");
        }

        protected override void OnDispose()
        {
            Console.WriteLine($"[Disposed]  e={Entity}");
        }
    }

    /// <summary>
    /// Entry point demonstrating the ECS binding pipeline in a console environment.
    /// </summary>
    internal static class Program
    {
        private static World? _w;
        private static Entity _e;
        private static ConsoleViewBinder? _view;
        
        static void Main()
        {
            // --- Booting ECS ---
            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 256),
                null,
                null, 
                Console.WriteLine,
                false,
                () =>
                {
                    var ecsLogger = new EcsLogger();
                    EcsRuntimeOptions.Log = ecsLogger;

                    var world = EcsKernel.World;
                    
                    // Create entity and associate with a console view
                    var e = world.CreateEntity();
                    var view = new ConsoleViewBinder();
                    world.BindingRouter?.Attach(e, view);

                    // Add and modify Position component
                    world.Add(e, new Position(1, 1));
                    world.Replace(e, new Position(2.5f, 4));
                    
                    world.Add(e, new Health(10, 100));

                    _w = world;
                    _e = e;
                    _view = view;
                }
            );
            
            const float fixedDelta = 1f / 60f;   // 60Hz
            var sw = Stopwatch.StartNew();
            double prev = sw.Elapsed.TotalSeconds;

            Console.WriteLine("Running... press any key to exit.");

            bool loop = true;
            int exitStep = 0;
            while (loop)
            {
                if (exitStep == 1)
                {
                    Console.WriteLine("Exiting... step 1");
                    if (_view != null && _w != null && _w.IsAlive(_e))
                    {
                        // Two-kind of unbind view
                        //_w.ComponentDeltaDispatcher.Detach(_e, _view);
                        _w.DestroyEntity(_e);
                        
                        _view.Dispose();
                        _view = null;
                    }
                    loop = false;
                }
                
                if (exitStep == 0 && Console.KeyAvailable)
                {
                    _ = Console.ReadKey(intercept: true);
                    if (_view != null && _w != null && _w.IsAlive(_e))
                    {
                        _w.Remove<Position>(_e);
                        exitStep++;
                    }
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
