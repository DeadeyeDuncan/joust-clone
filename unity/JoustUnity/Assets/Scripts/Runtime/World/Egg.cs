using System;
using Joust.Flow;
using UnityEngine;

namespace Joust.World
{
    /// <summary>
    /// What a rider becomes when unseated. It falls, settles, and hatches on a
    /// timer into a rider one tier stronger — unless the player collects it
    /// first, which both scores and denies the respawn.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Egg : MonoBehaviour
    {
        [SerializeField] private float hatchSeconds = 8f;
        [SerializeField] private EnemyTier layingTier = EnemyTier.Bounder;

        private float _age;
        private bool _resolved;

        /// <summary>The tier of the rider that laid this egg.</summary>
        public EnemyTier LayingTier => layingTier;

        /// <summary>What this egg will hatch into, one tier up.</summary>
        public EnemyTier HatchTier => EggRules.Promote(layingTier);

        /// <summary>Raised as (egg, promoted tier). Carries the PROMOTED tier so a
        /// spawner cannot accidentally respawn the tier that was just killed.</summary>
        public event Action<Egg, EnemyTier> Hatched;

        public event Action<Egg> Collected;

        public void Configure(EnemyTier tier, float secondsToHatch)
        {
            layingTier = tier;
            hatchSeconds = secondsToHatch;
        }

        /// <summary>Idempotent, and prevents any later hatch.</summary>
        public void Collect()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            Collected?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (_resolved)
            {
                return;
            }

            _age += Time.deltaTime;
            if (_age < hatchSeconds)
            {
                return;
            }

            _resolved = true;
            Hatched?.Invoke(this, HatchTier);
            Destroy(gameObject);
        }
    }
}
