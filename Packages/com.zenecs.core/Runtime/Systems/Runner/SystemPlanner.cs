// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core
// File: SystemPlanner.cs
// Purpose: System ordering (grouping + topological sort + phase validation + lifecycle view).
// Key concepts:
//   • Builds order edges only within the same group (cross-group constraints are ignored with warnings).
//   • Deterministic order via Kahn topological sort with lexical tie-break by type name.
//   • Throws if a cycle is detected (should be fixed during initialization).
//   • Provides a Plan that includes Initialize/Shutdown lifecycle order views.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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

            /// <summary>
            /// Sorted results per execution group.
            /// </summary>
            public IReadOnlyList<ISystem> FrameSetup { get; }
            public IReadOnlyList<ISystem> Simulation { get; }
            public IReadOnlyList<ISystem> Presentation { get; }

            /// <summary>
            /// Convenience view — full forward execution order.
            /// </summary>
            public IEnumerable<ISystem> AllInExecutionOrder =>
                FrameSetup.Concat(Simulation).Concat(Presentation);

            /// <summary>
            /// Lifecycle order: initialization follows forward order.
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleInitializeOrder =>
                AllInExecutionOrder.OfType<ISystemLifecycle>();

            /// <summary>
            /// Lifecycle order: shutdown follows reverse order.
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleShutdownOrder =>
                AllInExecutionOrder.Reverse().OfType<ISystemLifecycle>();
        }

        /// <summary>
        /// Builds a deterministic system plan with topological sorting per group.
        /// </summary>
        /// <param name="systems">System instances to include.</param>
        /// <param name="warn">Optional callback for warning messages.</param>
        /// <returns>A constructed <see cref="Plan"/>, or null if input is null.</returns>
        public static Plan? Build(IEnumerable<ISystem>? systems, Action<string>? warn = null)
        {
            if (systems == null) return null;

            var buckets = new Dictionary<SystemGroup, List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)>>()
            {
                [SystemGroup.FrameSetup]   = new(),
                [SystemGroup.Simulation]   = new(),
                [SystemGroup.Presentation] = new()
            };

            // 1) Classification + constraint collection (including phase/group validation)
            foreach (ISystem s in systems)
            {
                Type t = s.GetType();

                // ── Phase and group validation ───────────────────────────────
                ValidatePhaseMarkers(t);
                // ─────────────────────────────────────────────────────────────

                SystemGroup group = ResolveGroup(t);
                var before = t.GetCustomAttributes<OrderBeforeAttribute>().Select(a => a.Target).ToHashSet();
                var after  = t.GetCustomAttributes<OrderAfterAttribute>().Select(a => a.Target).ToHashSet();
                buckets[group].Add((t, s, before, after));
            }

            // 2) Sort each group independently
            List<ISystem> setup = TopoSortWithinGroup(buckets[SystemGroup.FrameSetup], warn);
            List<ISystem> simu  = TopoSortWithinGroup(buckets[SystemGroup.Simulation], warn);
            List<ISystem> pres  = TopoSortWithinGroup(buckets[SystemGroup.Presentation], warn);

            return new Plan(setup, simu, pres);
        }

        /// <summary>
        /// Resolves the group of a system type via attribute or marker interface inference.
        /// </summary>
        private static SystemGroup ResolveGroup(Type t)
        {
            // 1) Explicit attribute has highest priority
            if (t.IsDefined(typeof(FrameSetupGroupAttribute), false))   return SystemGroup.FrameSetup;
            if (t.IsDefined(typeof(PresentationGroupAttribute), false)) return SystemGroup.Presentation;
            if (t.IsDefined(typeof(SimulationGroupAttribute), false))   return SystemGroup.Simulation;

            // 2) Infer via marker interfaces
            if (typeof(IPresentationSystem).IsAssignableFrom(t)) return SystemGroup.Presentation;
            if (typeof(IFrameSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFixedRunSystem).IsAssignableFrom(t))     return SystemGroup.Simulation;
            if (typeof(IVariableRunSystem).IsAssignableFrom(t))  return SystemGroup.Simulation;

            // 3) Default fallback
            return SystemGroup.Simulation;
        }

        /// <summary>
        /// Validates that a system does not implement multiple phase markers or use conflicting group attributes.
        /// Throws immediately during initialization if invalid.
        /// </summary>
        private static void ValidatePhaseMarkers(Type t)
        {
            // Only one phase marker interface allowed
            int phaseCount =
                (typeof(IFixedRunSystem).IsAssignableFrom(t)     ? 1 : 0) +
                (typeof(IVariableRunSystem).IsAssignableFrom(t)  ? 1 : 0) +
                (typeof(IPresentationSystem).IsAssignableFrom(t) ? 1 : 0);
            if (phaseCount > 1)
                throw new InvalidOperationException($"{t.Name} implements multiple phase markers.");

            // Only one group attribute allowed
            int groupAttrCount =
                (t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(SimulationGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(PresentationGroupAttribute), false) ? 1 : 0);
            if (groupAttrCount > 1)
                throw new InvalidOperationException($"{t.Name} has multiple group attributes.");

            // Detect contradictions between attribute and inferred marker
            if (groupAttrCount == 1)
            {
                var inferred =
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

        /// <summary>
        /// Performs topological sorting within a single system group using Kahn’s algorithm.
        /// </summary>
        /// <remarks>
        /// - Only constraints within the same group are honored.<br/>
        /// - Cross-group Before/After references are ignored (with warnings if provided).<br/>
        /// - Tie-breaks by type full name to ensure deterministic order.
        /// </remarks>
        private static List<ISystem> TopoSortWithinGroup(
            List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)> list,
            Action<string>? warn)
        {
            // Node set
            var nodes = list.ToDictionary(x => x.type, x => x.inst);

            // Only interpret edges within the same group
            var edges = new Dictionary<Type, HashSet<Type>>(); // from -> to (from executes before to)
            var indeg = new Dictionary<Type, int>();

            void ensure(Type t)
            {
                edges.TryAdd(t, new HashSet<Type>());
                indeg.TryAdd(t, 0);
            }

            foreach ((Type type, ISystem _, HashSet<Type> before, HashSet<Type> after) in list)
            {
                ensure(type);

                // OrderBefore: type → target
                foreach (Type target in before)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderBefore] {type.Name} → {target.Name} ignored (not in same group)");
                        continue;
                    }
                    if (edges[type].Add(target))
                        indeg[target] = indeg.GetValueOrDefault(target) + 1;
                }

                // OrderAfter: target → type
                foreach (Type target in after)
                {
                    if (!nodes.ContainsKey(target))
                    {
                        warn?.Invoke($"[OrderAfter] {type.Name} ← {target.Name} ignored (not in same group)");
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

            // Kahn algorithm + deterministic tie-break
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

            // Detect cycle
            return result.Count != nodes.Count
                ? throw new InvalidOperationException("Detected a cyclic dependency among systems within the same group.")
                : result;
        }
    }
}
