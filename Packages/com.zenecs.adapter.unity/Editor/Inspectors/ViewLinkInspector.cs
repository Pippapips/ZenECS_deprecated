// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using ZenECS.Adapter.Unity.Presentation;
// using ZenECS.Core;
//
// [CustomEditor(typeof(ViewLink))]
// public sealed class ViewLinkInspector : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         var link = (ViewLink)target;
//         using (new EditorGUI.DisabledScope(true))
//         {
//             EditorGUILayout.IntField("HandleId", link.HandleId);
//             EditorGUILayout.IntField("EntityId", link.EntityId);
//         }
//
//         EditorGUILayout.Space();
//         using (new EditorGUILayout.HorizontalScope())
//         {
//             GUILayout.FlexibleSpace();
//             if (GUILayout.Button("Select Linked Entity in Explorer", 
//                     GUILayout.Width(300),
//                     GUILayout.Height(20)))
//             {
//                 TryOpenExplorerAndSelect(link);
//             }
//             GUILayout.FlexibleSpace();
//         }
//     }
//
//     private static void TryOpenExplorerAndSelect(ViewLink link)
//     {
//         if (!ZenECS.Adapter.Unity.Presentation.ViewHandleRegistry.TryGetByGO(link.gameObject, out _, out var eid))
//         {
//             EditorUtility.DisplayDialog("ZenECS", "이 GameObject 는 ZenECS 링크가 없습니다.", "확인");
//             return;
//         }
//         // EcsExplorerWindow API 가 공개되어 있다고 가정하고 호출
//         var wndType = System.Type.GetType(
//             "ZenECS.EditorTools.EcsExplorerWindow, Zenecs.Editor"
//         );
//         if (wndType == null)
//         {
//             EditorUtility.DisplayDialog("ZenECS", "EcsExplorerWindow 를 찾을 수 없습니다.", "확인");
//             return;
//         }
//         var wnd = EditorWindow.GetWindow(wndType);
//         // 공개 정적 메서드 패턴 예시: EcsExplorerWindow.SelectEntity(int entityId)
//         var mi = wndType.GetMethod("SelectEntity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);
//         if (mi != null)
//         {
//             if (mi.IsStatic) mi.Invoke(null, new object[] { eid });
//             else mi.Invoke(wnd, new object[] { eid });
//         }
//         else
//         {
//             // 차선책: 세계/엔티티를 전달할 수 없다면, 윈도우에 메시지를 보낼 인터페이스 활용
//             wnd.Repaint();
//         }
//     }
// }
// #endif