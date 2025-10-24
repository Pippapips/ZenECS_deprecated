using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;
using ZenECS.Core.Infrastructure;

namespace ZenEcsCoreSamples.WriteHooks
{
    public struct Mana { public int Value; public Mana(int v){Value=v;} }

    public static class Program
    {
        class EcsLogger : EcsRuntimeOptions.ILogger
        {
            public void Info(string msg)
            {
                Console.WriteLine($"EcsLogger: {msg}");
            }
            
            public void Warn(string msg)
            {
                Console.WriteLine($"EcsLogger Warning: {msg}");
            }
            
            public void Error(string msg)
            {
                Console.WriteLine($"EcsLogger Error: {msg}");
            }
        }
        
        public static void Main()
        {
            var ecsLogger = new EcsLogger();
            EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
            EcsRuntimeOptions.Log = ecsLogger;
            
            var world = new World(new WorldConfig(initialEntityCapacity: 4));

            // Local per-world validator: forbid negative Mana
            world.AddValidator<Mana>(m => m.Value >= 0);

            // Local per-world write permission: disallow writes to odd entity IDs
            world.AddWritePermission((w, e, t) => (e.Id % 2) != 0);

            var even = world.CreateEntity(); // Id=1 initially; nextId starts at 1, but after first create -> 1
            var odd  = world.CreateEntity();

            // Try: even id may fail depending on id allocation; ensure we check behavior:
            TryAdd(world, even, new Mana(10));   // permitted?
            world.Add(even, new Mana(-5));   // rejected by validator
            world.Replace(odd, new Mana(3)); // rejected by permission (odd id)
            world.Remove<Mana>(odd);         // rejected by permission

            Console.WriteLine("Done.");
        }

        static void TryAdd<T>(World w, Entity e, in T v) where T : struct
        {
            try { w.Add(e, v); Console.WriteLine($"Add<{typeof(T).Name}> OK on e:{e.Id} -> {v}"); }
            catch (Exception ex) { Console.WriteLine($"Add<{typeof(T).Name}> FAIL on e:{e.Id} :: {ex.Message}"); }
        }
    }
}
