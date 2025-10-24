// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentBinder.cs
// Purpose: View-binding contract for a single component type (non-generic shape).
// Key concepts:
//   • Used by UI/Presentation layers to bind, apply, and unbind a component's data.
//   • Complements IComponentBinder<T> for non-generic dispatch paths.
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
    /// Non-generic binder interface used for runtime dispatch when the component type
    /// is known only at runtime.
    /// </summary>
    public interface IComponentBinder
    {
        /// <summary>The concrete component type this binder handles.</summary>
        Type ComponentType { get; }

        /// <summary>Binds the view to the entity for this component type.</summary>
        void Bind(World w, Entity e, IViewBinder v);

        /// <summary>Applies a boxed component value to the bound view.</summary>
        void Apply(World w, Entity e, object value, IViewBinder v);

        /// <summary>Unbinds the view from the entity.</summary>
        void Unbind(World w, Entity e, IViewBinder v);
    }
}