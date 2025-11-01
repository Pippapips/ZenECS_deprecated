#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core.Binding;
using ZenECS.EditorCommon;

[CustomEditor(typeof(EntityBlueprint))]
public sealed class EntityBlueprintInspector : Editor
{
    // ───────── Components (BlueprintData) ─────────
    SerializedProperty _dataProp;        // _data
    SerializedProperty _compEntriesProp; // _data.entries
    ReorderableList _componentsList;

    // ───────── Contexts (managed reference) ─────────
    SerializedProperty _contextsProp; // _contexts (List<IContext>)
    ReorderableList _contextsList;

    // ───────── Binders (managed reference) ─────────
    SerializedProperty _bindersProp; // _binders (List<IBinder>)
    ReorderableList _bindersList;

    readonly Dictionary<string, bool> _fold = new();
    const float PAD = 6f;

    void OnEnable()
    {
        // Components
        _dataProp = serializedObject.FindProperty("_data");
        _compEntriesProp = _dataProp?.FindPropertyRelative("entries");
        if (_compEntriesProp != null && _compEntriesProp.isArray)
            BuildComponentsList();

        // Contexts
        _contextsProp = serializedObject.FindProperty("_contexts");
        if (_contextsProp != null && _contextsProp.isArray)
            BuildContextsList();

        // Binders
        _bindersProp = serializedObject.FindProperty("_binders");
        if (_bindersProp != null && _bindersProp.isArray)
            BuildBindersList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_componentsList != null)
        {
            EditorGUILayout.Space(3);
            _componentsList.DoLayoutList();
        }

        if (_contextsList != null)
        {
            EditorGUILayout.Space(8);
            _contextsList.DoLayoutList();
        }

        if (_bindersList != null)
        {
            EditorGUILayout.Space(8);
            _bindersList.DoLayoutList();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // =====================================================================
    // Components (BlueprintData 그대로)
    // =====================================================================
    void BuildComponentsList()
    {
        _componentsList = new ReorderableList(serializedObject, _compEntriesProp, true, true, true, true);

        _componentsList.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Components (BlueprintData)", EditorStyles.boldLabel);

        _componentsList.onAddDropdownCallback = (rect, list) =>
        {
            var all = ZenComponentPickerWindow.FindAllZenComponents().ToList();
            var disabled = new HashSet<Type>();

            for (int i = 0; i < _compEntriesProp.arraySize; i++)
            {
                var tname = _compEntriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("typeName").stringValue;
                var rt = BlueprintData.Resolve(tname);
                if (rt != null) disabled.Add(rt);
            }

            ZenComponentPickerWindow.Show(
                all, disabled,
                onPick: pickedType =>
                {
                    serializedObject.Update();
                    int idx = _compEntriesProp.arraySize;
                    _compEntriesProp.InsertArrayElementAtIndex(idx);
                    var elem = _compEntriesProp.GetArrayElementAtIndex(idx);
                    elem.FindPropertyRelative("typeName").stringValue = pickedType.AssemblyQualifiedName;

                    var inst = ZenDefaults.CreateWithDefaults(pickedType);
                    elem.FindPropertyRelative("json").stringValue = ComponentJson.Serialize(inst, pickedType);

                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                },
                activatorRectGui: rect,
                title: "Add Component"
            );
        };

        _componentsList.onRemoveCallback = rl =>
        {
            if (rl.index >= 0 && rl.index < _compEntriesProp.arraySize)
                _compEntriesProp.DeleteArrayElementAtIndex(rl.index);
        };

        _componentsList.elementHeightCallback = (index) =>
        {
            var e = _compEntriesProp.GetArrayElementAtIndex(index);
            var tname = e.FindPropertyRelative("typeName").stringValue;
            var key = $"comp:{index}:{tname}";

            float headerH = EditorGUIUtility.singleLineHeight + 6f;
            bool open = _fold.TryGetValue(key, out var o) ? o : true;
            if (!open) return headerH;

            var t = BlueprintData.Resolve(tname);
            bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);
            if (!hasFields) return headerH;

            var jsonProp = e.FindPropertyRelative("json");
            var obj = ComponentJson.Deserialize(jsonProp.stringValue, t);
            float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
            return headerH + 6f + Mathf.Max(0f, bodyH) + 6f;
        };

        _componentsList.drawElementCallback = (rect, index, active, focused) =>
        {
            var e = _compEntriesProp.GetArrayElementAtIndex(index);
            var pType = e.FindPropertyRelative("typeName");
            var pJson = e.FindPropertyRelative("json");
            var t = BlueprintData.Resolve(pType.stringValue);

            var key = $"comp:{index}:{pType.stringValue}";
            if (!_fold.ContainsKey(key)) _fold[key] = true;

            bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);

            var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
            bool openBefore = _fold[key];
            bool openNow = EditorGUI.Foldout(rHead, openBefore, t != null ? t.Name : "(Missing Type)", true, EditorStyles.foldoutHeader);
            _fold[key] = hasFields && openNow;

            if (!hasFields || !_fold[key]) return;

            var top = rHead.yMax + 6f;
            var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - 6f);

            if (t != null)
            {
                var obj = ComponentJson.Deserialize(pJson.stringValue, t);
                EditorGUI.BeginChangeCheck();
                ZenComponentFormGUI.DrawObject(rBody, obj, t, false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Blueprint Component");
                    pJson.stringValue = ComponentJson.Serialize(obj, t);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            }
        };
    }

    // =====================================================================
    // Contexts (managed reference: IContext 인스턴스 그대로 편집)
    // =====================================================================
    void BuildContextsList()
    {
        _contextsList = new ReorderableList(serializedObject, _contextsProp, true, true, true, true);
        _contextsList.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Contexts (managed reference, shared per-entity)", EditorStyles.boldLabel);

        _contextsList.onAddDropdownCallback = (rect, list) =>
        {
            // 1) 타입 수집: IContext 파생 + 비추상 + 파라미터 없는 생성자
            IEnumerable<Type> AllContexts()
                => UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(ZenECS.Core.Binding.IContext))
                    .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
            // 2) 이미 추가된 타입 비활성
            var disabled = new HashSet<Type>();
            for (int i = 0; i < _contextsProp.arraySize; i++)
            {
                var p = _contextsProp.GetArrayElementAtIndex(i);
                var inst = p?.managedReferenceValue;
                if (inst != null) disabled.Add(inst.GetType());
            }
            // 3) Picker 팝업
            ZenContextPickerWindow.Show(
                allContextTypes: AllContexts(),
                disabled: disabled,
                onPick: pickedType =>
                {
                    serializedObject.Update();
                    int idx = _contextsProp.arraySize;
                    _contextsProp.InsertArrayElementAtIndex(idx);
                    var elem = _contextsProp.GetArrayElementAtIndex(idx);
                    elem.managedReferenceValue = Activator.CreateInstance(pickedType);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                },
                activatorRectGui: rect,
                title: "Add Context"
            );
        };

        _contextsList.onRemoveCallback = rl =>
        {
            if (rl.index >= 0 && rl.index < _contextsProp.arraySize)
                _contextsProp.DeleteArrayElementAtIndex(rl.index);
        };

        _contextsList.elementHeightCallback = (index) =>
        {
            var p = _contextsProp.GetArrayElementAtIndex(index);
            float headerH = EditorGUIUtility.singleLineHeight + PAD;

            string key = $"ctx:{index}";
            bool open = _fold.TryGetValue(key, out var o) ? o : true;
            if (!open) return headerH;

            return headerH + PAD + EditorGUI.GetPropertyHeight(p, true) + PAD;
        };

        _contextsList.drawElementCallback = (rect, index, active, focused) =>
        {
            var p = _contextsProp.GetArrayElementAtIndex(index);
            var inst = p?.managedReferenceValue;
            string title = inst != null ? inst.GetType().Name : "(None)";

            string key = $"ctx:{index}";
            if (!_fold.ContainsKey(key)) _fold[key] = true;

            var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
            bool open = EditorGUI.Foldout(rHead, _fold[key], title, true, EditorStyles.foldoutHeader);
            _fold[key] = open;
            if (!open) return;

            var top = rHead.yMax + PAD;
            var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - PAD);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rBody, p, includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        };
    }

    // =====================================================================
    // Binders (managed reference: 누락 컨텍스트 경고 배지 포함)
    // =====================================================================
    void BuildBindersList()
    {
        _bindersList = new ReorderableList(serializedObject, _bindersProp, true, true, true, true);
        _bindersList.drawHeaderCallback = r =>
            EditorGUI.LabelField(r, "Binders (managed reference)", EditorStyles.boldLabel);

        _bindersList.onAddDropdownCallback = (rect, list) =>
        {
            // 1) 전체 바인더 타입 수집
            IEnumerable<Type> AllBinders()
                => UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(IBinder))
                    .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);
            // 2) 이미 추가된 바인더 타입은 disabled로 표시
            var disabled = new HashSet<Type>();
            for (int i = 0; i < _bindersProp.arraySize; i++)
            {
                var p = _bindersProp.GetArrayElementAtIndex(i);
                var inst = p?.managedReferenceValue;
                if (inst != null) disabled.Add(inst.GetType());
            }
            // 3) ZenBinderPickerWindow로 검색/선택 UI 표시
            ZenBinderPickerWindow.Show(
                allBinderTypes: AllBinders(),
                disabled: disabled,
                onPick: pickedType =>
                {
                    serializedObject.Update();
                    int idx = _bindersProp.arraySize;
                    _bindersProp.InsertArrayElementAtIndex(idx);
                    var elem = _bindersProp.GetArrayElementAtIndex(idx);
                    elem.managedReferenceValue = Activator.CreateInstance(pickedType);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                },
                activatorRectGui: rect,
                title: "Add Binder"
            );
        };

        _bindersList.onRemoveCallback = rl =>
        {
            if (rl.index >= 0 && rl.index < _bindersProp.arraySize)
                _bindersProp.DeleteArrayElementAtIndex(rl.index);
        };

        _bindersList.elementHeightCallback = (index) =>
        {
            var p = _bindersProp.GetArrayElementAtIndex(index);

            float headerH = EditorGUIUtility.singleLineHeight + PAD;
            float badgesH = 20f; // Required context pills row

            string key = $"binder:{index}";
            bool open = _fold.TryGetValue(key, out var o) ? o : true;

            if (!open) return headerH + badgesH + PAD;

            float bodyH = EditorGUI.GetPropertyHeight(p, GUIContent.none, true);
            return headerH + PAD + badgesH + PAD + bodyH + PAD;
        };

        _bindersList.drawElementCallback = (rect, index, active, focused) =>
        {
            var p = _bindersProp.GetArrayElementAtIndex(index);
            var inst = p?.managedReferenceValue;
            string title = inst != null ? inst.GetType().Name : "(None)";

            string key = $"binder:{index}";
            if (!_fold.ContainsKey(key)) _fold[key] = true;

            // Header
            var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, EditorGUIUtility.singleLineHeight);
            bool open = EditorGUI.Foldout(rHead, _fold[key], title, true, EditorStyles.foldoutHeader);
            _fold[key] = open;

            // --- Required Context Badges (just below header)
            var provided = GetProvidedContextTypes(_contextsProp);
            var required = GetRequiredContextsForBinder(inst);

            var rBadges = new Rect(rect.x + 8, rHead.yMax + 4f, rect.width - 16f, 20f);
            DrawRequiredContextBadges(rBadges, required, provided);

            // Warn icon if any missing
            if (required.Length > 0 && !required.All(t => IsSatisfied(t, provided)))
            {
                var warn = EditorGUIUtility.IconContent("console.warnicon.sml");
                var rIcon = new Rect(rHead.xMax - 18f, rHead.y, 16f, 16f);
                GUI.Label(rIcon, warn);
            }

            if (!open) return;

            // Body
            var top = rBadges.yMax + PAD;
            var rBody = new Rect(rect.x + 8, top, rect.width - 16, rect.yMax - top - PAD);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rBody, p, includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        };
    }

    // =====================================================================
    // Badge helpers
    // =====================================================================
    static Type[] GetRequiredContextsForBinder(object binderInstance)
    {
        if (binderInstance == null) return Array.Empty<Type>();
        var t = binderInstance.GetType();
        return t.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequireContext<>))
            .Select(x => x.GetGenericArguments()[0])
            .Distinct()
            .ToArray();
    }

    static List<Type> GetProvidedContextTypes(SerializedProperty contextsProp)
    {
        var list = new List<Type>();
        if (contextsProp == null || !contextsProp.isArray) return list;

        for (int i = 0; i < contextsProp.arraySize; i++)
        {
            var p = contextsProp.GetArrayElementAtIndex(i);
            var inst = p?.managedReferenceValue;
            if (inst != null) list.Add(inst.GetType());
        }
        return list;
    }

    static bool IsSatisfied(Type required, IEnumerable<Type> providedTypes)
    {
        foreach (var pt in providedTypes)
            if (required.IsAssignableFrom(pt))
                return true;
        return false;
    }

    static void DrawRequiredContextBadges(Rect area, Type[] required, IReadOnlyList<Type> provided)
    {
        const float h = 18f;
        float x = area.x;
        float y = area.y;
        float pad = 4f;

        if (required.Length == 0)
        {
            GUI.Label(new Rect(x, y, area.width, h), "No required contexts", EditorStyles.miniLabel);
            return;
        }

        foreach (var req in required)
        {
            string label = req.Name;
            bool ok = IsSatisfied(req, provided);

            var content = new GUIContent(label, ok ? "Context present" : "Missing context");
            var size = GUI.skin.label.CalcSize(content);
            var w = Mathf.Clamp(size.x + 16f, 60f, area.width);

            var r = new Rect(x, y, w, h);

            var bg = ok ? new Color(0.20f, 0.65f, 0.20f, 0.22f) : new Color(0.85f, 0.25f, 0.25f, 0.22f);
            EditorGUI.DrawRect(r, bg);

            var outline = new Color(0f, 0f, 0f, 0.15f);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), outline);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), outline);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), outline);
            EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), outline);

            GUI.Label(r, content, EditorStyles.miniBoldLabel);

            x += w + pad;
            if (x + 60f > area.xMax)
            {
                x = area.x;
                y += h + 2f;
            } // wrap
        }
    }
}
#endif