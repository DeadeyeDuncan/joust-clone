using System.IO;
using System.Linq;
using Joust.Combat;
using Joust.Movement;
using Joust.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Joust.Editor
{
    /// <summary>
    /// Builds the M0 art-verification scene: a Joust-shaped arena assembled from
    /// imported CC0 models, lit under URP, with the flight prototype on a real
    /// character model.
    ///
    /// This is the scene the F8 screenshots are taken from. It proves the whole
    /// import recipe end to end — scale, pivot, shared atlas material, lighting —
    /// rather than asserting any one step in isolation.
    /// </summary>
    public static class ArenaSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena.unity";
        private const string ArtRoot = "Assets/Art/Kenney/Platformer";
        private const string NatureRoot = "Assets/Art/Kenney/Nature";
        private const string MaterialPath = "Assets/Art/Kenney/KenneyAtlas.mat";
        private const float ArenaHalfWidth = 16f;

        // The reference implementation's platform layout, in its own 640x360
        // logical-pixel space, converted to world units. Keeping the shape makes
        // the arena read as Joust rather than as a generic platformer.
        private static readonly (float X, float Y, float Width)[] Layout =
        {
            (240f, 64f, 160f),
            (24f, 120f, 96f),
            (520f, 120f, 96f),
            (200f, 160f, 64f),
            (376f, 160f, 64f),
            (96f, 300f, 120f),
            (424f, 300f, 120f),
        };

        [MenuItem("Joust/Build Arena Scene")]
        public static void BuildArenaScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var material = CreateAtlasMaterial();
            SetupLighting();
            SetupCamera();
            BuildArena(material);
            BuildActors(material);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Arena scene written to {ScenePath}");
        }

        /// <summary>
        /// Kenney kits share a single colormap atlas, so one URP Lit material
        /// covers every model in the kit. Creating it explicitly avoids relying
        /// on the FBX importer's embedded-material guess, which produces
        /// Built-in Standard materials that render magenta under URP.
        /// </summary>
        private static Material CreateAtlasMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader not found - is the pipeline active?");
                EditorApplication.Exit(1);
            }

            var material = new Material(shader) { name = "KenneyAtlas" };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtRoot}/colormap.png");
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
            }

            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void SetupLighting()
        {
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var sun = lights.FirstOrDefault(l => l.type == LightType.Directional);
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.52f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.33f, 0.31f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.16f, 0.12f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.35f, 0.28f, 0.33f);
            RenderSettings.fogDensity = 0.012f;
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
            }

            // Side-on with a slight perspective, so depth reads without moving
            // gameplay off the XY plane.
            camera.transform.position = new Vector3(0f, 3.2f, -26f);
            camera.transform.rotation = Quaternion.Euler(3f, 0f, 0f);
            camera.fieldOfView = 40f;
            camera.backgroundColor = new Color(0.16f, 0.13f, 0.19f);
        }

        private static GameObject Load(string root, string modelName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{root}/{modelName}.fbx");
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = new Material[renderer.sharedMaterials.Length == 0 ? 1 : renderer.sharedMaterials.Length];
                for (var i = 0; i < slots.Length; i++)
                {
                    slots[i] = material;
                }

                renderer.sharedMaterials = slots;
            }
        }

        private static void BuildArena(Material material)
        {
            var tileModel = Load(ArtRoot, "block-grass-large");
            if (tileModel == null)
            {
                Debug.LogError($"platform model missing under {ArtRoot}");
                EditorApplication.Exit(1);
                return;
            }

            var parent = new GameObject("Arena").transform;
            const float tileWorldSize = 2f;

            foreach (var (x, y, width) in Layout)
            {
                var centreX = (x + width / 2f) / 640f * (ArenaHalfWidth * 2f) - ArenaHalfWidth;
                var worldY = (360f - y) / 360f * 13f - 5.5f;
                var worldWidth = width / 640f * (ArenaHalfWidth * 2f);
                var tiles = Mathf.Max(1, Mathf.RoundToInt(worldWidth / tileWorldSize));

                for (var i = 0; i < tiles; i++)
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(tileModel, parent);
                    tile.name = $"platform_{x:F0}_{y:F0}_{i}";
                    var offset = (i - (tiles - 1) / 2f) * tileWorldSize;
                    tile.transform.position = new Vector3(centreX + offset, worldY, 0f);
                    tile.transform.localScale = Vector3.one * tileWorldSize;
                    ApplyMaterial(tile, material);

                    if (tile.GetComponentInChildren<Collider>() == null)
                    {
                        var box = tile.AddComponent<BoxCollider>();
                        box.size = new Vector3(1f, 0.5f, 1f);
                        box.center = new Vector3(0f, -0.25f, 0f);
                    }
                }
            }

            BuildLava();
        }

        /// <summary>
        /// Stand-in for the lava floor. Built with its own untextured emissive
        /// material: assigning the Kenney colour atlas here would sample the
        /// palette strip across the floor rather than reading as molten rock.
        /// </summary>
        private static void BuildLava()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "lava_floor";
            floor.transform.position = new Vector3(0f, -8.5f, 0f);
            floor.transform.localScale = new Vector3(90f, 1.5f, 14f);

            var lava = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "LavaStandIn"
            };
            lava.SetTexture("_BaseMap", null);
            lava.SetColor("_BaseColor", new Color(0.42f, 0.08f, 0.03f));
            lava.SetColor("_EmissionColor", new Color(2.2f, 0.45f, 0.08f));
            lava.SetFloat("_Smoothness", 0.35f);
            lava.EnableKeyword("_EMISSION");
            lava.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Kenney/LavaStandIn.mat") == null)
            {
                AssetDatabase.CreateAsset(lava, "Assets/Art/Kenney/LavaStandIn.mat");
            }

            floor.GetComponent<MeshRenderer>().sharedMaterial = lava;

            var glow = new GameObject("lava_glow").AddComponent<Light>();
            glow.type = LightType.Directional;
            glow.transform.rotation = Quaternion.Euler(-55f, 12f, 0f);
            glow.color = new Color(1f, 0.42f, 0.16f);
            glow.intensity = 0.55f;
            glow.shadows = LightShadows.None;
        }

        private static void BuildActors(Material material)
        {
            var playerModel = Load(ArtRoot, "character-oobi");
            var riderA = Load(ArtRoot, "character-oodi");
            var riderB = Load(ArtRoot, "character-ooli");

            var player = SpawnActor(playerModel, "player", new Vector3(0f, 4.5f, 0f), material);
            if (player != null)
            {
                var body = player.AddComponent<Rigidbody>();
                body.useGravity = false;
                player.AddComponent<FlightPrototype>();
                player.AddComponent<ScreenWrapPrototype>().Configure(ArenaHalfWidth, 4f);

                var capsule = player.AddComponent<CapsuleCollider>();
                capsule.height = 1.6f;
                capsule.radius = 0.4f;
                capsule.center = new Vector3(0f, 0.8f, 0f);
            }

            SpawnRider(riderA, "rider_high", new Vector3(-6.5f, 1.2f, 0f), 1.6f, material);
            SpawnRider(riderB, "rider_low", new Vector3(6.5f, 0.2f, 0f), 0.3f, material);
        }

        private static GameObject SpawnActor(GameObject model, string name, Vector3 position, Material material)
        {
            if (model == null)
            {
                Debug.LogError($"model for {name} missing");
                return null;
            }

            var actor = (GameObject)PrefabUtility.InstantiatePrefab(model);
            actor.name = name;
            actor.transform.position = position;
            actor.transform.localScale = Vector3.one * 1.6f;
            ApplyMaterial(actor, material);
            return actor;
        }

        private static void SpawnRider(GameObject model, string name, Vector3 position, float lanceHeight, Material material)
        {
            var rider = SpawnActor(model, name, position, material);
            if (rider == null)
            {
                return;
            }

            var trigger = rider.AddComponent<CapsuleCollider>();
            trigger.height = 1.6f;
            trigger.radius = 0.5f;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            trigger.isTrigger = true;

            var body = rider.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var lance = new GameObject("lance").transform;
            lance.SetParent(rider.transform, false);
            lance.localPosition = new Vector3(0f, lanceHeight, 0f);

            rider.AddComponent<JoustContact>().Configure(lance, 0.5f);
        }
    }
}
