using System;

namespace ZenECS.Core.Serialization.Formats.Json
{
    /// <summary>
    /// 컴포넌트 JSON 직렬화 베이스.
    /// - 구현체는 Serialize(T) / Deserialize(string)만 작성하면 된다.
    /// - JsonWorldSerializer는 component payload를 string으로 기록한다.
    /// </summary>
    public abstract class JsonComponentFormatter<T> : IComponentFormatter<T> where T : struct
    {
        public Type ComponentType => typeof(T);

        public abstract string Serialize(in T value);
        public abstract T Deserialize(string json);

        public void Write(in T value, ISnapshotBackend backend)
        {
            var json = Serialize(value);
            backend.WriteString(json);
        }

        public T ReadTyped(ISnapshotBackend backend)
        {
            var json = backend.ReadString();
            return Deserialize(json);
        }

        void IComponentFormatter.Write(object boxed, ISnapshotBackend backend)
            => Write((T)boxed, backend);

        object IComponentFormatter.Read(ISnapshotBackend backend)
            => ReadTyped(backend);
    }
}