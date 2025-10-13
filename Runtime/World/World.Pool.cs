using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // Type -> IComponentPool 생성 팩토리 캐시
        static readonly Dictionary<Type, Func<IComponentPool>> _poolFactories = new();

        static Func<IComponentPool> GetOrBuildPoolFactory(Type compType)
        {
            if (_poolFactories.TryGetValue(compType, out var f)) return f;

            var closed = typeof(ComponentPool<>).MakeGenericType(compType);
            try
            {
                var ctor = closed.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    // () => { var p = new ComponentPool<T>(); p.EnsureInitialized(); return p; }
                    var newExpr = Expression.New(ctor);
                    var varP = Expression.Variable(typeof(IComponentPool), "p");
                    var assign = Expression.Assign(varP, Expression.Convert(newExpr, typeof(IComponentPool)));
                    var callInit = Expression.Call(varP,
                        typeof(IComponentPool).GetMethod(nameof(IComponentPool.EnsureInitialized)));
                    var block = Expression.Block(new[] { varP }, assign, callInit, varP);
                    f = Expression.Lambda<Func<IComponentPool>>(block).Compile();
                }
                else
                {
                    f = () =>
                    {
                        var obj = (IComponentPool)FormatterServices.GetUninitializedObject(closed);
                        obj.EnsureInitialized();
                        return obj;
                    };
                }
            }
            catch
            {
                f = () =>
                {
                    var obj = (IComponentPool)FormatterServices.GetUninitializedObject(closed);
                    obj.EnsureInitialized();
                    return obj;
                };
            }

            _poolFactories[compType] = f;
            return f;
        }
    }
}