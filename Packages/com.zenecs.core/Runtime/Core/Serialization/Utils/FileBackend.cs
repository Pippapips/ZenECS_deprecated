using System;
using System.IO;
using System.Text;

namespace ZenECS.Core.Serialization
{
    /// <summary>파일 기반 ISnapshotBackend. 리틀엔디안 고정.</summary>
    public sealed class FileBackend : ISnapshotBackend
    {
        private readonly FileStream fs;
        private readonly BinaryWriter w;
        private readonly BinaryReader r;

        public FileBackend(string path, FileMode mode)
        {
            fs = new FileStream(path, mode, FileAccess.ReadWrite, FileShare.Read);
            w  = new BinaryWriter(fs, Encoding.UTF8, true);
            r  = new BinaryReader(fs, Encoding.UTF8, true);
        }

        public void Dispose() { r.Dispose(); w.Dispose(); fs.Dispose(); }

        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            var tmp = data.ToArray();
            fs.Write(tmp, 0, tmp.Length);
        }

        public void ReadBytes(Span<byte> dst, int length)
        {
            var tmp = r.ReadBytes(length);
            tmp.CopyTo(dst);
        }

        public void WriteInt(int v)
        {
            var b = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            fs.Write(b, 0, 4);
        }
        public int ReadInt()
        {
            var b = r.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToInt32(b, 0);
        }

        public void WriteUInt(uint v)
        {
            var b = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            fs.Write(b, 0, 4);
        }
        public uint ReadUInt()
        {
            var b = r.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToUInt32(b, 0);
        }

        public void WriteFloat(float v)
        {
            var b = BitConverter.GetBytes(v);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            fs.Write(b, 0, 4);
        }
        public float ReadFloat()
        {
            var b = r.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            return BitConverter.ToSingle(b, 0);
        }

        public void WriteBool(bool v) => w.Write(v);
        public bool ReadBool() => r.ReadBoolean();
        
        public void WriteString(string s) => w.Write(s ?? string.Empty);
        public string ReadString() => r.ReadString();

        public long Position { get => fs.Position; set => fs.Position = value; }
        public long Length => fs.Length;
    }
}
