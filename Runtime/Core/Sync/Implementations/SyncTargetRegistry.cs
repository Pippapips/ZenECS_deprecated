#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Sync
{
    public sealed class SyncTargetRegistry : ISyncTargetRegistry
    {
        // 메인스레드 전제 (Unity)
        private readonly Dictionary<int, ISyncTarget> _registries = new();

        public SyncTargetRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachSyncTargetRegistry(this);
        }

        public event Action<Entity, ISyncTarget>? Registered;

        public bool Register(Entity e, ISyncTarget t, bool replaceIfExists = true)
        {
            if (_registries.ContainsKey(e.Id) && !replaceIfExists) return false;
            _registries[e.Id] = t;
            Registered?.Invoke(e, t); 
            return true;
        }

        public bool Unregister(Entity e, ISyncTarget t)
            => _registries.TryGetValue(e.Id, out var cur) && ReferenceEquals(cur, t) && _registries.Remove(e.Id);

        public ISyncTarget? Resolve(Entity e)
            => _registries.GetValueOrDefault(e.Id);

        public void Clear() => _registries.Clear();
    }
}