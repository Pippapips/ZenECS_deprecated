#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Events
{
    /// <summary>
    /// 컴포넌트 변경/추가/삭제가 일어났을 때 브로드캐스트하는 전역 이벤트 허브.
    /// 비용은 구독자가 있을 때만 발생하도록 가볍게 유지.
    /// </summary>
    internal static class ComponentEvents
    {
        internal static event Action<World, Entity, Type>? ComponentChanged;
        internal static event Action<World, Entity, Type>? ComponentAdded;
        internal static event Action<World, Entity, Type>? ComponentRemoved;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseChanged(World w, Entity e, Type t) => ComponentChanged?.Invoke(w, e, t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseAdded(World w, Entity e, Type t) => ComponentAdded?.Invoke(w, e, t);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseRemoved(World w, Entity e, Type t) => ComponentRemoved?.Invoke(w, e, t);
    }
}