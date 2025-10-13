// Runtime/Core/Messaging/Diagnostics/MessageCounters.cs
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZenECS.Core.Messaging.Diagnostics
{
    public sealed class MessageCounters
    {
        readonly ConcurrentDictionary<Type, long> published = new();
        readonly ConcurrentDictionary<Type, long> consumed  = new();

        public void IncPublish(Type messageType, long count = 1)
        {
            published.AddOrUpdate(messageType, count, (_, v) => v + count);
        }

        public void IncConsume(Type messageType, long count = 1)
            => consumed.AddOrUpdate(messageType, count, (_, v) => v + count);

        public long GetPublished(Type t) => published.TryGetValue(t, out var v) ? v : 0;
        public long GetConsumed (Type t) => consumed .TryGetValue(t, out var v) ? v : 0;

        public (Type type, long pub, long con)[] Snapshot()
        {
            // 각 딕셔너리의 고정 스냅샷
            var pubs = published.ToArray(); // KeyValuePair<Type,long>[]
            var cons = consumed .ToArray();

            // 타입별로 합치기
            var map = new Dictionary<Type, (long pub, long con)>(pubs.Length + cons.Length);

            for (int i = 0; i < pubs.Length; i++)
            {
                var k = pubs[i].Key; var v = pubs[i].Value;
                map[k] = (v, 0);
            }
            for (int i = 0; i < cons.Length; i++)
            {
                var k = cons[i].Key; var v = cons[i].Value;
                if (map.TryGetValue(k, out var ex)) map[k] = (ex.pub, v);
                else map[k] = (0, v);
            }

            // 배열로 변환
            var result = new (Type type, long pub, long con)[map.Count];
            int idx = 0;
            foreach (var kv in map)
                result[idx++] = (kv.Key, kv.Value.pub, kv.Value.con);

            return result;
        }
    }
}