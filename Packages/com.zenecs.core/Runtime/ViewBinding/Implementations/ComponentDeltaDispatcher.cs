#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.ViewBinding
{
    public sealed class ComponentDeltaDispatcher : IComponentDeltaDispatcher
    {
        private readonly World _world;
        private readonly ITypeDispatcher _type;
        private readonly IEnsurePolicy _ensure;

        private readonly Dictionary<Entity, ViewBinderSet> _sets = new();
        private readonly Dictionary<Type, List<IViewBinder>> _subscribers = new();
        private readonly Dictionary<(Entity, Type), (byte kind, object val)> _delta = new();

        // 이번 프레임 델타를 받은 바인더
        private readonly HashSet<IViewBinder> _touched = new();

        // 바인더가 스스로 예약한 Apply
        private readonly HashSet<IViewBinder> _scheduled = new();
        
        public ComponentDeltaDispatcher(World w, ITypeDispatcher? type = null, IEnsurePolicy? ensure = null)
        {
            _world = w ?? throw new ArgumentNullException(nameof(w));
            _type = type ?? new ReflectionCachedTypeDispatcher();
            _ensure = ensure ?? new NoOpEnsurePolicy();
        }

        public void Attach(Entity e, IViewBinder b)
        {
            _ensure.EnsureFor(_world, e, b);
            b.Bind(_world, e);
            if (!_sets.TryGetValue(e, out var set)) _sets[e] = set = new ViewBinderSet();
            set.Attach(b);
            IndexBinderSubscriptions(b);

            _scheduled.Add(b); // Bind 직후 1회 Apply            
        }

        public void Detach(Entity e, IViewBinder b)
        {
            if (_sets.TryGetValue(e, out var set) && set.Detach(b))
                b.Unbind();
            DeindexBinderSubscriptions(b);
        }
        
        /* -------- Subscription Indexing -------- */

        private void IndexBinderSubscriptions(IViewBinder binder)
        {
            foreach (var itf in binder.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType) continue;
                if (itf.GetGenericTypeDefinition() != typeof(IBinds<>)) continue;
                var t = itf.GetGenericArguments()[0];
                if (!_subscribers.TryGetValue(t, out var list)) _subscribers[t] = list = new();
                list.Add(binder);
            }
        }

        private void DeindexBinderSubscriptions(IViewBinder binder)
        {
            foreach (var itf in binder.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType) continue;
                if (itf.GetGenericTypeDefinition() != typeof(IBinds<>)) continue;
                var t = itf.GetGenericArguments()[0];
                if (_subscribers.TryGetValue(t, out var list))
                    list.Remove(binder);
            }
        }
        
        /* 델타 분배 중: 해당 바인더를 'touched'에 마킹 */
        private void DispatchToBinders(Type t, Entity e, byte kind, object boxed, ViewBinderSet set)
        {
            var span = set.EnumerateSorted();
            for (int i = 0; i < span.Length; i++)
            {
                var b = span[i];
                _type.DispatchDelta(t, b, e, kind, boxed);
                // DispatchDelta 내부에서 타입 미스면 바로 리턴, 히트했다면 touched
                // 간단화를 위해: 히트 여부를 반환받거나, 아래처럼 한 번 더 체크
                if (ImplementsBinds(b, t)) _touched.Add(b);
            }
        }
        
        private static bool ImplementsBinds(IViewBinder b, Type t)
        {
            foreach (var itf in b.GetType().GetInterfaces())
                if (itf.IsGenericType && itf.GetGenericTypeDefinition()==typeof(IBinds<>) && itf.GetGenericArguments()[0]==t)
                    return true;
            return false;
        }
        
        // --- 코어에서 들어오는 델타 ---
        public void DispatchAdded<T>(Entity e, in T v) where T : struct => _delta[(e, typeof(T))] = ((byte)ComponentDeltaKind.Added, v);
        public void DispatchChanged<T>(Entity e, in T v) where T : struct => _delta[(e, typeof(T))] = ((byte)ComponentDeltaKind.Changed, v);
        public void DispatchRemoved<T>(Entity e) where T : struct => _delta[(e, typeof(T))] = ((byte)ComponentDeltaKind.Removed, null)!;
        public void DispatchEntityDestroyed(Entity e)
        {
            if (_sets.Remove(e, out var set))
            {
                var span = set.EnumerateSorted();
                for (int i = 0; i < span.Length; i++) span[i].Unbind();
            }
            // 해당 엔티티의 델타 제거
            var toRemove = new List<(Entity, Type)>();
            foreach (var k in _delta.Keys) if (k.Item1.Equals(e)) toRemove.Add(k);
            foreach (var k in toRemove) _delta.Remove(k);
        }

        // --- 프레임 말 ---
        public void RunApply()
        {
            // 1) 델타 분배
            foreach (var kv in _delta)
            {
                var (e, t) = kv.Key;
                if (!_sets.TryGetValue(e, out var set)) continue;
                var (kind, boxed) = kv.Value;
                DispatchToBinders(t, e, kind, boxed, set);
            }
            _delta.Clear();

            // 2) Apply 대상 집합 만들기: touched ∪ scheduled ∪ AlwaysApply
            foreach (var set in _sets.Values)
            {
                var span = set.EnumerateSorted();
                for (int i = 0; i < span.Length; i++)
                {
                    var b = span[i];
                    if (b is IAlwaysApply || _touched.Contains(b) || _scheduled.Contains(b))
                        b.Apply();
                }
            }

            // 3) 클린업 & 다음 프레임 예약 이월
            _touched.Clear();
            _scheduled.Clear();
        }
    }
}