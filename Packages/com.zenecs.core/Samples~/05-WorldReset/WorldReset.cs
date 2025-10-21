using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;

namespace ZenEcsCoreSamples.WorldReset
{
    public struct Health { public int Value; public Health(int v){Value=v;} }

    public static class Program
    {
        public static void Main()
        {
            var world = new World(new WorldConfig(initialEntityCapacity: 8, initialFreeIdCapacity: 8));

            // Seed some data
            var e1 = world.CreateEntity();
            var e2 = world.CreateEntity();
            world.Add(e1, new Health(100));
            world.Add(e2, new Health(50));
            Console.WriteLine($"Before reset: alive={world.AliveCount}, e1.Has(Health)={world.Has<Health>(e1)}");

            // Option A: keep capacity (fastest) — clears data but preserves internal arrays/pools
            world.ResetButKeepCapacity();
            Console.WriteLine($"After ResetButKeepCapacity: alive={world.AliveCount}");

            // Re-seed
            var e3 = world.CreateEntity();
            world.Add(e3, new Health(77));
            Console.WriteLine($"Re-seed: alive={world.AliveCount}, e3.Has(Health)={world.Has<Health>(e3)}");

            // Option B: HardReset — rebuild internal structures from initial config
            world.HardReset();
            Console.WriteLine($"After HardReset: alive={world.AliveCount}");
        }
    }
}
