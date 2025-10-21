#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    /// <summary>런타임에서 StableId ↔ Type, Type ↔ Formatter를 제공</summary>
    public static class ComponentRegistry
    {
        static readonly Dictionary<string, Type> id2Type = new();
        static readonly Dictionary<Type, string> type2Id = new();

        public static bool TryGetType(string id, out Type? t) => id2Type.TryGetValue(id, out t);
        public static bool TryGetId(Type t, out string? id) => type2Id.TryGetValue(t, out id);

        static readonly Dictionary<Type, IComponentFormatter> formatters = new();

        // 포맷터 타입 → “소스(어트리뷰트/직접 지정)로 선언된 StableId”
        static readonly Dictionary<Type, string> declaredSidByFormatterType = new();

        public static void Register<T>(string stableId) where T : struct
            => Register(stableId, typeof(T));

        public static void Register(string stableId, Type type)
        {
            id2Type[stableId] = type;
            type2Id[type] = stableId;
        }

        public static void RegisterFormatter(IComponentFormatter f)
        {
            formatters[f.ComponentType] = f;

            // 에디터에서만: ZenFormatterForAttribute 읽어 선언 StableId 메모
            TryCaptureFormatterStableIdFromAttribute(f.GetType(), out var sid);
            if (!string.IsNullOrEmpty(sid))
                declaredSidByFormatterType[f.GetType()] = sid!;
        }

        // 런타임에서도 쓰기 쉬운 오버로드: 등록과 동시에 StableId를 명시 주입
        public static void RegisterFormatter(IComponentFormatter f, string declaredStableId)
        {
            RegisterFormatter(f);
            if (!string.IsNullOrEmpty(declaredStableId))
                declaredSidByFormatterType[f.GetType()] = declaredStableId;
        }

        // Validator: 등록된 모든 포맷터에 대해 “컴포넌트 StableId == 포맷터 선언 StableId”를 체크
        public static int ValidateStrictStableIdMatch(bool throwOnError = true, Action<string>? log = null)
        {
            log ??= msg => System.Diagnostics.Debug.WriteLine(msg);
            int issues = 0;

            foreach (var (compType, fmt) in formatters)
            {
                // 컴포넌트 쪽 StableId
                if (!type2Id.TryGetValue(compType, out var compSid) || string.IsNullOrEmpty(compSid))
                {
                    issues++;
                    var m =
                        $"[ZenECS] Component '{compType.FullName}' has NO registered StableId, but formatter '{fmt.GetType().FullName}' is registered.";
                    if (throwOnError) throw new InvalidOperationException(m);
                    else log(m);
                    continue;
                }

                // 포맷터 쪽 “선언 StableId” (어트리뷰트/수동주입)
                if (!declaredSidByFormatterType.TryGetValue(fmt.GetType(), out var fmtSid) ||
                    string.IsNullOrEmpty(fmtSid))
                {
                    issues++;
                    var m =
                        $"[ZenECS] Formatter '{fmt.GetType().FullName}' exposes NO declared StableId; cannot match against component sid='{compSid}'. " +
                        $"(Pass it in RegisterFormatter(f, stableId) or enable Editor attribute extraction.)";
                    if (throwOnError) throw new InvalidOperationException(m);
                    else log(m);
                    continue;
                }

                if (!string.Equals(compSid, fmtSid, StringComparison.Ordinal))
                {
                    issues++;
                    var m =
                        $"[ZenECS] StableId mismatch: Component='{compType.FullName}' sid='{compSid}' <-> Formatter='{fmt.GetType().FullName}' sid='{fmtSid}'.";
                    if (throwOnError) throw new InvalidOperationException(m);
                    else log(m);
                }
            }

            return issues;
        }

        // --- 헬퍼: 에디터에서만 ZenFormatterForAttribute를 사용해 StableId 추출 ---
        static bool TryCaptureFormatterStableIdFromAttribute(Type formatterType, out string? sid)
        {
            sid = null;
#if UNITY_EDITOR
            // 어트리뷰트 풀네임/타입은 프로젝트에 맞춰 조정
            var attrs = formatterType.GetCustomAttributes(inherit:false);
            foreach (var a in attrs)
            {
                var at = a.GetType();
                if (at.Name == "ZenFormatterForAttribute" || at.FullName?.EndsWith(".ZenFormatterForAttribute") == true)
                {
                    // public string StableId { get; }
                    var p = at.GetProperty("StableId", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
                    if (p != null && p.PropertyType == typeof(string))
                    {
                        sid = p.GetValue(a) as string;
                        if (!string.IsNullOrEmpty(sid)) return true;
                    }
                }
            }
#endif
            return false;
        }

        public static IComponentFormatter GetFormatter(Type t)
        {
            if (!formatters.TryGetValue(t, out var f))
                throw new InvalidOperationException($"Formatter not registered for {t.FullName}");
            return f;
        }
    }
}
