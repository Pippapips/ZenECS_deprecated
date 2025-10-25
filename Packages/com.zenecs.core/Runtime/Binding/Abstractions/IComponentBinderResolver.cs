// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentBinderResolver.cs
// Purpose: Read-only resolver facade for component binders.
// Key concepts:
//   • Decouples binders lookup from the concrete registry implementation.
//   • Exposes the set of registered component types for discovery.
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
    /// Read-only facade to resolve binders without exposing registration APIs.
    /// </summary>
    internal interface IComponentBinderResolver
    {
        /// <summary>Attempts to resolve a binder for the given component type.</summary>
        bool TryResolve(Type componentType, out IComponentBinder binder);

        /// <summary>Returns the collection of registered component types.</summary>
        IReadOnlyCollection<Type> RegisteredComponentTypes { get; }
    }
}