using System;

namespace ZenECS.Core.Serialization.Formats.Binary
{
    // 예시: float3/Quaternion 등은 Surrogate를 내부에서 사용하도록 확장 가능
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