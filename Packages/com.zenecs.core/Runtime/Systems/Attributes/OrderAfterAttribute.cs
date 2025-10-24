// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: OrderAfterAttribute.cs
// Purpose: Specifies execution order constraints between systems.
// Key concepts:
//   • Declares that this system must run <b>after</b> another target system.
//   • Multiple attributes can be used to define multiple dependencies.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Declares that this system should execute <b>after</b> the specified target system type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderAfterAttribute : Attribute
    {
        /// <summary>The target system type that must execute before this one.</summary>
        public Type Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderAfterAttribute"/> class.
        /// </summary>
        /// <param name="target">The target system type to execute before this system.</param>
        public OrderAfterAttribute(Type target) => Target = target;
    }
}