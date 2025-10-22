#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ZenECS.Core.Binding.Util
{
    public static class BinderFastInvokerCache
    {
        private static readonly ConcurrentDictionary<Type, Action<World, Entity, IViewBinder, IComponentBinder>> _applyNoBox = new();
        public static Action<World, Entity, IViewBinder, IComponentBinder> GetApplyNoBox(Type t) => _applyNoBox.GetOrAdd(t, Create);

        private static Action<World, Entity, IViewBinder, IComponentBinder> Create(Type t)
        {
            var w = Expression.Parameter(typeof(World), "w");
            var e = Expression.Parameter(typeof(Entity), "e");
            var v = Expression.Parameter(typeof(IViewBinder), "v");
            var b = Expression.Parameter(typeof(IComponentBinder), "b");

            var genericTryGetDef =
                typeof(World)
                    .GetMethods(System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                        (m.Name == "TryGet" || m.Name == "TryGetComponentInternal") &&
                        m.IsGenericMethodDefinition &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(Entity) &&
                        m.GetParameters()[1].ParameterType.IsByRef)
                ?? throw new InvalidOperationException("World.TryGet<T>(...) 또는 World.TryGetComponentInternal<T>(...)가 필요합니다.");
            var tryGet = genericTryGetDef.MakeGenericMethod(t); // ← 닫힌 메서드로 변환            

            var typed = Expression.Convert(b, typeof(IComponentBinder<>).MakeGenericType(t));
            var apply = typed.Type.GetMethod("Apply", new[] { typeof(World), typeof(Entity), t.MakeByRefType(), typeof(IViewBinder) })!;

            var val = Expression.Variable(t, "val");
            var ifTry = Expression.IfThen(Expression.Call(w, tryGet, e, val), Expression.Call(typed, apply, w, e, val, v));
            var block = Expression.Block(new[] { val }, ifTry);
            return Expression.Lambda<Action<World, Entity, IViewBinder, IComponentBinder>>(block, w, e, v, b).Compile();
        }
    }
}