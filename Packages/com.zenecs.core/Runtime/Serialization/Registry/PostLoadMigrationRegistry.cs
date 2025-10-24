// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: PostLoadMigrationRegistry.cs
// Purpose: Holds and executes post-load migration tasks after world snapshot loading.
// Key concepts:
//   • Ensures unique registration by type to avoid duplicates.
//   • Executes migrations in ascending Order, then by type name for stability.
//   • Provides Clear() for reset/testing environments.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Central registry for post-load migrations executed after snapshot load.
    /// </summary>
    public static class PostLoadMigrationRegistry
    {
        private static readonly List<IPostLoadMigration> _migs = new();
        private static readonly HashSet<Type> _migTypes = new();

        /// <summary>
        /// Registers a migration. Ignores duplicates (same type).
        /// </summary>
        public static bool Register(IPostLoadMigration mig)
        {
            if (mig == null) return false;
            var t = mig.GetType();
            if (_migTypes.Contains(t)) return false;

            _migTypes.Add(t);
            _migs.Add(mig);
            return true;
        }

        /// <summary>
        /// Checks whether a migration of type <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : IPostLoadMigration
            => _migTypes.Contains(typeof(T));

        /// <summary>
        /// Creates and registers a migration only if it has not been registered yet.
        /// </summary>
        public static bool EnsureRegistered<T>(Func<T> factory) where T : class, IPostLoadMigration
        {
            if (IsRegistered<T>()) return false;
            var instance = factory();
            return Register(instance);
        }

        /// <summary>
        /// Runs all registered migrations ordered by Order and type name (for deterministic execution).
        /// </summary>
        public static void RunAll(World world)
        {
            if (_migs.Count == 0) return;

            foreach (var m in _migs
                         .OrderBy(m => m.Order)
                         .ThenBy(m => m.GetType().FullName, StringComparer.Ordinal))
            {
                m.Run(world);
            }
        }

        /// <summary>
        /// Clears all registered migrations and types (for testing or reset).
        /// </summary>
        public static void Clear()
        {
            _migs.Clear();
            _migTypes.Clear();
        }
    }
}
