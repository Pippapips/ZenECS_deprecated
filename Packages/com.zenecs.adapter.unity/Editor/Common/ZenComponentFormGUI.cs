#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text; // ★ UTF-8 바이트 길이 계산용
using Unity.Collections; // ★ FixedString64Bytes
using UnityEditor;
using UnityEngine;

namespace ZenECS.EditorCommon
{
    /// IMGUI 공용 폼 드로어: 기본형/Unity 타입 + Unity.Mathematics(float2/3/4,int/uint/bool2/3/4, quaternion)
    public static class ZenComponentFormGUI
    {
        static bool IsReadOnly(System.Type componentType, System.Reflection.MemberInfo member)
        {
            // 멤버나 타입에 [ReadOnlyInInspector]가 붙었는가?
            return System.Attribute.IsDefined(member,
                       typeof(ZenECS.Adapter.Unity.Attributes.ReadOnlyInInspectorAttribute), inherit: true)
                   || System.Attribute.IsDefined(componentType,
                       typeof(ZenECS.Adapter.Unity.Attributes.ReadOnlyInInspectorAttribute), inherit: true);
        }

        public static float DrawObject(Rect area, object obj, Type typeOverride = null, bool rowHeight = true)
        {
            if (obj == null) return 0f;
            var t = typeOverride ?? obj.GetType();
            float y = area.y;

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var ft = f.FieldType;
                var cur = f.GetValue(obj);
                float rowH = RowH(ft, rowHeight);
                var r = new Rect(area.x, y, area.width, rowH);
                var label = ObjectNames.NicifyVariableName(f.Name);

                var ro = IsReadOnly(f.DeclaringType, f);
                using (new UnityEditor.EditorGUI.DisabledScope(ro))
                {
                    if (TryDrawMathVector(r, label, ft, ref cur) || TryDrawQuaternionMath(r, label, ft, ref cur))
                    {
                        f.SetValue(obj, cur);
                        y += rowH;
                        continue;
                    }

                    if (TryDrawBuiltin(r, label, ft, cur, out var nv))
                    {
                        if (!Equals(nv, cur))
                        {
                            if (ft == typeof(Quaternion) && nv is Vector3 euler)
                                f.SetValue(obj, Quaternion.Euler(euler));
                            else f.SetValue(obj, nv);
                        }
                    }
                    else EditorGUI.LabelField(r, label, $"(Unsupported: {ft.Name})");
                }

                y += rowH;
            }

            return y - area.y;
        }

        public static bool TryDrawBuiltin(Rect r, string label, Type ft, object cur, out object nv)
        {
            nv = cur;

            // --- byte (0~255) ---
            if (ft == typeof(byte))
            {
                int v = cur is byte b ? b : 0;
                v = EditorGUI.IntField(r, label, v);
                v = Mathf.Clamp(v, byte.MinValue, byte.MaxValue);
                nv = (byte)v;
                return true;
            }

            // --- FixedString64Bytes (UTF-8 64바이트 제한) ---
            if (ft == typeof(FixedString64Bytes))
            {
                string s = cur is FixedString64Bytes fs ? fs.ToString() : string.Empty;
                string newS = EditorGUI.TextField(r, label, s);

                // UTF-8 64바이트 초과 시 안전하게 잘라냄(문자 경계 보존)
                if (Encoding.UTF8.GetByteCount(newS) > 64)
                    newS = TruncateUtf8ByByteLimit(newS, 64);

                nv = new FixedString64Bytes(newS);
                return true;
            }

            if (ft == typeof(int))
            {
                nv = EditorGUI.IntField(r, label, cur is int i ? i : 0);
                return true;
            }
            else if (ft == typeof(float))
            {
                nv = EditorGUI.FloatField(r, label, cur is float f ? f : 0f);
                return true;
            }
            else if (ft == typeof(bool))
            {
                nv = EditorGUI.Toggle(r, label, cur is bool b2 && b2);
                return true;
            }
            else if (ft == typeof(string))
            {
                nv = EditorGUI.TextField(r, label, cur as string ?? "");
                return true;
            }
            else if (ft == typeof(Vector2))
            {
                nv = EditorGUI.Vector2Field(r, label, cur is Vector2 v2 ? v2 : default);
                return true;
            }
            else if (ft == typeof(Vector3))
            {
                nv = EditorGUI.Vector3Field(r, label, cur is Vector3 v3 ? v3 : default);
                return true;
            }
            else if (ft == typeof(Vector4))
            {
                nv = EditorGUI.Vector4Field(r, label, cur is Vector4 v4 ? v4 : default);
                return true;
            }
            else if (ft == typeof(Quaternion))
            {
                nv = EditorGUI.Vector3Field(r, label + " (Euler)",
                    (cur is Quaternion q ? q : Quaternion.identity).eulerAngles);
                return true;
            }
            else if (ft == typeof(Color))
            {
                nv = EditorGUI.ColorField(r, label, cur is Color c ? c : Color.white);
                return true;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(ft))
            {
                nv = EditorGUI.ObjectField(r, label, cur as UnityEngine.Object, ft, true);
                return true;
            }

            return false;
        }

        // Unity.Mathematics 벡터 (float2/3/4, int/uint/bool2/3/4)
        static bool TryDrawMathVector(Rect r, string label, Type t, ref object cur)
        {
            if (!IsMatVector(t, out var dim, out var elem)) return false;
            var fx = t.GetField("x");
            var fy = t.GetField("y");
            var fz = dim >= 3 ? t.GetField("z") : null;
            var fw = dim >= 4 ? t.GetField("w") : null;

            object inst = cur ?? SafeNew(t);
            object vx = fx?.GetValue(inst), vy = fy?.GetValue(inst);
            object vz = fz != null ? fz.GetValue(inst) : null, vw = fw != null ? fw.GetValue(inst) : null;

            if (elem == typeof(float) || elem == typeof(double))
            {
                if (dim == 2)
                {
                    var v = new Vector2(ToF(vx), ToF(vy));
                    var nv = EditorGUI.Vector2Field(r, label, v);
                    if ((v - nv).sqrMagnitude > 1e-12f)
                    {
                        fx?.SetValue(inst, ElemF(elem, nv.x));
                        fy?.SetValue(inst, ElemF(elem, nv.y));
                    }

                    cur = inst;
                    return true;
                }

                if (dim == 3)
                {
                    var v = new Vector3(ToF(vx), ToF(vy), ToF(vz));
                    var nv = EditorGUI.Vector3Field(r, label, v);
                    if ((v - nv).sqrMagnitude > 1e-12f)
                    {
                        fx?.SetValue(inst, ElemF(elem, nv.x));
                        fy?.SetValue(inst, ElemF(elem, nv.y));
                        fz?.SetValue(inst, ElemF(elem, nv.z));
                    }

                    cur = inst;
                    return true;
                }

                if (dim == 4)
                {
                    var v = new Vector4(ToF(vx), ToF(vy), ToF(vz), ToF(vw));
                    var nv = EditorGUI.Vector4Field(r, label, v);
                    if ((v - nv).sqrMagnitude > 1e-12f)
                    {
                        fx?.SetValue(inst, ElemF(elem, nv.x));
                        fy?.SetValue(inst, ElemF(elem, nv.y));
                        fz?.SetValue(inst, ElemF(elem, nv.z));
                        fw?.SetValue(inst, ElemF(elem, nv.w));
                    }

                    cur = inst;
                    return true;
                }
            }
            else if (elem == typeof(int) || elem == typeof(uint))
            {
                var lh = EditorGUIUtility.singleLineHeight;
                var rL = new Rect(r.x, r.y, EditorGUIUtility.labelWidth, lh);
                EditorGUI.LabelField(rL, label);
                float colW = (r.width - EditorGUIUtility.labelWidth) / dim;
                var rx = new Rect(r.x + EditorGUIUtility.labelWidth + 0 * colW, r.y, colW - 2, lh);
                var ry = new Rect(r.x + EditorGUIUtility.labelWidth + 1 * colW, r.y, colW - 2, lh);
                var rz = dim >= 3
                    ? new Rect(r.x + EditorGUIUtility.labelWidth + 2 * colW, r.y, colW - 2, lh)
                    : Rect.zero;
                var rw = dim >= 4
                    ? new Rect(r.x + EditorGUIUtility.labelWidth + 3 * colW, r.y, colW - 2, lh)
                    : Rect.zero;

                int ix = ToI(vx), iy = ToI(vy), iz = dim >= 3 ? ToI(vz) : 0, iw = dim >= 4 ? ToI(vw) : 0;
                ix = EditorGUI.IntField(rx, ix);
                iy = EditorGUI.IntField(ry, iy);
                if (dim >= 3) iz = EditorGUI.IntField(rz, iz);
                if (dim >= 4) iw = EditorGUI.IntField(rw, iw);
                fx?.SetValue(inst, ElemI(elem, ix));
                fy?.SetValue(inst, ElemI(elem, iy));
                if (dim >= 3) fz?.SetValue(inst, ElemI(elem, iz));
                if (dim >= 4) fw?.SetValue(inst, ElemI(elem, iw));
                cur = inst;
                return true;
            }
            else if (elem == typeof(bool))
            {
                var lh = EditorGUIUtility.singleLineHeight;
                var rL = new Rect(r.x, r.y, EditorGUIUtility.labelWidth, lh);
                EditorGUI.LabelField(rL, label);
                float colW = (r.width - EditorGUIUtility.labelWidth) / dim;
                var rx = new Rect(r.x + EditorGUIUtility.labelWidth + 0 * colW, r.y, colW - 2, lh);
                var ry = new Rect(r.x + EditorGUIUtility.labelWidth + 1 * colW, r.y, colW - 2, lh);
                var rz = dim >= 3
                    ? new Rect(r.x + EditorGUIUtility.labelWidth + 2 * colW, r.y, colW - 2, lh)
                    : Rect.zero;
                var rw = dim >= 4
                    ? new Rect(r.x + EditorGUIUtility.labelWidth + 3 * colW, r.y, colW - 2, lh)
                    : Rect.zero;

                bool bx = ToB(vx), by = ToB(vy), bz = dim >= 3 && ToB(vz), bw = dim >= 4 && ToB(vw);
                bx = EditorGUI.ToggleLeft(rx, "x", bx);
                by = EditorGUI.ToggleLeft(ry, "y", by);
                if (dim >= 3) bz = EditorGUI.ToggleLeft(rz, "z", bz);
                if (dim >= 4) bw = EditorGUI.ToggleLeft(rw, "w", bw);
                fx?.SetValue(inst, bx);
                fy?.SetValue(inst, by);
                if (dim >= 3) fz?.SetValue(inst, bz);
                if (dim >= 4) fw?.SetValue(inst, bw);
                cur = inst;
                return true;
            }

            return false;
        }

        static bool TryDrawQuaternionMath(Rect r, string label, Type ft, ref object cur)
        {
            if (ft.FullName != "Unity.Mathematics.quaternion") return false;

            object inst = cur ?? SafeNew(ft);
            float qx = 0, qy = 0, qz = 0, qw = 1;

            var fValue = ft.GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (fValue != null)
            {
                var val = fValue.GetValue(inst);
                var t4 = val?.GetType();
                qx = (float)(t4?.GetField("x")?.GetValue(val) ?? 0f);
                qy = (float)(t4?.GetField("y")?.GetValue(val) ?? 0f);
                qz = (float)(t4?.GetField("z")?.GetValue(val) ?? 0f);
                qw = (float)(t4?.GetField("w")?.GetValue(val) ?? 1f);
            }
            else
            {
                qx = (float)(ft.GetField("x")?.GetValue(inst) ?? 0f);
                qy = (float)(ft.GetField("y")?.GetValue(inst) ?? 0f);
                qz = (float)(ft.GetField("z")?.GetValue(inst) ?? 0f);
                qw = (float)(ft.GetField("w")?.GetValue(inst) ?? 1f);
            }

            var rawQ = new Quaternion(qx, qy, qz, qw);
            var euler = EditorGUI.Vector3Field(r, label + " (Euler)", rawQ.eulerAngles);
            var newQ = Quaternion.Euler(euler);
            if (newQ == rawQ) return true;

            if (fValue != null)
            {
                var val = fValue.GetValue(inst);
                var t4 = val?.GetType();
                t4?.GetField("x")?.SetValue(val, newQ.x);
                t4?.GetField("y")?.SetValue(val, newQ.y);
                t4?.GetField("z")?.SetValue(val, newQ.z);
                t4?.GetField("w")?.SetValue(val, newQ.w);
                fValue.SetValue(inst, val);
            }
            else
            {
                ft.GetField("x")?.SetValue(inst, newQ.x);
                ft.GetField("y")?.SetValue(inst, newQ.y);
                ft.GetField("z")?.SetValue(inst, newQ.z);
                ft.GetField("w")?.SetValue(inst, newQ.w);
            }

            cur = inst;
            return true;
        }

        static object SafeNew(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor != null) return Activator.CreateInstance(t);
            return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
        }

        static float ToF(object o) => o is double d ? (float)d : (o is float f ? f : 0f);
        static int ToI(object o) => o is uint u ? (int)u : (o is int i ? i : 0);
        static bool ToB(object o) => o is bool b && b;
        static object ElemF(Type elem, float v) => elem == typeof(double) ? (object)(double)v : v;
        static object ElemI(Type elem, int v) => elem == typeof(uint) ? (object)(uint)Mathf.Max(0, v) : v;

        // UTF-8 바이트 수 기준으로 안전하게 잘라내기(문자 경계 보존)
        static string TruncateUtf8ByByteLimit(string src, int maxBytes)
        {
            if (string.IsNullOrEmpty(src)) return string.Empty;
            var enc = Encoding.UTF8;
            if (enc.GetByteCount(src) <= maxBytes) return src;

            int lo = 0, hi = src.Length, ans = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int cnt = enc.GetByteCount(src.AsSpan(0, mid));
                if (cnt <= maxBytes)
                {
                    ans = mid;
                    lo = mid + 1;
                }
                else hi = mid - 1;
            }

            return src.Substring(0, ans);
        }

        static bool IsMatVector(Type t, out int dim, out Type elem)
        {
            dim = 0;
            elem = null;
            if (t == null || t.Namespace != "Unity.Mathematics") return false;

            // 이름 패턴: float2/3/4, double2/3/4, int2/3/4, uint2/3/4, bool2/3/4
            string n = t.Name;
            bool head =
                n.StartsWith("float") || n.StartsWith("double") ||
                n.StartsWith("int") || n.StartsWith("uint") ||
                n.StartsWith("bool");

            if (!head) return false;

            if (n.EndsWith("2")) dim = 2;
            else if (n.EndsWith("3")) dim = 3;
            else if (n.EndsWith("4")) dim = 4;
            if (dim == 0) return false;

            if (n.StartsWith("float")) elem = typeof(float);
            else if (n.StartsWith("double")) elem = typeof(double);
            else if (n.StartsWith("int")) elem = typeof(int);
            else if (n.StartsWith("uint")) elem = typeof(uint);
            else if (n.StartsWith("bool")) elem = typeof(bool);

            return elem != null;
        }

        static float RowH(Type ft, bool rowHeight)
        {
            float h = EditorGUIUtility.singleLineHeight;
            float pad = Mathf.Max(2f, EditorGUIUtility.standardVerticalSpacing);
            if (!rowHeight) return h + pad;

            // 살짝 더 높은 컨트롤들: Vector2/3/4, Quaternion(Euler로 표시), 수학 벡터/쿼터니언
            if (ft == typeof(Vector2) || ft == typeof(Vector3) || ft == typeof(Vector4) ||
                ft == typeof(Quaternion) ||
                (ft != null && (ft.FullName == "Unity.Mathematics.quaternion" || IsMatVector(ft, out _, out _))))
            {
                return h + pad + 30f; // ← 한 줄 높이 + 여유
            }

            // 나머지(기본형/byte/FixedString64Bytes 등)
            return h + pad; // ← 표준 여유
        }

        public static float CalcHeightForObject(object obj, Type typeOverride = null, bool rowHeight = true)
        {
            if (obj == null) return 0f;
            var t = typeOverride ?? obj.GetType();

            float y = 0f;

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var ft = f.FieldType;

                // 수학 벡터/Quaternion(수학) → 한 줄
                if (IsMatVector(ft, out _, out _) || ft.FullName == "Unity.Mathematics.quaternion")
                {
                    y += RowH(ft, rowHeight);
                    continue;
                }

                // 유니티 기본형/Vector2/3/4/Quaternion/Color/Object, byte, FixedString64Bytes 등 → 한 줄
                if (ft == typeof(int) || ft == typeof(float) || ft == typeof(bool) || ft == typeof(string) ||
                    ft == typeof(Vector2) || ft == typeof(Vector3) || ft == typeof(Vector4) ||
                    ft == typeof(Quaternion) || ft == typeof(Color) ||
                    typeof(UnityEngine.Object).IsAssignableFrom(ft) ||
                    ft == typeof(byte) || ft == typeof(Unity.Collections.FixedString64Bytes))
                {
                    y += RowH(ft, rowHeight);
                    continue;
                }

                // (미지원 타입)도 한 줄로 잡아 라벨이 겹치지 않게
                y += RowH(ft, rowHeight);
            }

            // 약간의 바닥 여유
            return y + 2f;
        }

        public static bool HasDrawableFields(Type t)
        {
            if (t == null) return false;
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var ft = f.FieldType;

                // 여기 목록은 DrawValue/TryDrawBuiltin/TryDrawMathVector에서 지원하는 타입과 일치시켜야 함
                if (ft == typeof(int) || ft == typeof(float) || ft == typeof(bool) || ft == typeof(string) ||
                    ft == typeof(byte) || ft == typeof(Unity.Collections.FixedString64Bytes) ||
                    ft == typeof(Vector2) || ft == typeof(Vector3) || ft == typeof(Vector4) ||
                    ft == typeof(Quaternion) || ft == typeof(Color) ||
                    typeof(UnityEngine.Object).IsAssignableFrom(ft) ||
                    ft.FullName == "Unity.Mathematics.quaternion" || IsMatVector(ft, out _, out _))
                {
                    return true; // 하나라도 있으면 그릴 게 있음
                }
            }

            return false;
        }
    }
}
#endif