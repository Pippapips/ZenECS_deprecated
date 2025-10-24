// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentBinderRegistry.cs
// Purpose: Default registry mapping component types to their view binders (factory or singleton).
// Key concepts:
//   • Dictionary-based registry intended for main-thread use (swap to concurrent/locks if needed).
//   • Singleton registrations take precedence over factories during resolve.
//   • Disposes registered singletons on Clear/Dispose when they implement IDisposable.
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
    /// Default implementation of <see cref="IComponentBinderRegistry"/> that stores
    /// component-type → binder mappings. Designed for main-thread usage; if you need
    /// multithreaded access, replace the dictionaries with concurrent variants or add locks.
    /// </summary>
    public sealed class ComponentBinderRegistry : IComponentBinderRegistry
    {
        private readonly HashSet<Type> _registeredTypes = new();

        /// <summary>
        /// Component type → factory mapping (creates a new instance on each resolve).
        /// </summary>
        private readonly Dictionary<Type, Func<IComponentBinder>> _factories = new();

        /// <summary>
        /// Component type → singleton mapping (always returns the same instance).
        /// </summary>
        private readonly Dictionary<Type, IComponentBinder> _singletons = new();

        private bool _disposed;

        public ComponentBinderRegistry()
        {
            Infrastructure.EcsRuntimeDirectory.AttachComponentBinderRegistry(this);
        }

        /// <summary>
        /// Registers a factory that produces a binder instance for component type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// If a singleton already exists for <typeparamref name="T"/>, the current policy keeps it.
        /// Adjust here if you want factory registration to replace existing singletons.
        /// </remarks>
        public void RegisterFactory<T>(Func<IComponentBinder> factory)
        {
            _registeredTypes.Add(typeof(T));
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            ThrowIfDisposed();

            _factories[typeof(T)] = () => factory();
            // When a factory is registered while a singleton exists, we keep both;
            // singleton will take precedence on resolve (see Resolve implementation).
        }

        /// <summary>
        /// Registers a singleton binder instance for component type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// Singleton precedence: if both a singleton and a factory are registered,
        /// <see cref="Resolve(Type)"/> will return the singleton first.
        /// </remarks>
        public void RegisterSingleton<T>(IComponentBinder instance)
        {
            if (instance is null) throw new ArgumentNullException(nameof(instance));
            ThrowIfDisposed();

            _registeredTypes.Add(typeof(T));
            _singletons[typeof(T)] = instance;
        }

        /// <summary>
        /// Resolves a binder by runtime component type.
        /// </summary>
        /// <returns>
        /// The registered singleton (if any), otherwise a new instance from the factory (if any),
        /// or <c>null</c> if not registered.
        /// </returns>
        public IComponentBinder? Resolve(Type componentType)
        {
            if (componentType is null) throw new ArgumentNullException(nameof(componentType));
            ThrowIfDisposed();

            // 1) Prefer singleton
            if (_singletons.TryGetValue(componentType, out var inst))
                return inst;

            // 2) Otherwise construct from factory
            if (_factories.TryGetValue(componentType, out var f))
                return f();

            // 3) Unregistered → null
            return null;
        }

        /// <summary>
        /// Resolves a binder by generic component type.
        /// </summary>
        public IComponentBinder? Resolve<T>()
            => Resolve(typeof(T));

        /// <summary>
        /// Unregisters factory/singleton for the type. Disposes the singleton if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <returns><c>true</c> if any registration was removed; otherwise <c>false</c>.</returns>
        public bool Unregister(Type componentType)
        {
            ThrowIfDisposed();
            var removed = false;

            if (_singletons.Remove(componentType, out var singleton))
            {
                (singleton as IDisposable)?.Dispose();
                removed = true;
            }

            removed |= _factories.Remove(componentType);

            // If neither mapping remains, drop from the registered set.
            if (!_singletons.ContainsKey(componentType) && !_factories.ContainsKey(componentType))
                _registeredTypes.Remove(componentType);

            return removed;
        }

        /// <summary>
        /// Clears all registrations. Disposes singleton instances that implement <see cref="IDisposable"/>.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();

            foreach (var s in _singletons.Values)
                (s as IDisposable)?.Dispose();

            _singletons.Clear();
            _factories.Clear();
            _registeredTypes.Clear();
        }

        /// <summary>
        /// Snapshot view of all component types currently registered.
        /// </summary>
        public IReadOnlyCollection<Type> RegisteredComponentTypes => _registeredTypes;

        /// <summary>
        /// Try-pattern resolver combining both singleton and factory paths.
        /// </summary>
        public bool TryResolve(Type componentType, out IComponentBinder binder)
        {
            if (_singletons.TryGetValue(componentType, out var s))
            {
                binder = s;
                return true;
            }
            if (_factories.TryGetValue(componentType, out var f))
            {
                binder = f();
                return true;
            }
            binder = default!;
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ComponentBinderRegistry));
        }
    }
}
