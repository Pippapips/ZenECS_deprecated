// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: BinaryComponentFormatter.cs
// Purpose: Base class for binary serialization formatters of components.
// Key concepts:
//   • Implements both generic and non-generic IComponentFormatter contracts.
//   • Used by snapshot backends for compact binary storage.
//   • Derived formatters must define ReadTyped / Write for specific structs.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core.Serialization.Formats.Binary
{
    /// <summary>
    /// Abstract base for binary formatters providing typed serialization
    /// and boxed fallback through <see cref="IComponentFormatter"/>.
    /// </summary>
    public abstract class BinaryComponentFormatter<T> : IComponentFormatter<T> where T : struct
    {
        /// <inheritdoc />
        public Type ComponentType => typeof(T);

        /// <inheritdoc />
        public abstract void Write(in T value, ISnapshotBackend backend);

        /// <inheritdoc />
        public abstract T ReadTyped(ISnapshotBackend backend);

        void IComponentFormatter.Write(object boxed, ISnapshotBackend backend)
            => Write((T)boxed, backend);

        object IComponentFormatter.Read(ISnapshotBackend backend)
            => ReadTyped(backend);
    }
}