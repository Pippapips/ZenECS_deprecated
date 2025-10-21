#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ZenECS.Core.Internal;
using ZenECS.Core.Serialization;

namespace ZenECS.Core
{
    public sealed partial class World
    {
        /// <summary>World meta snapshot (without pool values).</summary>
        public readonly struct WorldSnapshot
        {
            public readonly int NextId;
            public readonly int[] Generation; // per-slot generation
            public readonly int[] FreeIds;    // free stack snapshot
            public readonly byte[] AliveBits; // BitSet serialized

            public WorldSnapshot(int nextId, int[] gen, int[] freeIds, byte[] aliveBits)
            {
                NextId = nextId;
                Generation = gen;
                FreeIds = freeIds;
                AliveBits = aliveBits;
            }
        }

        // =========================
        // PUBLIC: Full Binary (ZENSNAP1, Little-Endian canonical)
        // =========================

        public void SaveFullSnapshotBinary(Stream s)
        {
            if (s == null || !s.CanWrite) throw new ArgumentException("Stream not writable", nameof(s));
            using var bw = new BinaryWriter(s, Encoding.UTF8, leaveOpen: true);

            // Magic only (BinaryWriter/Reader is little-endian by spec)
            bw.Write(new byte[] { (byte)'Z', (byte)'E', (byte)'N', (byte)'S', (byte)'N', (byte)'A', (byte)'P', (byte)'1' });

            // 1) world meta
            SaveWorldMetaBinary(bw);

            // 2) component pools (formatter-required)
            SaveAllComponentPoolsBinary(bw);
        }

        public void LoadFullSnapshotBinary(Stream s)
        {
            if (s == null || !s.CanRead) throw new ArgumentException("Stream not readable", nameof(s));
            using var br = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);

            // Magic
            Span<byte> magic = stackalloc byte[8];
            int read = br.Read(magic);
            if (read != 8 || magic[0] != (byte)'Z' || magic[1] != (byte)'E' || magic[2] != (byte)'N' ||
                magic[3] != (byte)'S' || magic[4] != (byte)'N' || magic[5] != (byte)'A' || magic[6] != (byte)'P' || magic[7] != (byte)'1')
                throw new InvalidOperationException("Invalid full snapshot header");

            // 1) world meta
            LoadWorldMetaBinary(br);

            // reset pools
            foreach (var kv in pools) kv.Value.ClearAll();

            // 2) component pools (formatter-required)
            LoadAllComponentPoolsBinary(br);

            // 3) Post-Load Migrations (월드 전개 이후 후처리)
            ZenECS.Core.Serialization.PostLoadMigrationRegistry.RunAll(this);
        }

        // =========================
        // META (internal)
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
            var genCopy = new int[generation.Length];
            Array.Copy(generation, genCopy, generation.Length);

            var freeCopy = freeIds.ToArray();
            var aliveBytes = alive.ToByteArray();

            return new WorldSnapshot(nextId, genCopy, freeCopy, aliveBytes);
        }

        private void ApplySnapshot(in WorldSnapshot snap)
        {
            if (snap.Generation.Length > generation.Length)
                Array.Resize(ref generation, snap.Generation.Length);

            Array.Copy(snap.Generation, generation, snap.Generation.Length);
            nextId = snap.NextId;

            alive.FromByteArray(snap.AliveBits);

            freeIds.Clear();
            for (int i = 0; i < snap.FreeIds.Length; i++)
                freeIds.Push(snap.FreeIds[i]);
        }

        // =========================
        // POOLS (internal) — formatter REQUIRED
        // =========================

        private void SaveAllComponentPoolsBinary(BinaryWriter bw)
        {
            var mask = alive;
            bw.Write(pools.Count); // pool count

            foreach (var (type, pool) in pools)
            {
                // Resolve formatter (required)
                IComponentFormatter? formatter = null;
                try { formatter = ComponentRegistry.GetFormatter(type); } catch { /* noop */ }
                if (formatter == null)
                    throw new NotSupportedException(
                        $"No BinaryComponentFormatter registered for '{type.FullName}'. " +
                        "All components must provide a formatter to save portable binary snapshots.");

                // Header
                ComponentRegistry.TryGetId(type, out var stableIdRaw);
                string stableId = stableIdRaw ?? string.Empty;
                string typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                bw.Write(stableId);
                bw.Write(typeName);

                // Alive count
                int count = 0;
                foreach (var (id, _) in pool.EnumerateAll())
                    if (mask.Get(id)) count++;
                bw.Write(count);

                // Entries
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
                string stableId = br.ReadString(); // may be empty
                string typeName  = br.ReadString();

                // Resolve type + formatter (formatter REQUIRED)
                IComponentFormatter? formatter = null;
                Type? resolvedType = null;

                if (!string.IsNullOrEmpty(stableId) && ComponentRegistry.TryGetType(stableId, out var t))
                {
                    resolvedType = t;
                    try
                    {
                        if (t != null)
                        {
                            formatter = ComponentRegistry.GetFormatter(t);
                        }
                    } catch { /* noop */ }
                }

                if (resolvedType == null)
                    resolvedType = Type.GetType(typeName, throwOnError: true)
                                   ?? throw new InvalidOperationException($"Type not found: {typeName}");

                if (formatter == null)
                {
                    try { formatter = ComponentRegistry.GetFormatter(resolvedType); }
                    catch { /* noop */ }
                }

                if (formatter == null)
                    throw new NotSupportedException(
                        $"No BinaryComponentFormatter registered for '{resolvedType.FullName}'. " +
                        "All components must provide a formatter to load portable binary snapshots.");

                // Align to formatter.ComponentType to avoid type-identity mismatch
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
