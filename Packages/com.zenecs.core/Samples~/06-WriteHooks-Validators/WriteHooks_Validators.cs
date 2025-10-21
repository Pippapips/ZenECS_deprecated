using System;
using ZenECS.Core;
using ZenECS.Core.Extensions;

namespace ZenEcsCoreSamples.WriteHooks
{
    public struct Mana { public int Value; public Mana(int v){Value=v;} }

    public static class Program
    {
        public static void Main()
        {
            var world = new World(new WorldConfig(initialEntityCapacity: 4));

            // Local per-world validator: forbid negative Mana
            world.AddValidator(m => ((Mana)m).Value >= 0);

            // Local per-world write permission: disallow writes to odd entity IDs
            world.AddWritePermission((w, e, t) => (e.Id % 2) == 0);

            var even = world.CreateEntity(); // Id=1 initially; nextId starts at 1, but after first create -> 1
            var odd  = world.CreateEntity();

            // Try: even id may fail depending on id allocation; ensure we check behavior:
            TryAdd(world, even, new Mana(10));            // permitted?
            TryAdd(world, odd,  new Mana(-5));            // rejected by validator
            TryReplace(world, odd, new Mana(3));          // rejected by permission (odd id)
            TryRemove<Mana>(world, odd);                  // rejected by permission

            Console.WriteLine("Done.");
        }

        static void TryAdd<T>(World w, Entity e, in T v) where T : struct
        {
            try { w.Add(e, v); Console.WriteLine($"Add<{typeof(T).Name}> OK on e:{e.Id} -> {v}"); }
            catch (Exception ex) { Console.WriteLine($"Add<{typeof(T).Name}> FAIL on e:{e.Id} :: {ex.Message}"); }
        }

        static void TryReplace<T>(World w, Entity e, in T v) where T: struct
        {
            try { w.Replace(e, v); Console.WriteLine($"Replace<{typeof(T).Name}> OK on e:{e.Id} -> {v}"); }
            catch (Exception ex) { Console.WriteLine($"Replace<{typeof(T).Name}> FAIL on e:{e.Id} :: {ex.Message}"); }
        }

        static void TryRemove<T>(World w, Entity e) where T: struct
        {
            try { w.Remove<T>(e); Console.WriteLine($"Remove<{typeof(T).Name}> OK on e:{e.Id}"); }
            catch (Exception ex) { Console.WriteLine($"Remove<{typeof(T).Name}> FAIL on e:{e.Id} :: {ex.Message}"); }
        }
    }
}
