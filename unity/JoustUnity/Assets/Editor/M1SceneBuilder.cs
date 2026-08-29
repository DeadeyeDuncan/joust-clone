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
                new Color(0.86f, 0.78f, 0.55f), tuning, EnemyTier.Bounder, isPlayer: true);

            player.AddComponent<PlayerInput>();
            player.AddComponent<ScreenWrapPrototype>()
                .Configure(ArenaMetrics.ArenaHalfWidth, 4f);
            return player;
        }

        private static void BuildBounder(TuningProfile tuning, Transform target)
        {
            var bounder = BuildRiderBody("bounder", new Vector3(
                ArenaMetrics.WorldX(120f), ArenaMetrics.WorldY(120f), 0f),
                new Color(0.66f, 0.15f, 0.11f), tuning, EnemyTier.Bounder, isPlayer: false);

            bounder.AddComponent<BuzzardAI>().Configure(target);
            bounder.AddComponent<ScreenWrapPrototype>()
                .Configure(ArenaMetrics.ArenaHalfWidth, 4f);
        }

        /// <summary>
        /// A rider at the arcade's footprint: 40x32 logical pixels, so 2.0 x 1.6
        /// world units, with the lance above the mount where the duel rule can
        /// read it.
        /// </summary>
        private static GameObject BuildRiderBody(string name, Vector3 position, Color plumage,
            TuningProfile tuning, EnemyTier tier, bool isPlayer)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var material = PersistPlumage(name, plumage);

            var mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mount.name = "mount";
            mount.transform.SetParent(root.transform, false);
            mount.transform.localPosition = new Vector3(0f, ArenaMetrics.Units(10f), 0f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mount.transform.localScale = new Vector3(
                ArenaMetrics.Units(22f), ArenaMetrics.Units(20f), ArenaMetrics.Units(22f));
            mount.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(mount.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(
                ArenaMetrics.Units(16f), ArenaMetrics.Units(24f), 0f);
            head.transform.localScale = Vector3.one * ArenaMetrics.Units(9f);
            head.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var lance = new GameObject("lance").transform;
            lance.SetParent(root.transform, false);
            lance.localPosition = new Vector3(
                ArenaMetrics.Units(18f), ArenaMetrics.Units(30f), 0f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "shaft";
            shaft.transform.SetParent(lance, false);
            shaft.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
            shaft.transform.localScale = new Vector3(
                ArenaMetrics.Units(1.5f), ArenaMetrics.Units(14f), ArenaMetrics.Units(1.5f));
            shaft.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = ArenaMetrics.Units(32f);
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
