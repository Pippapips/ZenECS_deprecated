// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: WorldComponentsOpsExtensions.cs
// Purpose: Thin wrapper for EcsActions providing ergonomic extension methods for World.
// Key concepts:
//   • All methods directly delegate to EcsActions (single point of validation & event dispatch).
//   • Includes Try-variants for non-throw policies.
//   • Supports Add, Replace, Remove, Has, Read, and GetOrAdd semantics.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;
using ZenECS.Core.Infrastructure;

namespace ZenECS.Core.Extensions
{
    public static class WorldComponentsOpsExtensions
    {
        // ---------------------------------------------------------------------
        // Add / Replace / Remove
        // ---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this World w, Entity e, in T value) where T : struct
            => EcsActions.Add(w, e, in value, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Replace<T>(this World w, Entity e, in T value) where T : struct
            => EcsActions.Replace(w, e, in value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove<T>(this World w, Entity e) where T : struct
            => EcsActions.Remove<T>(w, e);

        // ---------------------------------------------------------------------
        // Try-variants
        // ---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<T>(this World w, Entity e, in T value) where T : struct
        {
            try { EcsActions.Add(w, e, in value, null); return true; }
            catch { return false; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReplace<T>(this World w, Entity e, in T value) where T : struct
        {
            try { EcsActions.Replace(w, e, in value); return true; }
            catch { return false; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRemove<T>(this World w, Entity e) where T : struct
        {
            try { EcsActions.Remove<T>(w, e); return true; }
            catch { return false; }
        }

        // ---------------------------------------------------------------------
        // Has / Read
        // ---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this World w, Entity e) where T : struct
            => EcsActions.Has<T>(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Read<T>(this World w, Entity e) where T : struct
            => ref EcsActions.Read<T>(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRead<T>(this World w, Entity e, out T value) where T : struct
        {
            if (!w.Has<T>(e)) { value = default; return false; }
            value = EcsActions.Read<T>(w, e);
            return true;
        }

        // ---------------------------------------------------------------------
        // GetOrAdd
        // ---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetOrAdd<T>(this World w, Entity e) where T : struct
        {
            if (!w.Has<T>(e))
                w.Add(e, default(T));
            return ref w.Read<T>(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetOrAdd<T>(this World w, Entity e, in T initial) where T : struct
        {
            if (!w.Has<T>(e))
                w.Add(e, in initial);
            return ref w.Read<T>(e);
        }
    }
}
