// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: StreamSnapshotBackend.cs
// Purpose: Stream-based implementation of ISnapshotBackend.
// Key concepts:
//   • Provides binary read/write over .NET Stream with UTF8 encoding.
//   • Supports both read-only and write-only modes.
//   • Handles primitive and string types with strict length validation.
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Stream-backed snapshot backend used for reading/writing component data.
    /// Little-endian by default. Supports both read and write operations.
    /// </summary>
    internal sealed class StreamSnapshotBackend : ISnapshotBackend, IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryWriter? _bw;
        private readonly BinaryReader? _br;
        private readonly bool _writeMode;

        public StreamSnapshotBackend(Stream stream, bool writable, bool leaveOpen = true, Encoding? encoding = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _writeMode = writable;
            encoding ??= Encoding.UTF8;

            if (writable)
                _bw = new BinaryWriter(stream, encoding, leaveOpen);
            else
                _br = new BinaryReader(stream, encoding, leaveOpen);
        }

        public void Dispose()
        {
            _bw?.Dispose();
            _br?.Dispose();
        }

        // ---- Byte block operations ---------------------------------------------------
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            EnsureWrite();
            if (data.Length > 0) _bw!.Write(data);
        }

        public void ReadBytes(Span<byte> dst, int length)
        {
            EnsureRead();
            int read = _br!.Read(dst[..length]);
            if (read != length) throw new EndOfStreamException();
        }

        // ---- Primitive read/write ----------------------------------------------------
        public void WriteInt(int v) { EnsureWrite(); _bw!.Write(v); }
        public int ReadInt() { EnsureRead(); return _br!.ReadInt32(); }

        public void WriteUInt(uint v) { EnsureWrite(); _bw!.Write(v); }
        public uint ReadUInt() { EnsureRead(); return _br!.ReadUInt32(); }

        public void WriteFloat(float v) { EnsureWrite(); _bw!.Write(v); }
        public float ReadFloat() { EnsureRead(); return _br!.ReadSingle(); }

        public void WriteBool(bool v) { EnsureWrite(); _bw!.Write(v); }
        public bool ReadBool() { EnsureRead(); return _br!.ReadBoolean(); }

        public void WriteString(string s)
        {
            EnsureWrite();
            s ??= string.Empty;
            var bytes = Encoding.UTF8.GetBytes(s);
            _bw!.Write(bytes.Length);
            if (bytes.Length > 0) _bw.Write(bytes);
        }

        public string ReadString()
        {
            EnsureRead();
            int len = _br!.ReadInt32();
            if (len == 0) return string.Empty;
            var buf = new byte[len];
            int read = _br.Read(buf, 0, len);
            if (read != len) throw new EndOfStreamException();
            return Encoding.UTF8.GetString(buf);
        }

        // ---- Cursor management -------------------------------------------------------
        public long Position { get => _stream.Position; set => _stream.Position = value; }
        public long Length => _stream.Length;
        public void Rewind() => _stream.Position = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWrite()
        {
            if (!_writeMode) throw new InvalidOperationException("Backend is read-only.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureRead()
        {
            if (_writeMode) throw new InvalidOperationException("Backend is write-only.");
        }

        public void Flush() => _bw?.Flush();
    }
}
