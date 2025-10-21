#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Binding;

namespace ZenECS.Core.Sync
{
    public sealed class ViewBinderRegistry : IViewBinderRegistry
    {
        // 메인스레드 전제 (Unity)
        private readonly Dictionary<Entity, IViewBinder> _registries = new();

        public ViewBinderRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachSyncTargetRegistry(this);
        }

        public event Action<Entity, IViewBinder>? Registered;

        public bool Register(Entity e, IViewBinder v, bool replaceIfExists = true)
        {
            if (_registries.ContainsKey(e) && !replaceIfExists) return false;
            _registries[e] = v;
            Registered?.Invoke(e, v);
            return true;
        }

        public bool Unregister(Entity e, IViewBinder target)
            => _registries.TryGetValue(e, out IViewBinder? cur) && ReferenceEquals(cur, target) && _registries.Remove(e);

        public IViewBinder? Resolve(Entity e)
            => _registries.GetValueOrDefault(e);

        public void Clear() => _registries.Clear();
    }
}
