// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IProjection.cs
// Purpose: View-model style reconciler that syncs ECS state to a binder using binders registry.
// Key concepts:
//   • Implements a one-shot reconciliation step per entity.
//   • Uses IComponentBinderResolver to fetch concrete binders.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable

namespace ZenECS.Core.Binding
{
    /// <summary>
    /// Performs a reconciliation step between ECS state and a view binder for an entity.
    /// </summary>
    internal interface IProjection
    {
        /// <summary>
        /// Syncs world state for <paramref name="e"/> into <paramref name="v"/> using the provided resolver.
        /// </summary>
        void Reconcile(World w, Entity e, IViewBinder v, IComponentBinderResolver resolver);
    }
}