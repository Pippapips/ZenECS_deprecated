#nullable enable
using System;
using System.Runtime.CompilerServices;
using ZenECS.Core.Events;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>모든 쓰기(Add/Replace/Remove)의 단일 관문(검증/권한/이벤트/지연반영)</summary>
    internal static class EcsActions
    {
        // 전역(Global) 훅: 월드에 등록된 훅이 없을 때만 사용
        public static volatile Func<World, Entity, Type, bool>? WritePermissionHook;
        public static volatile Func<object, bool>? ValidateHook;
        public static bool PreferDeferredAdds = true; // 루프 중 구조적 변경 안전 기본값

        // ===== Hook Registration API =====
        /// <summary>쓰기 퍼미션 훅 전체를 교체합니다. null이면 모든 쓰기 허용.</summary>
        public static void SetWritePermission(Func<World, Entity, Type, bool>? hook)
            => WritePermissionHook = hook;

        /// <summary>기존 퍼미션 훅과 AND 체이닝합니다. (모든 훅이 true여야 허용)</summary>
        public static void AddWritePermission(Func<World, Entity, Type, bool> hook)
            => WritePermissionHook = ChainAnd(WritePermissionHook, hook);

        /// <summary>검증 훅 전체를 교체합니다. null이면 모든 값 허용.</summary>
        public static void SetValidator(Func<object, bool>? hook)
            => ValidateHook = hook;

        /// <summary>특정 T 컴포넌트만 검증을 추가(AND)합니다.</summary>
        public static void AddValidator<T>(Func<T, bool> predicate) where T : struct
            => ValidateHook = ChainValidate(ValidateHook, o => o is T v && predicate(v));

        /// <summary>등록된 훅 초기화</summary>
        public static void ClearHooks()
        {
            WritePermissionHook = null;
            ValidateHook = null;
        }

        // ===== Hook Combinators =====
        private static Func<World, Entity, Type, bool> ChainAnd(
            Func<World, Entity, Type, bool>? a,
            Func<World, Entity, Type, bool> b)
            => (w, e, t) => (a?.Invoke(w, e, t) ?? true) && b(w, e, t);
        private static Func<object, bool> ChainValidate(
            Func<object, bool>? a,
            Func<object, bool> b)
            => o => (a?.Invoke(o) ?? true) && b(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add<T>(World w, Entity e, in T value, World.CommandBuffer? cb = null) where T : struct
        {
            var perm = w.WritePermissionHook ?? WritePermissionHook;
            if (!(perm?.Invoke(w, e, typeof(T)) ?? true)) return;
            var val = w.ValidateHook ?? ValidateHook;
            if (!(val?.Invoke(value) ?? true)) return;
            if (w.HasComponentInternal<T>(e)) return;

            if (cb != null)
            {
                if (PreferDeferredAdds)
                {
                    var tmp = w.BeginWrite(); // 임시 CB 생성
                    tmp.Add(e, in value);
                    w.Schedule(tmp); // 프레임 경계에서 적용
                }
                else
                {
                    cb.Add(e, in value); // 이미 받은 CB로 기록만
                }
                return;
            }

            // 즉시 적용 (CommandBuffer 없이 바로)
            ref var r = ref w.RefComponentInternal<T>(e);
            r = value;
            ComponentEvents.RaiseAdded(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddOrReplace<T>(World w, Entity e, in T value, World.CommandBuffer? cb = null) where T : struct
        {
            if (w.HasComponentInternal<T>(e))
            {
                Replace(w, e, in value);
                return;
            }
            Add(w, e, in value, cb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetOrAdd<T>(World w, Entity e, in T initial, World.CommandBuffer? cb = null) where T : struct
        {
            if (w.TryGetComponentInternal<T>(e, out var v)) return v;
            Add(w, e, in initial, cb);
            return initial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Replace<T>(World w, Entity e, in T value) where T : struct
        {
            var perm = w.WritePermissionHook ?? WritePermissionHook;
            if (!(perm?.Invoke(w, e, typeof(T)) ?? true)) return;
            var val = w.ValidateHook ?? ValidateHook;
            if (!(val?.Invoke(value) ?? true)) return;
            ref var r = ref w.RefComponentInternal<T>(e);
            r = value;
            ComponentEvents.RaiseChanged(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Remove<T>(World w, Entity e) where T : struct
        {
            var perm = w.WritePermissionHook ?? WritePermissionHook;
            if (!(perm?.Invoke(w, e, typeof(T)) ?? true)) return;
            if (w.RemoveComponentInternal<T>(e))
                ComponentEvents.RaiseRemoved(w, e, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Toggle<T>(World w, Entity e, bool on) where T : struct
        {
            if (on) AddOrReplace(w, e, default(T));
            else Remove<T>(w, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Has<T>(World w, Entity e) where T : struct
        {
            var perm = w.WritePermissionHook ?? WritePermissionHook;
            if (!(perm?.Invoke(w, e, typeof(T)) ?? true)) return false;
            return w.HasComponentInternal<T>(e);
        }
    }
}
