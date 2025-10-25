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
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Stream-backed snapshot backend used for reading and writing component data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This backend uses <see cref="BinaryReader"/> / <see cref="BinaryWriter"/> over a provided
    /// <see cref="Stream"/> (UTF-8 by default). It operates in either read-only or write-only mode,
    /// enforced at runtime via <see cref="EnsureRead"/> / <see cref="EnsureWrite"/>.
    /// </para>
    /// <para>
    /// All numeric primitives are handled in the platform default endianness used by
    /// <see cref="BinaryReader"/> / <see cref="BinaryWriter"/> (little-endian on common .NET platforms).
    /// </para>
    /// </remarks>
    internal sealed class StreamSnapshotBackend : ISnapshotBackend, IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryWriter? _bw;
        private readonly BinaryReader? _br;
        private readonly bool _writeMode;

        /// <summary>
        /// Creates a new <see cref="StreamSnapshotBackend"/> around the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The underlying stream used for I/O operations.</param>
        /// <param name="writable">
        /// <see langword="true"/> to enable write-only mode (uses <see cref="BinaryWriter"/>);
        /// <see langword="false"/> to enable read-only mode (uses <see cref="BinaryReader"/>).
        /// </param>
        /// <param name="leaveOpen">
        /// When <see langword="true"/>, the underlying stream is left open when this backend is disposed.
        /// </param>
        /// <param name="encoding">
        /// Text encoding for string serialization. Defaults to <see cref="Encoding.UTF8"/> when <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Disposes the underlying <see cref="BinaryReader"/> or <see cref="BinaryWriter"/>.
        /// </summary>
        /// <remarks>
        /// When constructed with <paramref name="leaveOpen"/> = <see langword="true"/>, the underlying
        /// <see cref="Stream"/> is not disposed.
        /// </remarks>
        public void Dispose()
        {
            _bw?.Dispose();
            _br?.Dispose();
        }

        // ---- Byte block operations ---------------------------------------------------

        /// <summary>
        /// Writes the provided byte span to the underlying stream.
        /// </summary>
        /// <param name="data">The bytes to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            EnsureWrite();
            if (data.Length > 0) _bw!.Write(data);
        }

        /// <summary>
        /// Reads exactly <paramref name="length"/> bytes from the underlying stream into <paramref name="dst"/>.
        /// </summary>
        /// <param name="dst">Destination span that will receive the bytes.</param>
        /// <param name="length">The exact number of bytes to read.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        /// <exception cref="EndOfStreamException">Thrown when the stream does not supply the requested <paramref name="length"/> bytes.</exception>
        public void ReadBytes(Span<byte> dst, int length)
        {
            EnsureRead();
            int read = _br!.Read(dst[..length]);
            if (read != length) throw new EndOfStreamException();
        }

        // ---- Primitive read/write ----------------------------------------------------

        /// <summary>Writes a 32-bit signed integer.</summary>
        /// <param name="v">Value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteInt(int v) { EnsureWrite(); _bw!.Write(v); }

        /// <summary>Reads a 32-bit signed integer.</summary>
        /// <returns>The value read.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        public int ReadInt() { EnsureRead(); return _br!.ReadInt32(); }

        /// <summary>Writes a 32-bit unsigned integer.</summary>
        /// <param name="v">Value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteUInt(uint v) { EnsureWrite(); _bw!.Write(v); }

        /// <summary>Reads a 32-bit unsigned integer.</summary>
        /// <returns>The value read.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        public uint ReadUInt() { EnsureRead(); return _br!.ReadUInt32(); }

        /// <summary>Writes a 32-bit floating-point number.</summary>
        /// <param name="v">Value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteFloat(float v) { EnsureWrite(); _bw!.Write(v); }

        /// <summary>Reads a 32-bit floating-point number.</summary>
        /// <returns>The value read.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        public float ReadFloat() { EnsureRead(); return _br!.ReadSingle(); }

        /// <summary>Writes a Boolean value.</summary>
        /// <param name="v">Value to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteBool(bool v) { EnsureWrite(); _bw!.Write(v); }

        /// <summary>Reads a Boolean value.</summary>
        /// <returns>The value read.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        public bool ReadBool() { EnsureRead(); return _br!.ReadBoolean(); }

        /// <summary>
        /// Writes a UTF-8 string with a 32-bit length prefix (byte count).
        /// </summary>
        /// <param name="s">The string to write. <see langword="null"/> is serialized as an empty string.</param>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in write mode.</exception>
        public void WriteString(string s)
        {
            EnsureWrite();
            s ??= string.Empty;
            var bytes = Encoding.UTF8.GetBytes(s);
            _bw!.Write(bytes.Length);
            if (bytes.Length > 0) _bw.Write(bytes);
        }

        /// <summary>
        /// Reads a UTF-8 string previously written with a 32-bit length prefix (byte count).
        /// </summary>
        /// <returns>The decoded string (never <see langword="null"/>).</returns>
        /// <exception cref="InvalidOperationException">Thrown when the backend is not in read mode.</exception>
        /// <exception cref="EndOfStreamException">Thrown when the stream does not supply the declared number of bytes.</exception>
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

        /// <summary>
        /// Gets or sets the current position within the underlying stream.
        /// </summary>
        public long Position { get => _stream.Position; set => _stream.Position = value; }

        /// <summary>
        /// Gets the total length, in bytes, of the underlying stream.
        /// </summary>
        public long Length => _stream.Length;

        /// <summary>
        /// Resets the stream position to the beginning.
        /// </summary>
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

        /// <summary>
        /// Flushes any buffered data in the underlying <see cref="BinaryWriter"/> to the stream.
        /// </summary>
        public void Flush() => _bw?.Flush();
    }
}
