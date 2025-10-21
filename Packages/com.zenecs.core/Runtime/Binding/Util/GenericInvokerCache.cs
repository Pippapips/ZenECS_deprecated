#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZenECS.Core.Binding.Util
{
    // -----------------------------------------------
    // GenericInvokerCache
    //  - 캐시 키: (TargetType, MethodName, GenericArg)
    //  - 값: MakeGenericMethod로 닫힌 MethodInfo
    //  - 효과: 매 호출마다 MakeGenericMethod 비용 제거
    // -----------------------------------------------
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;

    internal static class GenericInvokerCache
    {
        private static readonly ConcurrentDictionary<(Type TargetType, string Name, Type GenericArg), MethodInfo>
            _cache = new();

        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void Invoke(object target, string methodName, Type genericArg, params object?[] args)
        {
            var targetType = target.GetType();
            var key = (targetType, methodName, genericArg);

            var closed = _cache.GetOrAdd(key, k =>
            {
                var open = k.TargetType.GetMethod(k.Name, Flags)
                           ?? throw new MissingMethodException(k.TargetType.FullName, k.Name);
                return open.MakeGenericMethod(k.GenericArg);
            });

            closed.Invoke(target, args);
        }
    }
}