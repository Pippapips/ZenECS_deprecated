// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: BinderInvokerCache.cs
// Purpose: Builds boxed Apply/Bind/Unbind delegates via Expression trees.
// Key concepts:
//   • Used when zero-boxing path is unavailable or not required.
//   • Caches delegates per component type to avoid repeated reflection.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ZenECS.Core.Binding.Util
{
    public static class BinderInvokerCache
    {
        private static readonly ConcurrentDictionary<Type, Action<World, Entity, object?, IViewBinder, IComponentBinder>> _apply = new();
        private static readonly ConcurrentDictionary<Type, Action<World, Entity, IViewBinder, IComponentBinder>> _bind = new();
        private static readonly ConcurrentDictionary<Type, Action<World, Entity, IViewBinder, IComponentBinder>> _unbind = new();

        public static Action<World, Entity, object?, IViewBinder, IComponentBinder> GetApply(Type t) => _apply.GetOrAdd(t, CreateApply);
        public static Action<World, Entity, IViewBinder, IComponentBinder> GetBind(Type t) => _bind.GetOrAdd(t, CreateBind);
        public static Action<World, Entity, IViewBinder, IComponentBinder> GetUnbind(Type t) => _unbind.GetOrAdd(t, CreateUnbind);

        private static Action<World, Entity, object?, IViewBinder, IComponentBinder> CreateApply(Type t)
        {
            var w = Expression.Parameter(typeof(World), "w");
            var e = Expression.Parameter(typeof(Entity), "e");
            var valObj = Expression.Parameter(typeof(object), "value");
            var v = Expression.Parameter(typeof(IViewBinder), "v");
            var b = Expression.Parameter(typeof(IComponentBinder), "b");

            var typed = Expression.Convert(b, typeof(IComponentBinder<>).MakeGenericType(t));
            var val = Expression.Convert(valObj, t);
            var tmp = Expression.Variable(t, "val");
            var assign = Expression.Assign(tmp, val);
            var apply = typed.Type.GetMethod("Apply", new[] { typeof(World), typeof(Entity), t.MakeByRefType(), typeof(IViewBinder) })!;
            var call = Expression.Call(typed, apply, w, e, tmp, v);
            var body = Expression.Block(new[] { tmp }, assign, call);
            return Expression.Lambda<Action<World, Entity, object?, IViewBinder, IComponentBinder>>(body, w, e, valObj, v, b).Compile();
        }

        private static Action<World, Entity, IViewBinder, IComponentBinder> CreateBind(Type t)
        {
            var w = Expression.Parameter(typeof(World), "w");
            var e = Expression.Parameter(typeof(Entity), "e");
            var v = Expression.Parameter(typeof(IViewBinder), "v");
            var b = Expression.Parameter(typeof(IComponentBinder), "b");

            var typed = Expression.Convert(b, typeof(IComponentBinder<>).MakeGenericType(t));
            var bind = typed.Type.GetMethod("Bind", new[] { typeof(World), typeof(Entity), typeof(IViewBinder) })!;
            var call = Expression.Call(typed, bind, w, e, v);
            return Expression.Lambda<Action<World, Entity, IViewBinder, IComponentBinder>>(call, w, e, v, b).Compile();
        }

        private static Action<World, Entity, IViewBinder, IComponentBinder> CreateUnbind(Type t)
        {
            var w = Expression.Parameter(typeof(World), "w");
            var e = Expression.Parameter(typeof(Entity), "e");
            var v = Expression.Parameter(typeof(IViewBinder), "v");
            var b = Expression.Parameter(typeof(IComponentBinder), "b");

            var typed = Expression.Convert(b, typeof(IComponentBinder<>).MakeGenericType(t));
            var unbind = typed.Type.GetMethod("Unbind", new[] { typeof(World), typeof(Entity), typeof(IViewBinder) })!;
            var call = Expression.Call(typed, unbind, w, e, v);
            return Expression.Lambda<Action<World, Entity, IViewBinder, IComponentBinder>>(call, w, e, v, b).Compile();
        }
    }
}
