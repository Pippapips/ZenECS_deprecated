// TODO

using System;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Binding.Systems;
using ZenECS.Core.Extensions;
using ZenECS.Core.Systems;

namespace ZenEcsCoreSamples.ComponentFeed
{
    public struct TagA {}
    public struct ValueB { public int V; public ValueB(int v){V=v;} }

    public static class Program
    {
        public static void Main()
        {
            var w = new World(new WorldConfig(initialEntityCapacity: 8));
            var feed = new ComponentChangeFeed();

            // Subscribe to aggregated change batches (presentation phase)
            using var sub = feed.SubscribeRaw(batch => {
                Console.WriteLine($"Batch size: {batch.Count}");
                foreach (var rec in batch)
                    Console.WriteLine($" - e:{rec.Entity.Id} {rec.ComponentType.Name} {rec.Mask}");
            });

            // Build a minimal presentation pipeline with the hub that collects events and publishes to the feed
            var hub   = new ComponentBindingHubSystem(feed);
            // We won't use actual view binding in this sample; hub alone is enough to publish

            var runner = new SystemRunner(
                w,
                systems: new ISystem[]{ hub },
                bus: null,
                opt: new SystemRunnerOptions{ GuardWritesInPresentation = true }
            );

            runner.InitializeSystems();

            // Simulate one frame of structural changes
            var e1 = w.CreateEntity();
            w.Add(e1, new TagA());
            var e2 = w.CreateEntity();
            w.Add(e2, new ValueB(5));
            w.Replace(e2, new ValueB(9));
            w.Remove<ValueB>(e2);

            // Present once to flush and see one aggregated batch
            runner.LateFrame(interpolationAlpha: 1f);

            runner.ShutdownSystems();
        }
    }
}
