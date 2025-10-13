#nullable enable
using System.Runtime.CompilerServices;
using ZenECS.Core.Infrastructure;

namespace ZenECS.Core.Extensions
{
    /// <summary>얇은 래퍼: 내부는 항상 EcsActions 호출</summary>
    public static class WorldComponentsOpsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this World w, Entity e, in T v) where T:struct => EcsActions.Add(w, e, in v);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(this World w, Entity e, in T v, World.CommandBuffer cb) where T:struct => EcsActions.Add(w, e, in v, cb);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrReplace<T>(this World w, Entity e, in T v) where T:struct => EcsActions.AddOrReplace(w, e, in v);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Replace<T>(this World w, Entity e, in T v) where T:struct => EcsActions.Replace(w, e, in v);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove<T>(this World w, Entity e) where T:struct => EcsActions.Remove<T>(w, e);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Toggle<T>(this World w, Entity e, bool on) where T:struct => EcsActions.Toggle<T>(w, e, on);
    }
}