// SPIKE: throwaway, not carried into M1.
using UnityEngine;

namespace Joust.Combat
{
    [RequireComponent(typeof(Collider))]
    public class JoustContact : MonoBehaviour
    {
        [SerializeField] private Transform lance;
        [SerializeField] private float tieBand = 0.5f;

        public static int ResolutionCount { get; private set; }
        public static Transform LastWinner { get; private set; }
        public static JoustOutcome LastOutcome { get; private set; }

        public static void ResetResolutionCount()
        {
            ResolutionCount = 0;
            LastWinner = null;
            LastOutcome = JoustOutcome.Tie;
        }

        /// <summary>
        /// Wires the lance and tie band from code. The scene builder and the
        /// PlayMode tests both use this rather than editor-only serialization,
        /// so the same path is exercised in tests and in the scene.
        /// </summary>
        public void Configure(Transform lanceTransform, float tieBandHeight)
        {
            lance = lanceTransform;
            tieBand = tieBandHeight;
        }

        /// <summary>
        /// OnTriggerEnter fires on both colliders of a pair. Exactly one of the
        /// two must own the resolution, or the duel resolves twice and can kill
        /// both riders. Ownership goes to the smaller instance id.
        /// </summary>
        /// <remarks>
        /// Generic over IComparable rather than taking ints: Unity 6000.5 makes
        /// both Object.GetInstanceID() and EntityId's int conversion
        /// obsolete-as-error, so the runtime must compare EntityId values
        /// directly. Tests still exercise the rule with plain ints.
        /// </remarks>
        public static bool ShouldResolve<T>(T self, T other) where T : System.IComparable<T>
        {
            return self.CompareTo(other) < 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            var opponent = other.GetComponentInParent<JoustContact>();
            if (opponent == null || opponent == this)
            {
                return;
            }

            if (!ShouldResolve(GetEntityId(), opponent.GetEntityId()))
            {
                return;
            }

            var outcome = JoustResolver.Resolve(
                lance.position.y,
                opponent.lance.position.y,
                tieBand);

            ResolutionCount++;
            LastOutcome = outcome;
            LastWinner = outcome switch
            {
                JoustOutcome.AWins => transform,
                JoustOutcome.BWins => opponent.transform,
                _ => null
            };

            Debug.Log($"joust resolved: {outcome} (count={ResolutionCount})");
        }
    }
}
