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

namespace ZenECS.Core.Infrastructure
{
    /// <summary>
    /// Static directory used to attach optional runtime services (registries/gates) at startup.
    /// Consumers query this directory instead of referencing concrete types directly.
    /// </summary>
    public static class EcsRuntimeDirectory
    {
    }
}
