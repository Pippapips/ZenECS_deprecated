// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ZenFormatterForAttribute.cs
// Purpose: Editor/tooling-only attribute that declares which component a formatter handles.
// Key concepts:
//   • Excluded from runtime builds (guarded by UNITY_EDITOR).
//   • Associates a component type with a stable serialization format identifier.
//   • Supports marking the latest/default format used when saving.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Diagnostics; // Conditional

namespace ZenECS.Core
{
    /// <summary>
    /// Attribute used by editor/tooling to register a formatter for a specific component type.
    /// Excluded from runtime builds via <c>UNITY_EDITOR</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ZenFormatterForAttribute : Attribute
    {
        /// <summary>
        /// The component type this formatter is responsible for.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Stable identifier of the data format handled by this formatter
        /// (e.g., <c>com.game.position.v2</c>).
        /// </summary>
        public string StableId { get; }

        /// <summary>
        /// Indicates whether this formatter represents the latest/default format to use when saving.
        /// </summary>
        public bool IsLatest { get; }

        /// <summary>
        /// Creates a new formatter mapping.
        /// </summary>
        /// <param name="componentType">Target component type.</param>
        /// <param name="stableId">Stable ID of the serialization format (non-empty).</param>
        /// <param name="isLatest">Whether this is the latest/default format when saving.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="componentType"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="stableId"/> is null or whitespace.</exception>
        public ZenFormatterForAttribute(Type componentType, string stableId, bool isLatest = false)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            StableId = !string.IsNullOrWhiteSpace(stableId)
                ? stableId
                : throw new ArgumentException("StableId must be non-empty.", nameof(stableId));
            IsLatest = isLatest;
        }
    }
}
