using UnityEngine;

namespace Joust.World
{
    /// <summary>
    /// What happens to a rider that has just been unseated.
    ///
    /// Pure, so the rules are testable without physics: an enemy becomes an egg,
    /// the player loses a life, and the egg is thrown rather than dropped.
    /// </summary>
    public static class EggSpawnRules
    {
        /// <summary>Fraction of the rider's horizontal speed the egg keeps.</summary>
        public const float MomentumRetained = 0.45f;

        /// <summary>Upward pop given to every egg, in units per second.</summary>
        public const float Pop = 4.5f;

        public static bool ShouldDropEgg(bool wasPlayer) => !wasPlayer;

        /// <summary>
        /// The egg keeps part of the rider's horizontal momentum, so a kill at
        /// speed throws the egg across the arena, but its vertical velocity is
        /// always a fresh upward pop.
        ///
        /// Inheriting downward velocity from a diving rider would fire the egg
        /// straight into the lava and quietly deny the player the collection
        /// chain, which is where the scoring lives.
        /// </summary>
        public static Vector2 LaunchVelocity(Vector2 riderVelocity)
        {
            return new Vector2(riderVelocity.x * MomentumRetained, Pop);
        }
    }
}
