using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Infrastructure
{
    public static class WatchQueryRunner
    {
        /// <summary>[Watch]의 AllOf 컴포넌트를 모두 가진 엔티티를 수집(항상 동작)</summary>
        public static bool TryCollectByWatch(object system, World w, List<Entity> outList)
        {
            var attrs = system.GetType().GetCustomAttributes(typeof(WatchAttribute), false)
                .Cast<WatchAttribute>().ToArray();
            if (attrs.Length == 0) return false;

            var hasGeneric = typeof(World).GetMethod("Has", BindingFlags.Public | BindingFlags.Instance);
            var all = w.GetAllEntities(); // IReadOnlyList<Entity>

            foreach (var a in attrs)
            {
                var allOf = a.AllOf ?? Array.Empty<Type>();
                if (allOf.Length == 0) continue;

                foreach (var e in all)
                {
                    bool ok = true;
                    for (int i = 0; i < allOf.Length && ok; i++)
                    {
                        var has = hasGeneric.MakeGenericMethod(allOf[i]);
                        ok &= (bool)has.Invoke(w, new object[] { e });
                    }
                    if (ok) outList.Add(e);
                }
            }

            // 중복 제거(간단/할당 적음)
            if (outList.Count > 1)
            {
                var seen = new HashSet<int>(outList.Count);
                int write = 0;
                for (int i = 0; i < outList.Count; i++)
                    if (seen.Add(outList[i].Id))
                        outList[write++] = outList[i];
                if (write < outList.Count) outList.RemoveRange(write, outList.Count - write);
            }

            return true;
        }
    }
}