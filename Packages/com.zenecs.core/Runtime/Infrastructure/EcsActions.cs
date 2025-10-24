// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsActions.cs
// Purpose: Central entry point for all structural write operations (Add/Replace/Remove).
// Key concepts:
//   • Enforces validation and write permission before component modification.
//   • Dispatches component events (Added / Changed / Removed).
//   • Supports deferred adds via CommandBuffer when structural safety is required.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;

namespace ZenECS.Core.Infrastructure
{
    internal static class EcsActions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleDenied(string reason)
        {
            switch (EcsRuntimeOptions.WritePolicy)
            {
                case EcsRuntimeOptions.WriteFailurePolicy.Throw:
                    throw new InvalidOperationException(reason);
                case EcsRuntimeOptions.WriteFailurePolicy.Log:
                    EcsRuntimeOptions.Log.Warn(reason);
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>Default: enable safe structural changes via deferred adds.</summary>
        public static bool PreferDeferredAdds = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanRead<T>(World w, Entity e) where T : struct
            => w.EvaluateReadPermission(e, typeof(T));

        // ---------------------------------------------------------------------
        // Add
        // ---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add<T>(World w, Entity e, in T value, World.CommandBuffer? cb = null) where T : struct
        {
            if (!w.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return;
            }

            bool valid = w.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return;
            }
            else if (!w.ValidateObject(value!))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return;
            }

            if (w.HasComponentInternal<T>(e)) return;

            if (cb != null) { cb.Add(e, in value); return; }
            if (PreferDeferredAdds)
            {
                var tmp = w.BeginWrite();
                tmp.Add(e, in value);
                w.Schedule(tmp);
                return;
            }

            ref var r = ref w.RefComponentInternal<T>(e);
            r = value;
            ComponentEvents.RaiseAdded(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Replace<T>(World w, Entity e, in T value) where T : struct
        {
            if (!w.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return;
            }

            bool valid = w.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return;
            }
            else if (!w.ValidateObject(value!))
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return;
            }

            ref var r = ref w.RefComponentInternal<T>(e);
            r = value;
            ComponentEvents.RaiseChanged(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Remove<T>(World w, Entity e) where T : struct
        {
            if (!w.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Remove<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return;
            }

            if (w.RemoveComponentInternal<T>(e))
                ComponentEvents.RaiseRemoved(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Has<T>(World w, Entity e) where T : struct
            => w.HasComponentInternal<T>(e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref readonly T Read<T>(World w, Entity e) where T : struct
            => ref w.RefComponentInternal<T>(e);
    }
}
