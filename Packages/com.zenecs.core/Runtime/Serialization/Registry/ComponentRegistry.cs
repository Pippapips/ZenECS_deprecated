// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core
// File: ComponentRegistry.cs
// Purpose: Maintains global mappings between StableId ↔ Type and Type ↔ Formatter.
// Key concepts:
//   • Used by snapshot systems to resolve components during load/save.
//   • Supports StableId validation between components and formatters.
//   • Captures declared StableId via ZenFormatterForAttribute (Editor-only).
// 
// Copyright (c) 2025 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Global registry for component type and formatter lookup.
    /// Provides StableId ↔ Type and Type ↔ Formatter resolution.
    /// </summary>
    public static class ComponentRegistry
    {
        private static readonly Dictionary<string, Type> id2Type = new();
        private static readonly Dictionary<Type, string> type2Id = new();
        private static readonly Dictionary<Type, IComponentFormatter> formatters = new();

        // FormatterType → Declared StableId (via attribute or manual injection)
        private static readonly Dictionary<Type, string> declaredSidByFormatterType = new();

        public static bool TryGetType(string id, out Type? t) => id2Type.TryGetValue(id, out t);
        public static bool TryGetId(Type t, out string? id) => type2Id.TryGetValue(t, out id);

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
            // Editor-only attribute capture
            TryCaptureFormatterStableIdFromAttribute(f.GetType(), out var sid);
            if (!string.IsNullOrEmpty(sid))
                declaredSidByFormatterType[f.GetType()] = sid!;
        }

        /// <summary>
        /// Registers formatter and explicitly provides its declared StableId.
        /// </summary>
        public static void RegisterFormatter(IComponentFormatter f, string declaredStableId)
        {
            RegisterFormatter(f);
            if (!string.IsNullOrEmpty(declaredStableId))
                declaredSidByFormatterType[f.GetType()] = declaredStableId;
        }

        /// <summary>
        /// Validates strict StableId consistency between registered components and formatters.
        /// Throws or logs depending on <paramref name="throwOnError"/>.
        /// </summary>
        public static int ValidateStrictStableIdMatch(bool throwOnError = true, Action<string>? log = null)
        {
            log ??= msg => System.Diagnostics.Debug.WriteLine(msg);
            int issues = 0;

            foreach (var (compType, fmt) in formatters)
            {
                if (!type2Id.TryGetValue(compType, out var compSid) || string.IsNullOrEmpty(compSid))
                {
                    issues++;
                    var msg =
                        $"[ZenECS] Component '{compType.FullName}' has NO registered StableId, but formatter '{fmt.GetType().FullName}' is registered.";
                    if (throwOnError) throw new InvalidOperationException(msg);
                    else log(msg);
                    continue;
                }

                if (!declaredSidByFormatterType.TryGetValue(fmt.GetType(), out var fmtSid) ||
                    string.IsNullOrEmpty(fmtSid))
                {
                    issues++;
                    var msg =
                        $"[ZenECS] Formatter '{fmt.GetType().FullName}' exposes NO declared StableId; cannot match against component sid='{compSid}'. " +
                        "(Provide it in RegisterFormatter(f, stableId) or use the ZenFormatterForAttribute in Editor.)";
                    if (throwOnError) throw new InvalidOperationException(msg);
                    else log(msg);
                    continue;
                }

                if (!string.Equals(compSid, fmtSid, StringComparison.Ordinal))
                {
                    issues++;
                    var msg =
                        $"[ZenECS] StableId mismatch: Component='{compType.FullName}' sid='{compSid}' <-> Formatter='{fmt.GetType().FullName}' sid='{fmtSid}'.";
                    if (throwOnError) throw new InvalidOperationException(msg);
                    else log(msg);
                }
            }

            return issues;
        }

        // Editor-only: attempts to read StableId from ZenFormatterForAttribute on the formatter type.
        private static bool TryCaptureFormatterStableIdFromAttribute(Type formatterType, out string? sid)
        {
            sid = null;
#if UNITY_EDITOR
            var attrs = formatterType.GetCustomAttributes(inherit: false);
            foreach (var a in attrs)
            {
                var at = a.GetType();
                if (at.Name == "ZenFormatterForAttribute" || at.FullName?.EndsWith(".ZenFormatterForAttribute") == true)
                {
                    var p = at.GetProperty("StableId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
