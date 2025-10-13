using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZenECS.Core;
using ZenECS.Core.Extensions;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// 박스드 컴포넌트를 World.AddBoxed(Entity e, Type t, object boxed)로 캐시 호출.
    /// </summary>
    public static class BlueprintApplier
    {
        private static readonly Dictionary<Type, Action<World, Entity, object>> _cache = new();

        public static void AddBoxed(World w, Entity e, object boxed)
        {
            if (boxed == null) return;
            var t = boxed.GetType();
            if (!_cache.TryGetValue(t, out var act))
            {
                var _miReplace = typeof(WorldComponentsOpsExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == nameof(WorldComponentsOpsExtensions.Replace)
                        && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 3);
                if (_miReplace != null)
                {
                    var g = _miReplace.MakeGenericMethod(t);
                    act = (ww, ee, boxed) =>
                        g.Invoke(null, new object[] { ww, ee, boxed });
                    _cache[t] = act;
                }
                else
                {
                    return;
                }
            }
            act(w, e, boxed);
        }
    }
}