#nullable enable

namespace ZenECS.Core.Binding
{
    public sealed class ContextEnsurer : IContextEnsurer
    {
        private readonly IContextRegistry _reg;
        private readonly IContextFactoryHub _hub;

        public ContextEnsurer(IContextRegistry reg, IContextFactoryHub hub) { _reg = reg; _hub = hub; }

        public bool EnsureForBinder(World w, Entity e, IBinder binder)
        {
            bool any = false;

            foreach (var itf in binder.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType || itf.GetGenericTypeDefinition() != typeof(IRequireContext<>)) continue;

                var t = itf.GetGenericArguments()[0];
                var mi = typeof(ContextEnsurer)  // 이 메서드가 들어있는 클래스 타입
                    .GetMethod(nameof(EnsureOne), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(t);

                any |= (bool)mi.Invoke(this, new object[] { w, e })!;
            }
            return any;
        }

        private bool EnsureOne<T>(World w, Entity e) where T : class, IContext
        {
            if (_reg.TryGet<T>(w, e, out _)) return false;
            if (!_hub.TryCreate<T>(w, e, out var ctx) || ctx is null) return false;

            _reg.Register(w, e, ctx);
            return true;
        }
    }
}