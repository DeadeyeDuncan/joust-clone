using System.IO;
using Joust.Combat;
using Joust.Movement;
using Joust.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Joust.Editor
{
    public static class SpikeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Spike.unity";
        private const float ArenaHalfWidth = 16f;

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
            player.AddComponent<ScreenWrapPrototype>().Configure(ArenaHalfWidth, 4f);

            CreateRider("rider_high", new Vector3(-5f, 7.5f, 0f), 1.5f);
            CreateRider("rider_low", new Vector3(5f, 7.5f, 0f), 0.2f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Spike scene written to {ScenePath}");
        }

        /// <summary>
        /// A rider is a trigger-collider capsule with a lance child at the given
        /// height. Configure() is used rather than editor-only serialization, so
        /// the scene and the PlayMode tests wire the component the same way.
        /// </summary>
        private static void CreateRider(string name, Vector3 position, float lanceHeight)
        {
            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = name;
            rider.transform.position = position;

            var collider = rider.GetComponent<Collider>();
            collider.isTrigger = true;

            var lance = new GameObject("lance").transform;
            lance.SetParent(rider.transform, false);
            lance.localPosition = new Vector3(0f, lanceHeight, 0f);

            var body = rider.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            rider.AddComponent<JoustContact>().Configure(lance, 0.5f);
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
