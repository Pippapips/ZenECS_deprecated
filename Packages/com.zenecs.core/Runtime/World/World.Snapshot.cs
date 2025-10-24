// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — World subsystem
// File: World.Snapshot.cs
// Purpose: Binary snapshot save/load for world metadata and component pools.
// Key concepts:
//   • Signature 'ZENSNAP1' (little-endian, canonical format).
//   • Captures alive bitset, generations, free IDs, and pool contents.
//   • Requires registered IComponentFormatter implementations per component.
//
// Copyright (c) 2025 Pippapips Limited
// License: MIT (see LICENSE or https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
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
        /// Represents world metadata without component pool data.
        /// </summary>
        public readonly struct WorldSnapshot
        {
            public readonly int NextId;
            public readonly int[] Generation;
            public readonly int[] FreeIds;
            public readonly byte[] AliveBits;

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
        /// Saves the full world state as a portable binary snapshot ("ZENSNAP1").
        /// </summary>
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
        /// Loads a full binary snapshot ("ZENSNAP1") into the world.
        /// </summary>
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
        // Metadata serialization
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
        // Component pool serialization
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
