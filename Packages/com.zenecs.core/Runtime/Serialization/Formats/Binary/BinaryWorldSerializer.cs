using System;

namespace ZenECS.Core.Serialization.Formats.Binary
{
    /// <summary>간단한 바이트 스트림 기반 직렬화(데모용). 실제 게임에서는 버전/마이그레이션 고려 확장 필요.</summary>
    public sealed class BinaryWorldSerializer : IWorldSerializer
    {
        public void Save(World world, ISnapshotBackend backend)
        {
            // 헤더(버전)
            backend.WriteInt(1);

            var entities = world.GetAllEntities();
            backend.WriteInt(entities.Count);

            foreach (var e in entities)
            {
                backend.WriteInt(e.Id);

                // 컴포넌트 나열
                int compCount = 0;
                foreach (var _ in world.GetAllComponents(e)) compCount++;
                backend.WriteInt(compCount);

                foreach (var (type, boxed) in world.GetAllComponents(e))
                {
                    if (!ComponentRegistry.TryGetId(type, out var id))
                        throw new InvalidOperationException($"StableId missing for {type.FullName}");

                    backend.WriteString(id);
                    var fmt = ComponentRegistry.GetFormatter(type);
                    fmt.Write(boxed, backend);
                }
            }
        }

        public void Load(World world, ISnapshotBackend backend)
        {
            int version = backend.ReadInt();
            if (version != 1) throw new NotSupportedException($"Unsupported snapshot version: {version}");

            // 기존 월드 정리(단순화)
            foreach (var e in world.GetAllEntities()) world.DestroyEntity(e);

            int entityCount = backend.ReadInt();
            for (int i = 0; i < entityCount; i++)
            {
                int id = backend.ReadInt();
                var e = world.CreateEntity(id);

                int compCount = backend.ReadInt();
                for (int c = 0; c < compCount; c++)
                {
                    string stableId = backend.ReadString();
                    if (!ComponentRegistry.TryGetType(stableId, out var t))
                        throw new InvalidOperationException($"Unknown component id: {stableId}");

                    var fmt = ComponentRegistry.GetFormatter(t);
                    var boxed = fmt.Read(backend);
                    world.AddBoxed(e, t, boxed);
                }
            }
        }
    }
}
