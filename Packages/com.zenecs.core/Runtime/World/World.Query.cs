﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Query.cs
// Purpose: Query builder and iterator for ref-based component enumeration.
// Key concepts:
//   • Seeds enumeration from the smallest pool for efficiency.
//   • Filter WithAny/WithoutAny to constrain sets.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using ZenECS.Core.Internal;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// Selects a seed pool — the smallest non-empty pool among the given ones.
        /// Choosing the smallest pool minimizes the initial candidate set for iteration.
        /// </summary>
        /// <param name="arr">Candidate component pools (some may be <see langword="null"/>).</param>
        /// <returns>
        /// The smallest non-empty pool to iterate first; <see langword="null"/> if all are <see langword="null"/>.
        /// </returns>
        private static IComponentPool? Seed(params IComponentPool?[] arr)
        {
            IComponentPool? best = null;
            var min = int.MaxValue;
            var hasAny = false;
            for (int i = 0; i < arr.Length; i++)
            {
                var p = arr[i];
                if (p == null) continue;
                hasAny = true;
                if (p.Count < min)
                {
                    min = p.Count;
                    best = p;
                }
            }
            return hasAny ? best : null;
        }

        // ==============================
        // Query factories for 1–8 types
        // ==============================

        /// <summary>
        /// Creates an enumerable that yields entities containing component <typeparamref name="T1"/>.
        /// </summary>
        /// <typeparam name="T1">Component value type.</typeparam>
        /// <param name="f">Optional <see cref="Filter"/> to constrain the result set.</param>
        /// <returns>Type-safe query enumerable.</returns>
        public QueryEnumerable<T1> Query<T1>(Filter f = default) where T1 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        /// <typeparam name="T1">First component value type.</typeparam>
        /// <typeparam name="T2">Second component value type.</typeparam>
        /// <param name="f">Optional <see cref="Filter"/> to constrain the result set.</param>
        /// <returns>Type-safe query enumerable.</returns>
        public QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct where T2 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>, <typeparamref name="T2"/>, and <typeparamref name="T3"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>…<typeparamref name="T4"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>…<typeparamref name="T5"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>…<typeparamref name="T6"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>…<typeparamref name="T7"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct => new(this, f);

        /// <summary>
        /// Creates an enumerable that yields entities containing components <typeparamref name="T1"/>…<typeparamref name="T8"/>.
        /// </summary>
        public QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct where T8 : struct => new(this, f);

        // ========== Enumerables ==========
        // Each struct below provides a type-safe query enumerator that yields entities matching all component constraints and filters.

        /// <summary>
        /// Enumerable over entities matching component <typeparamref name="T1"/> and an optional <see cref="Filter"/>.
        /// </summary>
        /// <typeparam name="T1">Component value type.</typeparam>
        public struct QueryEnumerable<T1> where T1 : struct
        {
            private readonly World _w;
            private readonly Filter _f;

            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }

            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>
            /// Value-type enumerator that streams matching entities without allocations.
            /// </summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a;
                private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> _it;
                private Entity _cur;

                /// <summary>
                /// Initializes the enumerator for a given world and filter.
                /// </summary>
                /// <param name="w">Target world.</param>
                /// <param name="f">Filter to apply.</param>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>();
                    _rf = w.ResolveFilter(f);
                    var seed = _a;
                    _it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>
                /// Advances to the next matching entity.
                /// </summary>
                /// <returns><see langword="true"/> if an entity is available; otherwise <see langword="false"/>.</returns>
                public bool MoveNext()
                {
                    while (_it.MoveNext())
                    {
                        int id = _it.Current.id;
                        if ((_a == null || _a.Has(id)) && MeetsFilter(id, _rf))
                        {
                            _cur = new Entity(id, _w._generation[id]);
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        // Similar pattern repeats for T1–T8 combinations.
        // Each Enumerator iterates over the smallest pool and filters by Has<T> and user-defined filters.

        /// <summary>
        /// Enumerable over entities matching components <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        public struct QueryEnumerable<T1, T2>
            where T1 : struct where T2 : struct
        {
            private readonly World _w;
            private readonly Filter _f;

            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }

            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with <typeparamref name="T1"/> and <typeparamref name="T2"/>.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b;
                private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it;
                private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>();
                    _b = w.TryGetPoolInternal<T2>();
                    _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) && MeetsFilter(id, _rf))
                        {
                            _cur = new Entity(id, _w._generation[id]);
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3>
            where T1 : struct where T2 : struct where T3 : struct
        {
            private readonly World _w;
            private readonly Filter _f;

            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) && (_c == null || _c.Has(id)) && MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>…<typeparamref name="T4"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3, T4>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            private readonly World _w; private readonly Filter _f;
            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c, _d; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>(); _d = w.TryGetPoolInternal<T4>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c, _d);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) &&
                            (_c == null || _c.Has(id)) && (_d == null || _d.Has(id)) && MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>…<typeparamref name="T5"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3, T4, T5>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        {
            private readonly World _w; private readonly Filter _f;
            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c, _d, _e; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>();
                    _d = w.TryGetPoolInternal<T4>(); _e = w.TryGetPoolInternal<T5>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c, _d, _e);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) &&
                            (_c == null || _c.Has(id)) && (_d == null || _d.Has(id)) &&
                            (_e == null || _e.Has(id)) && MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>…<typeparamref name="T6"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        {
            private readonly World _w; private readonly Filter _f;
            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c, _d, _e, _f6; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>();
                    _d = w.TryGetPoolInternal<T4>(); _e = w.TryGetPoolInternal<T5>(); _f6 = w.TryGetPoolInternal<T6>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c, _d, _e, _f6);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) &&
                            (_c == null || _c.Has(id)) && (_d == null || _d.Has(id)) &&
                            (_e == null || _e.Has(id)) && (_f6 == null || _f6.Has(id)) && MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>…<typeparamref name="T7"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
        {
            private readonly World _w; private readonly Filter _f;
            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c, _d, _e, _f6, _g; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>();
                    _d = w.TryGetPoolInternal<T4>(); _e = w.TryGetPoolInternal<T5>(); _f6 = w.TryGetPoolInternal<T6>(); _g = w.TryGetPoolInternal<T7>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c, _d, _e, _f6, _g);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) &&
                            (_c == null || _c.Has(id)) && (_d == null || _d.Has(id)) &&
                            (_e == null || _e.Has(id)) && (_f6 == null || _f6.Has(id)) &&
                            (_g == null || _g.Has(id)) && MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>Enumerable over entities matching components <typeparamref name="T1"/>…<typeparamref name="T8"/>.</summary>
        public struct QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
        {
            private readonly World _w; private readonly Filter _f;
            internal QueryEnumerable(World w, Filter f) { this._w = w; this._f = f; }
            /// <summary>Gets the value-type enumerator.</summary>
            public Enumerator GetEnumerator() => new(_w, _f);

            /// <summary>Value-type enumerator for entities with the required components.</summary>
            public struct Enumerator
            {
                private readonly World _w;
                private readonly IComponentPool? _a, _b, _c, _d, _e, _f6, _g, _h; private readonly ResolvedFilter _rf;
                private readonly IEnumerator<(int id, object boxed)> it; private Entity _cur;

                /// <summary>Initializes the enumerator for a given world and filter.</summary>
                public Enumerator(World w, Filter f)
                {
                    this._w = w;
                    _a = w.TryGetPoolInternal<T1>(); _b = w.TryGetPoolInternal<T2>(); _c = w.TryGetPoolInternal<T3>(); _d = w.TryGetPoolInternal<T4>();
                    _e = w.TryGetPoolInternal<T5>(); _f6 = w.TryGetPoolInternal<T6>(); _g = w.TryGetPoolInternal<T7>(); _h = w.TryGetPoolInternal<T8>(); _rf = w.ResolveFilter(f);
                    var seed = Seed(_a, _b, _c, _d, _e, _f6, _g, _h);
                    it = (seed != null ? seed.EnumerateAll().GetEnumerator() : Empty().GetEnumerator());
                    _cur = default;
                }

                /// <summary>The current entity.</summary>
                public Entity Current => _cur;

                /// <summary>Advances to the next matching entity.</summary>
                public bool MoveNext()
                {
                    while (it.MoveNext())
                    {
                        int id = it.Current.id;
                        if ((_a == null || _a.Has(id)) && (_b == null || _b.Has(id)) &&
                            (_c == null || _c.Has(id)) && (_d == null || _d.Has(id)) &&
                            (_e == null || _e.Has(id)) && (_f6 == null || _f6.Has(id)) &&
                            (_g == null || _g.Has(id)) && (_h == null || _h.Has(id)) &&
                            MeetsFilter(id, _rf))
                        { _cur = new Entity(id, _w._generation[id]); return true; }
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns an empty sequence for use when no seed pool is available.
        /// </summary>
        private static IEnumerable<(int, object)> Empty() { yield break; }
    }
}
