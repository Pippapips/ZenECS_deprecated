// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IViewBinder.cs
// Purpose: Minimal handle for binding a UI/view to an ECS Entity.
// Key concepts:
//   • Stores the associated Entity and an implementation-specific handle ID.
//   • Allows rebinding to a different entity at runtime.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Minimal view binder contract that associates a view with an ECS <see cref="Entity"/>.
    /// </summary>
    public interface IViewBinder
    {
        /// <summary>The currently bound entity.</summary>
        Entity Entity { get; }

        /// <summary>Implementation-specific handle ID for the view.</summary>
        int HandleId { get; }

        /// <summary>Rebinds this view binder to a different entity.</summary>
        void SetEntity(Entity e);
    }
}