// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: Entity.cs
// Purpose: Lightweight 64-bit entity handle (generation|id) and helpers.
// Key concepts:
//   • Upper 32 bits: generation; lower 32 bits: id.
//   • Provides equality/validity semantics.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Packed 64-bit entity handle:
    /// <code>[ Gen (32 bits) | Id (32 bits) ]</code>
    /// <para>
    /// Value-type identifier used to address entities inside a <see cref="World"/>.
    /// All data access should go through the world to validate liveness/generation.
    /// </para>
    /// </summary>
    public readonly struct Entity : IEquatable<Entity>
    {
        /// <summary>
        /// The raw 64-bit handle value (upper 32 bits = generation, lower 32 bits = id).
        /// </summary>
        public readonly ulong Handle;

        /// <summary>Bit shift amount for extracting the generation (upper 32 bits).</summary>
        public const int GenShift = 32;

        /// <summary>Mask for the lower 32 bits (entity id).</summary>
        public const ulong IdMask  = 0x00000000_FFFFFFFFUL;

        /// <summary>Mask for the upper 32 bits (generation).</summary>
        public const ulong GenMask = 0xFFFFFFFF_00000000UL;

        /// <summary>
        /// Gets the entity id (lower 32 bits).
        /// </summary>
        public int Id  => (int)(Handle & IdMask);

        /// <summary>
        /// Gets the generation (upper 32 bits).
        /// </summary>
        public int Gen => (int)(Handle >> GenShift);

        /// <summary>
        /// Creates a new packed handle from an <paramref name="id"/> and <paramref name="gen"/>.
        /// </summary>
        /// <param name="id">Entity id (0-based index into world storage).</param>
        /// <param name="gen">Entity generation; increments when an id is recycled.</param>
        public Entity(int id, int gen)
        {
            Handle = Pack(id, gen);
        }

        /// <summary>
        /// Packs an id and generation into a 64-bit handle.
        /// </summary>
        /// <param name="id">Entity id (lower 32 bits).</param>
        /// <param name="gen">Generation (upper 32 bits).</param>
        /// <returns>64-bit packed handle.</returns>
        public static ulong Pack(int id, int gen)
        {
            // Lower 32 bits = Id, Upper 32 bits = Generation
            return ((ulong)(uint)gen << GenShift) | (uint)id;
        }

        /// <summary>
        /// Unpacks a 64-bit handle into its (<c>id</c>, <c>gen</c>) components.
        /// </summary>
        /// <param name="handle">The raw 64-bit handle.</param>
        /// <returns>Tuple: (<c>id</c>, <c>gen</c>).</returns>
        public static (int id, int gen) Unpack(ulong handle)
        {
            return ((int)(handle & IdMask), (int)(handle >> GenShift));
        }

        /// <inheritdoc />
        public bool Equals(Entity other) => Handle == other.Handle;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Entity e && e.Handle == Handle;

        /// <inheritdoc />
        public override int GetHashCode() => Handle.GetHashCode();

        /// <summary>
        /// Returns a human-readable string showing id and generation.
        /// </summary>
        public override string ToString() => $"Entity({Id}:{Gen})";

        /// <summary>
        /// Explicitly converts an <see cref="Entity"/> to its id (lower 32 bits).
        /// Useful when only the id is required (e.g., array indexing).
        /// </summary>
        public static explicit operator int(Entity e) => e.Id;

        /// <summary>
        /// Explicitly converts an <see cref="Entity"/> to its raw 64-bit handle.
        /// Useful for serialization or dictionary keys.
        /// </summary>
        public static explicit operator ulong(Entity e) => e.Handle;
    }
}
