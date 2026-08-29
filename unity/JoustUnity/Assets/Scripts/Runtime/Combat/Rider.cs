using System;
using Joust.Flow;
using UnityEngine;

namespace Joust.Combat
{
    /// <summary>
    /// A mounted combatant: the player or a buzzard rider. Holds the lance whose
    /// height decides duels, and the tier that decides both its score value and
    /// what its egg hatches into.
    /// </summary>
    public class Rider : MonoBehaviour
    {
        [SerializeField] private Transform lance;
        [SerializeField] private EnemyTier tier = EnemyTier.Bounder;
        [SerializeField] private bool isPlayer;

        public Transform Lance => lance;
        public EnemyTier Tier => tier;
        public bool IsPlayer => isPlayer;
        public bool Mounted { get; private set; } = true;

        /// <summary>Raised once, the first time this rider is unseated.</summary>
        public event Action<Rider> Unseated;

        public void Configure(Transform lanceTransform, EnemyTier riderTier, bool player = false)
        {
            lance = lanceTransform;
            tier = riderTier;
            isPlayer = player;
        }

        /// <summary>
        /// Idempotent: a rider already off its mount cannot be unseated again,
        /// so a duplicate resolution cannot double-count a kill.
        /// </summary>
        public void Unseat()
        {
            if (!Mounted)
            {
                return;
            }

            Mounted = false;
            Unseated?.Invoke(this);
        }

        public float LanceHeight => lance != null ? lance.position.y : transform.position.y;
    }
}
