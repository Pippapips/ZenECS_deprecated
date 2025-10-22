using System;
namespace ZenECS.Core.Binding
{
    public interface IComponentBinder<T>
    {
        void Bind(World w, Entity e, IViewBinder v);
        void Apply(World w, Entity e, in T value, IViewBinder v);
        void Unbind(World w, Entity e, IViewBinder v);
    }
}
