// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Hook.cs
// Purpose: Read/Write permission hooks and value validation hooks (per-World).
// Key concepts:
//   • Fine-grained guards for safety and debugging.
//   • Can layer over global EcsActions if present.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // ===== Per-World Hooks =====

        /// <summary>
        /// World-scoped write-permission hook.
        /// Return <see langword="true"/> to allow a structural write (Add/Replace/Remove) of <paramref name="type"/> on <paramref name="entity"/>,
        /// or <see langword="false"/> to deny it. When <see langword="null"/>, the evaluation falls back to a global hook (if any).
        /// </summary>
        /// <remarks>
        /// This delegate is evaluated in addition to list-based permissions managed via
        /// <see cref="AddWritePermission(Func{World, Entity, Type, bool})"/>. All active hooks must allow the write.
        /// </remarks>
        public volatile Func<World, Entity, Type, bool>? WritePermissionHook;

        /// <summary>
        /// World-scoped read-permission hook.
        /// Return <see langword="true"/> to allow read access of component <paramref name="type"/> on <paramref name="entity"/>,
        /// or <see langword="false"/> to deny it. When <see langword="null"/>, the evaluation falls back to a global hook (if any).
        /// </summary>
        /// <remarks>
        /// This delegate is evaluated in addition to list-based permissions managed via
        /// <see cref="AddReadPermission(Func{World, Entity, Type, bool})"/>. All active hooks must allow the read.
        /// Note that read permissions are not applied to <c>Has&lt;T&gt;</c> checks for performance reasons.
        /// </remarks>
        public volatile Func<World, Entity, Type, bool>? ReadPermissionHook;

        /// <summary>
        /// World-scoped object-level value validator.
        /// Return <see langword="true"/> to accept a value (component payload), or <see langword="false"/> to reject it.
        /// When <see langword="null"/>, the evaluation falls back to a global validator (if any).
        /// </summary>
        /// <remarks>
        /// Use <see cref="AddValidator{T}(Func{T, bool})"/> for generic, type-safe validators that avoid boxing.
        /// </remarks>
        public volatile Func<object, bool>? ValidateHook;

        // ===== Hook storage (list-based) =====
        private readonly List<Func<World, Entity, Type, bool>> _writePerms = new(2);
        private readonly List<Func<World, Entity, Type, bool>> _readPerms  = new(1);
        private readonly List<Func<object, bool>> _objValidators           = new(2);

        // ===== Write permissions =====

        /// <summary>
        /// Adds a write-permission predicate evaluated for every structural write in this world.
        /// </summary>
        /// <param name="hook">Predicate that must return <see langword="true"/> to allow the write.</param>
        public void AddWritePermission(Func<World, Entity, Type, bool> hook)
        {
            if (hook != null) _writePerms.Add(hook);
        }

        /// <summary>
        /// Removes a previously added write-permission predicate.
        /// </summary>
        /// <param name="hook">The predicate to remove.</param>
        /// <returns><see langword="true"/> if the predicate was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveWritePermission(Func<World, Entity, Type, bool> hook)
            => _writePerms.Remove(hook);

        /// <summary>
        /// Clears all list-based write-permission predicates.
        /// </summary>
        public void ClearWritePermissions() => _writePerms.Clear();

        // ===== Read permissions (not applied to Has<T>) =====

        /// <summary>
        /// Adds a read-permission predicate evaluated for component reads in this world.
        /// </summary>
        /// <param name="hook">Predicate that must return <see langword="true"/> to allow the read.</param>
        public void AddReadPermission(Func<World, Entity, Type, bool> hook)
        {
            if (hook != null) _readPerms.Add(hook);
        }

        /// <summary>
        /// Removes a previously added read-permission predicate.
        /// </summary>
        /// <param name="hook">The predicate to remove.</param>
        /// <returns><see langword="true"/> if the predicate was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveReadPermission(Func<World, Entity, Type, bool> hook)
            => _readPerms.Remove(hook);

        /// <summary>
        /// Clears all list-based read-permission predicates.
        /// </summary>
        public void ClearReadPermissions() => _readPerms.Clear();

        // ===== Object-level validators (type-agnostic) =====

        /// <summary>
        /// Adds a world-level, type-agnostic value validator.
        /// </summary>
        /// <param name="hook">Validator that must return <see langword="true"/> to accept the value.</param>
        public void AddValidator(Func<object, bool> hook)
        {
            if (hook != null) _objValidators.Add(hook);
        }

        /// <summary>
        /// Removes a previously added world-level value validator.
        /// </summary>
        /// <param name="hook">The validator to remove.</param>
        /// <returns><see langword="true"/> if the validator was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveValidator(Func<object, bool> hook)
            => _objValidators.Remove(hook);

        /// <summary>
        /// Clears all world-level value validators.
        /// </summary>
        public void ClearValidators() => _objValidators.Clear();

        // ===== Hook Combinators =====
        private static Func<World, Entity, Type, bool> ChainAnd(
            Func<World, Entity, Type, bool>? a,
            Func<World, Entity, Type, bool> b)
            => (w, e, t) => (a?.Invoke(w, e, t) ?? true) && b(w, e, t);

        private static Func<object, bool> ChainValidate(
            Func<object, bool>? a,
            Func<object, bool> b)
            => o => (a?.Invoke(o) ?? true) && b(o);

        /// <summary>
        /// Adds a type-safe validator that is evaluated only for values of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">Predicate that must return <see langword="true"/> to accept values of type <typeparamref name="T"/>.</param>
        /// <remarks>
        /// Uses an internal per-type cache to avoid boxing and keep validation fast.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct
        {
            EnsureTypeValidator<T>().Add(predicate);
        }

        /// <summary>
        /// Removes a previously added type-safe validator for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="predicate">The predicate to remove.</param>
        /// <returns><see langword="true"/> if the predicate was removed; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct
        {
            return EnsureTypeValidator<T>().Remove(predicate);
        }

        /// <summary>
        /// Clears all type-specific validators registered via <see cref="AddValidator{T}(Func{T, bool})"/>.
        /// </summary>
        public void ClearTypedValidators() => _typedValidators.Clear();

        /// <summary>
        /// Clears all hook queues (read/write permissions, validators) and resets world-scoped delegates
        /// (<see cref="WritePermissionHook"/>, <see cref="ReadPermissionHook"/>, <see cref="ValidateHook"/>) to <see langword="null"/>.
        /// </summary>
        private void ClearAllHookQueues()
        {
            ClearWritePermissions();
            ClearReadPermissions();
            ClearValidators();
            ClearTypedValidators();

            WritePermissionHook = null;
            ReadPermissionHook  = null;
            ValidateHook        = null;
        }

        // ===== Evaluators =====

        /// <summary>
        /// Evaluates all list-based write-permission predicates for the specified component access.
        /// </summary>
        /// <param name="e">Entity handle.</param>
        /// <param name="t">Component type.</param>
        /// <returns>
        /// <see langword="true"/> if all write-permission predicates allow the operation
        /// or if none are registered; otherwise <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EvaluateWritePermission(Entity e, Type t)
        {
            // All hooks must return true to allow; if no hooks, allow.
            for (int i = 0; i < _writePerms.Count; i++)
                if (!_writePerms[i](this, e, t)) return false;
            return true;
        }

        /// <summary>
        /// Evaluates all list-based read-permission predicates for the specified component access.
        /// </summary>
        /// <param name="e">Entity handle.</param>
        /// <param name="t">Component type.</param>
        /// <returns>
        /// <see langword="true"/> if all read-permission predicates allow the operation
        /// or if none are registered; otherwise <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EvaluateReadPermission(Entity e, Type t)
        {
            for (int i = 0; i < _readPerms.Count; i++)
                if (!_readPerms[i](this, e, t)) return false;
            return true;
        }

        /// <summary>
        /// Evaluates all world-level object validators.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>
        /// <see langword="true"/> if all validators accept the value
        /// or if none are registered; otherwise <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ValidateObject(object value)
        {
            for (int i = 0; i < _objValidators.Count; i++)
                if (!_objValidators[i](value)) return false;
            return true;
        }

        // ===================== Type-specific validator cache ==========================
        private interface IBoxedTypeValidator
        {
            bool InvokeBoxed(object value);
        }

        private sealed class TypeValidator<T> : IBoxedTypeValidator where T : struct
        {
            private readonly List<Func<T, bool>> _preds = new(2);

            /// <summary>Adds a validator for values of <typeparamref name="T"/>.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Func<T, bool> p) => _preds.Add(p);

            /// <summary>Removes a validator for values of <typeparamref name="T"/>.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Remove(Func<T, bool> p) => _preds.Remove(p);

            /// <summary>
            /// Evaluates all validators for the provided value.
            /// </summary>
            /// <param name="v">Value to validate.</param>
            /// <returns><see langword="true"/> if all validators accept; otherwise <see langword="false"/>.</returns>
            public bool Invoke(in T v)
            {
                for (int i = 0; i < _preds.Count; i++)
                    if (!_preds[i](v))
                        return false;
                return true;
            }

            bool IBoxedTypeValidator.InvokeBoxed(object value)
                => value is T v && Invoke(in v);
        }

        private readonly Dictionary<Type, object> _typedValidators = new(64);

        /// <summary>
        /// Ensures and returns the per-type validator cache for <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TypeValidator<T> EnsureTypeValidator<T>() where T : struct
        {
            var t = typeof(T);
            if (!_typedValidators.TryGetValue(t, out var obj))
            {
                obj = new TypeValidator<T>();
                _typedValidators[t] = obj;
            }
            return (TypeValidator<T>)obj;
        }

        /// <summary>
        /// Evaluates type-specific validators for <paramref name="value"/> without boxing.
        /// Returns <see langword="true"/> if no validators are registered for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <remarks>
        /// Operates independently of the world-level <see cref="ValidateHook"/> and of global EcsActions hooks.
        /// Callers may evaluate both if needed.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ValidateTyped<T>(in T value) where T : struct
        {
            if (_typedValidators.TryGetValue(typeof(T), out var obj))
                return ((TypeValidator<T>)obj).Invoke(in value);
            return true;
        }
    }
}
