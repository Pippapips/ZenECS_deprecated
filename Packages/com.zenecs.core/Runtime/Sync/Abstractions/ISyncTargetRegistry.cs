#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Sync
{
    public interface ISyncTargetRegistry
    {
        event Action<Entity, ISyncTarget> Registered;
        bool Register(Entity e, ISyncTarget target, bool replaceIfExists = true);
        bool Unregister(Entity e, ISyncTarget target);
        ISyncTarget? Resolve(Entity e);
        void Clear();
    }
}