// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Query.Span.cs
// Purpose: Span-based zero-allocation entity collection and ref-processing utilities.
// Key concepts:
//   • Fill Span<Entity> directly from queries.
//   • Process component refs without boxing or GC pressure.
//   • Optimized for hot-path iteration and batch operations.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /*
         Example:
         Span<Entity> tmp = stackalloc Entity[2048];
         int n = world.QueryToSpan<Health, Damage, Owner, Team>(tmp, f);   // Query with 4 component constraints
         world.Process<Health>(tmp[..n], (ref Health h) => { h.Value = Math.Max(0, h.Value - 5); });
        */

        // ---- QueryToSpan T1..T8 ----
        public int QueryToSpan<T1>(Span<Entity> dst, Filter f = default) where T1 : struct
        { int n = 0; foreach (var e in Query<T1>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct
        { int n = 0; foreach (var e in Query<T1, T2>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3, T4>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3, T4>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3, T4, T5>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3, T4, T5>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3, T4, T5, T6>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3, T4, T5, T6>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3, T4, T5, T6, T7>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3, T4, T5, T6, T7>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        public int QueryToSpan<T1, T2, T3, T4, T5, T6, T7, T8>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            where T5 : struct where T6 : struct where T7 : struct where T8 : struct
        { int n = 0; foreach (var e in Query<T1, T2, T3, T4, T5, T6, T7, T8>(f)) { if (n >= dst.Length) break; dst[n++] = e; } return n; }

        /// <summary>
        /// Delegate for processing a component reference directly.
        /// </summary>
        public delegate void RefAction<T>(ref T value) where T : struct;

        /// <summary>
        /// Iterates over a span of entities and applies the given ref action to each component.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="ents">Span of entities.</param>
        /// <param name="action">Delegate invoked with a ref to each component.</param>
        public void Process<T>(ReadOnlySpan<Entity> ents, RefAction<T> action) where T : struct
        {
            for (int i = 0; i < ents.Length; i++)
            {
                var e = ents[i];
                if (!IsAlive(e)) continue;
                if (!HasComponentInternal<T>(e)) continue;
                action(ref RefComponentExistingInternal<T>(e));
            }
        }
    }
}
