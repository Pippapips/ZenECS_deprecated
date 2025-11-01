#nullable enable
using ZenECS.Core.Infrastructure;

namespace ZenECS.Core.Binding
{
    public static class Ctx
    {
        public static T Get<T>(World w, Entity e) where T : class, IContext
            => EcsKernel.ContextRegistry.Get<T>(w, e);
        public static bool TryGet<T>(World w, Entity e, out T ctx) where T : class, IContext
            => EcsKernel.ContextRegistry.TryGet(w, e, out ctx);
        public static bool Has<T>(World w, Entity e) where T : class, IContext
            => EcsKernel.ContextRegistry.Has<T>(w, e);
    }
}