﻿// ──────────────────────────────────────────────────────────────────────────────
/*
 ZenECS Core — World subsystem
 File: World.Snapshot.cs
 Purpose: Binary snapshot save/load for world metadata and component pools.
 Key concepts:
   • Signature 'ZENSNAP1' (little-endian, canonical format).
   • Captures alive bitset, generations, free IDs, and pool contents.
   • Requires registered IComponentFormatter implementations per component.

 Copyright (c) 2025 Pippapips Limited
 License: MIT (https://opensource.org/licenses/MIT)
 SPDX-License-Identifier: MIT
*/
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ZenECS.Core.Internal;
using ZenECS.Core.Serialization;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>
        /// An immutable snapshot of world metadata (no component data).
        /// </summary>
        /// <remarks>
        /// Captures the next entity id, per-entity generation counters, the free-list of recycled ids,
        /// and the bitset of currently alive entities. This structure is used as a compact header when
        /// saving or loading full snapshots.
        /// </remarks>
        public readonly struct WorldSnapshot
        {
            /// <summary>
            /// The next entity id to be assigned when a new entity is created.
            /// </summary>
            public readonly int NextId;

            /// <summary>
            /// Per-entity generation numbers used to validate stale handles.
            /// </summary>
            public readonly int[] Generation;

            /// <summary>
            /// A copy of the free-list containing recycled entity ids.
            /// </summary>
            public readonly int[] FreeIds;

            /// <summary>
            /// Bitset (little-endian byte order) indicating which entity ids are currently alive.
            /// </summary>
            public readonly byte[] AliveBits;

            /// <summary>
            /// Initializes a new <see cref="WorldSnapshot"/>.
            /// </summary>
            /// <param name="nextId">Next entity id to be used by the world.</param>
            /// <param name="gen">Array of generation counters (indexed by entity id).</param>
            /// <param name="freeIds">Array of recycled entity ids.</param>
            /// <param name="aliveBits">Alive bitset encoded as bytes.</param>
            public WorldSnapshot(int nextId, int[] gen, int[] freeIds, byte[] aliveBits)
            {
                NextId = nextId;
                Generation = gen;
                FreeIds = freeIds;
                AliveBits = aliveBits;
            }
        }

        // =========================
        // Public Snapshot I/O (binary)
        // =========================

        /// <summary>
        /// Saves the complete world state to a portable binary snapshot with the header <c>"ZENSNAP1"</c>.
        /// </summary>
        /// <param name="s">A writable stream that will receive the snapshot.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="s"/> is <see langword="null"/> or not writable.
        /// </exception>
        /// <remarks>
        /// <para>
        /// The format is little-endian and begins with the 8-byte ASCII signature <c>'Z','E','N','S','N','A','P','1'</c>,
        /// followed by world metadata (see <see cref="WorldSnapshot"/>) and then all component pools.
        /// </para>
        /// <para>
        /// Component serialization is delegated to registered <see cref="IComponentFormatter"/> implementations.
        /// Ensure that each component type has a formatter registered via <see cref="ComponentRegistry"/>.
        /// </para>
        /// </remarks>
        public void SaveFullSnapshotBinary(Stream s)
        {
            if (s == null || !s.CanWrite) throw new ArgumentException("Stream not writable", nameof(s));
            using var bw = new BinaryWriter(s, Encoding.UTF8, leaveOpen: true);

            // Magic header (BinaryWriter uses little-endian)
            bw.Write(new byte[] { (byte)'Z', (byte)'E', (byte)'N', (byte)'S', (byte)'N', (byte)'A', (byte)'P', (byte)'1' });

            SaveWorldMetaBinary(bw);
            SaveAllComponentPoolsBinary(bw);
        }

        /// <summary>
        /// Loads a complete world state from a portable binary snapshot with the header <c>"ZENSNAP1"</c>.
        /// </summary>
        /// <param name="s">A readable stream that provides the snapshot bytes.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="s"/> is <see langword="null"/> or not readable.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the snapshot header does not match the expected signature.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when a component type does not have a registered <see cref="IComponentFormatter"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// On load, existing component pools are cleared before data is restored. After all pools
        /// are populated, registered post-load migrations are executed via
        /// <c>PostLoadMigrationRegistry.RunAll(this)</c>.
        /// </para>
        /// <para>
        /// When a StableId is present in the snapshot, the type is resolved through
        /// <see cref="ComponentRegistry.TryGetType(string, out Type?)"/>; otherwise the formatter type name
        /// recorded in the snapshot is used as a fallback.
        /// </para>
        /// </remarks>
        public void LoadFullSnapshotBinary(Stream s)
        {
            if (s == null || !s.CanRead) throw new ArgumentException("Stream not readable", nameof(s));
            using var br = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);

            // Verify magic header
            Span<byte> magic = stackalloc byte[8];
            int read = br.Read(magic);
            if (read != 8 || magic[0] != (byte)'Z' || magic[1] != (byte)'E' || magic[2] != (byte)'N' ||
                magic[3] != (byte)'S' || magic[4] != (byte)'N' || magic[5] != (byte)'A' ||
                magic[6] != (byte)'P' || magic[7] != (byte)'1')
                throw new InvalidOperationException("Invalid full snapshot header");

            LoadWorldMetaBinary(br);

            // Reset existing pools
            foreach (var kv in _pools) kv.Value.ClearAll();

            LoadAllComponentPoolsBinary(br);

            // Run post-load migrations (registered user hooks)
            PostLoadMigrationRegistry.RunAll(this);
        }

        // =========================
        // Metadata serialization (private helpers)
        // =========================

        private void SaveWorldMetaBinary(BinaryWriter bw)
        {
            var snap = TakeSnapshot();

            bw.Write(snap.NextId);

            bw.Write(snap.Generation.Length);
            for (int i = 0; i < snap.Generation.Length; i++) bw.Write(snap.Generation[i]);

            bw.Write(snap.FreeIds.Length);
            for (int i = 0; i < snap.FreeIds.Length; i++) bw.Write(snap.FreeIds[i]);

            bw.Write(snap.AliveBits.Length);
            if (snap.AliveBits.Length > 0) bw.Write(snap.AliveBits);
        }

        private void LoadWorldMetaBinary(BinaryReader br)
        {
            int next = br.ReadInt32();

            int genLen = br.ReadInt32();
            var gen = genLen > 0 ? new int[genLen] : Array.Empty<int>();
            for (int i = 0; i < genLen; i++) gen[i] = br.ReadInt32();

            int freeLen = br.ReadInt32();
            var free = freeLen > 0 ? new int[freeLen] : Array.Empty<int>();
            for (int i = 0; i < freeLen; i++) free[i] = br.ReadInt32();

            int aliveLen = br.ReadInt32();
            var aliveBytes = aliveLen > 0 ? br.ReadBytes(aliveLen) : Array.Empty<byte>();

            var snap = new WorldSnapshot(next, gen, free, aliveBytes);
            ApplySnapshot(in snap);
        }

        private WorldSnapshot TakeSnapshot()
        {
            var genCopy = new int[_generation.Length];
            Array.Copy(_generation, genCopy, _generation.Length);

            var freeCopy = _freeIds.ToArray();
            var aliveBytes = _alive.ToByteArray();

            return new WorldSnapshot(_nextId, genCopy, freeCopy, aliveBytes);
        }

        private void ApplySnapshot(in WorldSnapshot snap)
        {
            if (snap.Generation.Length > _generation.Length)
                Array.Resize(ref _generation, snap.Generation.Length);

            Array.Copy(snap.Generation, _generation, snap.Generation.Length);
            _nextId = snap.NextId;

            _alive.FromByteArray(snap.AliveBits);

            _freeIds.Clear();
            for (int i = 0; i < snap.FreeIds.Length; i++)
                _freeIds.Push(snap.FreeIds[i]);
        }

        // =========================
        // Component pool serialization (private helpers)
        // =========================

        private void SaveAllComponentPoolsBinary(BinaryWriter bw)
        {
            var mask = _alive;
            bw.Write(_pools.Count);

            foreach (var (type, pool) in _pools)
            {
                IComponentFormatter? formatter = ComponentRegistry.GetFormatter(type);
                if (formatter == null)
                    throw new NotSupportedException($"No formatter registered for '{type.FullName}'.");

                ComponentRegistry.TryGetId(type, out var stableIdRaw);
                string stableId = stableIdRaw ?? string.Empty;
                string typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                bw.Write(stableId);
                bw.Write(typeName);

                int count = 0;
                foreach (var (id, _) in pool.EnumerateAll())
                    if (mask.Get(id)) count++;
                bw.Write(count);

                foreach (var (id, boxed) in pool.EnumerateAll())
                {
                    if (!mask.Get(id)) continue;
                    bw.Write(id);

                    using var ms = new MemoryStream();
                    using (var backend = new StreamSnapshotBackend(ms, writable: true, leaveOpen: true))
                    {
                        formatter.Write(boxed, backend);
                        backend.Flush();
                    }
                    var bytes = ms.ToArray();
                    bw.Write(bytes.Length);
                    if (bytes.Length > 0) bw.Write(bytes);
                }
            }
        }

        private void LoadAllComponentPoolsBinary(BinaryReader br)
        {
            int poolCount = br.ReadInt32();
            for (int p = 0; p < poolCount; p++)
            {
                string stableId = br.ReadString();
                string typeName = br.ReadString();

                IComponentFormatter? formatter = null;
                Type? resolvedType = null;

                if (!string.IsNullOrEmpty(stableId) && ComponentRegistry.TryGetType(stableId, out var t))
                {
                    resolvedType = t;
                    if (t != null) formatter = ComponentRegistry.GetFormatter(t);
                }

                if (resolvedType == null)
                    resolvedType = Type.GetType(typeName, throwOnError: true)
                        ?? throw new InvalidOperationException($"Type not found: {typeName}");

                formatter ??= ComponentRegistry.GetFormatter(resolvedType)
                    ?? throw new NotSupportedException(
                        $"No formatter registered for '{resolvedType.FullName}'.");

                resolvedType = formatter.ComponentType;
                var pool = GetOrCreatePoolByType(resolvedType);

                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int id = br.ReadInt32();
                    int size = br.ReadInt32();
                    var bytes = size > 0 ? br.ReadBytes(size) : Array.Empty<byte>();
                    using var ms = new MemoryStream(bytes, writable: false);
                    using var backend = new StreamSnapshotBackend(ms, writable: false, leaveOpen: true);
                    var value = formatter.Read(backend);
                    pool.SetBoxed(id, value);
                }
            }
        }
    }
}
