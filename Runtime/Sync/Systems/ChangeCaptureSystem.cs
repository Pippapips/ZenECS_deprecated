using System;
using System.Collections.Generic;
using ZenECS.Core.Events;
using ZenECS.Core.Sync.Events;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Sync.Systems
{
    [UpdateGroup(typeof(PresentationGroup)), OrderBefore(typeof(ChangeBatchDispatchSystem))]
    public sealed class ChangeCaptureSystem : IInitSystem, IDisposeSystem, ILateRunSystem
    {
        private readonly List<ChangeRecord> _batch = new();
        private readonly IChangeFeed _feed;

        public ChangeCaptureSystem(IChangeFeed feed)
        {
            _feed = feed;
        } 

        public void LateRun(World w)
        {
            if (_batch.Count == 0) return;
            _feed.PublishBatch(_batch);
            _batch.Clear();
        }

        public void Init(World w)
        {
            ComponentEvents.ComponentAdded += EcsEventsOnComponentAdded;
            ComponentEvents.ComponentRemoved += EcsEventsOnComponentRemoved;
            ComponentEvents.ComponentChanged += EcsEventsOnComponentChanged;
        }

        public void Dispose(World w)
        {
            ComponentEvents.ComponentAdded -= EcsEventsOnComponentAdded;
            ComponentEvents.ComponentRemoved -= EcsEventsOnComponentRemoved;
            ComponentEvents.ComponentChanged -= EcsEventsOnComponentChanged;
        }
        
        private void EcsEventsOnComponentAdded(World w, Entity e, Type t)
        {
            _batch.Add(new ChangeRecord(e, t, ChangeMask.Added));
        }
        
        private void EcsEventsOnComponentRemoved(World w, Entity e, Type t)
        {
            _batch.Add(new ChangeRecord(e, t, ChangeMask.Removed));
        }

        private void EcsEventsOnComponentChanged(World w, Entity e, Type t)
        {
            _batch.Add(new ChangeRecord(e, t, ChangeMask.Changed));
        }
    }
}