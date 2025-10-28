#nullable enable
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ZenECS.Core.Binding
{
    public sealed class ReflectionCachedTypeDispatcher : ITypeDispatcher
    {
        private static readonly MethodInfo _generic = typeof(ReflectionCachedTypeDispatcher)
            .GetMethod(nameof(CallGeneric), BindingFlags.NonPublic | BindingFlags.Static);
        private readonly ConcurrentDictionary<Type, Action<IBinder, Entity, byte, object>> _cache = new();
        public void DispatchDelta(Type t, IBinder b, Entity e, byte kind, object boxed)
        {
            var d = _cache.GetOrAdd(t, Create); d(b, e, kind, boxed);
        }
        private Action<IBinder, Entity, byte, object> Create(Type t)
            => (Action<IBinder, Entity, byte, object>)
                _generic.MakeGenericMethod(t).CreateDelegate(typeof(Action<IBinder, Entity, byte, object>));
        private static void CallGeneric<T>(IBinder b, Entity e, byte kind, object boxed) where T:struct
        {
            if (b is not IBinds<T> typed) return;
            var k = (ComponentDeltaKind)kind;
            var v = k==ComponentDeltaKind.Removed ? default : (boxed is T t ? t : default);
            typed.OnDelta(new ComponentDelta<T>(e, k, v));
        }
    }    
}
