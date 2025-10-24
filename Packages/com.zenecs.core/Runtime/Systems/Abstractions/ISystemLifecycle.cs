// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: ISystemLifecycle.cs
// Purpose: Defines optional lifecycle hooks for systems (Initialize/Shutdown).
// Key concepts:
//   • Allows setup/teardown logic to run before and after system execution.
//   • Called by SystemRunner during InitializeSystems() and ShutdownSystems().
//   • Systems implementing this interface should be idempotent and stateless.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Provides optional lifecycle hooks for systems that require initialization or teardown logic.
    /// </summary>
    public interface ISystemLifecycle : ISystem
    {
        /// <summary>
        /// Called once when the system runner initializes all systems.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        void Initialize(World w);

        /// <summary>
        /// Called once when the system runner shuts down all systems.
        /// </summary>
        /// <param name="w">The ECS world instance.</param>
        void Shutdown(World w);
    }
}