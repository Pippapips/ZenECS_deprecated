// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ZenComponentAttribute.cs
// Purpose: Editor/tooling-only attribute for component metadata collection.
// Key concepts:
//   • Excluded from runtime builds (guarded by UNITY_EDITOR).
//   • Optional StableId can be used for save/load or networking pipelines.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Diagnostics; // Conditional

namespace ZenECS.Adapter.Unity.Attributes
{
    /// <summary>
    /// Attribute used by editor/tooling to collect component metadata.
    /// This attribute is excluded from runtime builds.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenComponentAttribute : Attribute
    {
        /// <summary>
        /// Optional stable identifier for serialization/networking.
        /// </summary>
        public string? StableId { get; set; }
    }
}