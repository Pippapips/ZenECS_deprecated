using System;
using System.Collections.Generic;
using ZenECS.Core.Events;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    [OrderBefore(typeof(ComponentBatchDispatchSystem))]
    public sealed class ComponentBindingHubSystem : ISystemLifecycle, IPresentationSystem
    {
        private readonly System.Collections.Generic.Dictionary<(Entity, System.Type), ComponentChangeMask> _batch = new();
        private readonly IComponentChangeFeed _feed;

        public ComponentBindingHubSystem(IComponentChangeFeed feed)
        {
            _feed = feed;
        }

        public void Initialize(World w)
        {
            ComponentEvents.ComponentAdded += EcsEventsOnComponentAdded;
            ComponentEvents.ComponentRemoved += EcsEventsOnComponentRemoved;
            ComponentEvents.ComponentChanged += EcsEventsOnComponentChanged;
        }

        public void Shutdown(World w)
        {
            ComponentEvents.ComponentAdded -= EcsEventsOnComponentAdded;
            ComponentEvents.ComponentRemoved -= EcsEventsOnComponentRemoved;
            ComponentEvents.ComponentChanged -= EcsEventsOnComponentChanged;
        }

        public void Run(World w)
        {
            if (_batch.Count == 0) return;
            _feed.PublishBatch(System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(_batch, kv => new ComponentChangeRecord(kv.Key.Item1, kv.Key.Item2, kv.Value))));
            _batch.Clear();
        }

        public void Run(World w, float alpha = 1)
        {
            Run(w, alpha);
        }

        private void EcsEventsOnComponentAdded(World w, Entity e, Type t)
        {
            if (t is null) return;
            _batch[(e, t)] = ComponentChangeMask.Added;
        }

        private void EcsEventsOnComponentRemoved(World w, Entity e, Type t)
        {
            if (t is null) return;
            _batch[(e, t)] = ComponentChangeMask.Removed;
        }

        private void EcsEventsOnComponentChanged(World w, Entity e, Type t)
        {
            if (t is null) return;
            _batch[(e, t)] = ComponentChangeMask.Changed;
        }
    }
}
