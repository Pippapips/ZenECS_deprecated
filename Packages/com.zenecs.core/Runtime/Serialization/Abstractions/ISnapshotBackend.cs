// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ISnapshotBackend.cs
// Purpose: Abstracts I/O for snapshot read/write operations.
// Key concepts:
//   • Provides primitive read/write for bytes, numbers, strings, and booleans.
//   • Exposes stream-like Position/Length and Rewind capability.
//   • Implementations may wrap streams, memory buffers, or custom stores.
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
    /// Backend interface for snapshot I/O. Implementations should be deterministic and endian-stable.
    /// </summary>
    public interface ISnapshotBackend : IDisposable
    {
        // ---- Raw bytes -------------------------------------------------------
        void WriteBytes(ReadOnlySpan<byte> data);
        void ReadBytes(Span<byte> dst, int length);

        // ---- Primitives ------------------------------------------------------
        void WriteInt(int v);
        int  ReadInt();

        void WriteUInt(uint v);
        uint ReadUInt();

        void WriteFloat(float v);
        float ReadFloat();

        void WriteString(string s);
        string ReadString();

        void WriteBool(bool v);
        bool ReadBool();

        // ---- Cursor & length -------------------------------------------------
        /// <summary>Current cursor position.</summary>
        long Position { get; set; }

        /// <summary>Rewinds the cursor to the beginning.</summary>
        void Rewind();

        /// <summary>Total available length in the backend (if applicable).</summary>
        long Length { get; }
    }
}