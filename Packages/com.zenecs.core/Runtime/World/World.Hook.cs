#nullable enable
using System;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        // ===== Per-World Hooks =====
        /// <summary>이 월드에만 적용되는 쓰기 퍼미션 훅. null이면 전역(EcsActions) 훅 사용.</summary>
        public volatile Func<World, Entity, Type, bool>? WritePermissionHook;

        /// <summary>이 월드에만 적용되는 값 검증 훅. null이면 전역(EcsActions) 훅 사용.</summary>
        public volatile Func<object, bool>? ValidateHook;

        // 편의 메서드 (체이닝 지원)
        public void SetWritePermission(Func<World, Entity, Type, bool>? hook) => WritePermissionHook = hook;
        public void AddWritePermission(Func<World, Entity, Type, bool> hook)
            => WritePermissionHook = HookAnd(WritePermissionHook, hook);
        public void SetValidator(Func<object, bool>? hook) => ValidateHook = hook;
        public void AddValidator(Func<object, bool> hook)
            => ValidateHook = HookAnd(ValidateHook, hook);

        private void ClearAllHookQueues()
        {
            WritePermissionHook = null;
            ValidateHook = null;
        }

        // ---- local combinators (불변 delegate를 새로 만들어 교체)
        private static Func<World, Entity, Type, bool> HookAnd(
            Func<World, Entity, Type, bool>? a,
            Func<World, Entity, Type, bool> b)
            => (w, e, t) => (a?.Invoke(w, e, t) ?? true) && b(w, e, t);
        private static Func<object, bool> HookAnd(
            Func<object, bool>? a,
            Func<object, bool> b)
            => o => (a?.Invoke(o) ?? true) && b(o);
    }
}
