// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: OrderBeforeAttribute.cs
// Purpose: Specifies execution order constraints between systems.
// Key concepts:
//   • Declares that this system must run <b>before</b> another target system.
//   • Multiple attributes can be used to define multiple dependencies.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Declares that this system should execute <b>before</b> the specified target system type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class OrderBeforeAttribute : Attribute
    {
        /// <summary>The target system type that must execute after this one.</summary>
        public Type Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBeforeAttribute"/> class.
        /// </summary>
        /// <param name="target">The target system type to execute after this system.</param>
        public OrderBeforeAttribute(Type target) => Target = target;
    }
}