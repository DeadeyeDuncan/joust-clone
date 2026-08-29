using System.IO;
using Joust.Combat;
using Joust.Core;
using Joust.Enemies;
using Joust.Flow;
using Joust.Movement;
using Joust.Player;
using Joust.UI;
using Joust.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Joust.Editor
{
    /// <summary>
    /// Assembles the playable M1 scene: the gritty arena, a player, one bounder,
    /// the director and the HUD.
    ///
    /// It builds on top of the art scene rather than duplicating it, so the
    /// arena geometry has exactly one definition.
    /// </summary>
    public static class M1SceneBuilder
    {
        private const string ArtScene = "Assets/Scenes/ArenaGritty.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";
        private const string TuningPath = "Assets/Data/TuningProfile.asset";
        private const string PanelPath = "Assets/UI/HudPanelSettings.asset";
        private const string HudUxml = "Assets/UI/Hud.uxml";
        private const float MountHeightPx = 32f;

        [MenuItem("Joust/Build M1 Game Scene")]
        public static void BuildM1Scene()
        {
            // Regenerate the art scene, then open it and add gameplay on top.
            GrittyArenaBuilder.BuildGrittyArena();
            var scene = EditorSceneManager.OpenScene(ArtScene, OpenSceneMode.Single);

            StripPrototypes();

            var tuning = LoadOrCreateTuning();
            var director = new GameObject("GameDirector").AddComponent<GameDirector>();

            var player = BuildPlayer(tuning);
            BuildBounder(tuning, player.transform);
            BuildHud(director);

            EditorSceneManager.SaveScene(scene, GameScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"M1 game scene written to {GameScene}");
        }

        /// <summary>
        /// The art scene carries M0's throwaway prototypes AND decorative stand-in
        /// riders. Both are removed so the game scene runs only production
        /// components and shows only real actors.
        ///
        /// Stripping by component alone is not enough: the decorative riders carry
        /// no prototype component and survived, leaving four riders on screen.
        /// </summary>
        private static void StripPrototypes()
        {
            foreach (var flight in Object.FindObjectsByType<FlightPrototype>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(flight.gameObject);
            }

            foreach (var contact in Object.FindObjectsByType<JoustContact>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(contact.gameObject);
            }

            foreach (var name in new[] { "player", "rider_high", "rider_low" })
            {
                var stray = GameObject.Find(name);
                while (stray != null)
                {
                    Object.DestroyImmediate(stray);
                    stray = GameObject.Find(name);
                }
            }
        }

        private static TuningProfile LoadOrCreateTuning()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TuningProfile>(TuningPath);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory("Assets/Data");
            var profile = ScriptableObject.CreateInstance<TuningProfile>();
            AssetDatabase.CreateAsset(profile, TuningPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TuningProfile>(TuningPath);
        }

        private static GameObject BuildPlayer(TuningProfile tuning)
        {
            var player = BuildRiderBody("player", new Vector3(
                ArenaMetrics.WorldX(320f), ArenaMetrics.WorldY(150f), 0f),
                new Color(0.86f, 0.78f, 0.55f), tuning, EnemyTier.Bounder, isPlayer: true,
                mountModel: "Ostrich");

            player.AddComponent<PlayerInput>();
            player.AddComponent<ScreenWrapPrototype>()
                .Configure(ArenaMetrics.ArenaHalfWidth, 4f);
            return player;
        }

        private static void BuildBounder(TuningProfile tuning, Transform target)
        {
            var bounder = BuildRiderBody("bounder", new Vector3(
                ArenaMetrics.WorldX(120f), ArenaMetrics.WorldY(120f), 0f),
                new Color(0.66f, 0.15f, 0.11f), tuning, EnemyTier.Bounder, isPlayer: false,
                mountModel: "Vulture");

            bounder.AddComponent<BuzzardAI>().Configure(target);
            bounder.AddComponent<ScreenWrapPrototype>()
                .Configure(ArenaMetrics.ArenaHalfWidth, 4f);
        }

        /// <summary>
        /// A rider at the footprint of the original game: 40x32 logical pixels,
        /// so 2.0 x 1.6 world units, with the lance above the mount where the
        /// duel rule reads it.
        ///
        /// Uses the imported mount and knight models when present, falling back
        /// to primitives when they are not, so the scene always builds.
        /// </summary>
        private static GameObject BuildRiderBody(string name, Vector3 position, Color plumage,
            TuningProfile tuning, EnemyTier tier, bool isPlayer, string mountModel)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var mount = PlaceModel($"Assets/Art/PolyPizza/{mountModel}.glb", "mount", root.transform,
                ArenaMetrics.Units(MountHeightPx), Vector3.zero, seatOnBase: true);
            if (mount == null)
            {
                BuildPrimitiveMount(root.transform, PersistPlumage(name, plumage));
            }

            PlaceModel("Assets/Art/PolyPizza/Knight.glb", "knight", root.transform,
                ArenaMetrics.Units(20f),
                new Vector3(ArenaMetrics.Units(-2f), ArenaMetrics.Units(19f), 0f),
                seatOnBase: false);

            // The lance is what the duel rule measures, so it is authored here
            // rather than taken from any model.
            var lance = new GameObject("lance").transform;
            lance.SetParent(root.transform, false);
            lance.localPosition = new Vector3(
                ArenaMetrics.Units(13f), ArenaMetrics.Units(26f), 0f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "shaft";
            shaft.transform.SetParent(lance, false);
            shaft.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
            shaft.transform.localScale = new Vector3(
                ArenaMetrics.Units(1.4f), ArenaMetrics.Units(13f), ArenaMetrics.Units(1.4f));
            shaft.GetComponent<MeshRenderer>().sharedMaterial =
                PersistPlumage($"{name}_lance", new Color(0.24f, 0.22f, 0.20f));
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = ArenaMetrics.Units(MountHeightPx);
            capsule.radius = ArenaMetrics.Units(14f);
            capsule.center = new Vector3(0f, ArenaMetrics.Units(14f), 0f);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = ArenaMetrics.Units(20f);
            trigger.center = new Vector3(0f, ArenaMetrics.Units(16f), 0f);

            root.AddComponent<RiderMotor>().Configure(tuning);

            var rider = root.AddComponent<Rider>();
            rider.Configure(lance, tier, isPlayer);
            root.AddComponent<CombatContact>().Configure(rider, tuning.tieBandUnits);

            return root;
        }

        /// <summary>
        /// Instantiates a model, scales it to a target height, and seats it at a
        /// local offset.
        ///
        /// The model goes inside a container and is offset by its own measured
        /// bounds, so an arbitrary internal pivot in the source asset cannot
        /// shift it. Downloaded models put their origin wherever they like, and
        /// positioning them directly leaves parts floating away from the rig.
        /// </summary>
        private static GameObject PlaceModel(string assetPath, string name, Transform parent,
            float targetHeight, Vector3 localOffset, bool seatOnBase)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"model missing at {assetPath}; falling back to primitives");
                return null;
            }

            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            container.transform.localPosition = localOffset;
            container.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
            instance.name = $"{name}_model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            if (!TryMeasure(instance, out var bounds) || bounds.size.y <= 1e-5f)
            {
                return container;
            }

            instance.transform.localScale *= targetHeight / bounds.size.y;

            if (!TryMeasure(instance, out bounds))
            {
                return container;
            }

            // Move the model so the chosen anchor lands on the container origin:
            // the base for a mount that stands on a platform, the centre for a
            // rider that sits on one.
            var anchor = seatOnBase
                ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)
                : bounds.center;
            instance.transform.position += container.transform.position - anchor;

            return container;
        }

        private static bool TryMeasure(GameObject instance, out Bounds bounds)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static GameObject BuildPrimitiveMount(Transform parent, Material material)
        {
            var mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mount.name = "mount";
            mount.transform.SetParent(parent, false);
            mount.transform.localPosition = new Vector3(0f, ArenaMetrics.Units(10f), 0f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mount.transform.localScale = new Vector3(
                ArenaMetrics.Units(22f), ArenaMetrics.Units(20f), ArenaMetrics.Units(22f));
            mount.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(mount.GetComponent<Collider>());
            return mount;
        }

        /// <summary>
        /// Materials created with new Material() and left in memory all collapse
        /// to the last one when the scene is saved, so every rider comes out the
        /// same colour. Persisting each as an asset and using the loaded instance
        /// is the fix, the same one GrittyArenaBuilder needed.
        /// </summary>
        private static Material PersistPlumage(string name, Color plumage)
        {
            var path = $"Assets/Art/Materials/{name}_plumage.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor("_BaseColor", plumage);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            Directory.CreateDirectory("Assets/Art/Materials");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = $"{name}_plumage"
            };
            material.SetColor("_BaseColor", plumage);
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_Metallic", 0f);

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void BuildHud(GameDirector director)
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1920, 1080);
                AssetDatabase.CreateAsset(panel, PanelPath);
                AssetDatabase.SaveAssets();
                panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            }

            var go = new GameObject("HUD");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxml);

            var hud = go.AddComponent<HudController>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("director").objectReferenceValue = director;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
