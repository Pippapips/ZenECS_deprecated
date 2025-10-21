using System;

namespace ZenECS.Core.Binding
{
    public interface IComponentBinder
    {
        Type ComponentType { get; }
        void Bind(World w, Entity e, IViewBinder t);
        void Apply(World w, Entity e, object value, IViewBinder t);
        void Unbind(World w, Entity e, IViewBinder t);
    }
}