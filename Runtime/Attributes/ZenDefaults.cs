using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ZenECS.Core
{
    public static class ZenDefaults // Core 유틸
    {
        static readonly ConcurrentDictionary<Type, Func<object>> _getterCache = new();
        
        public static object CreateWithDefaults(Type t)
        {
            // static Default
            return TryGetStaticDefault(t, out var def) ? def :
                // fallback
                Activator.CreateInstance(t);
        }

        static bool TryGetStaticDefault(Type t, out object value)
        {
            // 캐시된 게터 사용 (리플렉션 비용 상쇄)
            var getter = _getterCache.GetOrAdd(t, static T =>
            {
                var fi = T.GetField("Default", BindingFlags.Public | BindingFlags.Static);
                if (fi == null || fi.FieldType != T) return null;
                return () => fi.GetValue(null);
            });
            if (getter != null) { value = getter(); return true; }
            value = null; return false;
        }        
    }
}