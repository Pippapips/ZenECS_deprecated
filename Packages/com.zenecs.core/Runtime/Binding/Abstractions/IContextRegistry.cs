#nullable enable

namespace ZenECS.Core.Binding
{
    public interface IContextRegistry
    {
        bool TryGet<T>(World w, Entity e, out T? ctx) where T:class, IContext;
        void Register<T>(World w, Entity e, T ctx) where T:class, IContext;
        void UnregisterAll(World w, Entity e);        
    }    
}
