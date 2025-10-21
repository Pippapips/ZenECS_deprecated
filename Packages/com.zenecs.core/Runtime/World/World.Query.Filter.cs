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
        // 필터 조합
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

        private void ResetQueryCaches()
        {
            filterCache?.Clear(); // 필터/마스크 캐시
        }

        // ---------- Filter Key / Cache ----------
        internal struct FilterKey : System.IEquatable<FilterKey>
        {
            public readonly ulong Hash;
            public FilterKey(ulong hash) { Hash = hash; }

            public bool Equals(FilterKey other) { return Hash == other.Hash; }
            public override bool Equals(object? obj) { return obj is FilterKey fk && fk.Hash == Hash; }
            public override int GetHashCode() { return Hash.GetHashCode(); }
        }

        internal sealed class ResolvedFilter
        {
            public IComponentPool[] withAll = Array.Empty<IComponentPool>();
            public IComponentPool[] withoutAll = Array.Empty<IComponentPool>();
            public IComponentPool[][] withAny = Array.Empty<IComponentPool[]>(); // buckets
            public IComponentPool[][] withoutAny = Array.Empty<IComponentPool[]>();
        }

        // Filter 캐싱 (풀/버킷 키 기반)
        private readonly ConcurrentDictionary<FilterKey, ResolvedFilter> filterCache = new();

        // ---------- Filter DSL ----------
        public readonly struct Filter
        {
            internal readonly Type[] withAll;
            internal readonly Type[] withoutAll;
            internal readonly Type[][] withAny;    // 그룹별 OR
            internal readonly Type[][] withoutAny; // 그룹별 OR

            internal Filter(Type[] wa, Type[] wo, Type[][] wan, Type[][] won)
            {
                withAll = wa;
                withoutAll = wo;
                withAny = wan;
                withoutAny = won;
            }

            public static Builder New => default;

            public readonly struct Builder
            {
                private readonly List<Type> wa;        // WithAll
                private readonly List<Type> wo;        // WithoutAll
                private readonly List<List<Type>> wan; // WithAny buckets
                private readonly List<List<Type>> won; // WithoutAny buckets

                public Builder With<T>() where T : struct => new(Append(wa, typeof(T)), wo, wan, won);
                public Builder Without<T>() where T : struct => new(wa, Append(wo, typeof(T)), wan, won);

                /// <summary>그룹 OR: 인자로 전달한 타입 중 하나라도 만족하면 통과</summary>
                public Builder WithAny(params Type[] types) => new(wa, wo, AppendBucket(wan, types), won);
                /// <summary>그룹 OR: 인자 중 하나라도 존재하면 탈락</summary>
                public Builder WithoutAny(params Type[] types) => new(wa, wo, wan, AppendBucket(won, types));

                public Filter Build()
                {
                    return new Filter(
                        wa?.ToArray() ?? Array.Empty<Type>(),
                        wo?.ToArray() ?? Array.Empty<Type>(),
                        ToJagged(wan),
                        ToJagged(won));
                }

                // ctors
                private Builder(List<Type> wa, List<Type> wo, List<List<Type>> wan, List<List<Type>> won)
                {
                    this.wa = wa;
                    this.wo = wo;
                    this.wan = wan;
                    this.won = won;
                }

                // helpers
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

        // 해시 키 생성(풀/버킷 키 기반) — 순서 독립
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
                    // 순서 독립 위해 정렬된 해시 누적
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

        // 필터 풀 배열로 해석 + 캐싱
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
                    pools.TryGetValue(types[i], out var p);
                    if (p == null) return null; // WithAll이지만 풀 없음 → 공집합
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
                        pools.TryGetValue(tset[j], out var p);
                        if (p == null) return null; // WithAll이지만 풀 없음 → 공집합
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
            } // 공집합

            filterCache[key] = rf;
            return rf;
        }

        // id가 필터 충족하는지 검사
        internal static bool MeetsFilter(int id, ResolvedFilter r)
        {
            // WithAll: 모두 있어야
            var wa = r.withAll;
            for (int i = 0; i < wa.Length; i++)
                if (!wa[i].Has(id))
                    return false;

            // WithoutAll: 하나라도 있으면 탈락
            var wo = r.withoutAll;
            for (int i = 0; i < wo.Length; i++)
                if (wo[i].Has(id))
                    return false;

            // WithAny: 각 버킷마다 "적어도 하나 존재"해야 통과
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

            // WithoutAny: 각 버킷에서 "하나라도 존재하면" 탈락
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
