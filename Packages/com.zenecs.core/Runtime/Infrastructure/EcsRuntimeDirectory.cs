using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core;
using ZenECS.Core.Sync;
using ZenECS.Core.Systems;
#if ZENECS_TRACE
using ZenECS.Core.Diagnostics;
#endif

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Holds runtime references to the active World and Systems and exposes simple discovery utilities.
    /// Thread-safe; all mutations are guarded by a single lock and events are raised outside the lock.
    /// </summary>
    public static class EcsRuntimeDirectory
    {
        private static readonly object _gate = new object();

        // Backing fields
        private static World? _world;
        private static ISystem[]? _systems;           // store as arrays for minimal overhead
        private static ISystem[]? _runningSystems;
        private static ISyncTargetRegistry? _syncTargetRegistry;
        private static ISyncHandlerRegistry? _syncHandlerRegistry;

#if ZENECS_TRACE
        private static EcsTraceCenter _traceCenter;
#endif

        /// <summary>Raised whenever World/Systems set changes (Attach/Detach).</summary>
        public static event Action? Changed;

        /// <summary>Raised when SyncTargetRegistry is attached.</summary>
        public static event Action? ChangedSyncTargetRegistry;

        /// <summary>Raised when SyncHandlerRegistry is attached.</summary>
        public static event Action? ChangedSyncHandlerRegistry;

        // Public snapshots (never return null)
        public static World? World
        {
            get { lock (_gate) return _world; }
        }

        public static IReadOnlyList<ISystem> Systems
        {
            get { lock (_gate) return _systems ?? []; }
        }

        public static IReadOnlyList<ISystem> RunningSystems
        {
            get { lock (_gate) return _runningSystems ?? []; }
        }

        public static ISyncTargetRegistry? SyncTargetRegistry
        {
            get { lock (_gate) return _syncTargetRegistry; }
        }

        public static ISyncHandlerRegistry? SyncHandlerRegistry
        {
            get { lock (_gate) return _syncHandlerRegistry; }
        }

#if ZENECS_TRACE
        public static EcsTraceCenter TraceCenter
        {
            get { lock (_gate) return _traceCenter; }
        }

        /// <summary>Attach a trace center (editor/runtime diagnostics).</summary>
        public static void AttachTraceCenter(EcsTraceCenter traceCenter)
        {
            Action changed;
            lock (_gate)
            {
                _traceCenter = traceCenter;
                // Reuse Changed to keep listeners simple (optional, comment out if undesired)
                changed = Changed;
            }
            changed?.Invoke();
        }
#endif

        /// <summary>
        /// Attach the active World and its systems. Calculates RunningSystems fast without LINQ.
        /// </summary>
        public static void Attach(World world, IReadOnlyList<ISystem> systems)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (systems == null) throw new ArgumentNullException(nameof(systems));

            Action? changed;
            lock (_gate)
            {
                _world   = world;
                _systems = CopyToArray(systems);
                _runningSystems = ExtractRunningSystems(_systems);
                changed = Changed;
            }
            changed?.Invoke();
        }

        /// <summary>
        /// Attach the SyncTarget registry (typically provided by the Unity Adapter).
        /// </summary>
        public static void AttachSyncTargetRegistry(ISyncTargetRegistry syncTargetRegistry)
        {
            Action? changedEvt;
            lock (_gate)
            {
                _syncTargetRegistry = syncTargetRegistry;
                changedEvt = ChangedSyncTargetRegistry;
            }
            changedEvt?.Invoke();
        }

        /// <summary>
        /// Attach the SyncHandler registry (typically provided by the Unity Adapter).
        /// </summary>
        public static void AttachSyncHandlerRegistry(ISyncHandlerRegistry syncHandlerRegistry)
        {
            Action? changedEvt;
            lock (_gate)
            {
                _syncHandlerRegistry = syncHandlerRegistry;
                changedEvt = ChangedSyncHandlerRegistry;
            }
            changedEvt?.Invoke();
        }

        /// <summary>
        /// Clear all references. Safe to call multiple times.
        /// </summary>
        public static void Detach()
        {
            Action? changed;
            lock (_gate)
            {
                _world = null;
                _systems = null;
                _runningSystems = null;
                _syncTargetRegistry = null;
                _syncHandlerRegistry = null;
#if ZENECS_TRACE
                _traceCenter = null;
#endif
                changed = Changed;
            }
            changed?.Invoke();
        }

        // -------- helpers --------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ISystem[] CopyToArray(IReadOnlyList<ISystem> src)
        {
            var len = src.Count;
            if (len == 0) return Array.Empty<ISystem>();
            var dst = new ISystem[len];
            for (int i = 0; i < len; i++) dst[i] = src[i];
            return dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ISystem[] ExtractRunningSystems(ISystem[]? all)
        {
            if (all == null || all.Length == 0) return Array.Empty<ISystem>();

            // Count first to allocate once.
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (IsRunningKind(all[i])) count++;
            }

            if (count == 0) return [];

            var dst = new ISystem[count];
            var j = 0;
            foreach (var s in all)
            {
                if (IsRunningKind(s)) dst[j++] = s;
            }
            return dst;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsRunningKind(ISystem s)
                => s is IRunSystem or IFixedRunSystem or ILateRunSystem;
        }
    }
}
