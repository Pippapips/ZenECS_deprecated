using System;
using System.IO;
using System.Text;

namespace ZenECS.Core.Serialization
{
    public sealed class MemoryBackend : ISnapshotBackend
    {
        private readonly MemoryStream ms = new MemoryStream();
        private readonly BinaryWriter w;
        private readonly BinaryReader r;

        public MemoryBackend()
        {
            w = new BinaryWriter(ms, Encoding.UTF8, true);
            r = new BinaryReader(ms, Encoding.UTF8, true);
        }

        public void Dispose() { r.Dispose(); w.Dispose(); ms.Dispose(); }

        public void WriteBytes(ReadOnlySpan<byte> data) => ms.Write(data);
        public void ReadBytes(Span<byte> dst, int length) => ms.Read(dst);

        public void WriteInt(int v) => w.Write(v);
        public int ReadInt() => r.ReadInt32();

        public void WriteUInt(uint v) => w.Write(v);
        public uint ReadUInt() => r.ReadUInt32();

        public void WriteFloat(float v) => w.Write(v);
        public float ReadFloat() => r.ReadSingle();

        public void WriteString(string s) => w.Write(s ?? string.Empty);
        public string ReadString() => r.ReadString();

        public void WriteBool(bool v) => w.Write(v);
        public bool ReadBool() => r.ReadBoolean();
        
        public long Position { get => ms.Position; set => ms.Position = value; }
        public long Length => ms.Length;

        public byte[] ToArray() => ms.ToArray();
    }
}