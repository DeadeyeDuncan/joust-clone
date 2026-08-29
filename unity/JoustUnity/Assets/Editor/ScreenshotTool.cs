using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Joust.Editor
{
    /// <summary>
    /// Renders a scene's main camera to a PNG without opening a window, so
    /// visual evidence can be produced from the command line alongside the
    /// test artifacts.
    ///
    /// Must be run WITHOUT -nographics: rendering needs a graphics device even
    /// when nothing is displayed.
    /// </summary>
    public static class ScreenshotTool
    {
        private const int DefaultWidth = 1280;
        private const int DefaultHeight = 720;

        [MenuItem("Joust/Capture Scene Screenshot")]
        public static void CaptureMenu()
        {
            Capture("Assets/Scenes/Spike.unity", ScreenshotPath("spike-scene.png"), DefaultWidth, DefaultHeight);
        }

        /// <summary>
        /// Command-line entry point. Reads -scene, -output, -width and -height
        /// from the arguments, all optional.
        /// </summary>
        public static void CaptureFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var scene = ArgValue(args, "-scene") ?? "Assets/Scenes/Spike.unity";
            var output = ArgValue(args, "-output") ?? ScreenshotPath("spike-scene.png");
            var width = int.TryParse(ArgValue(args, "-width"), out var w) ? w : DefaultWidth;
            var height = int.TryParse(ArgValue(args, "-height"), out var h) ? h : DefaultHeight;

            Capture(scene, output, width, height);
        }

        private static string ArgValue(string[] args, string flag)
        {
            var index = Array.IndexOf(args, flag);
            if (index < 0 || index + 1 >= args.Length)
            {
                return null;
            }

            return args[index + 1];
        }

        private static string ScreenshotPath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "artifacts", "screenshots", fileName));
        }

        public static void Capture(string scenePath, string outputPath, int width, int height)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var camera = Camera.main;
            if (camera == null)
            {
                camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault();
            }

            if (camera == null)
            {
                Debug.LogError($"screenshot failed: no camera in {scenePath}");
                EditorApplication.Exit(1);
                return;
            }

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);

            Debug.Log($"screenshot written to {outputPath} ({width}x{height})");
        }
    }
}
