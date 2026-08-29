using UnityEngine;

namespace Joust.Enemies
{
    /// <summary>What a rider wants to do this tick.</summary>
    public struct Decision
    {
        public float Thrust;
        public bool Flap;
    }

    /// <summary>
    /// Bounder-tier decision making, as a pure function.
    ///
    /// Randomness arrives as a parameter rather than being drawn inside, so
    /// "flaps sometimes" is a deterministic test rather than a statistical one.
    /// </summary>
    public static class BuzzardDecision
    {
        /// <summary>Chance of a flap when already level with the target.</summary>
        public const float FlapChanceWhenLevel = 0.45f;

        /// <summary>How far above the rider the target must be to force a climb.</summary>
        public const float ClimbThreshold = 0.5f;

        public static Decision Decide(Vector2 self, Vector2 target, float lavaLine, float random01)
        {
            var decision = new Decision
            {
                Thrust = Mathf.Approximately(target.x, self.x)
                    ? 0f
                    : Mathf.Sign(target.x - self.x)
            };

            // Survival first. A rider over the lava climbs even when the player
            // is somewhere far more interesting; the opposite ordering drowns it.
            if (self.y <= lavaLine)
            {
                decision.Flap = true;
                return decision;
            }

            if (target.y > self.y + ClimbThreshold)
            {
                decision.Flap = true;
                return decision;
            }

            decision.Flap = random01 < FlapChanceWhenLevel;
            return decision;
        }
    }
}
