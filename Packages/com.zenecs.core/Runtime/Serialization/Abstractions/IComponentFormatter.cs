// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: IComponentFormatter.cs
// Purpose: Defines serializers for component values used by snapshot backends.
// Key concepts:
//   • Non-generic and generic forms (boxed vs strongly-typed).
//   • Backends abstract the actual storage (stream, memory buffer, etc.).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Non-generic formatter contract for component values.
    /// Implementations should be symmetric (Write → Read) and version-tolerant when possible.
    /// </summary>
    public interface IComponentFormatter
    {
        /// <summary>The concrete component type handled by this formatter.</summary>
        Type ComponentType { get; }

        /// <summary>Writes a boxed component instance to the snapshot backend.</summary>
        void Write(object boxed, ISnapshotBackend backend);

        /// <summary>Reads a boxed component instance from the snapshot backend.</summary>
        object Read(ISnapshotBackend backend);
    }

    /// <summary>
    /// Strongly-typed formatter variant that avoids boxing and exposes typed read/write.
    /// </summary>
    public interface IComponentFormatter<T> : IComponentFormatter where T : struct
    {
        /// <summary>Writes a typed component value to the backend.</summary>
        void Write(in T value, ISnapshotBackend backend);

        /// <summary>Reads a typed component value from the backend.</summary>
        T ReadTyped(ISnapshotBackend backend);
    }
}