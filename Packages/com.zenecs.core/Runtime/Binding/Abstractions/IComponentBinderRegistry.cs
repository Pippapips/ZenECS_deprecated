// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentBinderRegistry.cs
// Purpose: Registry that maps component types to their binders (factory or singleton).
// Key concepts:
//   • Factory: creates a new binder instance on resolve.
//   • Singleton: always returns the same binder instance (only for stateless binders).
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
    /// Registry that maps component types to <see cref="IComponentBinder"/> instances.
    /// Supports either per-resolve factory or a singleton instance registration.
    /// </summary>
    public interface IComponentBinderRegistry : IDisposable
    {
        /// <summary>Registers a factory that creates a binder for component type <typeparamref name="T"/>.</summary>
        void RegisterFactory<T>(Func<IComponentBinder> factory);

        /// <summary>Registers a singleton binder instance for component type <typeparamref name="T"/>.</summary>
        /// <remarks>Use only for stateless binders.</remarks>
        void RegisterSingleton<T>(IComponentBinder instance);

        /// <summary>Resolves a binder by component type; may construct via factory. Returns <c>null</c> if none.</summary>
        IComponentBinder? Resolve(Type componentType);

        /// <summary>Resolves a binder by generic component type; may construct via factory. Returns <c>null</c> if none.</summary>
        IComponentBinder? Resolve<T>();

        /// <summary>Unregisters either the factory or singleton for the given component type. Returns <c>true</c> on success.</summary>
        bool Unregister(Type componentType);

        /// <summary>Clears all registrations; disposes singleton instances implementing <see cref="IDisposable"/>.</summary>
        void Clear();
    }
}