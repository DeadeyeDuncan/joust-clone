using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Joust.Editor
{
    public static class BuildScript
    {
        private const string Scene = "Assets/Scenes/ArenaGritty.unity";

        [MenuItem("Joust/Build Windows Player")]
        public static void BuildWindows()
        {
            var output = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "artifacts", "build", "Joust.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
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
