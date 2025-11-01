#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class ZenBinderPickerWindow : EditorWindow
{
    // API는 ZenComponentPickerWindow와 동일하게 맞춤
    public static void Show(
        IEnumerable<Type> allBinderTypes,
        HashSet<Type> disabled,
        Action<Type> onPick,
        Rect activatorRectGui,
        string title = "Add Binder")
    {
        var w = CreateInstance<ZenBinderPickerWindow>();
        w._title = title;
        w._onPick = onPick;
        w._all = allBinderTypes
            .Where(t => t != null && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Distinct()
            .OrderBy(t => t.FullName)
            .ToList();
        w._disabled = disabled ?? new HashSet<Type>();

        // 팝오버 스타일 위치
        var screenRect = GUIUtility.GUIToScreenRect(activatorRectGui);
        var size = new Vector2(520, 400);
        w.position = new Rect(
            Mathf.Clamp(screenRect.x, 0, Screen.currentResolution.width - size.x),
            Mathf.Clamp(screenRect.yMax, 0, Screen.currentResolution.height - size.y),
            size.x, size.y);

        w.ShowAsDropDown(screenRect, size);
        w.Focus();
    }

    string _title = "Add Binder";
    Action<Type> _onPick;
    List<Type> _all = new();
    HashSet<Type> _disabled = new();

    string _search = "";
    Vector2 _scroll;
    int _hoverIndex = -1;

    void OnGUI()
    {
        // 헤더
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
            GUI.SetNextControlName("SB_SEARCH");
            var next = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (next != _search)
            {
                _search = next;
                _hoverIndex = -1;
                Repaint();
            }
        }

        // 필터링
        var list = Filtered(_all, _search).ToList();

        // 스크롤 리스트
        var rowStyle = new GUIStyle(EditorStyles.label) { richText = true };
        using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = sv.scrollPosition;

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                bool isDisabled = _disabled.Contains(t);
                var r = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));

                // hover 처리
                if (r.Contains(Event.current.mousePosition))
                {
                    _hoverIndex = i;
                    if (Event.current.type == EventType.MouseMove) Repaint();
                }

                // 배경
                if (i == _hoverIndex)
                    EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 0.15f));

                // 표시 이름
                string nice = t.FullName;
                if (isDisabled) nice = $"<color=#888888>{nice}  (already added)</color>";

                EditorGUI.LabelField(r, nice, rowStyle);

                // 클릭
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    if (!isDisabled)
                    {
                        _onPick?.Invoke(t);
                        Close();
                        GUIUtility.ExitGUI();
                    }
                    Event.current.Use();
                }
            }

            // 빈 상태
            if (list.Count == 0)
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("No results", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }
        }

        // 키보드 네비
        HandleKeyboard(list);
    }

    IEnumerable<Type> Filtered(IEnumerable<Type> src, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return src;
        keyword = keyword.Trim();
        return src.Where(t =>
            t.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (t.FullName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
    }

    void HandleKeyboard(List<Type> list)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Escape)
        {
            Close();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.DownArrow)
        {
            _hoverIndex = Mathf.Clamp(_hoverIndex + 1, 0, Mathf.Max(0, list.Count - 1));
            e.Use();
            Repaint();
            return;
        }
        if (e.keyCode == KeyCode.UpArrow)
        {
            _hoverIndex = Mathf.Clamp(_hoverIndex - 1, 0, Mathf.Max(0, list.Count - 1));
            e.Use();
            Repaint();
            return;
        }

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            if (_hoverIndex >= 0 && _hoverIndex < list.Count)
            {
                var t = list[_hoverIndex];
                if (!_disabled.Contains(t))
                {
                    _onPick?.Invoke(t);
                    Close();
                }
                e.Use();
            }
        }
    }

    void OnEnable()
    {
        // 포커스 초기화
        EditorApplication.delayCall += () =>
        {
            Focus();
            EditorGUI.FocusTextInControl("SB_SEARCH");
        };
    }

    private void OnLostFocus()
    {
        Close();
    }
}
#endif
