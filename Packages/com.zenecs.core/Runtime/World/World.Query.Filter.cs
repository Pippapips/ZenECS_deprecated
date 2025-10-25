﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Query.Filter.cs
// Purpose: Composable filter definitions for queries (include/exclude component sets).
// Key concepts:
//   • WithAny / WithoutAny fluent API for logical OR groups.
//   • Used by Query to test entity membership efficiently.
//   • Cached per filter key for reuse.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /*
         Example:
         var f = World.Filter.New
             .With<Owner>()
             .Without<DeadTag>()
             .WithAny(typeof(Burning), typeof(Poisoned))   // Match if any of these exist
             .WithoutAny(typeof(Shielded), typeof(Invuln)) // Exclude if any of these exist
             .Build();

         foreach (var e in world.Query<Position, Velocity>(f))
         {
             ref var p = ref world.RefExisting<Position>(e);
             var  v    =  world.RefExisting<Velocity>(e);
             p.Value += v.Value * world.DeltaTime;
         }
        */

        /// <summary>
        /// Clears all cached query filters and their resolved component pool masks.
        /// </summary>
        /// <remarks>
        /// Call this when component pools are rebuilt or the world is reset in a way that invalidates cached lookups.
        /// </remarks>
        private void ResetQueryCaches()
        {
            filterCache?.Clear();
        }

        // ---------- Filter Key / Cache ----------

        /// <summary>
        /// Order-independent cache key for a composed filter.
        /// </summary>
        internal struct FilterKey : System.IEquatable<FilterKey>
        {
            /// <summary>Precomputed hash value representing the filter.</summary>
            public readonly ulong Hash;

            /// <summary>Creates a new <see cref="FilterKey"/> with the given <paramref name="hash"/>.</summary>
            public FilterKey(ulong hash) { Hash = hash; }

            /// <inheritdoc/>
            public bool Equals(FilterKey other) => Hash == other.Hash;
            /// <inheritdoc/>
            public override bool Equals(object? obj) => obj is FilterKey fk && fk.Hash == Hash;
            /// <inheritdoc/>
            public override int GetHashCode() => Hash.GetHashCode();
        }

        /// <summary>
        /// Resolved, pool-level representation of a filter used by the query engine.
        /// </summary>
        internal sealed class ResolvedFilter
        {
            /// <summary>Pools that must all be present on the entity.</summary>
            public IComponentPool[] withAll = Array.Empty<IComponentPool>();
            /// <summary>Pools that must all be absent from the entity.</summary>
            public IComponentPool[] withoutAll = Array.Empty<IComponentPool>();
            /// <summary>
            /// Buckets where at least one pool in each bucket must be present (logical OR per bucket, AND across buckets).
            /// </summary>
            public IComponentPool[][] withAny = Array.Empty<IComponentPool[]>();
            /// <summary>
            /// Buckets where any present pool in a bucket causes exclusion (logical OR per bucket, AND across buckets).
            /// </summary>
            public IComponentPool[][] withoutAny = Array.Empty<IComponentPool[]>();
        }

        /// <summary>
        /// Filter cache keyed by <see cref="FilterKey"/> (computed from included/excluded component types).
        /// </summary>
        private readonly ConcurrentDictionary<FilterKey, ResolvedFilter> filterCache = new();

        // ---------- Filter DSL ----------

        /// <summary>
        /// Immutable value describing a query filter with include/exclude constraints.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Builder"/> via <see cref="New"/> to compose filters fluently,
        /// then pass the result to <c>world.Query&lt;...>(filter)</c>.
        /// </remarks>
        public readonly struct Filter
        {
            internal readonly Type[] withAll;
            internal readonly Type[] withoutAll;
            internal readonly Type[][] withAny;
            internal readonly Type[][] withoutAny;

            internal Filter(Type[] wa, Type[] wo, Type[][] wan, Type[][] won)
            {
                withAll   = wa;
                withoutAll = wo;
                withAny   = wan;
                withoutAny = won;
            }

            /// <summary>
            /// Entry point for the fluent filter builder.
            /// </summary>
            public static Builder New => default;

            /// <summary>
            /// Fluent, immutable builder used to compose <see cref="Filter"/> instances.
            /// </summary>
            /// <remarks>
            /// Each method returns a new builder that includes the requested constraint; the original builder remains unchanged.
            /// </remarks>
            public readonly struct Builder
            {
                private readonly List<Type> wa;
                private readonly List<Type> wo;
                private readonly List<List<Type>> wan;
                private readonly List<List<Type>> won;

                /// <summary>
                /// Requires that entities include component <typeparamref name="T"/>.
                /// </summary>
                /// <typeparam name="T">Component value type.</typeparam>
                /// <returns>A new builder with the constraint added.</returns>
                public Builder With<T>() where T : struct => new(Append(wa, typeof(T)), wo, wan, won);

                /// <summary>
                /// Requires that entities exclude component <typeparamref name="T"/>.
                /// </summary>
                /// <typeparam name="T">Component value type.</typeparam>
                /// <returns>A new builder with the constraint added.</returns>
                public Builder Without<T>() where T : struct => new(wa, Append(wo, typeof(T)), wan, won);

                /// <summary>
                /// Adds a logical OR group: the entity passes if it contains <em>any one</em> of the specified types.
                /// </summary>
                /// <param name="types">One or more component types to OR together.</param>
                /// <returns>A new builder with the constraint added.</returns>
                public Builder WithAny(params Type[] types) => new(wa, wo, AppendBucket(wan, types), won);

                /// <summary>
                /// Adds a negative logical OR group: the entity fails if it contains <em>any one</em> of the specified types.
                /// </summary>
                /// <param name="types">One or more component types to OR together for exclusion.</param>
                /// <returns>A new builder with the constraint added.</returns>
                public Builder WithoutAny(params Type[] types) => new(wa, wo, wan, AppendBucket(won, types));

                /// <summary>
                /// Finalizes the builder into an immutable <see cref="Filter"/>.
                /// </summary>
                /// <returns>The composed filter.</returns>
                public Filter Build()
                {
                    return new Filter(
                        wa?.ToArray() ?? Array.Empty<Type>(),
                        wo?.ToArray() ?? Array.Empty<Type>(),
                        ToJagged(wan),
                        ToJagged(won));
                }

                private Builder(List<Type> wa, List<Type> wo, List<List<Type>> wan, List<List<Type>> won)
                {
                    this.wa = wa;
                    this.wo = wo;
                    this.wan = wan;
                    this.won = won;
                }

                private static List<Type> Append(List<Type> list, Type t)
                {
                    var l = list ?? new List<Type>(4);
                    l.Add(t);
                    return l;
                }
                private static List<List<Type>> AppendBucket(List<List<Type>> list, Type[] types)
                {
                    var l = list ?? new List<List<Type>>(2);
                    var b = new List<Type>(types.Length);
                    foreach (var t in types)
                        if (t != null)
                            b.Add(t);
                    if (b.Count > 0) l.Add(b);
                    return l;
                }
                private static Type[][] ToJagged(List<List<Type>> src)
                {
                    if (src == null || src.Count == 0) return Array.Empty<Type[]>();
                    var arr = new Type[src.Count][];
                    for (int i = 0; i < src.Count; i++) arr[i] = src[i].ToArray();
                    return arr;
                }
            }
        }

        /// <summary>
        /// Computes an order-independent key for the given filter, suitable for caching.
        /// </summary>
        /// <param name="f">The filter to hash.</param>
        /// <returns>A stable <see cref="FilterKey"/>.</returns>
        internal static FilterKey MakeKey(in Filter f)
        {
            unchecked
            {
                ulong h = 1469598103934665603ul; // FNV offset
                void Mix(Type t)
                {
                    h ^= (ulong)t.GetHashCode();
                    h *= 1099511628211ul;
                }
                void MixTypeSet(Type[] arr)
                {
                    if (arr == null) return;
                    foreach (var t in arr.OrderBy(x => x.FullName)) Mix(t);
                    h ^= 0x9E3779B185EBCA87ul;
                }
                void MixBuckets(Type[][] buckets)
                {
                    if (buckets == null) return;
                    foreach (var set in buckets)
                    {
                        MixTypeSet(set);
                        h ^= 0xC2B2AE3D27D4EB4Ful;
                    }
                }

                MixTypeSet(f.withAll);
                MixTypeSet(f.withoutAll);
                MixBuckets(f.withAny);
                MixBuckets(f.withoutAny);
                return new FilterKey(h);
            }
        }

        /// <summary>
        /// Resolves a high-level <see cref="Filter"/> to concrete component pool references and caches the result.
        /// </summary>
        /// <param name="f">The filter to resolve.</param>
        /// <returns>A <see cref="ResolvedFilter"/> ready for fast evaluation.</returns>
        /// <remarks>
        /// If a referenced component type does not have a registered pool, the corresponding section becomes empty,
        /// which may cause the filter to match no entities (for <c>With</c>/<c>WithAny</c>) or to skip checks.
        /// </remarks>
        internal ResolvedFilter ResolveFilter(in Filter f)
        {
            var key = MakeKey(f);
            if (filterCache.TryGetValue(key, out var cached)) return cached;

            IComponentPool[]? ToPools(Type[] types)
            {
                if (types == null || types.Length == 0) return Array.Empty<IComponentPool>();
                var arr = new IComponentPool[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    _pools.TryGetValue(types[i], out var p);
                    if (p == null) return null;
                    arr[i] = p;
                }
                return arr;
            }
            IComponentPool[][]? ToPoolBuckets(Type[][]? buckets)
            {
                if (buckets == null || buckets.Length == 0) return Array.Empty<IComponentPool[]>();
                var arr = new IComponentPool[buckets.Length][];
                for (int i = 0; i < buckets.Length; i++)
                {
                    var tset = buckets[i];
                    var ps = new IComponentPool[tset.Length];
                    for (int j = 0; j < tset.Length; j++)
                    {
                        _pools.TryGetValue(tset[j], out var p);
                        if (p == null) return null;
                        ps[j] = p;
                    }
                    arr[i] = ps;
                }
                return arr;
            }

            var rf = new ResolvedFilter
            {
                withAll = ToPools(f.withAll) ?? Array.Empty<IComponentPool>(),
                withoutAll = ToPools(f.withoutAll) ?? Array.Empty<IComponentPool>(),
                withAny = ToPoolBuckets(f.withAny) ?? Array.Empty<IComponentPool[]>(),
                withoutAny = ToPoolBuckets(f.withoutAny) ?? Array.Empty<IComponentPool[]>(),
            };
            if (rf.withAll == null)
            {
                rf.withAll = Array.Empty<IComponentPool>();
                rf.withAny = Array.Empty<IComponentPool[]>();
            }

            filterCache[key] = rf;
            return rf;
        }

        /// <summary>
        /// Tests whether the entity identified by <paramref name="id"/> satisfies the conditions of a resolved filter.
        /// </summary>
        /// <param name="id">Internal entity id (index into component pools).</param>
        /// <param name="r">Resolved filter to evaluate.</param>
        /// <returns><see langword="true"/> if the entity matches; otherwise <see langword="false"/>.</returns>
        internal static bool MeetsFilter(int id, ResolvedFilter r)
        {
            // WithAll: must contain all
            var wa = r.withAll;
            for (int i = 0; i < wa.Length; i++)
                if (!wa[i].Has(id))
                    return false;

            // WithoutAll: must not contain any
            var wo = r.withoutAll;
            for (int i = 0; i < wo.Length; i++)
                if (wo[i].Has(id))
                    return false;

            // WithAny: must contain at least one from each bucket
            var wan = r.withAny;
            for (int b = 0; b < wan.Length; b++)
            {
                var bucket = wan[b];
                bool any = false;
                for (int i = 0; i < bucket.Length; i++)
                    if (bucket[i] != null && bucket[i].Has(id))
                    {
                        any = true;
                        break;
                    }
                if (!any) return false;
            }

            // WithoutAny: fails if any from each bucket exist
            var won = r.withoutAny;
            for (int b = 0; b < won.Length; b++)
            {
                var bucket = won[b];
                for (int i = 0; i < bucket.Length; i++)
                    if (bucket[i] != null && bucket[i].Has(id))
                        return false;
            }

            return true;
        }
    }
}
