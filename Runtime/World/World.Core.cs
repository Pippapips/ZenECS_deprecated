#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;
using ZenECS.Core.Internal;
using ZenECS.Core.Serialization;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        private readonly BitSet alive = new BitSet(256);
        private int nextId = 1;
        private readonly Stack<int> freeIds = new(128);

        private readonly Dictionary<Type, IComponentPool> pools = new(256);

        // Filter 캐싱 (풀/버킷 키 기반)
        private readonly ConcurrentDictionary<FilterKey, ResolvedFilter> filterCache = new();

        private PostLoadMigrationRunner _migrationRunner;
        
        public World(IEnumerable<IPostLoadMigration>? migrations = null)
        {
            _migrationRunner = new PostLoadMigrationRunner(this, migrations);
        }

        internal void RunMigrations()
        {
            _migrationRunner?.Run();
        }

        // Scheduling
        public interface IJob
        {
            void Execute(World w);
        }

        private readonly ConcurrentQueue<IJob> jobQueue = new();

        // Timing
        public int Tick { get; private set; }
        public float DeltaTime { get; private set; }

        public void Advance(float dt)
        {
            DeltaTime = dt;
            Tick++;
            // 선택: 프레임 경계에서 예약된 잡 처리
            RunScheduledJobs();
        }

        // ---------- Scheduling ----------
        public void Schedule(IJob job)
        {
            if (job != null) jobQueue.Enqueue(job);
        }

        public int RunScheduledJobs()
        {
            int n = 0;
            while (jobQueue.TryDequeue(out var j))
            {
                j.Execute(this);
                n++;
            }

            return n;
        }

        // ---------- Entities ----------
        public Entity CreateEntity(int? fixedId = null)
        {
            int id;
            if (fixedId.HasValue)
            {
                id = fixedId.Value;
                EnsureEntityCapacity(id);
                alive.Set(id, true);
            }
            else if (freeIds.Count > 0)
            {
                id = freeIds.Pop();
                alive.Set(id, true);
            }
            else
            {
                id = nextId++;
                alive.Set(id, true);
            }

            var e = new Entity(id);
            EntityEvents.RaiseCreated(this, e);
            return e;
        }

        public void DestroyEntity(Entity e)
        {
            if (!IsAlive(e)) return;
            EntityEvents.RaiseDestroyRequested(this, e);
            foreach (var kv in pools) kv.Value.Remove(e.Id);
            alive.Set(e.Id, false);
            freeIds.Push(e.Id);
            EntityEvents.RaiseDestroyed(this, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity e) => alive.Get(e.Id);
        public int AliveCount => GetAllEntities().Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureEntityCapacity(int id)
        {
            if (!alive.Get(id)) alive.Set(id, false);
        }

        // ---------- Components ----------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            return pool != null && pool.Has(e.Id);
        }

        public bool TryGet<T>(Entity e, out T value) where T : struct
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
        internal void AddInternal<T>(Entity e, in T value) where T : struct
        {
#if !ZENECS_STRICT
            if (!IsAlive(e)) throw new InvalidOperationException($"Add<{typeof(T).Name}>: Entity {e.Id} dead.");
#endif
            var pool = GetPool<T>();
            pool.EnsureCapacity(e.Id);
            ref var r = ref ((ComponentPool<T>)pool).Ref(e.Id);
            r = value;
        }
        
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal bool RemoveInternal<T>(Entity e) where T : struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null) return false;
            var had = pool.Has(e.Id);
            pool.Remove(e.Id);
            return had;
        }
        
        // 외부에서 ref-쓰기 경로를 숨김. Systems는 같은 asmdef 또는 InternalsVisibleTo로 접근.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal ref T RefInternal<T>(Entity e) where T:struct
        {
            var pool = (ComponentPool<T>)GetPool<T>();
            return ref pool.Ref(e.Id);
        }
        
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal ref T RefExistingInternal<T>(Entity e) where T:struct
        {
            var pool = TryGetPoolInternal<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }
        public T Read<T>(Entity e) where T:struct
        {
            if (!TryGet(e, out T v)) throw new InvalidOperationException($"Read<{typeof(T).Name}> missing on {e.Id}");
            return v;
        }
        
        // ---------- Pools ----------
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
                pool.EnsureInitialized();

                pools.Add(t, pool);
            }

            return pool;
        }

        // ---------- Tooling / Save ----------
        public List<Entity> GetAllEntities()
        {
            var list = new List<Entity>(nextId);
            for (int id = 1; id < nextId; id++)
                if (alive.Get(id))
                    list.Add(new Entity(id));
            return list;
        }

        public IEnumerable<(Type type, object boxed)> GetAllComponents(Entity e)
        {
            foreach (var kv in pools)
                if (kv.Value.Has(e.Id))
                    yield return (kv.Key, kv.Value.GetBoxed(e.Id));
        }

        public void AddBoxed(Entity e, Type t, object boxed) => GetOrCreatePoolByType(t).SetBoxed(e.Id, boxed);
        public object GetBoxed(Entity e, Type t) => GetOrCreatePoolByType(t).GetBoxed(e.Id);
        public bool TryGetBoxed(Entity e, Type t, out object? boxed)
        {
            var pool = GetOrCreatePoolByType(t);
            var obj  = pool.GetBoxed(e.Id);
            boxed = obj;
            return obj != null;
        }
    }
}

// Usages
// // 1) 필터 조합
// var f = World.Filter.New
//     .With<Owner>()
//     .Without<DeadTag>()
//     .WithAny(typeof(Burning), typeof(Poisoned))   // 둘 중 하나 이상이면 OK
//     .WithoutAny(typeof(Shielded), typeof(Invuln)) // 둘 중 하나라도 있으면 제외
//     .Build();
//
// foreach (var e in world.Query<Position, Velocity>(f))
// {
//     ref var p = ref world.RefExisting<Position>(e);
//     var  v    =  world.RefExisting<Velocity>(e);
//     p.Value += v.Value * world.DeltaTime;
// }
//
// // 2) Span 일괄
// Span<Entity> tmp = stackalloc Entity[2048];
// int n = world.QueryToSpan<Health, Damage, Owner, Team>(tmp, f);   // T4 사용
// world.Process<Health>(tmp[..n], (ref Health h) => { h.Value = System.Math.Max(0, h.Value - 5); });
//
// // 3) 멀티스레드 커맨드 버퍼 + 스케줄
// var cb = world.BeginWrite();
// cb.Add(e, new Damage { Amount = 10 });
// cb.Remove<Stunned>(e);
// world.Schedule(cb);     // 프레임 경계에서 world.RunScheduledJobs() 시점에 적용
// // 또는 world.EndWrite(cb); 로 즉시 적용
//
// // 4) 커스텀 잡 스케줄
// world.Schedule(new HealJob(ents, amount: 3));
//
// struct HealJob : World.IJob
// {
//     private readonly Entity[] ents; private readonly int amount;
//     public HealJob(Entity[] ents, int amount){ this.ents = ents; this.amount = amount; }
//     public void Execute(World w)
//     {
//         for (int i=0;i<ents.Length;i++)
//             if (w.Has<Health>(ents[i]))
//             {
//                 ref var h = ref w.RefExisting<Health>(ents[i]);
//                 h.Value += amount;
//             }
//     }
// }