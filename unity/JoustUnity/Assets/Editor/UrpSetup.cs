using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Joust.Editor
{
    /// <summary>
    /// Creates the project URP assets and, critically, ACTIVATES the pipeline.
    /// A package in manifest.json proves only that assemblies exist; rendering
    /// through URP requires GraphicsSettings.defaultRenderPipeline to be set.
    /// </summary>
    public static class UrpSetup
    {
        private const string SettingsDir = "Assets/Settings";
        private const string RendererPath = SettingsDir + "/JoustURP_Renderer.asset";
        private const string PipelinePath = SettingsDir + "/JoustURP.asset";

        [MenuItem("Joust/Configure URP")]
        public static void ConfigureUrp()
        {
            Directory.CreateDirectory(SettingsDir);

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"URP configured. defaultRenderPipeline={GraphicsSettings.defaultRenderPipeline}");
        }
    }
}
