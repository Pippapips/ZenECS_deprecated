using System;

namespace ZenECS.Core.Serialization
{
    public interface ISnapshotBackend : IDisposable
    {
        void WriteBytes(ReadOnlySpan<byte> data);
        void ReadBytes(Span<byte> dst, int length);

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
            
        long Position { get; set; }
        long Length { get; }
    }
}