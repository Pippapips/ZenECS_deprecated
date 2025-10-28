#nullable enable

namespace ZenECS.Core.Binding
{
    public interface IContextFactoryHub
    {
        void Register<T>(IContextFactory<T> f) where T : class, IContext;
        bool TryCreate<T>(World w, Entity e, out T? ctx) where T : class, IContext;
    }
}
