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
    /// Builds the gritty-realism art-direction scene: PBR rock platforms lit by
    /// an HDRI dusk sky over an emissive lava pit, with a full post-processing
    /// stack.
    ///
    /// This exists to answer one art-direction question by execution rather than
    /// argument: can URP carry a grounded, dirty, dramatic look for this game,
    /// as opposed to the flat toy look of a cartoon asset kit?
    /// </summary>
    public static class GrittyArenaBuilder
    {
        private const string ScenePath = "Assets/Scenes/ArenaGritty.unity";
        private const string RockRoot = "Assets/Art/PolyHaven/Rock";
        private const string HdriPath = "Assets/Art/PolyHaven/HDRI/night_2k.hdr";
        private const string LavaRoot = "Assets/Art/PolyHaven/Lava";
        private const string MatDir = "Assets/Art/Materials";
        private const string VolumeProfilePath = "Assets/Settings/GrittyVolume.asset";
        private const float ArenaHalfWidth = 16f;

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

        [MenuItem("Joust/Build Gritty Arena")]
        public static void BuildGrittyArena()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var rock = CreateRockMaterial();
            var lava = CreateLavaMaterial();

            SetupSkyAndLighting();
            SetupCamera();
            BuildPlatforms(rock);
            BuildLavaPit(lava);
            BuildActors(rock);
            SetupPostProcessing();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Gritty arena written to {ScenePath}");
        }

        private static Material Lit(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("URP Lit shader missing - pipeline not active");
                EditorApplication.Exit(1);
            }

            return new Material(shader) { name = name };
        }

        private static Material CreateRockMaterial()
        {
            var path = $"{MatDir}/RockCliff.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var mat = Lit("RockCliff");
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_diff.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_nor.jpg");
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_ao.jpg");

            if (diff != null) mat.SetTexture("_BaseMap", diff);
            if (nor != null)
            {
                MarkAsNormalMap($"{RockRoot}/rock_nor.jpg");
                mat.SetTexture("_BumpMap", nor);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 1.4f);
            }

            if (ao != null)
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.SetFloat("_OcclusionStrength", 1f);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }

            // Rock is rough and non-metallic; a low smoothness is what stops it
            // reading as the plastic that killed the cartoon-kit look.
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetColor("_BaseColor", new Color(0.34f, 0.33f, 0.34f));
            mat.SetTextureScale("_BaseMap", new Vector2(1.6f, 0.8f));

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>
        /// A normal map imported as a plain colour texture is sampled wrong and
        /// produces flat or inverted lighting. The importer must be told.
        /// </summary>
        private static void MarkAsNormalMap(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.NormalMap)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static Material CreateLavaMaterial()
        {
            var path = $"{MatDir}/Lava.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var mat = Lit("Lava");
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>($"{LavaRoot}/crust_diff.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{LavaRoot}/crust_nor.jpg");
            if (diff != null)
            {
                mat.SetTexture("_BaseMap", diff);
                mat.SetTextureScale("_BaseMap", new Vector2(9f, 2.5f));
                // Emission reuses the crust texture, so the glow follows the
                // cracks in the rock instead of washing the whole slab out.
                mat.SetTexture("_EmissionMap", diff);
                mat.SetTextureScale("_EmissionMap", new Vector2(9f, 2.5f));
            }

            if (nor != null)
            {
                MarkAsNormalMap($"{LavaRoot}/crust_nor.jpg");
                mat.SetTexture("_BumpMap", nor);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 1.6f);
                mat.SetTextureScale("_BumpMap", new Vector2(9f, 2.5f));
            }

            mat.SetColor("_BaseColor", new Color(0.10f, 0.055f, 0.045f));
            mat.SetColor("_EmissionColor", new Color(1.35f, 0.30f, 0.055f));
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetFloat("_Smoothness", 0.22f);
            mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SetupSkyAndLighting()
        {
            var hdri = AssetDatabase.LoadAssetAtPath<Texture>(HdriPath);
            if (hdri != null)
            {
                var skyMat = new Material(Shader.Find("Skybox/Panoramic")) { name = "DuskSky" };
                skyMat.SetTexture("_MainTex", hdri);
                skyMat.SetFloat("_Exposure", 0.55f);
                skyMat.SetFloat("_Rotation", 205f);
                AssetDatabase.CreateAsset(skyMat, $"{MatDir}/DuskSky.mat");
                RenderSettings.skybox = skyMat;
                RenderSettings.ambientMode = AmbientMode.Skybox;
            }
            else
            {
                Debug.LogWarning($"HDRI not found at {HdriPath}, falling back to gradient ambient");
                RenderSettings.ambientMode = AmbientMode.Trilight;
            }

            RenderSettings.ambientIntensity = 0.35f;

            var sun = UnityEngine.Object
                .FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.name = "Sun";
            // Low, raking dusk light: long shadows, strong shape on the rock.
            // Cold moonlight from behind and above, so rock reads as silhouette
            // and rim rather than as an evenly lit prop.
            sun.transform.rotation = Quaternion.Euler(28f, 160f, 0f);
            sun.color = new Color(0.62f, 0.72f, 1f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.Soft;

            var bounce = new GameObject("LavaBounce").AddComponent<Light>();
            bounce.type = LightType.Directional;
            bounce.transform.rotation = Quaternion.Euler(-70f, 8f, 0f);
            bounce.color = new Color(1f, 0.32f, 0.09f);
            bounce.intensity = 1.35f;
            bounce.shadows = LightShadows.None;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Dense enough to swallow the HDRI horizon: the arena should read as
            // floating in darkness over lava, not as rocks in a landscape.
            RenderSettings.fogColor = new Color(0.055f, 0.045f, 0.055f);
            RenderSettings.fogDensity = 0.042f;
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, 1.4f, -21f);
            camera.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            camera.fieldOfView = 46f;
            camera.allowHDR = true;

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        private static void BuildPlatforms(Material rock)
        {
            var parent = new GameObject("Arena").transform;

            foreach (var (x, y, width) in Layout)
            {
                var centreX = (x + width / 2f) / 640f * (ArenaHalfWidth * 2f) - ArenaHalfWidth;
                var worldY = (360f - y) / 360f * 13f - 5.5f;
                var worldWidth = width / 640f * (ArenaHalfWidth * 2f);

                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"platform_{x:F0}_{y:F0}";
                slab.transform.SetParent(parent);
                slab.transform.position = new Vector3(centreX, worldY, 0f);
                slab.transform.localScale = new Vector3(worldWidth, 0.7f, 3.2f);
                slab.GetComponent<MeshRenderer>().sharedMaterial = rock;

                // Broken rock lip along the front edge, so slabs do not read as
                // clean boxes.
                var rng = new System.Random((int)(x * 31 + y));
                var chunks = Mathf.Max(2, Mathf.RoundToInt(worldWidth));
                for (var i = 0; i < chunks; i++)
                {
                    var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chunk.name = $"{slab.name}_chunk{i}";
                    chunk.transform.SetParent(slab.transform.parent);
                    var t = (i + 0.5f) / chunks;
                    var cx = centreX - worldWidth / 2f + t * worldWidth;
                    var drop = 0.25f + (float)rng.NextDouble() * 0.45f;
                    chunk.transform.position = new Vector3(cx, worldY - drop, -0.35f);
                    chunk.transform.localScale = new Vector3(
                        worldWidth / chunks * (0.75f + (float)rng.NextDouble() * 0.4f),
                        0.5f + (float)rng.NextDouble() * 0.5f,
                        2.4f);
                    chunk.transform.rotation = Quaternion.Euler(
                        (float)rng.NextDouble() * 8f - 4f,
                        (float)rng.NextDouble() * 14f - 7f,
                        (float)rng.NextDouble() * 10f - 5f);
                    chunk.GetComponent<MeshRenderer>().sharedMaterial = rock;
                    UnityEngine.Object.DestroyImmediate(chunk.GetComponent<Collider>());
                }
            }
        }

        private static void BuildLavaPit(Material lava)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "lava";
            floor.transform.position = new Vector3(0f, -9.2f, 0f);
            floor.transform.localScale = new Vector3(120f, 1.5f, 26f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = lava;

            // Point lights along the pit so the glow falls off across the arena
            // rather than lighting it flatly.
            for (var i = -2; i <= 2; i++)
            {
                var glow = new GameObject($"lava_glow_{i + 2}").AddComponent<Light>();
                glow.type = LightType.Point;
                glow.transform.position = new Vector3(i * 9f, -7f, 0f);
                glow.color = new Color(1f, 0.38f, 0.12f);
                glow.intensity = 14f;
                glow.range = 26f;
                glow.shadows = LightShadows.None;
            }
        }

        private static void BuildActors(Material rock)
        {
            var player = BuildRider("player", new Vector3(0f, 4.2f, 0f), rock);
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            player.AddComponent<FlightPrototype>();
            player.AddComponent<ScreenWrapPrototype>().Configure(ArenaHalfWidth, 4f);

            BuildRider("rider_high", new Vector3(-7.5f, 1.4f, 0f), rock);
            BuildRider("rider_low", new Vector3(7.5f, 0.2f, 0f), rock);
        }

        /// <summary>
        /// Placeholder rider built from primitives: a mount body, neck, head and
        /// a lance. Stands in until rigged art lands, but reads at silhouette
        /// level, which is what the art-direction question needs.
        /// </summary>
        private static GameObject BuildRider(string name, Vector3 position, Material material)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mount.name = "mount_body";
            mount.transform.SetParent(root.transform, false);
            mount.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mount.transform.localScale = new Vector3(0.62f, 0.95f, 0.62f);
            mount.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(mount.GetComponent<Collider>());

            var neck = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            neck.name = "mount_neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localPosition = new Vector3(0.75f, 1.05f, 0f);
            neck.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            neck.transform.localScale = new Vector3(0.2f, 0.55f, 0.2f);
            neck.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(neck.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "mount_head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(1.12f, 1.5f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());

            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "rider_body";
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3(-0.15f, 1.25f, 0f);
            rider.transform.localScale = new Vector3(0.34f, 0.42f, 0.34f);
            rider.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(rider.GetComponent<Collider>());

            var lance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lance.name = "lance";
            lance.transform.SetParent(root.transform, false);
            lance.transform.localPosition = new Vector3(0.95f, 1.62f, 0f);
            lance.transform.localRotation = Quaternion.Euler(0f, 0f, 78f);
            lance.transform.localScale = new Vector3(0.06f, 0.85f, 0.06f);
            lance.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(lance.GetComponent<Collider>());

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.55f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            return root;
        }

        /// <summary>
        /// Post-processing stack. Every override is added with overrides:true —
        /// VolumeProfile.Add&lt;T&gt;() defaults to false and silently discards
        /// every value written to the component.
        /// </summary>
        private static void SetupPostProcessing()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.55f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.75f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1f, 0.72f, 0.5f);

            var grading = profile.Add<ColorAdjustments>(true);
            grading.postExposure.overrideState = true;
            grading.postExposure.value = 0.15f;
            grading.contrast.overrideState = true;
            grading.contrast.value = 34f;
            grading.saturation.overrideState = true;
            grading.saturation.value = -12f;
            grading.colorFilter.overrideState = true;
            grading.colorFilter.value = new Color(1f, 0.93f, 0.86f);

            var curves = profile.Add<ShadowsMidtonesHighlights>(true);
            curves.shadows.overrideState = true;
            curves.shadows.value = new Vector4(0.82f, 0.88f, 1.05f, 0f);
            curves.highlights.overrideState = true;
            curves.highlights.value = new Vector4(1.08f, 0.95f, 0.82f, 0f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.52f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.5f;

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.overrideState = true;
            grain.intensity.value = 0.5f;
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Medium3;

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.ACES;

            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }
    }
}
