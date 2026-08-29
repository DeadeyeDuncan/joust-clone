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
        private const string GeneratedDir = "Assets/Art/Generated";
        private const string VolumeProfilePath = "Assets/Settings/GrittyVolume.asset";
        // Everything in this scene derives from the original game's logical
        // space: 640x360 pixels with the lava surface at y=344. One world unit
        // is 20 logical pixels, so the arena is 32 units wide and a mount is
        // 2.0 x 1.6 units - the proportions the arcade actually plays at.
        private const float PixelsPerUnit = 20f;
        private const float LogicalWidth = 640f;
        private const float LogicalLavaY = 344f;
        private const float ArenaHalfWidth = LogicalWidth / 2f / PixelsPerUnit;
        private const float MountWidthPx = 40f;
        private const float MountHeightPx = 32f;

        /// <summary>Logical pixel x to world x, centred on the arena.</summary>
        private static float WorldX(float pixelX) => (pixelX - LogicalWidth / 2f) / PixelsPerUnit;

        /// <summary>Logical pixel y (down-positive) to world y, lava surface at 0.</summary>
        private static float WorldY(float pixelY) => (LogicalLavaY - pixelY) / PixelsPerUnit;

        /// <summary>Logical pixel length to world units.</summary>
        private static float Units(float pixels) => pixels / PixelsPerUnit;

        // The original game's full ten-platform layout: (left x, y, width) in
        // logical pixels.
        private static readonly (float X, float Y, float Width)[] Layout =
        {
            (240f, 64f, 160f),
            (24f, 120f, 96f),
            (520f, 120f, 96f),
            (0f, 208f, 72f),
            (568f, 208f, 72f),
            (200f, 160f, 64f),
            (376f, 160f, 64f),
            (96f, 300f, 120f),
            (216f, 300f, 208f),
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
            BuildBackdrop(rock);
            BuildCaldera(lava);
            BuildActors(rock);
            BuildEmbers();
            BuildDust();
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

        /// <summary>
        /// Creating several material assets in a single frame and then assigning
        /// the in-memory instances made every renderer resolve to the same asset.
        /// Persisting immediately and returning the loaded asset keeps them
        /// distinct.
        /// </summary>
        private static Material Persist(Material mat, string path)
        {
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Material>(path);
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

            return Persist(mat, path);
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
            return Persist(mat, path);
        }

        /// <summary>
        /// Builds a material the same way RockCliff is built - base, normal and
        /// AO maps, tinted by colour. Colour-only materials with no maps were
        /// rendering as flat blue at capture time, so every surface material
        /// goes through here.
        /// </summary>
        private static Material TexturedLit(string name, string path, Color tint, float smoothness,
            float metallic, Vector2 tiling)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = Lit(name);
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_diff.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_nor.jpg");
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RockRoot}/rock_ao.jpg");

            if (diff != null)
            {
                mat.SetTexture("_BaseMap", diff);
                mat.SetTextureScale("_BaseMap", tiling);
            }

            if (nor != null)
            {
                MarkAsNormalMap($"{RockRoot}/rock_nor.jpg");
                mat.SetTexture("_BumpMap", nor);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 1.1f);
                mat.SetTextureScale("_BumpMap", tiling);
            }

            if (ao != null)
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.EnableKeyword("_OCCLUSIONMAP");
                mat.SetFloat("_OcclusionStrength", 0.9f);
            }

            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            return Persist(mat, path);
        }

        private static Material CreateArmourMaterial()
        {
            return TexturedLit("Armour", $"{MatDir}/Armour.mat",
                new Color(0.20f, 0.20f, 0.23f), 0.34f, 0.55f, new Vector2(3f, 3f));
        }

        private static Material CreatePlumageMaterial(string name, Color colour)
        {
            return TexturedLit(name, $"{MatDir}/{name}.mat", colour, 0.16f, 0f, new Vector2(3f, 3f));
        }

        /// <summary>
        /// Embers rising off the lava. Cheap, but it is what stops the pit
        /// reading as a flat lit plane and sells the heat.
        /// </summary>
        private static void BuildEmbers()
        {
            var go = new GameObject("embers");
            go.transform.position = new Vector3(0f, -10f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 6f;
            main.startSpeed = 2.2f;
            main.startSize = 0.09f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.55f, 0.15f), new Color(1f, 0.28f, 0.05f));
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.06f;

            var emission = ps.emission;
            emission.rateOverTime = 110f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(58f, 0.5f, 10f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.1f;
            noise.frequency = 0.25f;

            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.9f, 0.18f, 0.03f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f),
                        new GradientAlphaKey(0f, 1f) });
            colour.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var emberMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            {
                name = "Ember"
            };
            emberMat.SetColor("_BaseColor", new Color(1f, 0.5f, 0.15f, 1f));
            emberMat.SetFloat("_Surface", 1f);
            emberMat.SetFloat("_Blend", 1f);
            emberMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            emberMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(emberMat, $"{MatDir}/Ember.mat");
            renderer.sharedMaterial = emberMat;
        }

        /// <summary>Slow dust motes catching the moonlight, for depth.</summary>
        private static void BuildDust()
        {
            var go = new GameObject("dust");
            go.transform.position = new Vector3(0f, 0f, -2f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.25f;
            main.startSize = 0.045f;
            main.startColor = new Color(0.75f, 0.8f, 0.95f, 0.5f);
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.01f;

            var emission = ps.emission;
            emission.rateOverTime = 26f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(46f, 20f, 8f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.12f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var dustMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))
            {
                name = "Dust"
            };
            dustMat.SetColor("_BaseColor", new Color(0.8f, 0.85f, 1f, 0.35f));
            dustMat.SetFloat("_Surface", 1f);
            dustMat.SetFloat("_Blend", 1f);
            dustMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(dustMat, $"{MatDir}/Dust.mat");
            renderer.sharedMaterial = dustMat;
        }

        /// <summary>
        /// The world behind the arena. Without this the fight happens in a void:
        /// a canyon wall masks the HDRI horizon, side cliffs frame the play area,
        /// spires give parallax, and a glowing caldera behind the wall separates
        /// the silhouettes from the sky.
        /// </summary>
        private static void BuildBackdrop(Material rock)
        {
            // Backdrop blocks are many times the size of a platform, so reusing
            // the arena tiling stretched the texture across them. They get their
            // own material with a far larger repeat.
            rock = TexturedLit("RockFar", $"{MatDir}/RockFar.mat",
                new Color(0.30f, 0.29f, 0.30f), 0.06f, 0f, new Vector2(9f, 9f));
            var parent = new GameObject("Backdrop").transform;
            var rng = new System.Random(20260829);

            float Range(double a, double b) => (float)(a + rng.NextDouble() * (b - a));

            // Far canyon wall: a jagged ridge of rotated blocks.
            for (var i = -18; i <= 18; i++)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"ridge_{i + 18}";
                block.transform.SetParent(parent);
                var height = Range(9.0, 22.0);
                block.transform.position = new Vector3(
                    i * 4.4f + Range(-1.2, 1.2),
                    -14f + height / 2f,
                    Range(30.0, 38.0));
                block.transform.localScale = new Vector3(Range(4.0, 7.5), height, Range(6.0, 11.0));
                block.transform.rotation = Quaternion.Euler(Range(-4, 4), Range(-22, 22), Range(-6, 6));
                block.GetComponent<MeshRenderer>().sharedMaterial = rock;
                UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            }

            // Side cliffs, converging toward the camera to frame the arena.
            foreach (var side in new[] { -1f, 1f })
            {
                for (var i = 0; i < 7; i++)
                {
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slab.name = $"cliff_{(side < 0 ? "L" : "R")}{i}";
                    slab.transform.SetParent(parent);
                    var depth = 6f + i * 5.5f;
                    var height = Range(14.0, 28.0);
                    slab.transform.position = new Vector3(
                        side * (23f - i * 0.8f + Range(-1.5, 1.5)),
                        -14f + height / 2f,
                        depth);
                    slab.transform.localScale = new Vector3(Range(5.0, 9.0), height, Range(7.0, 12.0));
                    slab.transform.rotation = Quaternion.Euler(Range(-3, 3), Range(-18, 18), Range(-5, 5));
                    slab.GetComponent<MeshRenderer>().sharedMaterial = rock;
                    UnityEngine.Object.DestroyImmediate(slab.GetComponent<Collider>());
                }
            }

            // Mid-ground spires for parallax between the arena and the wall.
            for (var i = 0; i < 9; i++)
            {
                var spire = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spire.name = $"spire_{i}";
                spire.transform.SetParent(parent);
                var height = Range(7.0, 17.0);
                spire.transform.position = new Vector3(
                    Range(-30.0, 30.0), -13f + height / 2f, Range(17.0, 27.0));
                spire.transform.localScale = new Vector3(Range(1.6, 3.4), height, Range(1.6, 3.4));
                spire.transform.rotation = Quaternion.Euler(Range(-6, 6), Range(0, 90), Range(-6, 6));
                spire.GetComponent<MeshRenderer>().sharedMaterial = rock;
                UnityEngine.Object.DestroyImmediate(spire.GetComponent<Collider>());
            }
        }

        /// <summary>
        /// A second lava field behind the ridge, lighting it from below so the
        /// backdrop reads as depth rather than as a black cut-out.
        /// </summary>
        private static void BuildCaldera(Material lava)
        {
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pool.name = "caldera";
            pool.transform.position = new Vector3(0f, -13.5f, 26f);
            pool.transform.localScale = new Vector3(150f, 1f, 12f);
            pool.GetComponent<MeshRenderer>().sharedMaterial = lava;
            UnityEngine.Object.DestroyImmediate(pool.GetComponent<Collider>());

            for (var i = -3; i <= 3; i++)
            {
                var glow = new GameObject($"caldera_glow_{i + 3}").AddComponent<Light>();
                glow.type = LightType.Point;
                glow.transform.position = new Vector3(i * 12f, -10.5f, 26f);
                glow.color = new Color(1f, 0.34f, 0.10f);
                glow.intensity = 22f;
                glow.range = 40f;
                glow.shadows = LightShadows.None;
            }
        }

        private static void SetupSkyAndLighting()
        {
            var hdri = AssetDatabase.LoadAssetAtPath<Texture>(HdriPath);
            if (hdri != null)
            {
                var skyMat = new Material(Shader.Find("Skybox/Panoramic")) { name = "DuskSky" };
                skyMat.SetTexture("_MainTex", hdri);
                skyMat.SetFloat("_Exposure", 0.30f);
                skyMat.SetFloat("_Rotation", 165f);
                AssetDatabase.CreateAsset(skyMat, $"{MatDir}/DuskSky.mat");
                RenderSettings.skybox = skyMat;
                // Ambient taken from the blue night sky tinted every upward
                // face. An explicit trilight keeps the sky cool but lets the
                // lava warm everything from below.
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.20f, 0.22f, 0.30f);
                RenderSettings.ambientEquatorColor = new Color(0.20f, 0.15f, 0.14f);
                RenderSettings.ambientGroundColor = new Color(0.34f, 0.13f, 0.05f);
            }
            else
            {
                Debug.LogWarning($"HDRI not found at {HdriPath}, falling back to gradient ambient");
                RenderSettings.ambientMode = AmbientMode.Trilight;
            }

            RenderSettings.ambientIntensity = 1f;

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
            sun.transform.rotation = Quaternion.Euler(34f, 28f, 0f);
            sun.color = new Color(0.95f, 0.93f, 0.92f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;

            var rim = new GameObject("MoonRim").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.transform.rotation = Quaternion.Euler(18f, 205f, 0f);
            rim.color = new Color(0.62f, 0.72f, 0.95f);
            rim.intensity = 0.35f;
            rim.shadows = LightShadows.None;

            var bounce = new GameObject("LavaBounce").AddComponent<Light>();
            bounce.type = LightType.Directional;
            bounce.transform.rotation = Quaternion.Euler(-70f, 8f, 0f);
            bounce.color = new Color(1f, 0.32f, 0.09f);
            bounce.intensity = 1.5f;
            bounce.shadows = LightShadows.None;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Dense enough to swallow the HDRI horizon: the arena should read as
            // floating in darkness over lava, not as rocks in a landscape.
            RenderSettings.fogColor = new Color(0.10f, 0.075f, 0.075f);
            RenderSettings.fogDensity = 0.013f;
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(0f, WorldY(180f), -Units(560f));
            // Fog never touches the skybox, so the HDRI horizon cannot be fogged
            // away. It is masked with real cliff geometry instead (BuildBackdrop),
            // which gives depth a flat clear colour cannot.
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
            camera.fieldOfView = 38f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.030f, 0.026f, 0.038f);

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        private static void BuildPlatforms(Material rock)
        {
            var parent = new GameObject("Arena").transform;
            var scorch = CreateScorchMaterial();
            var index = 0;

            foreach (var (x, y, width) in Layout)
            {
                var centreX = WorldX(x + width / 2f);
                var worldY = WorldY(y);
                var worldWidth = Units(width);
                var seed = (int)(x * 131 + y * 17);
                var rng = new System.Random(seed);
                float Range(double a, double b) => (float)(a + rng.NextDouble() * (b - a));

                var group = new GameObject($"platform_{x:F0}_{y:F0}").transform;
                group.SetParent(parent);
                group.position = new Vector3(centreX, worldY, 0f);

                // The deck is a displaced rock mesh, not a box. Its top stays
                // near-flat so it is landable; the sides and underside erode.
                var deckSize = new Vector3(worldWidth, Units(26f), Units(64f));
                var deckMesh = ProceduralRock.CreateSlab(deckSize, seed, 0.20f, 0.86f);
                SaveMesh(deckMesh, $"deck_{index}");

                var deck = new GameObject("deck");
                deck.transform.SetParent(group, false);
                deck.transform.localPosition = new Vector3(0f, -deckSize.y * 0.35f, 0f);
                deck.AddComponent<MeshFilter>().sharedMesh = deckMesh;
                deck.AddComponent<MeshRenderer>().sharedMaterial = rock;

                // Collider stays a simple box: gameplay should not inherit the
                // noise in the visual mesh.
                var box = deck.AddComponent<BoxCollider>();
                box.size = new Vector3(worldWidth, Units(12f), Units(60f));
                box.center = new Vector3(0f, deckSize.y * 0.35f - Units(6f), 0f);

                // A few weathered boulders sitting on and under the deck, each a
                // distinct displaced mesh.
                var boulders = Mathf.Clamp(Mathf.RoundToInt(worldWidth * 0.6f), 2, 7);
                for (var i = 0; i < boulders; i++)
                {
                    var bSize = new Vector3(Range(0.35, 0.95), Range(0.28, 0.75), Range(0.4, 0.9));
                    var mesh = ProceduralRock.CreateSlab(bSize, seed * 31 + i * 7, 0.34f, 0.1f);
                    SaveMesh(mesh, $"boulder_{index}_{i}");

                    var boulder = new GameObject($"boulder_{i}");
                    boulder.transform.SetParent(group, false);
                    boulder.transform.localPosition = new Vector3(
                        Range(-worldWidth / 2f + 0.3, worldWidth / 2f - 0.3),
                        Range(-0.55, 0.18),
                        Range(-0.9, 0.9));
                    boulder.transform.localRotation = Quaternion.Euler(Range(0, 360), Range(0, 360), Range(0, 360));
                    boulder.AddComponent<MeshFilter>().sharedMesh = mesh;
                    boulder.AddComponent<MeshRenderer>().sharedMaterial = i % 3 == 0 ? scorch : rock;
                }

                // A single eroded spur hanging beneath, giving the platform mass.
                var spurSize = new Vector3(Range(0.6, 1.3), Range(1.4, 2.8), Range(0.7, 1.4));
                var spurMesh = ProceduralRock.CreateSlab(spurSize, seed * 17 + 5, 0.4f, 0.05f);
                SaveMesh(spurMesh, $"spur_{index}");

                var spur = new GameObject("spur");
                spur.transform.SetParent(group, false);
                spur.transform.localPosition = new Vector3(Range(-1.2, 1.2), -spurSize.y * 0.55f - Units(10f), Range(-0.4, 0.4));
                spur.transform.localRotation = Quaternion.Euler(Range(-10, 10), Range(0, 360), Range(-10, 10));
                spur.AddComponent<MeshFilter>().sharedMesh = spurMesh;
                spur.AddComponent<MeshRenderer>().sharedMaterial = scorch;

                index++;
            }
        }

        /// <summary>
        /// Generated meshes must be saved as assets, or the scene references a
        /// mesh that exists only in the editor session and renders as nothing
        /// when the scene is reopened.
        /// </summary>
        private static void SaveMesh(Mesh mesh, string name)
        {
            Directory.CreateDirectory(GeneratedDir);
            AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/{name}.asset");
        }

        /// <summary>
        /// Undersides face the lava, so they are darker and carry a faint heat
        /// glow. Using a separate material is what stops every platform reading
        /// as one uniform grey block.
        /// </summary>
        private static Material CreateScorchMaterial()
        {
            return TexturedLit("Scorched", $"{MatDir}/Scorched.mat",
                new Color(0.17f, 0.14f, 0.13f), 0.07f, 0f, new Vector2(2.2f, 1.4f));
        }

        private static void BuildLavaPit(Material lava)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "lava";
            floor.transform.position = new Vector3(0f, -Units(22f), Units(60f));
            floor.transform.localScale = new Vector3(Units(2800f), Units(30f), Units(200f));
            floor.GetComponent<MeshRenderer>().sharedMaterial = lava;

            // Point lights along the pit so the glow falls off across the arena
            // rather than lighting it flatly.
            for (var i = -2; i <= 2; i++)
            {
                var glow = new GameObject($"lava_glow_{i + 2}").AddComponent<Light>();
                glow.type = LightType.Point;
                glow.transform.position = new Vector3(i * 9f, -8.5f, 0f);
                glow.color = new Color(1f, 0.38f, 0.12f);
                glow.intensity = 11f;
                glow.range = 26f;
                glow.shadows = LightShadows.None;
            }
        }

        private static void BuildActors(Material rock)
        {
            var armour = CreateArmourMaterial();
            var ostrich = CreatePlumageMaterial("PlumageOstrich", new Color(0.88f, 0.80f, 0.58f));
            var bounder = CreatePlumageMaterial("PlumageBounder", new Color(0.68f, 0.14f, 0.10f));
            var lord = CreatePlumageMaterial("PlumageLord", new Color(0.24f, 0.34f, 0.72f));

            var player = BuildRider("player", new Vector3(WorldX(320f), WorldY(150f), 0f), ostrich, armour);
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            player.AddComponent<FlightPrototype>();
            player.AddComponent<ScreenWrapPrototype>().Configure(ArenaHalfWidth, 4f);

            BuildRider("rider_high", new Vector3(WorldX(150f), WorldY(190f), 0f), bounder, armour);
            BuildRider("rider_low", new Vector3(WorldX(500f), WorldY(230f), 0f), lord, armour);
        }

        /// <summary>
        /// Placeholder rider built from primitives: a mount body, neck, head and
        /// a lance. Stands in until rigged art lands, but reads at silhouette
        /// level, which is what the art-direction question needs.
        /// </summary>
        private static GameObject BuildRider(string name, Vector3 position, Material plumage, Material armour)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            root.transform.localScale = Vector3.one * Units(MountWidthPx) / 2f;

            var mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mount.name = "mount_body";
            mount.transform.SetParent(root.transform, false);
            mount.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mount.transform.localScale = new Vector3(0.62f, 0.95f, 0.62f);
            mount.GetComponent<MeshRenderer>().sharedMaterial = plumage;
            UnityEngine.Object.DestroyImmediate(mount.GetComponent<Collider>());

            var neck = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            neck.name = "mount_neck";
            neck.transform.SetParent(root.transform, false);
            neck.transform.localPosition = new Vector3(0.75f, 1.05f, 0f);
            neck.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            neck.transform.localScale = new Vector3(0.2f, 0.55f, 0.2f);
            neck.GetComponent<MeshRenderer>().sharedMaterial = plumage;
            UnityEngine.Object.DestroyImmediate(neck.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "mount_head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(1.12f, 1.5f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<MeshRenderer>().sharedMaterial = plumage;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());

            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "rider_body";
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3(-0.15f, 1.25f, 0f);
            rider.transform.localScale = new Vector3(0.34f, 0.42f, 0.34f);
            rider.GetComponent<MeshRenderer>().sharedMaterial = armour;
            UnityEngine.Object.DestroyImmediate(rider.GetComponent<Collider>());

            var lance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lance.name = "lance";
            lance.transform.SetParent(root.transform, false);
            lance.transform.localPosition = new Vector3(0.95f, 1.62f, 0f);
            lance.transform.localRotation = Quaternion.Euler(0f, 0f, 78f);
            lance.transform.localScale = new Vector3(0.06f, 0.85f, 0.06f);
            lance.GetComponent<MeshRenderer>().sharedMaterial = armour;
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
            grading.colorFilter.value = new Color(1f, 0.90f, 0.80f);

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
