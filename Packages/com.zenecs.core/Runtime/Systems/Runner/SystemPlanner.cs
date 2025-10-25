// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core
// File: SystemPlanner.cs
// Purpose: Defines deterministic system ordering with grouping, validation, and
//          topological sorting. Provides a lifecycle plan for initialization and
//          shutdown ordering.
// Key concepts:
//   • Builds order edges only within the same group (cross-group constraints ignored).
//   • Deterministic order via Kahn topological sort with lexical tie-break by type name.
//   • Throws when cycles are detected during initialization.
//   • Produces a Plan containing ordered sequences per phase and lifecycle views.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Provides deterministic planning and ordering for ECS systems.
    /// Groups systems by execution phase (FrameSetup, Simulation, Presentation),
    /// performs topological sorting within each group, and validates phase markers.
    /// </summary>
    public static class SystemPlanner
    {
        /// <summary>
        /// Represents an immutable result of system planning,
        /// including execution order and lifecycle views.
        /// </summary>
        public sealed class Plan
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Plan"/> class.
            /// </summary>
            /// <param name="frameSetup">Ordered systems for FrameSetup phase.</param>
            /// <param name="simulation">Ordered systems for Simulation phase.</param>
            /// <param name="presentation">Ordered systems for Presentation phase.</param>
            public Plan(IReadOnlyList<ISystem>? frameSetup,
                        IReadOnlyList<ISystem>? simulation,
                        IReadOnlyList<ISystem>? presentation)
            {
                FrameSetup   = frameSetup   ?? Array.Empty<ISystem>();
                Simulation   = simulation   ?? Array.Empty<ISystem>();
                Presentation = presentation ?? Array.Empty<ISystem>();
            }

            /// <summary>
            /// Ordered list of systems belonging to the FrameSetup phase.
            /// </summary>
            public IReadOnlyList<ISystem> FrameSetup { get; }

            /// <summary>
            /// Ordered list of systems belonging to the Simulation phase.
            /// </summary>
            public IReadOnlyList<ISystem> Simulation { get; }

            /// <summary>
            /// Ordered list of systems belonging to the Presentation phase.
            /// </summary>
            public IReadOnlyList<ISystem> Presentation { get; }

            /// <summary>
            /// Combined forward execution order across all groups.
            /// </summary>
            public IEnumerable<ISystem> AllInExecutionOrder =>
                FrameSetup.Concat(Simulation).Concat(Presentation);

            /// <summary>
            /// Ordered view of systems that implement <see cref="ISystemLifecycle"/> for initialization.
            /// Follows forward execution order.
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleInitializeOrder =>
                AllInExecutionOrder.OfType<ISystemLifecycle>();

            /// <summary>
            /// Ordered view of systems that implement <see cref="ISystemLifecycle"/> for shutdown.
            /// Follows reverse execution order.
            /// </summary>
            public IEnumerable<ISystemLifecycle> LifecycleShutdownOrder =>
                AllInExecutionOrder.Reverse().OfType<ISystemLifecycle>();
        }

        /// <summary>
        /// Builds a deterministic plan by analyzing, grouping, and sorting the given systems.
        /// </summary>
        /// <param name="systems">Collection of system instances to be ordered.</param>
        /// <param name="warn">Optional callback invoked for non-critical warnings.</param>
        /// <returns>A new <see cref="Plan"/> instance containing ordered systems.</returns>
        /// <exception cref="InvalidOperationException">Thrown when phase or group validation fails,
        /// or a cyclic dependency is detected within a group.</exception>
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

                // Validate markers and attributes
                ValidatePhaseMarkers(t);

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
        /// Determines which <see cref="SystemGroup"/> a given system type belongs to.
        /// Priority: explicit attribute &gt; marker interface &gt; default (Simulation).
        /// </summary>
        /// <param name="t">System type to resolve.</param>
        /// <returns>The resolved group enumeration value.</returns>
        private static SystemGroup ResolveGroup(Type t)
        {
            if (t.IsDefined(typeof(FrameSetupGroupAttribute), false))   return SystemGroup.FrameSetup;
            if (t.IsDefined(typeof(PresentationGroupAttribute), false)) return SystemGroup.Presentation;
            if (t.IsDefined(typeof(SimulationGroupAttribute), false))   return SystemGroup.Simulation;

            if (typeof(IPresentationSystem).IsAssignableFrom(t)) return SystemGroup.Presentation;
            if (typeof(IFrameSetupSystem).IsAssignableFrom(t))   return SystemGroup.FrameSetup;
            if (typeof(IFixedRunSystem).IsAssignableFrom(t))     return SystemGroup.Simulation;
            if (typeof(IVariableRunSystem).IsAssignableFrom(t))  return SystemGroup.Simulation;

            return SystemGroup.Simulation;
        }

        /// <summary>
        /// Validates phase markers and group attributes on a system type.
        /// Ensures that no multiple phase markers or conflicting attributes exist.
        /// </summary>
        /// <param name="t">System type to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when conflicting markers or attributes are found.</exception>
        private static void ValidatePhaseMarkers(Type t)
        {
            int phaseCount =
                (typeof(IFixedRunSystem).IsAssignableFrom(t)     ? 1 : 0) +
                (typeof(IVariableRunSystem).IsAssignableFrom(t)  ? 1 : 0) +
                (typeof(IPresentationSystem).IsAssignableFrom(t) ? 1 : 0);
            if (phaseCount > 1)
                throw new InvalidOperationException($"{t.Name} implements multiple phase markers.");

            int groupAttrCount =
                (t.IsDefined(typeof(FrameSetupGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(SimulationGroupAttribute), false)   ? 1 : 0) +
                (t.IsDefined(typeof(PresentationGroupAttribute), false) ? 1 : 0);
            if (groupAttrCount > 1)
                throw new InvalidOperationException($"{t.Name} has multiple group attributes.");

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
        /// Performs topological sorting for all systems in a single group.
        /// Uses Kahn’s algorithm with deterministic lexical tie-break.
        /// </summary>
        /// <param name="list">List of system nodes and their ordering constraints.</param>
        /// <param name="warn">Optional callback invoked for ignored cross-group references.</param>
        /// <returns>Sorted list of systems for execution within that group.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a cyclic dependency is detected.</exception>
        /// <remarks>
        /// <para>Only constraints within the same group are honored.</para>
        /// <para>Cross-group Before/After references are ignored but reported through <paramref name="warn"/>.</para>
        /// <para>Tie-breaker is the type’s full name to ensure deterministic order.</para>
        /// </remarks>
        private static List<ISystem> TopoSortWithinGroup(
            List<(Type type, ISystem inst, HashSet<Type> before, HashSet<Type> after)> list,
            Action<string>? warn)
        {
            var nodes = list.ToDictionary(x => x.type, x => x.inst);
            var edges = new Dictionary<Type, HashSet<Type>>();
            var indeg = new Dictionary<Type, int>();

            void ensure(Type t)
            {
                edges.TryAdd(t, new HashSet<Type>());
                indeg.TryAdd(t, 0);
            }

            foreach ((Type type, ISystem _, HashSet<Type> before, HashSet<Type> after) in list)
            {
                ensure(type);

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

            if (result.Count != nodes.Count)
                throw new InvalidOperationException("Detected a cyclic dependency among systems within the same group.");

            return result;
        }
    }
}
