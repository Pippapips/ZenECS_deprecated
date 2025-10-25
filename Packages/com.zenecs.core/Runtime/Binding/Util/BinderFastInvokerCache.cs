// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: BinderFastInvokerCache.cs
// Purpose: Builds zero-boxing Apply delegates using Expression trees and generic TryGet<T>.
// Key concepts:
//   • Caches per-component (Type) compiled delegates: (w,e,v,binder) → binder.Apply(ref T).
//   • Requires World.TryGet<T>(Entity, out T) or World.TryGetComponentInternal<T>(...) to exist.
//   • Falls back to BinderInvokerCache when not available.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ZenECS.Core.Binding.Util
{
    internal static class BinderFastInvokerCache
    {
        private static readonly ConcurrentDictionary<Type, Action<World, Entity, IViewBinder, IComponentBinder>> _applyNoBox = new();
        public static Action<World, Entity, IViewBinder, IComponentBinder> GetApplyNoBox(Type t) => _applyNoBox.GetOrAdd(t, Create);

        private static Action<World, Entity, IViewBinder, IComponentBinder> Create(Type t)
        {
            var w = Expression.Parameter(typeof(World), "w");
            var e = Expression.Parameter(typeof(Entity), "e");
            var v = Expression.Parameter(typeof(IViewBinder), "v");
            var b = Expression.Parameter(typeof(IComponentBinder), "b");

            var genericTryGetDef =
                typeof(World)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                        (m.Name == "TryGet" || m.Name == "TryGetComponentInternal") &&
                        m.IsGenericMethodDefinition &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(Entity) &&
                        m.GetParameters()[1].ParameterType.IsByRef)
                ?? throw new InvalidOperationException("World.TryGet<T>(...) or World.TryGetComponentInternal<T>(...) is required.");
            var tryGet = genericTryGetDef.MakeGenericMethod(t);

            var typed = Expression.Convert(b, typeof(IComponentBinder<>).MakeGenericType(t));
            var apply = typed.Type.GetMethod("Apply", new[] { typeof(World), typeof(Entity), t.MakeByRefType(), typeof(IViewBinder) })!;

            var val = Expression.Variable(t, "val");
            var ifTry = Expression.IfThen(Expression.Call(w, tryGet, e, val), Expression.Call(typed, apply, w, e, val, v));
            var block = Expression.Block(new[] { val }, ifTry);
            return Expression.Lambda<Action<World, Entity, IViewBinder, IComponentBinder>>(block, w, e, v, b).Compile();
        }
    }
}
