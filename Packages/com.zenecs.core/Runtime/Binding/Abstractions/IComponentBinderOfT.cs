// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentBinderOfT.cs
// Purpose: Type-safe view-binding contract for a specific component <T>.
// Key concepts:
//   • Provides ref-friendly Apply with no boxing.
//   • Typically wrapped by a non-generic adapter when needed.
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
    /// Generic binder interface for a specific component type <typeparamref name="T"/>.
    /// </summary>
    public interface IComponentBinder<T>
    {
        /// <summary>Binds the view to the entity for component <typeparamref name="T"/>.</summary>
        void Bind(World w, Entity e, IViewBinder v);

        /// <summary>Applies a component value to the view without boxing.</summary>
        void Apply(World w, Entity e, in T value, IViewBinder v);

        /// <summary>Unbinds the view from the entity.</summary>
        void Unbind(World w, Entity e, IViewBinder v);
    }
}