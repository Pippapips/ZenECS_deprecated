using System;
using System.IO;
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Serialization;
using ZenECS.Core.Serialization.Formats.Binary;

namespace ZenEcsCoreSamples.Snapshot
{
    // v1 Position component (for demo)
    public struct PositionV1 { public float X, Y; public PositionV1(float x,float y){X=x;Y=y;} }

    // v2 Position (renamed/changed shape) — target after migration
    public struct PositionV2 { public float X, Y; public int Layer; public PositionV2(float x,float y,int layer=0){X=x;Y=y;Layer=layer;} }

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

    // Formatter for V2
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

    public sealed class DemoPostLoadMigration : IPostLoadMigration
    {
        public int Order => 0;
        public void Run(World world)
        {
            // If any entity still has PositionV1, convert to V2 and remove V1.
            foreach (var e in world.Query<PositionV1>())
            {
                var p1 = world.Read<PositionV1>(e);
                world.Replace(e, new PositionV2(p1.X, p1.Y, layer: 1));
                world.Remove<PositionV1>(e);
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            // Register StableIds & formatters (runtime)
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            var world = new World(new WorldConfig(initialEntityCapacity: 8));

            // Create some data in V1
            var e = world.CreateEntity();
            world.Add(e, new PositionV1(3, 7));

            // Save snapshot (binary) to memory stream
            using var ms = new MemoryStream();
            world.SaveFullSnapshotBinary(ms);

            Console.WriteLine($"Saved snapshot bytes: {ms.Length}");

            // Load snapshot into a FRESH world
            var world2 = new World(new WorldConfig(initialEntityCapacity: 8));
            // We need the same registry entries in world2 runtime
            ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
            ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
            ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
            ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

            ms.Position = 0;
            world2.LoadFullSnapshotBinary(ms);

            // Post-migration
            var mig = new DemoPostLoadMigration();
            mig.Run(world2);

            // Verify
            foreach (var e2 in world2.Query<PositionV2>())
            {
                var p = world2.Read<PositionV2>(e2);
                Console.WriteLine($"Migrated entity {e2.Id} => PositionV2({p.X},{p.Y},layer:{p.Layer})");
            }
        }
    }
}
