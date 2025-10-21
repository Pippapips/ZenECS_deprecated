#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.EditorCommon;

[CustomEditor(typeof(EntityBlueprint))]
public class EntityBlueprintInspector : Editor
{
    SerializedProperty _dataProp, _entriesProp;
    ReorderableList _list;

    // === 캐시 ===
    readonly Dictionary<string, Type> _typeCache = new(); // key: tname(AQN) -> Type
    readonly Dictionary<int, int> _jsonLineCache = new(); // index -> last counted lines (옵션)

    // 상태 캐시: Fold / Form
    readonly Dictionary<string, bool> _fold = new();

    bool _repaintQueued;

    // ── 레이아웃 상수(계산/그리기에서 동일 사용)
    const float HEAD_PAD_V = 1f; // 헤더 위/아래 여유
    static float HEAD_H => EditorGUIUtility.singleLineHeight + HEAD_PAD_V * 2f;

    const float BODY_PAD_TOP = 8f; // 헤더-바디 사이 간격
    const float BODY_PAD_BOT = 4f; // 바디-셀 하단 간격

    void OnEnable()
    {
        _dataProp = serializedObject.FindProperty("_data");
        _entriesProp = _dataProp.FindPropertyRelative("entries");

        // 드래그 비활성, Add/Remove 버튼은 유지
        _list = new ReorderableList(serializedObject, _entriesProp, false, true, true, true);

        _list.drawHeaderCallback = r =>
        {
            int count = _entriesProp.arraySize;
            var rr = new Rect(r.x + 2, r.y + 1, r.width - 4, r.height - 2);
            var rArrow = new Rect(rr.x + 5, rr.y, 14f, rr.height);
            var rLabel = new Rect(rArrow.xMax - 12f, rr.y, rr.width - (rArrow.width + 8f), rr.height);

            bool allOpenNow = areAllComponentsOpenInHeader();
            EditorGUI.BeginChangeCheck();
            bool vis = EditorGUI.Foldout(rArrow, allOpenNow, GUIContent.none, /*toggleOnLabelClick*/ false);
            if (EditorGUI.EndChangeCheck())
            {
                setAllFold(vis); // 상태 갱신
                RepaintImmediate(); // ★ 즉시
            }

            EditorGUI.LabelField(rLabel, $"Components: {count}", EditorStyles.boldLabel);

            var rrr = new Rect(r.x + 2, r.yMax - SepThickness, r.width - 4, SepThickness);
            EditorGUI.DrawRect(rrr, SepColor(false));
        };

        _list.onAddDropdownCallback = (buttonRect, list) =>
        {
            // 전체 후보 타입
            var all = ZenECS.EditorCommon.ZenComponentPickerWindow.FindAllZenComponents().ToList();

            // 이미 포함된 타입은 비활성 처리
            var disabled = new HashSet<Type>();
            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                var tname = _entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("typeName").stringValue;
                var rt = ZenECS.Adapter.Unity.Blueprints.BlueprintData.Resolve(tname);
                if (rt != null) disabled.Add(rt);
            }

            // 버튼 바로 아래에 팝업
            ZenECS.EditorCommon.ZenComponentPickerWindow.Show(
                all,
                disabled,
                onPick: (pickedType) =>
                {
                    serializedObject.Update();

                    // 새 항목 추가
                    int idx = _entriesProp.arraySize;
                    _entriesProp.InsertArrayElementAtIndex(idx);
                    var elem = _entriesProp.GetArrayElementAtIndex(idx);
                    elem.FindPropertyRelative("typeName").stringValue = pickedType.AssemblyQualifiedName;

                    // 기본값을 JSON 스냅샷으로 저장
                    var inst = ZenECS.Core.ZenDefaults.CreateWithDefaults(pickedType);
                    elem.FindPropertyRelative("json").stringValue =
                        ZenECS.Adapter.Unity.Blueprints.ComponentJson.Serialize(inst, pickedType);

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                },
                activatorRectGui: buttonRect,
                title: "Add Component"
            );
        };

        // - 버튼 동작(선택된 항목 삭제)
        _list.onRemoveCallback = (rl) =>
        {
            var e = _entriesProp.GetArrayElementAtIndex(rl.index);
            var tname = e.FindPropertyRelative("typeName").stringValue;
            var t = resolveTypeCached(tname); // ★ 캐시 사용
            if (EditorUtility.DisplayDialog(
                    "Remove Component",
                    $"정말로 이 컴포넌트를 제거할까요?\n\n{t.Name}Component",
                    "Yes", "No"))
            {
                if (rl.index >= 0 && rl.index < _entriesProp.arraySize)
                {
                    _entriesProp.DeleteArrayElementAtIndex(rl.index);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        };

        _list.elementHeightCallback = (index) =>
        {
            var e = _entriesProp.GetArrayElementAtIndex(index);
            var tname = e.FindPropertyRelative("typeName").stringValue;
            var key = $"{index}:{tname}";

            float headerH = HEAD_H;

            // 접힘 상태 먼저 확인
            bool open = _fold.TryGetValue(key, out var o) ? o : true;
            if (!open) return headerH;

            var t = resolveTypeCached(tname);
            bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);
            if (!hasFields) return headerH; // ★ 필드 없으면 바디 X

            // 정확 높이 계산
            var jsonProp = e.FindPropertyRelative("json");
            var obj = ComponentJson.Deserialize(jsonProp.stringValue, t);
            float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t) - 2f; // CalcHeight가 더한 여유 2px 상쇄
            if (bodyH < 0f) bodyH = 0f;
            return headerH + BODY_PAD_TOP + bodyH + BODY_PAD_BOT;
        };

        _list.drawElementBackgroundCallback = (rect, index, active, focused) =>
        {
            // 1) 선택 배경을 먼저 직접 칠함 (Separator가 덮어버리는 문제 방지)
            if (Event.current.type == EventType.Repaint && active)
            {
                var rSel = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
                var bg = SelectedRowColor(focused);
                EditorGUI.DrawRect(rSel, bg);

                // 옅은 보더로 가독성 보강
                var border = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f, bg.a);
                EditorGUI.DrawRect(new Rect(rSel.x, rSel.y, rSel.width, 1), border);
                EditorGUI.DrawRect(new Rect(rSel.x, rSel.yMax - 1, rSel.width, 1), border);
                EditorGUI.DrawRect(new Rect(rSel.x, rSel.y, 1, rSel.height), border);
                EditorGUI.DrawRect(new Rect(rSel.xMax - 1, rSel.y, 1, rSel.height), border);
            }

            // 2) 마지막 항목이 아니면 하단 구분선
            if (index == _entriesProp.arraySize - 1) return;
            var r = new Rect(rect.x + 4, rect.yMax - SepThickness, rect.width - 8, SepThickness);
            EditorGUI.DrawRect(r, SepColor(active));
        };

        _list.drawElementCallback = (rect, index, active, focused) =>
        {
            var e = _entriesProp.GetArrayElementAtIndex(index);
            var pType = e.FindPropertyRelative("typeName");
            var pJson = e.FindPropertyRelative("json");
            var t = resolveTypeCached(pType.stringValue);

            var key = $"{index}:{pType.stringValue}";
            if (!_fold.ContainsKey(key)) _fold[key] = true;

            bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);

            var rHead = new Rect(rect.x + 4, rect.y + HEAD_PAD_V, rect.width - 8,
                EditorGUIUtility.singleLineHeight);

            bool openBefore = _fold[key];
            bool openNow = openBefore;
            ZenFoldoutHeader.DrawRow(ref openNow, rHead, t != null ? $"{t.Name}" : "(Missing Type)", t.ToString(),
                rRight =>
                {
                    using (new EditorGUI.DisabledScope(t == null))
                    {
                        var rReset = new Rect(rRight.xMax - 23, rRight.y + 2, 20, rRight.height);

                        // Reset — 필드 없으면 비활성
                        using (new EditorGUI.DisabledScope(!hasFields))
                        {
                            if (GUI.Button(rReset, "R"))
                            {
                                if (EditorUtility.DisplayDialog(
                                        "Reset Component",
                                        $"정말로 이 컴포넌트 값을 기본값으로 초기화할까요?\n\n{t.Name}Component",
                                        "Yes", "No"))
                                {
                                    var def = ZenECS.Core.ZenDefaults.CreateWithDefaults(t);
                                    pJson.stringValue = ComponentJson.Serialize(def, t);
                                    _jsonLineCache.Remove(index);
                                    requestRepaint();
                                }
                            }
                        }
                    }
                }, foldable: hasFields); // ★ 필드 없으면 화살표/펼침 없음

            // 필드 없으면 항상 접힘으로 고정
            _fold[key] = hasFields && openNow;

            // ★ 토글이 바뀐 순간 즉시 리페인트
            if (openNow != openBefore)
            {
                RepaintImmediate();
            }

            if (!hasFields || !_fold[key]) return;

            // ── 바디
            var top = rHead.yMax + BODY_PAD_TOP;
            var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - BODY_PAD_BOT);

            if (t != null)
            {
                var obj = ComponentJson.Deserialize(pJson.stringValue, t);
                EditorGUI.BeginChangeCheck();
                // Draw에 쓸 rBody 높이도 elementHeight 계산과 동일 기준이 되도록 보정
                var bh = ZenComponentFormGUI.CalcHeightForObject(obj, t) - 2f;
                if (bh < 0f) bh = 0f;
                var rbFixed = new Rect(rBody.x, rBody.y, rBody.width, bh);
                ZenComponentFormGUI.DrawObject(rbFixed, obj, t, false);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Blueprint Component");
                    pJson.stringValue = ComponentJson.Serialize(obj, t);
                    serializedObject.ApplyModifiedProperties();  // SerializedProperty 변경 반영
                    EditorUtility.SetDirty(target);              // 에셋 더티 표시 (저장 트리거)
                    // (선택) _jsonLineCache.Remove(index);  // 높이 캐시 쓰셨다면 갱신
                    // (선택) RepaintImmediate();            // 즉시 리페인트 원하시면
                }
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        _list.DoLayoutList();

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("position"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"));

        serializedObject.ApplyModifiedProperties();
    }

    // 헤더용: 전체 펼침 상태 판단
    private bool areAllComponentsOpenInHeader()
    {
        int size = _entriesProp != null ? _entriesProp.arraySize : 0;
        if (size == 0) return false;
        for (int i = 0; i < size; i++)
        {
            var sp = _entriesProp.GetArrayElementAtIndex(i);
            var tname = sp.FindPropertyRelative("typeName").stringValue;
            var key = $"{i}:{tname}";
            // 기본값은 펼침(true)
            if (!_fold.TryGetValue(key, out var open) || !open)
                return false;
        }

        return true;
    }

    private static int countLines(string s)
    {
        if (string.IsNullOrEmpty(s)) return 1;
        int c = 1;
        for (int i = 0; i < s.Length; i++)
            if (s[i] == '\n')
                c++;
        return c;
    }

    private void setAllFold(bool open)
    {
        for (int i = 0; i < _entriesProp.arraySize; i++)
        {
            var tname = _entriesProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("typeName").stringValue;
            _fold[$"{i}:{tname}"] = open;
        }

        _jsonLineCache.Clear();
        RepaintImmediate(); // ★ 즉시
    }

    private Type resolveTypeCached(string tname)
    {
        if (string.IsNullOrEmpty(tname)) return null;
        if (_typeCache.TryGetValue(tname, out var tt)) return tt;
        tt = BlueprintData.Resolve(tname);
        _typeCache[tname] = tt;
        return tt;
    }

    private void requestRepaint()
    {
        if (_repaintQueued) return;
        _repaintQueued = true;

        // 다음 에디터 틱에서 확실히 인스펙터를 다시 그리기
        EditorApplication.delayCall += () =>
        {
            _repaintQueued = false;

            // 이 Editor 인스턴스가 여전히 살아있다면
            if (this != null)
                Repaint(); // 인스펙터 리페인트

            // 혹시 에디터 전반 갱신이 필요한 케이스(리오더러 레이아웃 꼬임)까지 케어
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        };
    }

    static Color SepColor(bool selected)
    {
        // 라이트/다크 스킨별로 적당한 중간톤
        if (EditorGUIUtility.isProSkin)
            return selected ? new Color(0, 1, 0, 0.20f) : new Color(1, 1, 1, 0.08f);
        else
            return selected ? new Color(0f, 0f, 0f, 0.25f) : new Color(0f, 0f, 0f, 0.12f);
    }

    const float SepThickness = 1f;

    void RepaintImmediate()
    {
        _repaintQueued = false; // 대기 중 취소
        Repaint(); // 이 인스펙터
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); // 필요시 전체
    }

    static Color SelectedRowColor(bool focused)
    {
        if (EditorGUIUtility.isProSkin)
            return focused ? new Color(0.18f, 0.36f, 0.60f, 0.90f) : new Color(0.18f, 0.36f, 0.60f, 0.60f);
        else
            return focused ? new Color(0.20f, 0.40f, 0.80f, 0.85f) : new Color(0.20f, 0.40f, 0.80f, 0.70f);
    }
}
#endif