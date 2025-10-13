using System;
using System.Collections.Generic;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// Unity SO 밖에서도 쓸 수 있는 순수 런타임 블루프린트 데이터.
    /// (엔트리 json은 JsonUtility 스냅샷 포맷)
    /// </summary>
    [Serializable]
    public sealed class BlueprintData
    {
        [Serializable]
        public sealed class Entry
        {
            public string typeName; // AssemblyQualifiedName 권장
            public string json;     // ComponentJson 스냅샷 문자열
        }

        public List<Entry> entries = new();

        public void ApplyTo(World world, Entity e)
        {
            foreach (var it in entries)
            {
                if (string.IsNullOrEmpty(it?.typeName)) continue;
                var t = Resolve(it.typeName);
                if (t == null) continue;
                var boxed = ComponentJson.Deserialize(it.json, t);
                BlueprintApplier.AddBoxed(world, e, boxed);
            }
        }

        public static Type Resolve(string typeName)
        {
            var t = Type.GetType(typeName, false);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                if (asm.GetType(typeName, false) is { } tt) return tt;
            return null;
        }
    }
}