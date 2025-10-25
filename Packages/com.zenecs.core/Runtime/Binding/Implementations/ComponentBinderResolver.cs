// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentBinderResolver.cs
// Purpose: Read-only facade that resolves component → binder using a registry.
// Key concepts:
//   • Delegates lookups to IComponentBinderRegistry (singleton/factory aware).
//   • Exposes a discovery view of registered component types when supported.
//   • Keeps API surface minimal for consumer code that only needs resolution.
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
    /// Read-only resolver that forwards lookups to an underlying
    /// <see cref="IComponentBinderRegistry"/> without exposing registration APIs.
    /// </summary>
    internal sealed class ComponentBinderResolver : IComponentBinderResolver
    {
        private readonly IComponentBinderRegistry _registry;

        /// <summary>
        /// Creates a resolver bound to the given registry.
        /// </summary>
        /// <param name="registry">The backing registry used for lookups.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> is null.</exception>
        public ComponentBinderResolver(IComponentBinderRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Attempts to resolve a binder for the specified component <see cref="Type"/>.
        /// </summary>
        /// <param name="componentType">Runtime component type.</param>
        /// <param name="binder">Resolved binder if found.</param>
        /// <returns><c>true</c> if a binder exists; otherwise <c>false</c>.</returns>
        public bool TryResolve(Type componentType, out IComponentBinder binder)
        {
            var b = _registry.Resolve(componentType);
            if (b is not null) { binder = b; return true; }
            binder = default!;
            return false;
        }

        /// <summary>
        /// Provides the set of registered component types when obtainable from the underlying registry.
        /// Returns an empty collection if the registry does not expose this view.
        /// </summary>
        public IReadOnlyCollection<Type> RegisteredComponentTypes
            => (_registry as ComponentBinderRegistry)?.RegisteredComponentTypes
               ?? Array.Empty<Type>();
    }
}
