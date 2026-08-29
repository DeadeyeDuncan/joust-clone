using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Joust.Editor
{
    public static class BuildScript
    {
        private const string Scene = "Assets/Scenes/Game.unity";

        [MenuItem("Joust/Build Windows Player")]
        public static void BuildWindows()
        {
            var output = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "artifacts", "build", "Joust.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

            // IL2CPP is not installed on this machine: the editor ships the
            // il2cpp data folder but not the "Windows Build Support (IL2CPP)"
            // module, and the build fails with "Currently selected scripting
            // backend (IL2CPP) is not installed" (M0 finding F9). Mono is the
            // working backend until that module is added through the Hub.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.productName = "Joust";
            PlayerSettings.companyName = "DeadeyeDuncan";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"build result={summary.result} size={summary.totalSize} errors={summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
