using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if ZENECS_TRACE
using ZenECS.Core.Diagnostics;
#endif

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// 시스템 파이프라인 구성/실행기.
    /// - 그룹 정렬(Initialization→Simulation→Presentation)
    /// - 선언적 순서(OrderBefore/OrderAfter)
    /// - 라이프사이클 호출(Init/Run/Fixed/Late/Cleanup/Dispose)
    /// </summary>
    public sealed class SystemRunner
    {
        private readonly World world;

        private readonly List<IInitSystem> init = new List<IInitSystem>(32);
        private readonly List<IRunSystem> update = new List<IRunSystem>(128);
        private readonly List<IFixedRunSystem> fixedUpdate = new List<IFixedRunSystem>(32);
        private readonly List<ILateRunSystem> late = new List<ILateRunSystem>(64);
        private readonly List<ICleanupSystem> cleanup = new List<ICleanupSystem>(32);
        private readonly List<IDisposeSystem> dispose = new List<IDisposeSystem>(32);

#if ZENECS_TRACE
        [InjectOptional] private EcsTraceCenter _traceCenter;
#endif
        
        public SystemRunner(World world, IEnumerable<ISystem>? systems = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            BuildPipelines(systems ?? []);
        }

        private void BuildPipelines(IEnumerable<ISystem> systems)
        {
            var list = systems.ToList();
            ZenECS.Core.Infrastructure.EcsRuntimeDirectory.Attach(world, list);
            if (list.Count == 0) return;

            list.Sort(CompareByAttributes);

            foreach (var s in list)
            {
                if (s is IInitSystem i) init.Add(i);
                if (s is IRunSystem r) update.Add(r);
                if (s is IFixedRunSystem f) fixedUpdate.Add(f);
                if (s is ILateRunSystem l) late.Add(l);
                if (s is ICleanupSystem c) cleanup.Add(c);
                if (s is IDisposeSystem d) dispose.Add(d);
            }
        }

        // 그룹 우선순위 → 선언적 before/after
        private static int CompareByAttributes(ISystem a, ISystem b)
        {
            var ta = a.GetType();
            var tb = b.GetType();

            int GroupRank(Type t)
            {
                if (t == typeof(InitializationGroup)) return 0;
                if (t == typeof(SimulationGroup)) return 1;
                if (t == typeof(PresentationGroup)) return 2;
                return 1; // 기본은 Simulation과 동일 취급
            }

            var ga = ta.GetCustomAttribute<UpdateGroupAttribute>()?.GroupType;
            var gb = tb.GetCustomAttribute<UpdateGroupAttribute>()?.GroupType;
            int gcmp = GroupRank(ga) - GroupRank(gb);
            if (gcmp != 0) return gcmp;

            bool ABeforeB() =>
                ta.GetCustomAttributes<OrderBeforeAttribute>().Any(x => x.Target == tb) ||
                tb.GetCustomAttributes<OrderAfterAttribute>().Any(x => x.Target == ta);

            bool AAfterB() =>
                ta.GetCustomAttributes<OrderAfterAttribute>().Any(x => x.Target == tb) ||
                tb.GetCustomAttributes<OrderBeforeAttribute>().Any(x => x.Target == ta);

            if (ABeforeB() && !AAfterB()) return -1;
            if (AAfterB() && !ABeforeB()) return 1;
            return 0;
        }

        // ===== 라이프사이클 =====
        public void Init()
        {
            foreach (var s in init) s.Init(world);
        }

        /// <summary>프레임 업데이트(Advance→Run→Cleanup)</summary>
        public void Update(float deltaTime)
        {
            world.Advance(deltaTime); // Tick/DeltaTime 갱신 + 스케줄드 잡 처리
            for (int i = 0; i < update.Count; i++)
            {
#if ZENECS_TRACE
                if (_traceCenter != null)
                {
                    var name = update[i].GetType().Name;
                    using var scope = _traceCenter.SystemScope(name);
                    try
                    {
                        update[i].Run(world);
                    }
                    catch
                    {
                        ((EcsTraceCenter.Scope)scope).MarkException();
                        throw;
                    }
                }
                else
                {
                    update[i].Run(world);
                }
#else
                update[i].Run(world);
#endif
            }

            for (int i = 0; i < cleanup.Count; i++) cleanup[i].Cleanup(world);
        }

        /// <summary>고정 틱(물리 등가)</summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            for (int i = 0; i < fixedUpdate.Count; i++)
            {
#if ZENECS_TRACE
                if (_traceCenter != null)
                {
                    var name = fixedUpdate[i].GetType().Name;
                    using var scope = _traceCenter.SystemScope(name);
                    try
                    {
                        fixedUpdate[i].FixedRun(world, fixedDeltaTime);
                    }
                    catch
                    {
                        ((EcsTraceCenter.Scope)scope).MarkException();
                        throw;
                    }
                }
                else
                {
                    fixedUpdate[i].FixedRun(world, fixedDeltaTime);
                }
#else
                fixedUpdate[i].FixedRun(world, fixedDeltaTime);
#endif
            }
        }

        /// <summary>표현 단계(보통 Data→View 동기화)</summary>
        public void LateUpdate()
        {
            for (int i = 0; i < late.Count; i++)
            {
#if ZENECS_TRACE
                if (_traceCenter != null)
                {
                    var name = late[i].GetType().Name;
                    using var scope = _traceCenter.SystemScope(name);
                    try
                    {
                        late[i].LateRun(world);
                    }
                    catch
                    {
                        ((EcsTraceCenter.Scope)scope).MarkException();
                        throw;
                    }
                }
                else
                {
                    late[i].LateRun(world);
                }
#else
                late[i].LateRun(world);
#endif
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < dispose.Count; i++) dispose[i].Dispose(world);
            ZenECS.Core.Infrastructure.EcsRuntimeDirectory.Detach();
        }
    }
}