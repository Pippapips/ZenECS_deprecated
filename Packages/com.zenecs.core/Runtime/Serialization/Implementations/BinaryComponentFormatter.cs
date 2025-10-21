using System;

namespace ZenECS.Core.Serialization.Formats.Binary
{
    public abstract class BinaryComponentFormatter<T> : IComponentFormatter<T> where T : struct
    {
        public Type ComponentType => typeof(T);

        public abstract void Write(in T value, ISnapshotBackend backend);
        public abstract T ReadTyped(ISnapshotBackend backend);

        void IComponentFormatter.Write(object boxed, ISnapshotBackend backend)
            => Write((T)boxed, backend);

        object IComponentFormatter.Read(ISnapshotBackend backend)
            => ReadTyped(backend);
    }
}
