﻿// ──────────────────────────────────────────────────────────────────────────────
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
// ─────────────────────────────────────────────────────────────────────────────-
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Global registry for component type and formatter lookup.
    /// Provides StableId ↔ <see cref="Type"/> and <see cref="Type"/> ↔ <see cref="IComponentFormatter"/> resolution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This registry is intended to be populated during application startup (e.g., bootstrapping or editor bake).
    /// Concurrent writes are <b>not</b> thread-safe; perform registrations on a single thread before use.
    /// Read access after initialization is effectively read-only and safe for concurrent reads.
    /// </para>
    /// <para>
    /// Typical usage:
    /// <code>
    /// ComponentRegistry.Register&lt;Position&gt;("com.zenecs.position.v1");
    /// ComponentRegistry.RegisterFormatter(new PositionFormatter(), "com.zenecs.position.v1");
    /// ComponentRegistry.ValidateStrictStableIdMatch();
    /// </code>
    /// </para>
    /// </remarks>
    public static class ComponentRegistry
    {
        private static readonly Dictionary<string, Type> id2Type = new();
        private static readonly Dictionary<Type, string> type2Id = new();
        private static readonly Dictionary<Type, IComponentFormatter> formatters = new();

        // FormatterType → Declared StableId (via attribute or manual injection)
        private static readonly Dictionary<Type, string> declaredSidByFormatterType = new();

        /// <summary>
        /// Attempts to resolve a component <see cref="Type"/> from its StableId.
        /// </summary>
        /// <param name="id">StableId string (e.g., <c>"com.zenecs.position.v1"</c>).</param>
        /// <param name="t">When this method returns, contains the resolved type if found; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the mapping exists; otherwise <see langword="false"/>.</returns>
        public static bool TryGetType(string id, out Type? t) => id2Type.TryGetValue(id, out t);

        /// <summary>
        /// Attempts to resolve a StableId for the specified component <see cref="Type"/>.
        /// </summary>
        /// <param name="t">Component type previously registered with <see cref="Register(string, Type)"/> or <see cref="Register{T}(string)"/>.</param>
        /// <param name="id">When this method returns, contains the StableId if found; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the mapping exists; otherwise <see langword="false"/>.</returns>
        public static bool TryGetId(Type t, out string? id) => type2Id.TryGetValue(t, out id);

        /// <summary>
        /// Registers a mapping from StableId to component <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Component value type (typically a <see langword="struct"/>).</typeparam>
        /// <param name="stableId">StableId string to associate with <typeparamref name="T"/>.</param>
        /// <remarks>
        /// If the StableId or type was already registered, the mapping is overwritten.
        /// </remarks>
        public static void Register<T>(string stableId) where T : struct
            => Register(stableId, typeof(T));

        /// <summary>
        /// Registers a mapping from StableId to component <paramref name="type"/>.
        /// </summary>
        /// <param name="stableId">StableId string to associate with the type.</param>
        /// <param name="type">Component type to register.</param>
        /// <remarks>
        /// If the StableId or type was already registered, the mapping is overwritten.
        /// </remarks>
        public static void Register(string stableId, Type type)
        {
            id2Type[stableId] = type;
            type2Id[type] = stableId;
        }

        /// <summary>
        /// Registers a component formatter instance and captures its declared StableId from an editor attribute when available.
        /// </summary>
        /// <param name="f">Formatter instance to register.</param>
        /// <remarks>
        /// <para>
        /// Runtime registration associates the formatter with its <see cref="IComponentFormatter.ComponentType"/>.
        /// In Unity Editor, this also attempts to read a StableId from a <c>ZenFormatterForAttribute</c> on the formatter type.
        /// </para>
        /// <para>
        /// To enforce strict StableId alignment between components and formatters, call
        /// <see cref="ValidateStrictStableIdMatch(bool, Action{string}?)"/> after all registrations.
        /// </para>
        /// </remarks>
        public static void RegisterFormatter(IComponentFormatter f)
        {
            formatters[f.ComponentType] = f;
            // Editor-only attribute capture
            TryCaptureFormatterStableIdFromAttribute(f.GetType(), out var sid);
            if (!string.IsNullOrEmpty(sid))
                declaredSidByFormatterType[f.GetType()] = sid!;
        }

        /// <summary>
        /// Registers a component formatter instance and explicitly provides its declared StableId.
        /// </summary>
        /// <param name="f">Formatter instance to register.</param>
        /// <param name="declaredStableId">StableId that the formatter declares it serializes (e.g., the component's StableId).</param>
        /// <remarks>
        /// Use this overload when the editor-only attribute is unavailable (e.g., in player builds) or when you want to override it.
        /// </remarks>
        public static void RegisterFormatter(IComponentFormatter f, string declaredStableId)
        {
            RegisterFormatter(f);
            if (!string.IsNullOrEmpty(declaredStableId))
                declaredSidByFormatterType[f.GetType()] = declaredStableId;
        }

        /// <summary>
        /// Validates strict StableId consistency between registered component types and their registered formatters.
        /// </summary>
        /// <param name="throwOnError">
        /// When <see langword="true"/>, throws on the first inconsistency; otherwise logs via <paramref name="log"/> and returns the number of issues.
        /// </param>
        /// <param name="log">Optional logger used when <paramref name="throwOnError"/> is <see langword="false"/>.</param>
        /// <returns>The number of issues detected (0 when consistent).</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="throwOnError"/> is <see langword="true"/> and:
        /// <list type="bullet">
        /// <item>no StableId is registered for a component that has a formatter registered;</item>
        /// <item>no declared StableId is available for a formatter;</item>
        /// <item>or the component StableId and formatter declared StableId do not match.</item>
        /// </exception>
        /// <remarks>
        /// Call this after all <see cref="Register(string, Type)"/> and <see cref="RegisterFormatter(IComponentFormatter, string)"/> calls.
        /// </remarks>
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

        /// <summary>
        /// Attempts to read a declared StableId from an editor-only attribute on the formatter type.
        /// </summary>
        /// <param name="formatterType">The formatter's <see cref="Type"/>.</param>
        /// <param name="sid">When this method returns, contains the declared StableId if found; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a StableId was captured from the attribute; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// Effective only inside Unity Editor (<c>UNITY_EDITOR</c>). In player builds this always returns <see langword="false"/>.
        /// </remarks>
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

        /// <summary>
        /// Gets the registered formatter for the given component <see cref="Type"/>.
        /// </summary>
        /// <param name="t">Component type.</param>
        /// <returns>The formatter instance previously registered for <paramref name="t"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no formatter is registered for <paramref name="t"/>.</exception>
        public static IComponentFormatter GetFormatter(Type t)
        {
            if (!formatters.TryGetValue(t, out var f))
                throw new InvalidOperationException($"Formatter not registered for {t.FullName}");
            return f;
        }
    }
}
