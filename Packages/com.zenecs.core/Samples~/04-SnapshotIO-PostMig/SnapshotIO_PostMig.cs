// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core Samples 04 Snapshot IO + Post Migration
// File: SnapshotIO_PostMig.cs
// Purpose: Demonstrates snapshot save/load and post-load migration using the
//          ZenECS serialization and migration pipeline.
// Key concepts:
//   • Binary snapshot save/load via SnapshotBackend
//   • Versioned component migration (PositionV1 → PositionV2)
//   • PostLoadMigration hook example
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ZenECS;
using ZenECS.Core;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.Snapshot
{
    // ──────────────────────────────────────────────────────────────────────────
    // Versioned Components
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct PositionV1
    {
        public readonly float X, Y;
        public PositionV1(float x, float y) { X = x; Y = y; }
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    public readonly struct PositionV2
    {
        public readonly float X, Y;
        public readonly int Layer;
        public PositionV2(float x, float y, int layer = 0)
        {
            X = x; Y = y; Layer = layer;
        }
        public override string ToString() => $"({X:0.##}, {Y:0.##}, layer:{Layer})";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Binary Formatters
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class PositionV1Formatter : BinaryComponentFormatter<PositionV1>
    {
        public override void Write(in PositionV1 v, ISnapshotBackend b)
        {
            b.WriteFloat(v.X);
            b.WriteFloat(v.Y);
        }
        public override PositionV1 ReadTyped(ISnapshotBackend b)
            => new PositionV1(b.ReadFloat(), b.ReadFloat());
    }

    public sealed class PositionV2Formatter : BinaryComponentFormatter<PositionV2>
    {
        public override void Write(in PositionV2 v, ISnapshotBackend b)
        {
            b.WriteFloat(v.X);
            b.WriteFloat(v.Y);
            b.WriteInt(v.Layer);
        }
        public override PositionV2 ReadTyped(ISnapshotBackend b)
            => new PositionV2(b.ReadFloat(), b.ReadFloat(), b.ReadInt());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Post-Load Migration
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class DemoPostLoadMigration : IPostLoadMigration
    {
        public int Order => 0;

        public void Run(World world)
        {
            foreach (var e in world.Query<PositionV1>())
            {
                var old = world.Read<PositionV1>(e);
                world.Replace(e, new PositionV2(old.X, old.Y, layer: 1));
                world.Remove<PositionV1>(e);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Systems
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a snapshot in memory, reloads it into a new world, performs
    /// migration (V1 → V2), and logs results.
    /// </summary>
    [SimulationGroup]
    public sealed class SnapshotDemoSystem : IVariableRunSystem
    {
        private bool _done;

        public void Run(World w)
        {
            if (_done) return;
            Console.WriteLine("=== Snapshot I/O + Post-Migration Demo ===");

            // Register StableIds & formatters at runtime
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            // Create data in V1
            var e = w.CreateEntity();
            w.Add(e, new PositionV1(3, 7));

            // Save snapshot (binary) into memory stream
            using var ms = new MemoryStream();
            w.SaveFullSnapshotBinary(ms);
            Console.WriteLine($"Saved snapshot bytes: {ms.Length}");

            // Load snapshot into a NEW world
            var world2 = new World(new WorldConfig(initialEntityCapacity: 8));
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            ms.Position = 0;
            world2.LoadFullSnapshotBinary(ms);

            // Run migration
            new DemoPostLoadMigration().Run(world2);

            // Verify results
            foreach (var e2 in world2.Query<PositionV2>())
            {
                var p = world2.Read<PositionV2>(e2);
                Console.WriteLine($"Migrated entity {e2.Id} → {p}");
            }

            _done = true;
        }
    }

    [PresentationGroup]
    public sealed class PrintSummarySystem : IPresentationSystem
    {
        public void Run(World w, float alpha)
        {
            // read-only logging for demonstration
            foreach (var e in w.Query<PositionV2>())
            {
                var p = w.Read<PositionV2>(e);
                Console.WriteLine($"Frame {w.FrameCount} Entity {e.Id}: PositionV2={p}");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Program Entry
    // ──────────────────────────────────────────────────────────────────────────
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== ZenECS Core Sample - SnapshotIO + PostMigration (Kernel) ===");

            EcsKernel.Start(
                new WorldConfig(initialEntityCapacity: 8),
                new ISystem[]
                {
                    new SnapshotDemoSystem(),  // Simulation system that performs IO + migration
                    new PrintSummarySystem(),  // Presentation system (read-only)
                },
                options: null,
                mainThreadGate: null,
                systemRunnerLog: Console.WriteLine,
                configure: (world, bus) => { }
            );

            const float fixedDelta = 1f / 60f;
            const int maxSubSteps = 4;

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
}
