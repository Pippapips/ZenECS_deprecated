// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentChangeRecord.cs
// Purpose: Lightweight record describing a single component change on an entity.
// Key concepts:
//   • Bitmask-based change kind (Added / Changed / Removed).
//   • Immutable value type suitable for batching and event feeds.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Bitmask describing the kind of component change observed.
    /// Multiple flags may be combined if a component was added and changed in the same frame, etc.
    /// </summary>
    [Flags]
    public enum ComponentChangeMask
    {
        /// <summary>The component was added to the entity.</summary>
        Added   = 1,

        /// <summary>The component value was modified on the entity.</summary>
        Changed = 2,

        /// <summary>The component was removed from the entity.</summary>
        Removed = 4
    }

    /// <summary>
    /// Immutable record that captures which component on which entity changed and how.
    /// </summary>
    public readonly struct ComponentChangeRecord
    {
        /// <summary>The target entity.</summary>
        public readonly Entity Entity;

        /// <summary>The concrete component <see cref="Type"/> that changed.</summary>
        public readonly Type ComponentType;

        /// <summary>The change mask (see <see cref="ComponentChangeMask"/>).</summary>
        public readonly ComponentChangeMask Mask;

        /// <summary>
        /// Creates a new change record.
        /// </summary>
        /// <param name="e">The entity whose component changed.</param>
        /// <param name="t">The component type.</param>
        /// <param name="m">The change mask.</param>
        public ComponentChangeRecord(Entity e, Type t, ComponentChangeMask m)
        {
            Entity = e;
            ComponentType = t;
            Mask = m;
        }
    }
}
