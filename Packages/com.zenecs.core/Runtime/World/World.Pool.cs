using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ZenECS.Core.Internal;
using System.Runtime.CompilerServices; // RuntimeHelpers
using System.Reflection;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        private readonly Dictionary<Type, IComponentPool> pools;

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
                    var block = Expression.Block(new[] { varP }, assign, varP);
                    f = Expression.Lambda<Func<IComponentPool>>(block).Compile();
                }
                else
                {
                    f = () =>
                    {
                        return (IComponentPool)RuntimeHelpers.GetUninitializedObject(closed);;
                    };
                }
            }
            catch
            {
                f = () =>
                {
                    return (IComponentPool)RuntimeHelpers.GetUninitializedObject(closed);
                };
            }

            _poolFactories[compType] = f;
            return f;
        }
    }
}
