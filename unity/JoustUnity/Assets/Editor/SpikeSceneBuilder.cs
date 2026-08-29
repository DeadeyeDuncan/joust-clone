using System.IO;
using Joust.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Joust.Editor
{
    public static class SpikeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Spike.unity";

        [MenuItem("Joust/Build Spike Scene")]
        public static void BuildSpikeScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 6f, -22f);
                camera.transform.rotation = Quaternion.identity;
                camera.fieldOfView = 35f;
            }

            CreatePlatform("platform_left", new Vector3(-9f, 0f, 0f), new Vector3(7f, 1f, 3f));
            CreatePlatform("platform_mid", new Vector3(0f, 5f, 0f), new Vector3(6f, 1f, 3f));
            CreatePlatform("platform_right", new Vector3(9f, 0f, 0f), new Vector3(7f, 1f, 3f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "player";
            player.transform.position = new Vector3(0f, 9f, 0f);
            player.AddComponent<Rigidbody>();
            player.AddComponent<FlightPrototype>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Spike scene written to {ScenePath}");
        }

        private static void CreatePlatform(string name, Vector3 position, Vector3 scale)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.position = position;
            platform.transform.localScale = scale;
        }
    }
}
