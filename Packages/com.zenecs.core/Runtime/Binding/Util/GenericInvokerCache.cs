// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: GenericInvokerCache.cs
// Purpose: Caches closed generic MethodInfo to avoid repeated MakeGenericMethod overhead.
// Key concepts:
//   • Cache key: (TargetType, MethodName, GenericArg).
//   • Value: closed MethodInfo created once and reused.
//   • Thin helper to invoke generic methods by type at runtime.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ZenECS.Core.Binding.Util
{
    internal static class GenericInvokerCache
    {
        private static readonly ConcurrentDictionary<(Type TargetType, string Name, Type GenericArg), MethodInfo>
            _cache = new();

        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Invokes a generic method on <paramref name="target"/> by closing it with <paramref name="genericArg"/>.
        /// </summary>
        public static void Invoke(object target, string methodName, Type genericArg, params object?[] args)
        {
            var targetType = target.GetType();
            var key = (targetType, methodName, genericArg);

            var closed = _cache.GetOrAdd(key, k =>
            {
                var open = k.TargetType.GetMethod(k.Name, Flags)
                           ?? throw new MissingMethodException(k.TargetType.FullName, k.Name);
                return open.MakeGenericMethod(k.GenericArg);
            });

            closed.Invoke(target, args);
        }
    }
}