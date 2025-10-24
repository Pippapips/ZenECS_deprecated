// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IViewBinderRegistry.cs
// Purpose: Tracks active view binders by entity, with registration events.
// Key concepts:
//   • Supports replace-if-exists registration semantics.
//   • Emits a Registered event to simplify subscription wiring.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Registry for associating entities with active view binders.
    /// </summary>
    public interface IViewBinderRegistry
    {
        /// <summary>Raised when a binder is registered for an entity.</summary>
        event Action<Entity, IViewBinder> Registered;

        /// <summary>Registers a binder for an entity; optionally replaces an existing binder.</summary>
        bool Register(Entity e, IViewBinder v, bool replaceIfExists = true);

        /// <summary>Unregisters a binder for an entity; returns <c>true</c> if the target was removed.</summary>
        bool Unregister(Entity e, IViewBinder target);

        /// <summary>Resolves the currently registered binder for an entity, or <c>null</c> if none.</summary>
        IViewBinder? Resolve(Entity e);

        /// <summary>Clears all registrations.</summary>
        void Clear();
    }
}