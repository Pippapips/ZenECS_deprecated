using System;

namespace ZenECS.Core.Serialization
{
    public interface IComponentFormatter
    {
        Type ComponentType { get; }
        void Write(object boxed, ISnapshotBackend backend);
        object Read(ISnapshotBackend backend);
    }

    public interface IComponentFormatter<T> : IComponentFormatter where T : struct
    {
        void Write(in T value, ISnapshotBackend backend);
        T ReadTyped(ISnapshotBackend backend);
    }
}