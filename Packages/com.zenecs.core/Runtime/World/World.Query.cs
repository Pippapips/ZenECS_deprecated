#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // 씨드 선택(가장 작은 풀)
        private static IComponentPool? Seed(params IComponentPool?[] arr)
        {
            IComponentPool? best = null; var min = int.MaxValue; var hasAny = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var p = arr[i];
                if (p == null) continue;
                hasAny = true;
                if (p.Count < min) { min = p.Count; best = p; }
            }
            return hasAny ? best : null;
        }

        // Public Query 팩토리 (필터 선택)
        public QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct => new(this, f);
        public QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct => new(this, f);
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct where T8 : struct => new(this, f);

        // ========== Enumerables ==========
        public struct QueryEnumerable<T1> where T1 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = a;
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2>
            where T1 : struct where T2 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3>
            where T1 : struct where T2 : struct where T3 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) && (c == null || c.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3, T4>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c, d; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>(); d = w.TryGetPoolInternal<T4>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c, d);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) &&
                            (c == null || c.Has(id)) && (d == null || d.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3, T4, T5>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c, d, e; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>();
                    d = w.TryGetPoolInternal<T4>(); e = w.TryGetPoolInternal<T5>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c, d, e);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) &&
                            (c == null || c.Has(id)) && (d == null || d.Has(id)) &&
                            (e == null || e.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c, d, e, f6; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>();
                    d = w.TryGetPoolInternal<T4>(); e = w.TryGetPoolInternal<T5>(); f6 = w.TryGetPoolInternal<T6>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c, d, e, f6);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) &&
                            (c == null || c.Has(id)) && (d == null || d.Has(id)) &&
                            (e == null || e.Has(id)) && (f6 == null || f6.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c, d, e, f6, g; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>();
                    d = w.TryGetPoolInternal<T4>(); e = w.TryGetPoolInternal<T5>(); f6 = w.TryGetPoolInternal<T6>(); g = w.TryGetPoolInternal<T7>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c, d, e, f6, g);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) &&
                            (c == null || c.Has(id)) && (d == null || d.Has(id)) &&
                            (e == null || e.Has(id)) && (f6 == null || f6.Has(id)) &&
                            (g == null || g.Has(id)) && MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
        {
            private readonly World w; private readonly Filter f;
            internal QueryEnumerable(World w, Filter f) { this.w = w; this.f = f; }
            public Enumerator GetEnumerator() => new(w, f);

            public struct Enumerator
            {
                private readonly World w;
                private readonly IComponentPool? a, b, c, d, e, f6, g, h; private readonly ResolvedFilter rf;
                private IEnumerator<(int id, object boxed)> it; private Entity cur;

                public Enumerator(World w, Filter f)
                {
                    this.w = w;
                    a = w.TryGetPoolInternal<T1>(); b = w.TryGetPoolInternal<T2>(); c = w.TryGetPoolInternal<T3>(); d = w.TryGetPoolInternal<T4>();
                    e = w.TryGetPoolInternal<T5>(); f6 = w.TryGetPoolInternal<T6>(); g = w.TryGetPoolInternal<T7>(); h = w.TryGetPoolInternal<T8>(); rf = w.ResolveFilter(f);
                    IComponentPool? seed = Seed(a, b, c, d, e, f6, g, h);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    cur = default;
                }

                public Entity Current => cur;

                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((a == null || a.Has(id)) && (b == null || b.Has(id)) &&
                            (c == null || c.Has(id)) && (d == null || d.Has(id)) &&
                            (e == null || e.Has(id)) && (f6 == null || f6.Has(id)) &&
                            (g == null || g.Has(id)) && (h == null || h.Has(id)) &&
                            MeetsFilter(id, rf))
                        { cur = new Entity(id, w.generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        private static IEnumerable<(int, object)> Empty() { yield break; }
    }
}
