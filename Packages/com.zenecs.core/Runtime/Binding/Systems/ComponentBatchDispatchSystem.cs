#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding.Util;
using ZenECS.Core.Systems;

namespace ZenECS.Core.Binding.Systems
{
    [PresentationGroup]
    [OrderAfter(typeof(ComponentBindingHubSystem))]
    [OrderBefore(typeof(ViewBindingSystem))]
    public sealed class ComponentBatchDispatchSystem : ISystemLifecycle, IPresentationSystem
    {
        private IDisposable? _subscriptionForFeed;

        private readonly IComponentChangeFeed _feed;
        private readonly ViewBindingSystem _host;
        private readonly Queue<ComponentChangeRecord> _queue = new();

        public ComponentBatchDispatchSystem(IComponentChangeFeed feed, ViewBindingSystem host)
        {
            _feed = feed;
            _host = host;
        }

        public void Initialize(World w)
        {
            _subscriptionForFeed = _feed.SubscribeRaw(OnBatch);
        }

        public void Shutdown(World w)
        {
            _subscriptionForFeed?.Dispose();
            _subscriptionForFeed = null;
            _queue.Clear();
        }

        public void Run(World w)
        {
            while (_queue.Count > 0)
            {
                var rec = _queue.Dequeue();
                var t = rec.ComponentType;
                if ((rec.Mask & ComponentChangeMask.Removed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentRemoved),
                        genericArg: t,
                        w, rec.Entity);
                    continue;
                }

                if (!TryGetCurrentValue(w, rec.Entity, t, out var boxed)) continue;

                if ((rec.Mask & ComponentChangeMask.Added) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentAdded),
                        genericArg: t,
                        w, rec.Entity, boxed);
                }

                if ((rec.Mask & ComponentChangeMask.Changed) != 0)
                {
                    GenericInvokerCache.Invoke(
                        target: _host,
                        methodName: nameof(ViewBindingSystem.OnComponentChanged),
                        genericArg: t,
                        w, rec.Entity, boxed);
                }
            }
        }

        public void Run(World w, float alpha = 1)
        {
            Run(w, alpha);
        }

        private void OnBatch(IReadOnlyList<ComponentChangeRecord> recs)
        {
            foreach (var t in recs)
            {
                _queue.Enqueue(t);
            }
        }

        private static bool TryGetCurrentValue(World w, Entity e, System.Type t, out object? value)
        {
            return w.TryGetBoxed(e, t, out value);
        }
    }
}
