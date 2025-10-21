/*
    ZenECS.Core
    시스템 정렬기 (그룹 + 토폴로지 정렬 + 페이즈 검증 + 라이프사이클 지원)
    - 동일 그룹 내에서만 Before/After 간선을 만듭니다(교차-그룹 제약은 경고 후 무시).
    - Kahn 토폴로지 정렬 + 타입 이름 사전식 tie-break로 결정적 순서 보장.
    - 사이클이면 예외 발생(초기화 시 바로 잡도록).
    - Plan에 Initialize/Shutdown 호출 순서를 위한 뷰를 제공합니다.
    MIT | © 2025 Pippapips Limited
*/
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZenECS.Core.Systems
{
    internal static class SystemPlanner
    {
        internal sealed class Plan
        {
            public Plan(IReadOnlyList<ISystem>? frameSetup,
                IReadOnlyList<ISystem>? simulation,
                IReadOnlyList<ISystem>? presentation)
            {
                FrameSetup   = frameSetup   ?? Array.Empty<ISystem>();
                Simulation   = simulation   ?? Array.Empty<ISystem>();
                Presentation = presentation ?? Array.Empty<ISystem>();
            }

            // 실행 그룹별 정렬 결과
            public IReadOnlyList<ISystem> FrameSetup { get; }
            public IReadOnlyList<ISystem> Simulation { get; }
            public IReadOnlyList<ISystem> Presentation { get; }

            // 편의 뷰: 전체 실행 순서(정방향)
            public IEnumerable<ISystem> AllInExecutionOrder =>
                FrameSetup.Concat(Simulation).Concat(Presentation);

            // 라이프사이클: 시작은 정방향, 종료는 역순
            public IEnumerable<ISystemLifecycle> LifecycleInitializeOrder =>
                AllInExecutionOrder.OfType<ISystemLifecycle>();

            public IEnumerable<ISystemLifecycle> LifecycleShutdownOrder =>
                AllInExecutionOrder.Reverse().OfType<ISystemLifecycle>();
        }

        public static Plan? Build(IEnumerable<ISystem>? systems, Action<string>? warn = null)
        {
            if (systems == null) return null;

            var buckets = new Dictionary<SystemGroup, List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)>>()
            {
                [SystemGroup.FrameSetup]   = new(),
                [SystemGroup.Simulation]   = new(),
                [SystemGroup.Presentation] = new()
            };

            // 1) 분류 + 제약 수집 (+ 페이즈/그룹 검증)
            foreach (ISystem s in systems)
            {
                Type t = s.GetType();

                // ── 추가: 페이즈/그룹 검증 ───────────────────────────────────────────
                ValidatePhaseMarkers(t);
                // ───────────────────────────────────────────────────────────────────

                SystemGroup group = ResolveGroup(t);
                var before = t.GetCustomAttributes<OrderBeforeAttribute>().Select(a => a.Target).ToHashSet();
                var after  = t.GetCustomAttributes<OrderAfterAttribute>().Select(a => a.Target).ToHashSet();
                buckets[group].Add((t, s, before, after));
            }

            // 2) 각 그룹별 정렬
            List<ISystem> setup = TopoSortWithinGroup(buckets[SystemGroup.FrameSetup], warn);
            List<ISystem> simu  = TopoSortWithinGroup(buckets[SystemGroup.Simulation], warn);
            List<ISystem> pres  = TopoSortWithinGroup(buckets[SystemGroup.Presentation], warn);

            return new Plan(setup, simu, pres);
        }

        private static SystemGroup ResolveGroup(Type t)
        {
            // 1) 속성 우선
            if (t.IsDefined(typeof(FrameSetupGroupAttribute), false))   return SystemGroup.FrameSetup;
            if (t.IsDefined(typeof(PresentationGroupAttribute), false)) return SystemGroup.Presentation;
            if (t.IsDefined(typeof(SimulationGroupAttribute), false))   return SystemGroup.Simulation;

            // 2) 마커 인터페이스로 유추
            if (typeof(IPresentationSystem).IsAssignableFrom(t)) return SystemGroup.Presentation;
            if (typeof(IFrameSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFixedRunSystem).IsAssignableFrom(t))     return SystemGroup.Simulation;
            if (typeof(IVariableRunSystem).IsAssignableFrom(t))  return SystemGroup.Simulation;

            // 3) 기본값
            return SystemGroup.Simulation;
        }

        /// <summary>
        /// 한 타입이 복수의 페이즈 마커를 동시에 구현하거나,
        /// 복수의 그룹 속성을 혼용하면 예외를 던져 초기화 단계에서 즉시 실패시킵니다.
        /// </summary>
        private static void ValidatePhaseMarkers(Type t)
        {
            // 하나의 페이즈 마커만 허용
            int phaseCount =
                (typeof(IFixedRunSystem).IsAssignableFrom(t)     ? 1 : 0) +
                (typeof(IVariableRunSystem).IsAssignableFrom(t)  ? 1 : 0) +
                (typeof(IPresentationSystem).IsAssignableFrom(t) ? 1 : 0);
            if (phaseCount > 1)
                throw new InvalidOperationException($"{t.Name} implements multiple phase markers.");

            // 하나의 그룹 속성만 허용
            int groupAttrCount =
                (t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(SimulationGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(PresentationGroupAttribute), false) ? 1 : 0);
            if (groupAttrCount > 1)
                throw new InvalidOperationException($"{t.Name} has multiple group attributes.");

            // 속성 vs 유추 모순 감지
            if (groupAttrCount == 1)
            {
                var inferred = // 위 ResolveGroup의 “인터페이스 유추 부분”만 돌려서 계산
                    typeof(IPresentationSystem).IsAssignableFrom(t) ? SystemGroup.Presentation :
                        typeof(IFrameSetupSystem).IsAssignableFrom(t)   ? SystemGroup.FrameSetup :
                            (typeof(IFixedRunSystem).IsAssignableFrom(t) || typeof(IVariableRunSystem).IsAssignableFrom(t))
                                ? SystemGroup.Simulation : (SystemGroup?)null;

                var attrGroup =
                    t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? SystemGroup.FrameSetup :
                    t.IsDefined(typeof(PresentationGroupAttribute), false) ? SystemGroup.Presentation :
                    SystemGroup.Simulation;

                if (inferred.HasValue && inferred.Value != attrGroup)
                    throw new InvalidOperationException($"{t.Name} group attribute conflicts with its marker interface.");
            }
        }

        private static List<ISystem> TopoSortWithinGroup(
            List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)> list,
            Action<string>? warn)
        {
            // 노드 집합
            var nodes = list.ToDictionary(x => x.type, x => x.inst);

            // 동일 그룹 내 대상만 간선으로 해석
            var edges = new Dictionary<Type, HashSet<Type>>(); // from -> to (from이 to보다 먼저)
            var indeg = new Dictionary<Type, int>();

            void ensure(Type t)
            {
                edges.TryAdd(t, new HashSet<Type>());
                indeg.TryAdd(t, 0);
            }

            foreach ((Type type, ISystem _, HashSet<Type> before, HashSet<Type> after) in list)
            {
                ensure(type);

                // OrderBefore: type -> target
                foreach (Type target in before)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderBefore] {type.Name} → {target.Name} 무시(동일 그룹 미포함)");
                        continue;
                    }
                    if (edges[type].Add(target))
                        indeg[target] = indeg.GetValueOrDefault(target) + 1;
                }

                // OrderAfter: target -> type
                foreach (Type target in after)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderAfter] {type.Name} ← {target.Name} 무시(동일 그룹 미포함)");
                        continue;
                    }
                    if (!edges.TryGetValue(target, out HashSet<Type>? set))
                    {
                        set = new HashSet<Type>();
                        edges[target] = set;
                        indeg.TryAdd(target, 0);
                    }
                    if (set.Add(type))
                        indeg[type] = indeg.GetValueOrDefault(type) + 1;
                }
            }

            // Kahn 알고리즘 + 이름순 tie-break
            var q = new SortedSet<Type>(
                indeg.Where(p => p.Value == 0).Select(p => p.Key),
                Comparer<Type>.Create((a, b) => string.CompareOrdinal(a.FullName, b.FullName))
            );

            var result = new List<ISystem>();
            while (q.Count > 0)
            {
                Type u = q.Min!;
                q.Remove(u);
                result.Add(nodes[u]);

                if (!edges.TryGetValue(u, out HashSet<Type>? tos))
                    continue;

                foreach (Type v in tos)
                {
                    indeg[v]--;
                    if (indeg[v] == 0) q.Add(v);
                }
            }

            return result.Count != nodes.Count
                ? throw new InvalidOperationException("시스템 순서 제약에 사이클이 있습니다(같은 그룹 내).")
                : result;
        }
    }
}
