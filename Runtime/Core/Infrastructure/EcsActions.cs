#nullable enable
using System;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>모든 쓰기(Add/Replace/Remove)의 단일 관문(검증/권한/이벤트/지연반영)</summary>
    internal static class EcsActions
    {
        public static Func<World, Entity, Type, bool>? HasWritePermissionHook;
        public static Func<object, bool>? ValidateHook;
        public static bool PreferDeferredAdds = true; // 루프 중 구조적 변경 안전 기본값

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add<T>(World w, Entity e, in T value, World.CommandBuffer? cb = null) where T : struct
        {
            if (!(HasWritePermissionHook?.Invoke(w, e, typeof(T)) ?? true)) return;
            if (!(ValidateHook?.Invoke(value) ?? true)) return;
            if (w.Has<T>(e)) return;

            if (cb != null)
            {
                if (PreferDeferredAdds)
                {
                    var tmp = w.BeginWrite();    // 임시 CB 생성
                    tmp.Add(e, in value);
                    w.Schedule(tmp);             // 프레임 경계에서 적용
                }
                else
                {
                    cb.Add(e, in value); // 이미 받은 CB로 기록만
                }
                return;
            }

            // 즉시 적용 (CommandBuffer 없이 바로)
            ref var r = ref w.RefInternal<T>(e);
            r = value;
            ComponentEvents.RaiseAdded(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddOrReplace<T>(World w, Entity e, in T value, World.CommandBuffer? cb = null) where T : struct
        {
            if (w.Has<T>(e)) { Replace(w, e, in value); return; }
            Add(w, e, in value, cb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetOrAdd<T>(World w, Entity e, in T initial, World.CommandBuffer? cb = null) where T : struct
        {
            if (w.TryGet<T>(e, out var v)) return v;
            Add(w, e, in initial, cb);
            return initial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Replace<T>(World w, Entity e, in T value) where T : struct
        {
            if (!(HasWritePermissionHook?.Invoke(w, e, typeof(T)) ?? true)) return;
            if (!(ValidateHook?.Invoke(value) ?? true)) return;

            ref var r = ref w.RefInternal<T>(e);
            r = value;
            ComponentEvents.RaiseChanged(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Remove<T>(World w, Entity e) where T : struct
        {
            if (!(HasWritePermissionHook?.Invoke(w, e, typeof(T)) ?? true)) return;
            if (w.RemoveInternal<T>(e))
                ComponentEvents.RaiseRemoved(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Toggle<T>(World w, Entity e, bool on) where T:struct
        {
            if (on) AddOrReplace(w, e, default(T));
            else    Remove<T>(w, e);
        }
    }
}