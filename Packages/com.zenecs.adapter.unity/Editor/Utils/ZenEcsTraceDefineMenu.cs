#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ZenECS.EditorUtils
{
    public static class ZenECSTraceDefineMenu
    {
        const string Symbol = "ZENECS_TRACE";
        const string MenuPath = "ZenECS/Tools/Enable (ZENECS_TRACE)";

        [MenuItem(MenuPath)]
        public static void Toggle()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target));
            if (defines.Contains(Symbol))
            {
                defines = string.Join(";", defines.Split(';')).Replace(Symbol, "");
                Debug.Log("[ZenECS] Tracing disabled.");
            }
            else
            {
                defines = string.IsNullOrEmpty(defines) ? Symbol : (defines + ";" + Symbol);
                Debug.Log("[ZenECS] Tracing enabled.");
            }
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target), defines);
        }

        [MenuItem(MenuPath, true)]
        public static bool ToggleValidate()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(target));
            Menu.SetChecked(MenuPath, defines.Contains(Symbol));
            return true;
        }
    }
}
#endif