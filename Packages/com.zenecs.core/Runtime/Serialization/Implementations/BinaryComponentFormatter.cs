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
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;

namespace ZenECS.Core.Serialization.Formats.Binary
{
    /// <summary>
    /// Base class for binary component formatters that provides a strongly-typed
    /// serialization API and a boxed fallback through the non-generic
    /// <see cref="IComponentFormatter"/> interface.
    /// </summary>
    /// <typeparam name="T">
    /// The component value type to serialize. This should be a blittable or
    /// otherwise compact <see langword="struct"/> for best performance.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Implementors must provide <see cref="Write(in T, ISnapshotBackend)"/> and
    /// <see cref="ReadTyped(ISnapshotBackend)"/>. The explicit interface implementations
    /// map boxed calls to the typed versions to keep a single serialization code path.
    /// </para>
    /// <para>
    /// For maximum forward compatibility, prefer an explicit, versioned field layout
    /// (e.g., write a small header or field count) rather than relying purely on
    /// <see cref="System.Runtime.InteropServices.MemoryMarshal"/> tricks.
    /// </para>
    /// </remarks>
    public abstract class BinaryComponentFormatter<T> : IComponentFormatter<T> where T : struct
    {
        /// <summary>
        /// Gets the component <see cref="Type"/> handled by this formatter.
        /// </summary>
        /// <remarks>
        /// Used by registries and snapshot systems to resolve the correct formatter at runtime.
        /// </remarks>
        public Type ComponentType => typeof(T);

        /// <summary>
        /// Writes the component value to the given snapshot <paramref name="backend"/> in a compact
        /// binary form.
        /// </summary>
        /// <param name="value">Component value to serialize.</param>
        /// <param name="backend">Snapshot backend that provides the binary sink.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="backend"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// Implementations should be deterministic and stable across versions, or emit enough
        /// metadata to support migrations.
        /// </remarks>
        public abstract void Write(in T value, ISnapshotBackend backend);

        /// <summary>
        /// Reads a component value of type <typeparamref name="T"/> from the given snapshot
        /// <paramref name="backend"/>.
        /// </summary>
        /// <param name="backend">Snapshot backend that provides the binary source.</param>
        /// <returns>The deserialized component value.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="backend"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.IO.EndOfStreamException">
        /// Thrown when insufficient data is available to complete the read.
        /// </exception>
        public abstract T ReadTyped(ISnapshotBackend backend);

        /// <summary>
        /// Non-generic write entrypoint for boxed component values.
        /// Forwards to <see cref="Write(in T, ISnapshotBackend)"/>.
        /// </summary>
        /// <param name="boxed">Boxed instance of <typeparamref name="T"/>.</param>
        /// <param name="backend">Snapshot backend that provides the binary sink.</param>
        /// <exception cref="InvalidCastException">
        /// Thrown when <paramref name="boxed"/> is not a <typeparamref name="T"/>.
        /// </exception>
        void IComponentFormatter.Write(object boxed, ISnapshotBackend backend)
            => Write((T)boxed, backend);

        /// <summary>
        /// Non-generic read entrypoint that returns a boxed component value.
        /// Forwards to <see cref="ReadTyped(ISnapshotBackend)"/>.
        /// </summary>
        /// <param name="backend">Snapshot backend that provides the binary source.</param>
        /// <returns>A boxed <typeparamref name="T"/> value.</returns>
        object IComponentFormatter.Read(ISnapshotBackend backend)
            => ReadTyped(backend);
    }
}
