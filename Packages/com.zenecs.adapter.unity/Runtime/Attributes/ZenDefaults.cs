// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ZenDefaults.cs
// Purpose: Utility for creating instances using a type's static `Default` field when available.
// Key concepts:
//   • Caches reflection-based getters to amortize lookup cost.
//   • Falls back to Activator.CreateInstance when no Default is exposed.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ZenECS.Adapter.Unity.Attributes
{
    /// <summary>
    /// Core utility for creating objects with a type-specific default when available.
    /// If a public static field named <c>Default</c> exists and matches the declaring type,
    /// it will be used; otherwise <see cref="Activator.CreateInstance(Type)"/> is used.
    /// </summary>
    public static class ZenDefaults
    {
        /// <summary>
        /// Cache of compiled getters for each type's static <c>Default</c> field,
        /// used to offset reflection lookup cost on repeated calls.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<object>?> _getterCache = new();

        /// <summary>
        /// Creates a new instance of <paramref name="t"/> using its static <c>Default</c> field
        /// if present; otherwise uses <see cref="Activator.CreateInstance(Type)"/>.
        /// </summary>
        /// <param name="t">The type to instantiate.</param>
        /// <returns>An instance created from <c>Default</c> or via the activator; may be <c>null</c> for reference types without default ctor.</returns>
        public static object? CreateWithDefaults(Type t)
        {
            // Prefer type's public static Default field when available
            return TryGetStaticDefault(t, out var def)
                ? def
                : Activator.CreateInstance(t);
        }

        /// <summary>
        /// Tries to obtain a value from a type's public static <c>Default</c> field.
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <param name="value">The resolved default value, if any.</param>
        /// <returns><c>true</c> if a compatible Default field exists; otherwise <c>false</c>.</returns>
        private static bool TryGetStaticDefault(Type t, out object? value)
        {
            // Use cached getter (reduces reflection overhead)
            var getter = _getterCache.GetOrAdd(t, static T =>
            {
                var fi = T.GetField("Default", BindingFlags.Public | BindingFlags.Static);
                if (fi == null || fi.FieldType != T) return null;
                return () => fi.GetValue(null)!;
            });

            if (getter != null)
            {
                value = getter();
                return true;
            }

            value = null;
            return false;
        }
    }
}
