#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace ZenECS.EditorCommon
{
    /// 통일된 접이식 헤더(화살표+제목+우측 버튼 슬롯)
    public static class ZenFoldoutHeader
    {
        static GUIStyle _foldout;

        static GUIStyle FoldoutStyle
        {
            get
            {
                if (_foldout == null)
                {
                    _foldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold, // 제목은 굵게 유지
                    };
                }

                return _foldout;
            }
        }

        static GUIStyle _boldLabel;

        public static GUIStyle BoldLabel
        {
            get
            {
                if (_boldLabel == null)
                {
                    _boldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                }

                return _boldLabel;
            }
        }

        static GUIStyle _miniLabel;

        public static GUIStyle MiniLabel
        {
            get
            {
                if (_miniLabel == null)
                {
                    _miniLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                    };
                }

                return _miniLabel;
            }
        }

        static readonly float _rowH = EditorGUIUtility.singleLineHeight;

        public struct Scope : IDisposable
        {
            public Scope(bool opened)
            {
                /* noop, 현재는 레이아웃만 마무리 */
            }

            public void Dispose() => EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 헤더를 그리고, body는 호출측에서 바로 이어서 그리면 됨.
        /// foldable=false 이면 화살표 없이 굵은 라벨만 출력되고 isOpen은 강제로 false.
        /// </summary>
        public static Scope Begin(ref bool isOpen, string title, Action drawRightButtons = null, bool foldable = true)
        {
            EditorGUILayout.BeginVertical("box");
            var rHead = GUILayoutUtility.GetRect(10, _rowH + 4f);

            var left = new Rect(rHead.x + 2, rHead.y + 2, rHead.width - RightButtonsWidth - 4, _rowH);
            var right = new Rect(rHead.xMax - RightButtonsWidth, rHead.y + 2, RightButtonsWidth, _rowH);

            if (foldable)
            {
                isOpen = EditorGUI.Foldout(left, isOpen, title, true, FoldoutStyle);
            }
            else
            {
                // 접힘 불가: 아이콘 없이 라벨만, 펼침은 항상 false
                isOpen = false;
                EditorGUI.LabelField(left, title, BoldLabel);
            }

            // 우측 버튼 영역
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GUILayout.BeginArea(right);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                drawRightButtons?.Invoke();
            }

            GUILayout.EndArea();
            EditorGUI.indentLevel = oldIndent;

            return new Scope(isOpen);
        }

        public static bool ToggleButton(bool state, string label, float width = 60f)
            => GUILayout.Toggle(state, label, "Button", GUILayout.Width(width));

        public static bool SmallButton(string label, float width = 56f)
            => GUILayout.Button(label, GUILayout.Width(width));

        // === 절대좌표 버전(리스트 요소용) =========================================
        public const float RightButtonsWidth = 186f;

        /// <summary>
        /// 리스트/절대좌표 버전의 헤더. foldable=false 이면 화살표 없이 라벨만 그립니다.
        /// </summary>
        public static void DrawRow(ref bool isOpen, Rect fullRect, string title, string nameSpace,
            Action<Rect> drawRightButtons = null,
            bool foldable = true, bool noMarginTitle = true)
        {
            var rowH = EditorGUIUtility.singleLineHeight;
            var left = new Rect(fullRect.x + 2, fullRect.y + 1, fullRect.width - RightButtonsWidth - 4, rowH);
            var right = new Rect(fullRect.xMax - RightButtonsWidth, fullRect.y + 1, RightButtonsWidth, rowH);

            if (foldable)
            {
                // 화살표 영역과 라벨 영역을 분리
                float arrowW = noMarginTitle ? 0 : 16;
                var arrowRect = new Rect(left.x, left.y, arrowW, left.height);
                var labelRect = new Rect(left.x + arrowW + 2f, left.y, left.width - arrowW - 2f, left.height);

                // 라벨클릭으로는 토글되지 않도록 false
                isOpen = EditorGUI.Foldout(arrowRect, isOpen, GUIContent.none, false, FoldoutStyle);

                if (!string.IsNullOrEmpty(nameSpace))
                {
                    if (noMarginTitle)
                    {
                        var nameGc = new GUIContent(title);
                        var nsGc = new GUIContent($"[{nameSpace}]");

                        var bold = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleLeft
                        };
                        var mini = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            normal =
                            {
                                textColor = EditorGUIUtility.isProSkin
                                    ? new Color(0.75f, 0.75f, 0.75f, 1)
                                    : new Color(0.35f, 0.35f, 0.35f, 1)
                            }
                        };

                        var gap = 6;
                        DrawInlineLabels(labelRect, nameGc, nsGc, gap, bold, mini);
                    }
                    else
                    {
                        var nameGc = new GUIContent(title);
                        var nsGc = new GUIContent($"[{nameSpace}]");
                        DrawTwoLineLabels(labelRect, nameGc, nsGc);
                        GUILayoutUtility.GetRect(1, 16);
                    }
                }
                else
                {
                    EditorGUI.LabelField(labelRect, title, BoldLabel);
                }
            }
            else
            {
                isOpen = false;
                EditorGUI.LabelField(left, title, BoldLabel);
            }

            drawRightButtons?.Invoke(right);
        }

        static void DrawTwoLineLabels(Rect rect,
            GUIContent line1, GUIContent line2,
            GUIStyle style1 = null, GUIStyle style2 = null,
            float vGap = 2f)
        {
            style1 ??= EditorStyles.label;
            style2 ??= EditorStyles.miniLabel;
            style2.normal.textColor = new Color(0.55f, 0.55f, 0.55f, 1);

            float h1 = style1.CalcHeight(line1, rect.width);
            float h2 = style2.CalcHeight(line2, rect.width);

            var r1 = new Rect(rect.x, rect.y, rect.width, h1);
            var r2 = new Rect(rect.x, r1.yMax + vGap, rect.width, h2);

            EditorGUI.LabelField(r1, line1, style1);
            EditorGUI.LabelField(r2, line2, style2);
        }
        
        static void DrawInlineLabels(Rect lineRect,
            GUIContent left, GUIContent right,
            float gap = 6f,
            GUIStyle leftStyle = null, GUIStyle rightStyle = null)
        {
            leftStyle ??= EditorStyles.label; // 필요시 EditorStyles.boldLabel 등 지정
            rightStyle ??= EditorStyles.miniLabel; // 네임스페이스처럼 옅은 글씨에 좋아요

            // 1) 왼쪽 라벨의 실제 필요폭 계산 (클리핑 전에)
            var leftSize = leftStyle.CalcSize(left);
            float leftW = Mathf.Min(leftSize.x, lineRect.width);

            // 2) 좌/우 Rect 계산
            var rLeft = new Rect(lineRect.x, lineRect.y, leftW, lineRect.height);
            var rRight = new Rect(rLeft.xMax + gap, lineRect.y,
                Mathf.Max(0, lineRect.xMax - (rLeft.xMax + gap)),
                lineRect.height);

            // 3) 오른쪽은 클리핑을 켭니다 (넘치면 깔끔히 잘림)
            var rightClipped = new GUIStyle(rightStyle) { clipping = TextClipping.Clip };

            // 4) 그리기
            EditorGUI.LabelField(rLeft, left, leftStyle);
            if (rRight.width > 1f)
                EditorGUI.LabelField(rRight, right, rightClipped);
        }
    }
}
#endif