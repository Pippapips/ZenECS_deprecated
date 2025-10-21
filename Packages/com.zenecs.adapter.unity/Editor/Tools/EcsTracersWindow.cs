#if UNITY_EDITOR && ZENECS_TRACE
using UnityEditor;
using UnityEngine;
using ZenECS.Core.Infrastructure;

namespace ZenECS.EditorTools
{
    public sealed class EcsTracersWindow : EditorWindow
    {
        [MenuItem("ZenECS/Tools/ECS Tracers")]
        public static void Open()
        {
            var wnd = GetWindow<EcsTracersWindow>("ECS Tracers");
            wnd.minSize = new Vector2(640, 360);
            wnd.Show();
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnGUI()
        {
            var r = new Rect(0, 0, position.width, position.height);
            EcsExplorerTracePanel.Draw(r, this);
        }

        void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingPlayMode || s == PlayModeStateChange.EnteredEditMode)
            {
                // 프레임 카운터 초기화 + 패널 내부 루프/캐시 정리
                try
                {
                    EcsRuntimeDirectory.TraceCenter?.ResetFrame();
                }
                catch
                {
                    /* ignore */
                }

                EcsExplorerTracePanel.Cleanup();
                // 자동 닫지 않을 경우엔 마지막 스냅샷이 보이도록 한 번 더 Repaint
                Repaint();
            }
        }
    }
}
#endif