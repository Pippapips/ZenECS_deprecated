#if UNITY_EDITOR && ZENECS_TRACE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Core.Diagnostics;
using ZenECS.Core.Infrastructure;
using ZenECS.Core.Messaging.Diagnostics;

namespace ZenECS.EditorTools
{
    internal static class EcsExplorerTracePanel
    {
        enum Tab { World, Systems, Messages, ViewBinding }

        // ---- Editor-only 상태 (런타임 영향 X)
        static Tab _tab = Tab.World;
        static string _search = "";
        static int _topN = 10;
        static bool _autoRefresh = true;
        static double _refreshSec = 0.5;
        static double _next = 0;
        static bool _paused = false;
        static readonly HashSet<string> _pinned = new();

        // 스파크라인용 짧은 히스토리 (시스템 LastMs)
        static readonly Dictionary<string, Queue<double>> _sysLastHist = new();
        const int Sparklen = 60;

        public static void Draw(Rect rect, EditorWindow host)
        {
            EnsureUpdateLoop(host);
            
            var trace = ZenECS.Core.Infrastructure.EcsRuntimeDirectory.TraceCenter;

            // 상단 툴바
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Segmented Tabs
                _tab = (Tab) GUILayout.Toolbar((int)_tab, new[] { "World", "Systems", "Messages", "ViewBinding" }, EditorStyles.toolbarButton, GUILayout.Width(360));

                GUILayout.Space(8);
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(160));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20))) _search = string.Empty;

                GUILayout.Space(8);
                GUILayout.Label($"Top N", EditorStyles.miniLabel, GUILayout.Width(38));
                _topN = EditorGUILayout.IntSlider(_topN, 5, 50, GUILayout.Width(160));

                GUILayout.FlexibleSpace();

                _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(48));
                EditorGUI.BeginDisabledGroup(!_autoRefresh);
                GUILayout.Label("sec", EditorStyles.miniLabel, GUILayout.Width(22));
                _refreshSec = EditorGUILayout.Slider((float)_refreshSec, 0.2f, 2.0f, GUILayout.Width(140));
                EditorGUI.EndDisabledGroup();

                _paused = GUILayout.Toggle(_paused, "Pause", EditorStyles.toolbarButton, GUILayout.Width(60));

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60))) ClearCounters(trace);
                if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(90))) ExportCsv(trace);
                if (GUILayout.Button("Pop-out", EditorStyles.toolbarButton, GUILayout.Width(70))) EcsTracersWindow.Open();
            }

            // 자동 리프레시 타이밍 관리
            if (_autoRefresh && !_paused && EditorApplication.timeSinceStartup > _next)
            {
                _next = EditorApplication.timeSinceStartup + _refreshSec;
                host.Repaint();
                if (trace != null)
                    trace.ResetFrame();
            }

            // 준비 상태가 아니면 안내 메시지 출력 후 리턴
            if (!IsTraceReady())
            {
                DrawNotReadyMessage();
                return;
            }
            
            // 본문
            switch (_tab)
            {
                case Tab.World:      DrawWorld(trace, rect); break;
                case Tab.Systems:    DrawSystems(trace, rect); break;
                case Tab.Messages:   DrawMessages(trace, rect); break;
                case Tab.ViewBinding:DrawViewBinding(trace, rect); break;
            }
        }

        static bool Match(string name) => string.IsNullOrEmpty(_search) || name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;

        static void DrawWorld(EcsTraceCenter t, Rect rect)
        {
            var w = t.World;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField($"Entities: {EcsRuntimeDirectory.World?.AliveCount ?? 0}");
                EditorGUILayout.LabelField($"Created: {w.EntityCreated} / DestroyRequested: {w.EntityDestroyRequested} / Destroyed: {w.EntityDestroyed}");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTypeTopN("Added",   w.ComponentAddedByType, _topN);
                DrawTypeTopN("Changed", w.ComponentChangedByType, _topN);
                DrawTypeTopN("Removed", w.ComponentRemovedByType, _topN);
            }
        }

        static void DrawSystems(EcsTraceCenter t, Rect rect)
        {
            // 리플렉션으로 내부 딕셔너리 접근 (t.GetType().GetField("_system", ...)) → 이전 구현 그대로 사용 가정
            var sysDict = (System.Collections.IDictionary)t.GetType()
                .GetField("_system", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(t);

            if (sysDict == null)
            {
                EditorGUILayout.HelpBox("System Trace가 아직 초기화되지 않았습니다.", MessageType.Info);
                return;
            }

            // 그리드 헤더
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("☆", GUILayout.Width(24));
                GUILayout.Label("System", EditorStyles.boldLabel, GUILayout.Width(rect.width * 0.35f));
                GUILayout.Label("Calls", GUILayout.Width(80));
                GUILayout.Label("Ex", GUILayout.Width(40));
                GUILayout.Label("Last(ms)", GUILayout.Width(80));
                GUILayout.Label("Avg(ms)", GUILayout.Width(80));
                GUILayout.Label("Frame", GUILayout.Width(60));
                GUILayout.Label("Trend", GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.Space(4);

            // 나열 (Pinned 먼저)
            var items = new List<(string name, EcsTraceCenter.SystemRunStats s)>();
            foreach (System.Collections.DictionaryEntry e in sysDict)
                items.Add(((string)e.Key, (EcsTraceCenter.SystemRunStats)e.Value));
            items = items
                .Where(x => Match(x.name))
                .OrderByDescending(x => _pinned.Contains(x.name))
                .ThenByDescending(x => x.s.AvgMs)
                .Take(200) // safety
                .ToList();

            foreach (var (name, s) in items)
            {
                using (new GUILayout.HorizontalScope())
                {
                    var pinned = _pinned.Contains(name);
                    var starTxt = pinned ? "★" : "☆";
                    if (GUILayout.Button(starTxt, GUILayout.Width(24)))
                    {
                        if (pinned) _pinned.Remove(name); else _pinned.Add(name);
                    }

                    GUILayout.Label(name, GUILayout.Width(rect.width * 0.35f));
                    GUILayout.Label($"{s.Calls}", GUILayout.Width(80));
                    GUILayout.Label($"{s.Exceptions}", GUILayout.Width(40));
                    GUILayout.Label($"{s.LastMs:F2}", GUILayout.Width(80));
                    GUILayout.Label($"{s.AvgMs:F2}", GUILayout.Width(80));
                    GUILayout.Label($"{s.FrameCalls}", GUILayout.Width(60));

                    // 스파크라인 업데이트 & 그리기
                    var q = _sysLastHist.GetValueOrDefault(name);
                    if (q == null) { q = new Queue<double>(Sparklen); _sysLastHist[name] = q; }
                    if (!_paused)
                    {
                        if (q.Count >= Sparklen) q.Dequeue();
                        q.Enqueue(s.LastMs);
                    }
                    var r = GUILayoutUtility.GetRect(100, 18, GUILayout.ExpandWidth(true));
                    DrawSparkline(r, q);
                }
            }
        }

        static void DrawMessages(EcsTraceCenter t, Rect rect)
        {
            var snap = t.Message.Snapshot()
                .OrderByDescending(x => x.pub + x.con)
                .ToArray();

            if (snap.Length == 0)
            {
                EditorGUILayout.HelpBox("메시지 트래픽이 없습니다.\n• ZENECS_TRACE ON\n• Bus Tracing 래핑 확인\n• Publish/Subscribe 발생 여부 확인", MessageType.Info);
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("☆", GUILayout.Width(24));
                GUILayout.Label("Type", EditorStyles.boldLabel, GUILayout.Width(rect.width * 0.5f));
                GUILayout.Label("Published", GUILayout.Width(100));
                GUILayout.Label("Consumed", GUILayout.Width(100));
            }

            var list = snap.Where(x => Match(x.type.Name)).ToList();
            var top = list.Take(_topN);

            foreach (var x in top)
            {
                using (new GUILayout.HorizontalScope())
                {
                    var key = x.type.FullName ?? x.type.Name;
                    var pinned = _pinned.Contains(key);
                    var starTxt = pinned ? "★" : "☆";
                    if (GUILayout.Button(starTxt, GUILayout.Width(24)))
                    {
                        if (pinned) _pinned.Remove(key); else _pinned.Add(key);
                    }
                    GUILayout.Label(x.type.Name, GUILayout.Width(rect.width * 0.5f));
                    GUILayout.Label($"{x.pub}", GUILayout.Width(100));
                    GUILayout.Label($"{x.con}", GUILayout.Width(100));
                }
            }
        }

        static void DrawViewBinding(EcsTraceCenter t, Rect rect)
        {
            var v = t.ViewBinding;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Total", $"Bind={v.Bind}  Unbind={v.Unbind}  Apply={v.Apply}  Failures={v.Failures}");
                EditorGUILayout.LabelField("Frame", $"Bind={v.FrameBind}  Unbind={v.FrameUnbind}  Apply={v.FrameApply}  Failures={v.FrameFailures}");
            }
        }

        static void DrawTypeTopN(string title, System.Collections.Concurrent.ConcurrentDictionary<Type,long> dict, int top)
        {
            GUILayout.BeginVertical("box", GUILayout.MinWidth(200));
            GUILayout.Label(title, EditorStyles.boldLabel);
            foreach (var kv in dict.Where(k => Match(k.Key.Name)).OrderByDescending(kv => kv.Value).Take(top))
                GUILayout.Label($"{kv.Key.Name} : {kv.Value}");
            GUILayout.EndVertical();
        }

        static void DrawSparkline(Rect r, IEnumerable<double> values)
        {
            var arr = values?.ToArray();
            if (arr == null || arr.Length == 0) return;

            Handles.DrawSolidRectangleWithOutline(r, new Color(0,0,0,0.03f), new Color(0,0,0,0.08f));

            double min = arr.Min(), max = arr.Max();
            if (Math.Abs(max - min) < 1e-6) max = min + 1e-6;

            Vector3 prev = Vector3.zero;
            for (int i = 0; i < arr.Length; i++)
            {
                float t = (float)i / (arr.Length - 1);
                float x = Mathf.Lerp(r.xMin+2, r.xMax-2, t);
                float y = Mathf.Lerp(r.yMax-2, r.yMin+2, (float)((arr[i]-min)/(max-min)));
                var p = new Vector3(x,y,0);
                if (i>0) Handles.DrawLine(prev, p);
                prev = p;
            }
        }

        static void ClearCounters(EcsTraceCenter t)
        {
            if (EcsRuntimeDirectory.TraceCenter == null) return;
            
            // 아주 단순하게 새 인스턴스 교체 (런타임 영향 없이, 에디터에서만)
            // 필요하다면 EcsTraceCenter에 Reset 메서드를 만들어 호출해도 됩니다.
            var field = typeof(EcsTraceCenter).GetField("Message", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(t, new MessageCounters());
            // ViewBinding/World은 누적치를 유지하는 쪽이 보통 더 유용
        }

        static void ExportCsv(EcsTraceCenter t)
        {
            if (EcsRuntimeDirectory.TraceCenter == null) return;
            
            var path = EditorUtility.SaveFilePanel("Export Trace CSV", "", $"trace_{DateTime.Now:yyyyMMdd_HHmmss}.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            using var w = new StreamWriter(path);
            w.WriteLine("Section,Name,Metric1,Metric2,Metric3,Metric4");

            // Systems
            var sysDict = (System.Collections.IDictionary)t.GetType()
                .GetField("_system", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(t);
            if (sysDict != null)
            {
                foreach (System.Collections.DictionaryEntry e in sysDict)
                {
                    var name = (string)e.Key;
                    var s = (EcsTraceCenter.SystemRunStats)e.Value;
                    w.WriteLine($"Systems,{name},{s.Calls},{s.Exceptions},{s.LastMs:F3},{s.AvgMs:F3}");
                }
            }

            // Messages
            foreach (var x in t.Message.Snapshot())
                w.WriteLine($"Messages,{x.type.Name},{x.pub},{x.con},,");

            // ViewBinding
            var v = t.ViewBinding;
            w.WriteLine($"ViewBinding,Total,{v.Bind},{v.Unbind},{v.Apply},{v.Failures}");
            w.WriteLine($"ViewBinding,Frame,{v.FrameBind},{v.FrameUnbind},{v.FrameApply},{v.FrameFailures}");

            w.Flush();
            EditorUtility.RevealInFinder(path);
        }
        
        // ==== 준비 여부 판단 ====
        static bool IsTraceReady()
        {
            return EcsRuntimeDirectory.TraceCenter != null;
        }

        public static void DrawNotReadyMessage()
        {
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new GUILayout.VerticalScope(GUILayout.Width(520)))
                {
                    EditorGUILayout.HelpBox(
                        "Tracer가 아직 준비되지 않았습니다.\n" +
                        "• ZENECS_TRACE 심볼이 켜져 있어야 합니다.\n" +
                        "• Play 모드에서 시스템/메시지가 실제로 실행되어야 데이터가 쌓입니다.\n" +
                        "• (옵션) Bus가 TracingMessageBus로 래핑되었는지 확인하세요.",
                        MessageType.Info);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }
        
        // ==== 백그라운드 업데이트 루프 ====
        static bool _updateHooked = false;
        static WeakReference<EditorWindow> _hostRef;
        static double _tickNext = 0;
        static void EnsureUpdateLoop(EditorWindow host)
        {
            if (!_updateHooked)
            {
                _hostRef = new WeakReference<EditorWindow>(host);
                EditorApplication.update += UpdateTick;
                _updateHooked = true;
                _tickNext = EditorApplication.timeSinceStartup + 0.5;
            }
        }
        static void UpdateTick()
        {
            if (_paused || !_autoRefresh) return;
            var now = EditorApplication.timeSinceStartup;
            if (now < _tickNext) return;
            _tickNext = now + _refreshSec;
            if (_hostRef != null && _hostRef.TryGetTarget(out var wnd) && wnd != null)
            {
                // Draw가 돌지 않아도 주기적으로 화면 갱신 + 카운터 프레임 리셋
                wnd.Repaint();
                if (EcsRuntimeDirectory.TraceCenter != null)
                {
                    try { ZenECS.Core.Infrastructure.EcsRuntimeDirectory.TraceCenter.ResetFrame(); } catch { /* ignore */ }
                }
            }
            else
            {
                // 창이 사라졌으면 언훅
                EditorApplication.update -= UpdateTick;
                _updateHooked = false;
            }
        }
        
        // ===== 외부에서 호출하는 정리(청소) 루틴 =====
        public static void Cleanup()
        {
            try
            {
                if (_updateHooked)
                {
                    EditorApplication.update -= UpdateTick;
                    _updateHooked = false;
                }
            }
            catch { /* ignore */ }
            _hostRef = null;
            _sysLastHist.Clear();
            _pinned.Clear();
            _tickNext = 0;
        }        
    }
}
#endif
