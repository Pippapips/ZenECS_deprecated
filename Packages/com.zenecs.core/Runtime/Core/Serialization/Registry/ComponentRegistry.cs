using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    /// <summary>런타임에서 StableId ↔ Type, Type ↔ Formatter를 제공</summary>
    public static class ComponentRegistry
    {
        static readonly Dictionary<string, Type> id2Type = new();
        static readonly Dictionary<Type, string> type2Id = new();
        static readonly Dictionary<Type, IComponentFormatter> formatters = new();

        // ✅ 추가: 제네릭/비제네릭 등록 API
        public static void Register<T>(string stableId) where T : struct
            => Register(stableId, typeof(T));

        public static void Register(string stableId, Type type)
        {
            id2Type[stableId] = type;
            type2Id[type]     = stableId;
        }
        
        public static bool TryGetType(string id, out Type t) => id2Type.TryGetValue(id, out t);
        public static bool TryGetId(Type t, out string id)   => type2Id.TryGetValue(t, out id);

        public static void RegisterFormatter(IComponentFormatter f)
        {
            formatters[f.ComponentType] = f;
        }

        public static IComponentFormatter GetFormatter(Type t)
        {
            if (!formatters.TryGetValue(t, out var f))
                throw new InvalidOperationException($"Formatter not registered for {t.FullName}");
            return f;
        }
    }
}