using Joust.Combat;
using Joust.Core;
using Joust.Flow;
using Joust.Movement;
using Joust.World;
using UnityEngine;

namespace Joust.Enemies
{
    /// <summary>
    /// Builds riders at runtime.
    ///
    /// Rider construction used to live in the editor scene builder, which was
    /// fine while every rider existed before play started. Hatching creates them
    /// mid-game, so the assembly has to be available at runtime and in one place
    /// only — otherwise the hatched riders and the placed ones drift apart.
    /// </summary>
    public class RiderFactory : MonoBehaviour
    {
        [SerializeField] private TuningProfile tuning;
        [SerializeField] private GameObject playerMountModel;
        [SerializeField] private GameObject enemyMountModel;
        [SerializeField] private GameObject knightModel;
        [SerializeField] private Material playerMaterial;

        public TuningProfile Tuning => tuning;

        public void Configure(TuningProfile profile, GameObject playerMount, GameObject enemyMount,
            GameObject knight)
        {
            tuning = profile;
            playerMountModel = playerMount;
            enemyMountModel = enemyMount;
            knightModel = knight;
        }

        public Rider CreatePlayer(Vector3 position)
        {
            return Create("player", position, EnemyTier.Bounder, isPlayer: true,
                playerMountModel, new Color(0.86f, 0.78f, 0.55f), 1f);
        }

        public Rider CreateEnemy(EnemyTier tier, Vector3 position)
        {
            var definition = EnemyDefinition.Default(tier);
            var rider = Create($"enemy_{tier}", position, tier, isPlayer: false,
                enemyMountModel, definition.Colour, definition.SpeedMultiplier);

            var ai = rider.gameObject.AddComponent<BuzzardAI>();
            ai.ConfigureProfile(definition.Profile);
            return rider;
        }

        private Rider Create(string name, Vector3 position, EnemyTier tier, bool isPlayer,
            GameObject mountModel, Color tint, float speedMultiplier)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var mount = PlaceModel(mountModel, "mount", root.transform,
                ArenaMetrics.Units(32f), Vector3.zero, seatOnBase: true, tint);
            if (mount == null)
            {
                BuildFallbackMount(root.transform, tint);
            }

            PlaceModel(knightModel, "knight", root.transform, ArenaMetrics.Units(20f),
                new Vector3(ArenaMetrics.Units(-2f), ArenaMetrics.Units(19f), 0f),
                seatOnBase: false, tint: null);

            var lance = new GameObject("lance").transform;
            lance.SetParent(root.transform, false);
            lance.localPosition = new Vector3(ArenaMetrics.Units(13f), ArenaMetrics.Units(26f), 0f);

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

            var motor = root.AddComponent<RiderMotor>();
            motor.Configure(tuning);
            motor.SpeedMultiplier = speedMultiplier;

            var rider = root.AddComponent<Rider>();
            rider.Configure(lance, tier, isPlayer);
            root.AddComponent<CombatContact>().Configure(rider, tuning != null ? tuning.tieBandUnits : 0.2f);
            root.AddComponent<ScreenWrapPrototype>().Configure(ArenaMetrics.ArenaHalfWidth, 4f);

            return rider;
        }

        /// <summary>
        /// Anchors a model by measured bounds inside a container, so an arbitrary
        /// pivot in the source asset cannot shift it.
        /// </summary>
        private static GameObject PlaceModel(GameObject prefab, string name, Transform parent,
            float targetHeight, Vector3 localOffset, bool seatOnBase, Color? tint)
        {
            if (prefab == null)
            {
                return null;
            }

            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            container.transform.localPosition = localOffset;
            container.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var instance = Instantiate(prefab, container.transform);
            instance.name = $"{name}_model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            if (!TryMeasure(instance, out var bounds) || bounds.size.y <= 1e-5f)
            {
                return container;
            }

            instance.transform.localScale *= targetHeight / bounds.size.y;

            if (TryMeasure(instance, out bounds))
            {
                var anchor = seatOnBase
                    ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)
                    : bounds.center;
                instance.transform.position += container.transform.position - anchor;
            }

            if (tint.HasValue)
            {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    var material = new Material(renderer.sharedMaterial);
                    material.SetColor("_BaseColor", tint.Value);
                    renderer.material = material;
                }
            }

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

        private static void BuildFallbackMount(Transform parent, Color tint)
        {
            var mount = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mount.name = "mount";
            mount.transform.SetParent(parent, false);
            mount.transform.localPosition = new Vector3(0f, ArenaMetrics.Units(10f), 0f);
            mount.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mount.transform.localScale = new Vector3(
                ArenaMetrics.Units(22f), ArenaMetrics.Units(20f), ArenaMetrics.Units(22f));
            mount.GetComponent<MeshRenderer>().material.color = tint;
            Destroy(mount.GetComponent<Collider>());
        }
    }
}
