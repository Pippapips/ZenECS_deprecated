#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Sync.Events;
using ZenECS.Core.Sync.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Sync.Systems
{
    [UpdateGroup(typeof(PresentationGroup)),
     OrderAfter(typeof(ChangeCaptureSystem)),
     OrderBefore(typeof(SyncHostSystem))]
    public sealed class ChangeBatchDispatchSystem : IInitSystem, IDisposeSystem, ILateRunSystem
    {
        private IDisposable? _subscriptionForFeed;
        
        private readonly IChangeFeed _feed;
        private readonly World _world;
        private readonly SyncHostSystem _host;
        private readonly Queue<ChangeRecord> _queue = new();

        public ChangeBatchDispatchSystem(IChangeFeed feed, World world, SyncHostSystem host)
        {
            _feed = feed;
            _world = world;
            _host = host;
        }

        public void Init(World w)
        {
            _subscriptionForFeed = _feed.SubscribeRaw(OnBatch);
        }

        public void Dispose(World w)
        {
            _subscriptionForFeed?.Dispose();
            _subscriptionForFeed = null;
            _queue.Clear();
        }

        private void OnBatch(IReadOnlyList<ChangeRecord> recs)
        {
            foreach (var t in recs)
                _queue.Enqueue(t);
        }

        public void LateRun(World w)
        {
            while (_queue.Count > 0)
            {
                var rec = _queue.Dequeue();
                var t = rec.ComponentType;
                if ((rec.Mask & ChangeMask.Removed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(SyncHostSystem.OnComponentRemoved),
                        genericArg: t,
                        _world, rec.Entity);
                    continue;
                }

                if (!TryGetCurrentValue(_world, rec.Entity, t, out var boxed)) continue;
                
                if ((rec.Mask & ChangeMask.Added) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(SyncHostSystem.OnComponentAdded),
                        genericArg: t,
                        _world, rec.Entity, boxed);
                }

                if ((rec.Mask & ChangeMask.Changed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(SyncHostSystem.OnComponentChanged),
                        genericArg: t,
                        _world, rec.Entity, boxed);
                }
            }
        }

        private static bool TryGetCurrentValue(World w, Entity e, System.Type t, out object? value)
        {
            return w.TryGetBoxed(e, t, out value);
        }
    }
}