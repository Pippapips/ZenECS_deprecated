﻿// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: WorldComponentsOpsExtensions.cs
// Purpose: Thin wrapper for EcsActions providing ergonomic extension methods for World.
// Key concepts:
//   • All methods directly delegate to EcsActions (single point of validation & event dispatch).
//   • Includes Try-variants for non-throw policies.
//   • Supports Add, Replace, Remove, Has, Read, and GetOrAdd semantics.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Runtime.CompilerServices;
using ZenECS.Core.Infrastructure;

namespace ZenECS.Core
{
    /// <summary>
    /// Provides strongly-typed component operations as <see langword="extension"/> methods for <see cref="World"/>.
    /// These are thin delegations to <see cref="EcsActions"/> to keep a single point of validation,
    /// policy checks, hooks/event dispatch, and metrics.
    /// </summary>
    /// <remarks>
    /// All methods are marked with <see cref="MethodImplOptions.AggressiveInlining"/> and are intended for
    /// hot paths. Prefer the <c>Try*</c> variants when you want to avoid exceptions and branch on success.
    /// </remarks>
    public static class WorldComponentsOpsExtensions
    {
        // ---------------------------------------------------------------------
        // Add / Replace / Remove
        // ---------------------------------------------------------------------

        /// <summary>
        /// Adds a component of type <typeparamref name="T"/> to the specified <paramref name="e"/>.
        /// If the component already exists, behavior follows the current <see cref="EcsActions"/> policy (may throw).
        /// </summary>
        /// <typeparam name="T">Component value type (unmanaged struct recommended).</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="value">Component value to add.</param>
        /// <exception cref="InvalidOperationException">Propagated from <see cref="EcsActions"/> when disallowed.</exception>
        /// <remarks>Validation, hooks, and write-permission checks are performed by <see cref="EcsActions"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this World w, Entity e, in T value) where T : struct
            => EcsActions.Add(w, e, in value, null);

        /// <summary>
        /// Replaces (or adds, depending on policy) a component of type <typeparamref name="T"/> on <paramref name="e"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="value">New component value.</param>
        /// <exception cref="InvalidOperationException">Propagated if replacement is disallowed by policy.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Replace<T>(this World w, Entity e, in T value) where T : struct
            => EcsActions.Replace(w, e, in value);

        /// <summary>
        /// Removes component <typeparamref name="T"/> from <paramref name="e"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <exception cref="InvalidOperationException">Propagated when removal is disallowed or component missing (per policy).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove<T>(this World w, Entity e) where T : struct
            => EcsActions.Remove<T>(w, e);

        // ---------------------------------------------------------------------
        // Try-variants
        // ---------------------------------------------------------------------

        /// <summary>
        /// Attempts to add component <typeparamref name="T"/> to <paramref name="e"/> without throwing.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="value">Component value to add.</param>
        /// <returns><see langword="true"/> if the component was added; otherwise <see langword="false"/>.</returns>
        /// <remarks>All exceptions from <see cref="EcsActions.Add{T}(World, Entity, in T, object?)"/> are swallowed.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<T>(this World w, Entity e, in T value) where T : struct
        {
            try { EcsActions.Add(w, e, in value, null); return true; }
            catch { return false; }
        }

        /// <summary>
        /// Attempts to replace component <typeparamref name="T"/> on <paramref name="e"/> without throwing.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="value">New component value.</param>
        /// <returns><see langword="true"/> if the replacement succeeded; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReplace<T>(this World w, Entity e, in T value) where T : struct
        {
            try { EcsActions.Replace(w, e, in value); return true; }
            catch { return false; }
        }

        /// <summary>
        /// Attempts to remove component <typeparamref name="T"/> from <paramref name="e"/> without throwing.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <returns><see langword="true"/> if the component was removed; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRemove<T>(this World w, Entity e) where T : struct
        {
            try { EcsActions.Remove<T>(w, e); return true; }
            catch { return false; }
        }

        // ---------------------------------------------------------------------
        // Has / Read
        // ---------------------------------------------------------------------

        /// <summary>
        /// Checks whether <paramref name="e"/> currently has component <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <returns><see langword="true"/> if present; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this World w, Entity e) where T : struct
            => EcsActions.Has<T>(w, e);

        /// <summary>
        /// Returns a <see langword="ref readonly"/> reference to component <typeparamref name="T"/> on <paramref name="e"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <returns>A <see langword="ref readonly"/> reference to the stored component.</returns>
        /// <exception cref="InvalidOperationException">Propagated if the component is missing or access is denied by policy.</exception>
        /// <remarks>
        /// The reference is only valid while the underlying pool is stable. Do not capture beyond its immediate use.
        /// For safe value copies, use <see cref="TryRead{T}(World, Entity, out T)"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Read<T>(this World w, Entity e) where T : struct
            => ref EcsActions.Read<T>(w, e);

        /// <summary>
        /// Attempts to read component <typeparamref name="T"/> from <paramref name="e"/> into an output value.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="value">When this method returns, contains the component value if found; otherwise default.</param>
        /// <returns><see langword="true"/> if the component exists and was copied; otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Gets a <see langword="ref readonly"/> reference to component <typeparamref name="T"/> on <paramref name="e"/>,
        /// adding a default value if it is not present.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <returns>
        /// A <see langword="ref readonly"/> reference to the existing or newly-added component.
        /// </returns>
        /// <remarks>
        /// This is convenient for idempotent initialization. If you need to control the initial value,
        /// use the <see cref="GetOrAdd{T}(World, Entity, in T)"/> overload.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetOrAdd<T>(this World w, Entity e) where T : struct
        {
            if (!w.Has<T>(e))
                w.Add(e, default(T));
            return ref w.Read<T>(e);
        }

        /// <summary>
        /// Gets a <see langword="ref readonly"/> reference to component <typeparamref name="T"/> on <paramref name="e"/>,
        /// adding it with the provided <paramref name="initial"/> value if it is not present.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="w">Target world.</param>
        /// <param name="e">Target entity.</param>
        /// <param name="initial">Initial value to store when the component is missing.</param>
        /// <returns>
        /// A <see langword="ref readonly"/> reference to the existing or newly-added component.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetOrAdd<T>(this World w, Entity e, in T initial) where T : struct
        {
            if (!w.Has<T>(e))
                w.Add(e, in initial);
            return ref w.Read<T>(e);
        }
    }
}
