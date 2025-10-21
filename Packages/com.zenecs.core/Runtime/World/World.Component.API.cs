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
        /// <summary>
        /// 특정 엔티티의 모든 컴포넌트를 제거(엔티티는 유지).
        /// </summary>
        public void ClearComponentsOf(int entityId)
        {
            if (entityId <= 0 || entityId >= alive.Length || !alive.Get(entityId))
                return;

            foreach (var kv in GetAllPools())
            {
                kv.Value.Remove(entityId);
            }
        }

        /// <summary>
        /// 모든 엔티티의 컴포넌트를 제거(엔티티는 유지).
        /// 툴링/장면 전환 시 월드 구조를 유지하고 데이터만 비울 때 사용.
        /// </summary>
        public void ClearAllComponents()
        {
            var types = new List<Type>(pools.Keys);
            foreach (var t in types)
            {
                pools[t] = CreateEmptyPoolForType(t, alive.Length);
            }
        }

        public IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e)
        {
            foreach (var kv in pools)
                if (kv.Value.Has(e.Id))
                    yield return (kv.Key, kv.Value.GetBoxed(e.Id));
        }

        public void AddBoxed(Entity e, Type t, object boxed) => GetOrCreatePoolByType(t).SetBoxed(e.Id, boxed);
        public object? GetBoxed(Entity e, Type t) => GetOrCreatePoolByType(t).GetBoxed(e.Id);
        public bool TryGetBoxed(Entity e, Type t, out object? boxed)
        {
            var obj = GetBoxed(e, t);
            boxed = obj;
            return obj != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasComponentInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            return pool != null && pool.Has(e.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetComponentInternal<T>(Entity e, out T value) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool != null && pool.Has(e.Id))
            {
                value = ((ComponentPool<T>)pool).Get(e.Id);
                return true;
            }

            value = default;
            return false;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal void AddComponentInternal<T>(Entity e, in T value) where T : struct
        {
            if (!IsAlive(e)) throw new InvalidOperationException($"Add<{typeof(T).Name}>: Entity {e.Id} dead.");
            var pool = GetPool<T>();
            pool.EnsureCapacity(e.Id);
            ref var r = ref ((ComponentPool<T>)pool).Ref(e.Id);
            r = value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal bool RemoveComponentInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null) return false;
            var had = pool.Has(e.Id);
            pool.Remove(e.Id);
            return had;
        }

        // Use runtime for performance
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal ref T RefComponentInternal<T>(Entity e) where T:struct
        {
            var pool = (ComponentPool<T>)GetPool<T>();
            return ref pool.Ref(e.Id);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal ref T RefComponentExistingInternal<T>(Entity e) where T:struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }
    }
}
