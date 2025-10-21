#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public partial class World
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerable<KeyValuePair<Type, IComponentPool>> GetAllPools() => pools;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IComponentPool GetPool<T>() where T : struct
        {
            var t = typeof(T);
            if (!pools.TryGetValue(t, out var pool))
            {
                pool = new ComponentPool<T>();
                pools.Add(t, pool);
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentPool<T>? TryGetPoolInternal<T>() where T : struct
            => pools.TryGetValue(typeof(T), out var p) ? (ComponentPool<T>)p : null;

        private IComponentPool GetOrCreatePoolByType(Type t)
        {
            if (!pools.TryGetValue(t, out var pool))
            {
                var factory = GetOrBuildPoolFactory(t); // ✅ 안전 팩토리
                pool = factory();
                pool.EnsureCapacity(0); // 안전한 최소 확보
                pools.Add(t, pool);
            }

            return pool;
        }
    }
}
