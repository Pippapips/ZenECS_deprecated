// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: EcsRuntimeDirectory.cs
// Purpose: Global discovery surface for optional runtime services (registries, gates, etc.).
// Key concepts:
//   • Thin static directory to attach/detach services at startup.
//   • Keeps core assemblies decoupled from optional infrastructure packages.
//   • Safe no-op defaults when a service was not attached.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using ZenECS.Core.Binding;
using ZenECS.Core.Binding.Util;

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Static directory used to attach optional runtime services (registries/gates) at startup.
    /// Consumers query this directory instead of referencing concrete types directly.
    /// </summary>
    public static class EcsRuntimeDirectory
    {
        // ---- Binding & Sync registries -------------------------------------------------

        /// <summary>Component → binder registry (maybe <c>null</c> if not attached).</summary>
        public static IComponentBinderRegistry? ComponentBinderRegistry { get; private set; }

        /// <summary>Entity → view binder registry (maybe <c>null</c> if not attached).</summary>
        public static IViewBinderRegistry? ViewBinderRegistry { get; private set; }

        /// <summary>Main-thread marshaling gate (maybe <c>null</c> if not attached).</summary>
        public static IMainThreadGate? MainThreadGate { get; private set; }

        // ---- Attach helpers ------------------------------------------------------------

        /// <summary>Attaches a component binder registry for global discovery.</summary>
        public static void AttachComponentBinderRegistry(IComponentBinderRegistry reg) => ComponentBinderRegistry = reg;

        /// <summary>Attaches a sync-target (view binder) registry for global discovery.</summary>
        public static void AttachViewBinderRegistry(IViewBinderRegistry reg) => ViewBinderRegistry = reg;

        /// <summary>Attaches the main-thread gate implementation.</summary>
        public static void AttachMainThreadGate(IMainThreadGate gate) => MainThreadGate = gate;

        // ---- Detach helpers ------------------------------------------------------------

        /// <summary>Detaches the currently attached component binder registry.</summary>
        public static void DetachComponentBinderRegistry(IComponentBinderRegistry reg)
        {
            if (ComponentBinderRegistry == reg) ComponentBinderRegistry = null;
        }

        /// <summary>Detaches the currently attached view binder registry.</summary>
        public static void DetachViewBinderRegistry(IViewBinderRegistry reg)
        {
            if (ViewBinderRegistry == reg) ViewBinderRegistry = null;
        }

        /// <summary>Detaches the current main-thread gate if the instance matches.</summary>
        public static void DetachMainThreadGate(IMainThreadGate gate)
        {
            if (MainThreadGate == gate) MainThreadGate = null;
        }

        // ---- Reset --------------------------------------------------------------------

        /// <summary>
        /// Clears all attached services. Call during full shutdown/reset to avoid stale references.
        /// </summary>
        public static void Reset()
        {
            ComponentBinderRegistry = null;
            ViewBinderRegistry = null;
            MainThreadGate = null;
        }
    }
}
