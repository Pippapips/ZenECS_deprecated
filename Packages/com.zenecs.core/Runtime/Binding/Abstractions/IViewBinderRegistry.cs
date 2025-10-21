#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    public interface IViewBinderRegistry
    {
        event Action<Entity, IViewBinder> Registered;
        bool Register(Entity e, IViewBinder v, bool replaceIfExists = true);
        bool Unregister(Entity e, IViewBinder target);
        IViewBinder? Resolve(Entity e);
        void Clear();
    }
}