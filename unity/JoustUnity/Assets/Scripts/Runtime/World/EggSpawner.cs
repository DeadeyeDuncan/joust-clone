using System;
using Joust.Combat;
using Joust.Core;
using UnityEngine;

namespace Joust.World
{
    /// <summary>
    /// Turns unseated riders into eggs.
    ///
    /// Listens rather than polls: riders raise <see cref="Rider.Unseated"/>, and
    /// nothing in the combat code needs to know eggs exist.
    /// </summary>
    public class EggSpawner : MonoBehaviour
    {
        [SerializeField] private float hatchSeconds = 8f;
        [SerializeField] private Material eggMaterial;

        /// <summary>Raised as (egg) whenever one is created, for the wave runner to count.</summary>
        public event Action<Egg> EggSpawned;

        /// <summary>Raised when a player is unseated, which costs a life instead.</summary>
        public event Action<Rider> PlayerDown;

        public void Watch(Rider rider)
        {
            if (rider != null)
            {
                rider.Unseated += OnUnseated;
            }
        }

        public void Unwatch(Rider rider)
        {
            if (rider != null)
            {
                rider.Unseated -= OnUnseated;
            }
        }

        private void OnUnseated(Rider rider)
        {
            if (!EggSpawnRules.ShouldDropEgg(rider.IsPlayer))
            {
                PlayerDown?.Invoke(rider);
                return;
            }

            var body = rider.GetComponent<Rigidbody>();
            var velocity = body != null
                ? new Vector2(body.linearVelocity.x, body.linearVelocity.y)
                : Vector2.zero;

            Spawn(rider.transform.position, velocity, rider.Tier);
        }

        public Egg Spawn(Vector3 position, Vector2 riderVelocity, Flow.EnemyTier tier)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"egg_{tier}";
            go.transform.position = position;
            go.transform.localScale = new Vector3(
                ArenaMetrics.Units(12f), ArenaMetrics.Units(14f), ArenaMetrics.Units(12f));

            if (eggMaterial != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = eggMaterial;
            }

            var collider = go.GetComponent<SphereCollider>();
            collider.isTrigger = false;

            var body = go.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            var launch = EggSpawnRules.LaunchVelocity(riderVelocity);
            body.linearVelocity = new Vector3(launch.x, launch.y, 0f);

            var egg = go.AddComponent<Egg>();
            egg.Configure(tier, hatchSeconds);

            EggSpawned?.Invoke(egg);
            return egg;
        }
    }
}
