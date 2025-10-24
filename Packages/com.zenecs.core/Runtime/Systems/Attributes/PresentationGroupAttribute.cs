// ──────────────────────────────────────────────────────────────────────────────
// ZenECS.Core.Systems
// File: PresentationGroupAttribute.cs
// Purpose: Defines the system group responsible for presentation.
// Key concepts:
//   • Includes rendering, UI updates, and data binding systems.
//   • Typically read-only — structural changes are discouraged.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using System;

namespace ZenECS.Core.Systems
{
    /// <summary>
    /// Marks a system as part of the <b>Presentation</b> group.
    /// <para>Used for rendering, UI updates, or view binding (read-only phase).</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PresentationGroupAttribute : Attribute { }
}