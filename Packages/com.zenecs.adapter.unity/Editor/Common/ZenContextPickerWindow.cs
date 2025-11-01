#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class ZenContextPickerWindow : EditorWindow
{
    private const float ROW_HEIGHT = 22f;
    private const float PADDING    = 6f;

    private string _search = "";
    private Vector2 _scroll;
    private List<Type> _all = new();
    private HashSet<Type> _disabled = new();
    private Action<Type> _onPick = _ => { };
    private int _hover = -1;
    private GUIStyle _rowStyle;
    private GUIStyle _rowDisabledStyle;
    private GUIStyle _searchStyle;

    public static void Show(IEnumerable<Type> allContextTypes,
                            HashSet<Type> disabled,
                            Action<Type> onPick,
                            Rect activatorRectGui,
                            string title = "Add Context",
                            Vector2? size = null)
    {
        var win = CreateInstance<ZenContextPickerWindow>();
        win.titleContent = new GUIContent(title);
        win._all = allContextTypes?.ToList() ?? new List<Type>();
        win._disabled = disabled ?? new HashSet<Type>();
        win._onPick = onPick ?? (_ => { });

        var w = size?.x ?? 420f;
        var h = size?.y ?? 360f;

        // Convert GUI rect to screen rect and show as dropdown near the button
        var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(activatorRectGui.x, activatorRectGui.y));
        var screenRect = new Rect(screenPos.x, screenPos.y, activatorRectGui.width, activatorRectGui.height);

        win.ShowAsDropDown(screenRect, new Vector2(w, h));
        win.Focus();
    }

    private void OnEnable()
    {
        _rowStyle = new GUIStyle("PR Label")
        {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = ROW_HEIGHT
        };
        _rowDisabledStyle = new GUIStyle(_rowStyle)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f,1f,1f,0.35f) : new Color(0,0,0,0.45f) }
        };
        _searchStyle = "ToolbarSearchTextField";
        wantsMouseMove = true;
    }

    private void OnGUI()
    {
        DrawSearchBar();
        EditorGUILayout.Space(2);

        var filtered = Filtered().ToList();

        var viewRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        var contentRect = new Rect(0, 0, viewRect.width - 16, filtered.Count * ROW_HEIGHT + PADDING * 2);

        _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
        var y = PADDING;
        var i = 0;
        foreach (var t in filtered)
        {
            var r = new Rect(PADDING, y, contentRect.width - PADDING * 2, ROW_HEIGHT);
            var isDisabled = _disabled.Contains(t);
            var style = isDisabled ? _rowDisabledStyle : _rowStyle;

            // Hover
            if (r.Contains(Event.current.mousePosition))
                _hover = i;

            // Background hover highlight
            if (_hover == i && !isDisabled)
            {
                var bg = EditorGUIUtility.isProSkin ? new Color(1,1,1,0.06f) : new Color(0,0,0,0.06f);
                EditorGUI.DrawRect(r, bg);
            }

            // Label: FullName, and disabled tag
            var label = t.FullName ?? t.Name;
            if (isDisabled) label += "  (already added)";

            using (new EditorGUI.DisabledScope(isDisabled))
            {
                if (GUI.Button(r, label, style))
                {
                    if (!isDisabled)
                    {
                        _onPick(t);
                        Close();
                    }
                }
            }
            y += ROW_HEIGHT;
            i++;
        }
        GUI.EndScrollView();

        HandleKeyboard(filtered);
        if (Event.current.type == EventType.MouseMove) Repaint();
    }

    private void DrawSearchBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUI.SetNextControlName("ZenContextPickerSearch");
            _search = GUILayout.TextField(_search, _searchStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                _search = "";
                GUI.FocusControl("ZenContextPickerSearch");
            }
        }
    }

    private IEnumerable<Type> Filtered()
    {
        IEnumerable<Type> src = _all;
        if (!string.IsNullOrEmpty(_search))
        {
            var s = _search.Trim();
            src = src.Where(t =>
                (t.FullName ?? t.Name).IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        return src.OrderBy(t => t.FullName);
    }

    private void HandleKeyboard(List<Type> filtered)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        switch (e.keyCode)
        {
            case KeyCode.UpArrow:
                _hover = Mathf.Clamp((_hover < 0 ? filtered.Count : _hover) - 1, 0, Mathf.Max(0, filtered.Count - 1));
                e.Use(); Repaint(); break;
            case KeyCode.DownArrow:
                _hover = Mathf.Clamp(_hover + 1, 0, Mathf.Max(0, filtered.Count - 1));
                e.Use(); Repaint(); break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (_hover >= 0 && _hover < filtered.Count)
                {
                    var t = filtered[_hover];
                    if (!_disabled.Contains(t))
                    {
                        _onPick(t);
                        Close();
                        e.Use();
                    }
                }
                break;
            case KeyCode.Escape:
                Close(); e.Use(); break;
        }
    }
}
#endif
