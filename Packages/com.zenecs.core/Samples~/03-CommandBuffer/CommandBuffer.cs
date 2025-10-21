using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;

namespace ZenEcsCoreSamples.CommandBuffer
{
    public struct Health { public int Value; public Health(int v){Value=v;} public override string ToString()=>Value.ToString(); }
    public struct Stunned { public float Seconds; public Stunned(float s){Seconds=s;} }

    public static class Program
    {
        public static void Main()
        {
            var world = new World(new WorldConfig(initialEntityCapacity: 16));

            var e1 = world.CreateEntity();
            var e2 = world.CreateEntity();

            // Begin a write scope (thread-safe collection of ops)
            var cb = world.BeginWrite();
            cb.Add(e1, new Health(100));
            cb.Add(e2, new Health(80));
            cb.Add(e2, new Stunned(1.5f));

            // Replace and Remove also supported
            cb.Replace(e2, new Health(75));
            cb.Remove<Stunned>(e2);

            // Option A: schedule -> apply later at frame boundary
            world.Schedule(cb);
            Console.WriteLine("Scheduled ops. Before apply, Has<Health>(e1): " + world.Has<Health>(e1));

            // Apply scheduled
            world.RunScheduledJobs();

            Console.WriteLine($"After apply: e1 Health={world.Read<Health>(e1).Value}, e2 Health={world.Read<Health>(e2).Value}, Has<Stunned>(e2)={world.Has<Stunned>(e2)}");

            // Option B: Immediate apply via EndWrite
            var cb2 = world.BeginWrite();
            cb2.Replace(e1, new Health(42));
            world.EndWrite(cb2); // immediate
            Console.WriteLine($"Immediate apply result: e1 Health={world.Read<Health>(e1).Value}");
        }
    }
}
