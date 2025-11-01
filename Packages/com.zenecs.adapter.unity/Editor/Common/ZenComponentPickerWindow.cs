#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;

namespace ZenECS.EditorCommon
{
    /// <summary>
    /// 검색 가능한 컴포넌트 타입 선택 팝업 (공용).
    /// - 필터 입력
    /// - 이미 포함된 항목은 비활성/선택 불가
    /// 사용: ZenComponentPickerWindow.Show(allTypes, disabledSet, onPick, activatorRect?, title?)
    /// </summary>
    public sealed class ZenComponentPickerWindow : EditorWindow
    {
        // 창 모드
        public enum PickerOpenMode
        {
            DropDown,
            UtilityFixedWidth
        }

        // 고정 폭/높이 한계
        const float PICKER_FIXED_W = 560f;
        const float PICKER_MIN_H = 320f;
        const float PICKER_MAX_H = 2000f;
        const float PICKER_INIT_H = 680f;
        const float EDGE_PAD = 6f;
        const float ROW_H = 22f;

        // 현재 모드/자동닫힘 플래그
        PickerOpenMode _openMode = PickerOpenMode.DropDown;
        bool _closeOnLostFocus = true;

        List<string> _nsOptions; // "(All)", "(global)", "My.Gameplay", ...
        int _nsIndex = 0; // 현재 선택 인덱스

        // 스타일 캐시
        static GUIStyle _nameStyle;
        static GUIStyle _nsStyle;

        static GUIStyle NameStyle
        {
            get
            {
                if (_nameStyle == null)
                {
                    _nameStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        clipping = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _nameStyle;
            }
        }

        static GUIStyle NsStyle
        {
            get
            {
                if (_nsStyle == null)
                {
                    _nsStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        clipping = TextClipping.Clip,
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _nsStyle;
            }
        }

        List<Type> _all;
        HashSet<Type> _disabled;
        Action<Type> _onPick;

        string _title;
        string _filter = "";
        Vector2 _scroll;

        void OnEnable()
        {
            minSize = new Vector2(PICKER_FIXED_W, PICKER_MIN_H);
            maxSize = new Vector2(PICKER_FIXED_W, PICKER_MAX_H);
        }

        void OnLostFocus()
        {
            // 드롭다운/유틸리티 모두 포커스 잃으면 닫히도록 보강
            if (_closeOnLostFocus) Close();
        }

        void OnGUI()
        {
            // 유틸리티 모드에서 가로폭 고정(스크롤 측면 압력 방지)
            if (_openMode == PickerOpenMode.UtilityFixedWidth &&
                !Mathf.Approximately(position.width, PICKER_FIXED_W))
            {
                position = new Rect(position.x, position.y, PICKER_FIXED_W, position.height);
            }

            // ESC로 닫기
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                GUIUtility.ExitGUI();
            }

            titleContent = new GUIContent(_title);

            using (new EditorGUILayout.VerticalScope())
            {
                // ───────────────── Toolbar (Namespace 드롭다운(가로 꽉) + (우측) Search)
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    // 네임스페이스 드롭다운
                    if (_nsOptions != null && _nsOptions.Count > 0)
                    {
                        var oldIdx = _nsIndex;

                        // 가로로 확장해서 꽉 차도록
                        _nsIndex = EditorGUILayout.Popup(
                            _nsIndex,
                            _nsOptions.ToArray(),
                            EditorStyles.toolbarPopup,
                            GUILayout.ExpandWidth(true)
                        );

                        if (_nsIndex != oldIdx) Repaint();
                    }

                    // 드롭다운과 검색창 사이 살짝 간격
                    GUILayout.Space(6f);

                    // 검색창
                    var newFilter = GUILayout.TextField(_filter,
                        GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.textField,
                        // 기존 크기 느낌 유지: 최소 140, 최대 220 정도로 고정폭 느낌
                        GUILayout.MinWidth(140), GUILayout.MaxWidth(220));

                    if (newFilter != _filter)
                    {
                        _filter = newFilter;
                        Repaint();
                    }

                    if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22)))
                    {
                        _filter = "";
                        GUI.FocusControl(null);
                        Repaint();
                    }
                }

                EditorGUILayout.Space(2);

                // ───────────────── 리스트
                using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = sv.scrollPosition;

                    // 필터링: 네임스페이스(정확히 일치) → 텍스트 순으로
                    IEnumerable<Type> q = _all;
                    if (_nsOptions != null && _nsIndex > 0) // 0은 (All)
                    {
                        string sel = _nsOptions[_nsIndex];
                        if (sel == "(global)")
                        {
                            q = q.Where(t => string.IsNullOrEmpty(t?.Namespace));
                        }
                        else
                        {
                            q = q.Where(t => string.Equals(t?.Namespace, sel, StringComparison.Ordinal));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(_filter))
                    {
                        q = q.Where(t =>
                            t.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (t.FullName?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                    }

                    var list = q.ToList();
                    if (list.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No matching components.", MessageType.Info);
                        return;
                    }

                    foreach (var t in list)
                    {
                        bool disabled = _disabled.Contains(t);
                        using (new EditorGUI.DisabledScope(disabled))
                        {
                            var r = GUILayoutUtility.GetRect(10, ROW_H, GUILayout.ExpandWidth(true));
                            var left = new Rect(r.x + 4, r.y, r.width - 28, r.height);
                            var right = new Rect(r.xMax - 24, r.y + 1, 20, r.height - 2);

                            // 한 줄: (All)일 때만 "TypeName — Namespace", 그 외에는 "TypeName"만
                            var textRect = new Rect(left.x, left.y, left.width, left.height);
                            var typeGc = new GUIContent(t.Name, t.FullName);
                            var nsStr = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;
                            var sep = " — ";

                            if (_nsIndex == 0) // (All)
                            {
                                // 이름 먼저
                                GUI.Label(textRect, typeGc, NameStyle);
                                // 남은 공간에 네임스페이스
                                var nameW = NameStyle.CalcSize(typeGc).x;
                                var nsStartX = textRect.x + Mathf.Min(nameW, textRect.width - 1);
                                var nsGc = new GUIContent(sep + nsStr, t.FullName);
                                var nsRect = new Rect(nsStartX, textRect.y, textRect.width - (nsStartX - textRect.x),
                                    textRect.height);
                                GUI.Label(nsRect, nsGc, NsStyle);
                            }
                            else
                            {
                                // 정확 네임스페이스 필터가 선택된 경우: 이름만
                                GUI.Label(textRect, typeGc, NameStyle);
                            }

                            // 아이콘/선택
                            if (disabled)
                            {
                                GUI.Label(right, EditorGUIUtility.IconContent("CollabConflict Icon"));
                            }
                            else
                            {
                                if (GUI.Button(right, EditorGUIUtility.IconContent("d_Toolbar Plus"), GUIStyle.none))
                                {
                                    _onPick?.Invoke(t);
                                    Close();
                                }

                                if (Event.current.type == EventType.MouseDown &&
                                    r.Contains(Event.current.mousePosition) &&
                                    Event.current.clickCount == 2)
                                {
                                    _onPick?.Invoke(t);
                                    Close();
                                }
                            }
                        }
                    }
                }
            }
        }

        // 공용 탐색기: ZenComponentAttribute가 달린 타입들
        public static IEnumerable<Type> FindAllZenComponents()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try
                {
                    ts = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    ts = ex.Types.Where(x => x != null).ToArray();
                }

                foreach (var t in ts)
                {
                    if (t == null || t.IsAbstract || t.IsGenericType) continue;
                    if (!t.IsClass && !t.IsValueType) continue;
                    if (t.GetCustomAttribute<ZenComponentAttribute>() == null) continue;
                    yield return t;
                }
            }
        }

        /// <summary>
        /// 통합 Show: 버튼(gui rect) 기준으로 열기. 기본은 드롭다운(자동 닫힘).
        /// </summary>
        public static void Show(IEnumerable<Type> allTypes,
            IEnumerable<Type> disabled,
            Action<Type> onPick,
            Rect? activatorRectGui = null,
            string title = "Add Component",
            PickerOpenMode mode = PickerOpenMode.DropDown)
        {
            var win = CreateInstance<ZenComponentPickerWindow>();
            win._all = allTypes.Distinct().OrderBy(t => t.Name).ToList();
            win._disabled = new HashSet<Type>(disabled ?? Array.Empty<Type>());
            win._onPick = onPick;
            win._title = title;
            win._openMode = mode;
            win._closeOnLostFocus = true; // 자동 닫힘 활성

            // 네임스페이스 옵션 빌드 (생략 없이 그대로)
            var nsSet = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in win._all) nsSet.Add(t?.Namespace ?? "(global)");
            win._nsOptions = new List<string> { "(All)" };
            if (nsSet.Remove("(global)")) win._nsOptions.Add("(global)");
            win._nsOptions.AddRange(nsSet);

            float initH = Mathf.Clamp(PICKER_INIT_H, PICKER_MIN_H, PICKER_MAX_H);

            // 1) 버튼 스크린 Rect
            Rect anchorScr;
            if (activatorRectGui.HasValue && activatorRectGui.Value.width > 0f)
                anchorScr = GUIToScreenRect(activatorRectGui.Value);
            else
            {
                // 폴백: 마우스 위치
                var mp = Event.current != null
                    ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                    : new Vector2(Screen.currentResolution.width * 0.5f, Screen.currentResolution.height * 0.5f);
                anchorScr = new Rect(mp.x, mp.y, 1f, 1f);
            }

            // 2) 에디터 메인 창 Rect 기준으로 안전 배치/클램프
            var editorRect = GetEditorScreenRect();
            float x = anchorScr.xMin;
            // 오른쪽 넘치면 우측 정렬
            if (x + PICKER_FIXED_W > editorRect.xMax) x = anchorScr.xMax - PICKER_FIXED_W;
            // 좌/우 클램프
            x = Mathf.Clamp(x, editorRect.xMin + 6f, editorRect.xMax - PICKER_FIXED_W - 6f);

            // 아래 우선, 부족하면 위
            float y;
            float spaceBelow = editorRect.yMax - (anchorScr.yMax + 6f);
            float spaceAbove = (anchorScr.yMin - 6f) - editorRect.yMin;
            if (spaceBelow >= initH) y = anchorScr.yMax + 2f;
            else if (spaceAbove >= initH) y = anchorScr.yMin - initH - 2f;
            else
            {
                float maxH = Mathf.Clamp(Mathf.Max(spaceBelow, spaceAbove), PICKER_MIN_H, PICKER_MAX_H);
                initH = maxH;
                y = (spaceBelow >= spaceAbove)
                    ? Mathf.Clamp(anchorScr.yMax + 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f)
                    : Mathf.Clamp(anchorScr.yMin - initH - 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f);
            }

            if (mode == PickerOpenMode.DropDown)
            {
                // 드롭다운은 Unity가 배치하니 이슈 없음
                win.ShowAsDropDown(anchorScr, new Vector2(PICKER_FIXED_W, initH));
                win.Focus();
                return;
            }

            // 유틸리티 모드: 정확 좌표 배치
            win.position = new Rect(x, y, PICKER_FIXED_W, initH);
            win.ShowUtility();
            win.Focus();
        }

        static Rect GUIToScreenRect(Rect guiRect)
        {
            var tl = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.xMin, guiRect.yMin));
            return new Rect(tl.x, tl.y, guiRect.width, guiRect.height);
        }

        // 헬퍼: 에디터 메인 창 스크린 Rect
        static Rect GetEditorScreenRect()
        {
            // 메인 에디터 창의 스크린 좌표 영역
            var mw = EditorGUIUtility.GetMainWindowPosition(); // x,y,w,h in screen coords
            return mw;
        }
    }
}
#endif