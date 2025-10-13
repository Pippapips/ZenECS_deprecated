#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace ZenECS.Core.Events
{
    internal static class EntityEvents
    {
        internal static event Action<World, Entity>? EntityCreated;
        internal static event Action<World, Entity>? EntityDestroyRequested;
        internal static event Action<World, Entity>? EntityDestroyed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseCreated(World w, Entity e) => EntityCreated?.Invoke(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroyRequested(World w, Entity e) => EntityDestroyRequested?.Invoke(w, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RaiseDestroyed(World w, Entity e) => EntityDestroyed?.Invoke(w, e);
    }
}