using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization.Formats.Json
{
    /// <summary>경량 JSON 직렬화(데모용). 실제 프로젝트에서는 Newtonsoft/Unity.Json 등 어댑터 추가 권장.</summary>
    public sealed class JsonWorldSerializer : IWorldSerializer
    {
        public void Save(World world, ISnapshotBackend backend)
        {
            // 매우 단순한 JSON 작성 (실전에서는 proper JSON writer 권장)
            var entities = world.GetAllEntities();
            backend.WriteString("{\"v\":1,\"entities\":[");
            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                backend.WriteString("{\"id\":");
                backend.WriteString(e.Id.ToString());
                backend.WriteString(",\"components\":[");

                int k = 0;
                foreach (var (type, boxed) in world.GetAllComponents(e))
                {
                    if (k++ > 0) backend.WriteString(",");
                    if (!ComponentRegistry.TryGetId(type, out var id))
                        throw new InvalidOperationException($"StableId missing for {type.FullName}");
                    backend.WriteString("{\"id\":\"");
                    backend.WriteString(id);
                    backend.WriteString("\",\"bin\":\"");
                    // 간단화를 위해 바이너리로 한번 포맷 후 Base64로 박제
                    var mem = new MemoryBackend();
                    ComponentRegistry.GetFormatter(type).Write(boxed, mem);
                    backend.WriteString(Convert.ToBase64String(mem.ToArray()));
                    backend.WriteString("\"}");
                }
                backend.WriteString("]}");
                if (i < entities.Count - 1) backend.WriteString(",");
            }
            backend.WriteString("]}");
        }

        public void Load(World world, ISnapshotBackend backend)
        {
            throw new NotImplementedException("JsonWorldSerializer.Load: 파서가 필요합니다. (구현 의도적으로 비움)");
        }
    }
}