using System.IO;
using Joust.Combat;
using Joust.Core;
using Joust.Enemies;
using Joust.Flow;
using Joust.Movement;
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

            // Riders are built at runtime by the factory, not placed here, so a
            // hatched rider and a wave-one rider come from the same code path.
            var factory = new GameObject("RiderFactory").AddComponent<RiderFactory>();
            factory.Configure(
                tuning,
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PolyPizza/Ostrich.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PolyPizza/Vulture.glb"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PolyPizza/Knight.glb"));

            var eggSpawner = new GameObject("EggSpawner").AddComponent<EggSpawner>();
            var lava = new GameObject("LavaField").AddComponent<LavaField>();

            var runner = new GameObject("WaveRunner").AddComponent<WaveRunner>();
            runner.Configure(factory, eggSpawner, lava, director);

            BuildHud(director);

            EditorSceneManager.SaveScene(scene, GameScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"M1 game scene written to {GameScene}");
        }

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
