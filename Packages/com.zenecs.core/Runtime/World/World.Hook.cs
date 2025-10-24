// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Hook.cs
// Purpose: Read/Write permission hooks and value validation hooks (per-World).
// Key concepts:
//   • Fine-grained guards for safety and debugging.
//   • Can layer over global EcsActions if present.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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
        /// Write-permission hook scoped to this world only. If <c>null</c>, falls back to global (EcsActions) hook.
        /// </summary>
        public volatile Func<World, Entity, Type, bool>? WritePermissionHook;

        /// <summary>
        /// Read-permission hook scoped to this world only. If <c>null</c>, falls back to global (EcsActions) hook.
        /// </summary>
        public volatile Func<World, Entity, Type, bool>? ReadPermissionHook;

        /// <summary>
        /// Value-validation hook scoped to this world only. If <c>null</c>, falls back to global (EcsActions) hook.
        /// </summary>
        public volatile Func<object, bool>? ValidateHook;

        // ===== Hook storage (list-based) =====
        private readonly List<Func<World, Entity, Type, bool>> _writePerms = new(2);
        private readonly List<Func<World, Entity, Type, bool>> _readPerms  = new(1);
        private readonly List<Func<object, bool>> _objValidators           = new(2);

        // ===== Write permissions =====
        public void AddWritePermission(Func<World, Entity, Type, bool> hook)
        {
            if (hook != null) _writePerms.Add(hook);
        }
        public bool RemoveWritePermission(Func<World, Entity, Type, bool> hook)
            => _writePerms.Remove(hook);
        public void ClearWritePermissions() => _writePerms.Clear();

        // ===== Read permissions (not applied to Has<T>) =====
        public void AddReadPermission(Func<World, Entity, Type, bool> hook)
        {
            if (hook != null) _readPerms.Add(hook);
        }
        public bool RemoveReadPermission(Func<World, Entity, Type, bool> hook)
            => _readPerms.Remove(hook);
        public void ClearReadPermissions() => _readPerms.Clear();

        // ===== Object-level validators (type-agnostic) =====
        public void AddValidator(Func<object, bool> hook)
        {
            if (hook != null) _objValidators.Add(hook);
        }
        public bool RemoveValidator(Func<object, bool> hook)
            => _objValidators.Remove(hook);
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
        /// Adds a type-safe generic validator that is applied only when the value is of <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct
        {
            EnsureTypeValidator<T>().Add(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct
        {
            return EnsureTypeValidator<T>().Remove(predicate);
        }

        /// <summary>
        /// Clears all type-specific validators.
        /// </summary>
        public void ClearTypedValidators() => _typedValidators.Clear();
        
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EvaluateWritePermission(Entity e, Type t)
        {
            // All hooks must return true to allow; if no hooks, allow.
            for (int i = 0; i < _writePerms.Count; i++)
                if (!_writePerms[i](this, e, t)) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool EvaluateReadPermission(Entity e, Type t)
        {
            for (int i = 0; i < _readPerms.Count; i++)
                if (!_readPerms[i](this, e, t)) return false;
            return true;
        }

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(Func<T, bool> p) => _preds.Add(p);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Remove(Func<T, bool> p) => _preds.Remove(p);            
            
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
        /// Evaluates type-specific validators without boxing. 
        /// Returns <c>true</c> if no validators are registered for <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// Operates independently of the world-level <see cref="ValidateHook"/> (object-level) 
        /// and independently of global EcsActions hooks. Callers may evaluate both if needed.
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
