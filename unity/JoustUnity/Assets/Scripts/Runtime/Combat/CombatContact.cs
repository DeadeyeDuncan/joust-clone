using System;
using UnityEngine;

namespace Joust.Combat
{
    /// <summary>
    /// Turns a physics overlap into a duel result.
    ///
    /// Collider geometry never decides the winner: <see cref="JoustResolver"/>
    /// compares lance heights, and this component only decides which of the two
    /// participants runs that comparison.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CombatContact : MonoBehaviour
    {
        [SerializeField] private Rider rider;
        [SerializeField] private float tieBand = 0.2f;

        /// <summary>Raised once per overlap, as (winner, loser).</summary>
        public event Action<Rider, Rider> Resolved;

        /// <summary>Raised once per overlap when neither rider is unseated.</summary>
        public event Action<Rider, Rider> Tied;

        public void Configure(Rider owner, float tieBandUnits)
        {
            rider = owner;
            tieBand = tieBandUnits;
        }

        /// <summary>
        /// OnTriggerEnter fires on BOTH colliders of a pair, so exactly one
        /// participant must own the resolution or the duel resolves twice.
        ///
        /// Generic over IComparable because Unity 6000.5 makes both
        /// Object.GetInstanceID() and EntityId's int conversion
        /// obsolete-as-error, so EntityId values must be compared directly.
        /// </summary>
        public static bool ShouldResolve<T>(T self, T other) where T : IComparable<T>
        {
            return self.CompareTo(other) < 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            var opponent = other.GetComponentInParent<CombatContact>();
            if (opponent == null || ReferenceEquals(opponent, this))
            {
                return;
            }

            if (rider == null || opponent.rider == null)
            {
                return;
            }

            // Riders already unseated are collectable, not jousting.
            if (!rider.Mounted || !opponent.rider.Mounted)
            {
                return;
            }

            if (!ShouldResolve(GetEntityId(), opponent.GetEntityId()))
            {
                return;
            }

            var outcome = JoustResolver.Resolve(
                rider.LanceHeight, opponent.rider.LanceHeight, tieBand);

            switch (outcome)
            {
                case JoustOutcome.AWins:
                    opponent.rider.Unseat();
                    Resolved?.Invoke(rider, opponent.rider);
                    break;

                case JoustOutcome.BWins:
                    rider.Unseat();
                    Resolved?.Invoke(opponent.rider, rider);
                    break;

                default:
                    Tied?.Invoke(rider, opponent.rider);
                    break;
            }
        }
    }
}
