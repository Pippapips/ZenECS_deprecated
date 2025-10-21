#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ZenECS.EditorUtils
{
    [InitializeOnLoad]
    public static class ZenECSGitUrlDependencyInstaller
    {
        // Extenject
        const string EXTENJECT_NAME    = "com.extenject.zenject";
        const string EXTENJECT_GIT_URL = "https://github.com/Pippapips/Zenject.git?path=UnityProject/Assets/Plugins/Zenject";

        // UniRx는 반드시 UPM 호환 포크 URL 사용(루트 또는 ?path= 로 package.json 포함)
        const string UNIRX_NAME        = "com.neuecc.unirx";
        const string UNIRX_GIT_URL     = "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts";

        const string SESSION_PROMPT_KEY = "ZENECS_BOOTSTRAP__Prompted_GITURL";

        static ZenECSGitUrlDependencyInstaller()
        {
            EditorApplication.update += OnEditorLoadedOnce;
        }

        static void OnEditorLoadedOnce()
        {
            EditorApplication.update -= OnEditorLoadedOnce;

            if (SessionState.GetBool(SESSION_PROMPT_KEY, false)) return;

            bool hasExtenject = HasInstalled(EXTENJECT_NAME, "Zenject");
            bool hasUniRx     = HasInstalled(UNIRX_NAME, "UniRx");

            if (hasExtenject && hasUniRx) return;

            SessionState.SetBool(SESSION_PROMPT_KEY, true);

            string missing = $"{(hasExtenject ? "" : "Extenject ")}{(hasUniRx ? "" : "UniRx")}".Trim();
            int choice = EditorUtility.DisplayDialogComplex(
                "ZenECS: Install Dependencies",
                $"{missing} not found.\n\nInstall via Git URLs now?",
                "Install",   // 0
                "Skip",      // 1
                "Details"    // 2
            );

            if (choice == 0) InstallViaGitUrls(!hasExtenject, !hasUniRx);
            else if (choice == 2)
            {
                EditorUtility.DisplayDialog(
                    "Details",
                    "This will append Git URL dependencies to Packages/manifest.json:\n" +
                    $"- {EXTENJECT_NAME}: {EXTENJECT_GIT_URL}\n" +
                    $"- {UNIRX_NAME}: {UNIRX_GIT_URL}\n\n" +
                    "Ensure both URLs point to UPM-compatible repos (contain package.json).",
                    "OK"
                );
            }
        }

        [MenuItem("ZenECS/Setup/Install Dependencies")]
        public static void InstallMenu()
        {
            bool needsExtenject = !HasInstalled(EXTENJECT_NAME, "Zenject");
            bool needsUniRx     = !HasInstalled(UNIRX_NAME, "UniRx");

            if (!needsExtenject && !needsUniRx)
            {
                EditorUtility.DisplayDialog("ZenECS", "UniRx & Extenject are already installed.", "OK");
                return;
            }

            InstallViaGitUrls(needsExtenject, needsUniRx);
        }

        // ---------- Helpers ----------
        static string ManifestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/manifest.json"));

        static string ReadManifest()
        {
            if (!File.Exists(ManifestPath)) throw new FileNotFoundException("manifest.json not found", ManifestPath);
            return File.ReadAllText(ManifestPath, Encoding.UTF8);
        }

        static void WriteManifest(string json)
        {
            File.Copy(ManifestPath, ManifestPath + ".bak", overwrite: true);
            File.WriteAllText(ManifestPath, json, Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        static bool HasInstalled(string packageName, string assemblyHint)
        {
            try
            {
                var json = ReadManifest();
                if (json.Contains($"\"{packageName}\"")) return true;

                // 어셈블리 힌트(보조 체크)
                return CompilationPipeline.GetAssemblies()
                    .Any(a => a.name.IndexOf(assemblyHint, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch { return false; }
        }

        static void AddDependencyByGitUrl(string name, string url)
        {
            var json = ReadManifest();
            if (json.Contains($"\"{name}\"")) return; // 이미 등록됨

            // dependencies 블럭 뒤에 줄 삽입
            json = Regex.Replace(
                json,
                "\"dependencies\"\\s*:\\s*\\{",
                m => m.Value + $"\n    \"{name}\": \"{url}\",",
                RegexOptions.Multiline
            );
            WriteManifest(json);
        }

        public static void AddViaClientAdd_UniRx()
        {
            UnityEditor.PackageManager.Client.Add(UNIRX_GIT_URL);
        }

        public static void AddViaClientAdd_Zenject()
        {
            UnityEditor.PackageManager.Client.Add(EXTENJECT_GIT_URL);
        }

        static void InstallViaGitUrls(bool addExtenject, bool addUniRx)
        {
            try
            {
                if (addExtenject) AddViaClientAdd_Zenject();
                if (addUniRx)     AddViaClientAdd_UniRx();

#if UNITY_2021_2_OR_NEWER
                Client.Resolve();
#endif
                EditorUtility.DisplayDialog("ZenECS", "Git URL dependencies added. Unity will resolve packages now.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("ZenECS", "Git URL install failed:\n" + ex.Message, "OK");
            }
        }
    }
}
#endif
