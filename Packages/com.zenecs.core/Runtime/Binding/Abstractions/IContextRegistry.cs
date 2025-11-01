#nullable enable

namespace ZenECS.Core.Binding
{
    public interface IContextLookup
    {
        bool TryGet<T>(World w, Entity e, out T ctx) where T : class, IContext;
        T Get<T>(World w, Entity e) where T : class, IContext;
        bool Has<T>(World w, Entity e) where T : class, IContext;
        bool Has(World w, Entity e, IContext ctx);
    }

    public interface IContextRegistry : IContextLookup
    {
        // Register / Remove (registry manages Initialize/Deinitialize & initialized flag)
        void Register(World w, Entity e, IContext ctx);
        bool Remove(World w, Entity e, IContext ctx);
        bool Remove<T>(World w, Entity e) where T : class, IContext;

        // Reinitialize (fast path or Deinit→Init fallback)
        bool Reinitialize(World w, Entity e, IContext ctx);
        bool Reinitialize<T>(World w, Entity e) where T : class, IContext;

        // State / cleanup
        bool IsInitialized(World w, Entity e, IContext ctx);
        bool IsInitialized<T>(World w, Entity e) where T : class, IContext;
        void Clear(World w, Entity e);
        void ClearAll();
    }
}