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
    /// Enemy decision making, as a pure function.
    ///
    /// Randomness arrives as a parameter rather than being drawn inside, so
    /// "wanders sometimes" is a deterministic test rather than a statistical one.
    ///
    /// The three tiers differ in behaviour, not merely in speed: a Wanderer
    /// rarely commits, a Pursuer tracks the player, and a Dominator tracks AND
    /// climbs to stay above, because height wins duels.
    /// </summary>
    public static class BuzzardDecision
    {
        /// <summary>Chance a wanderer commits to chasing rather than drifting.</summary>
        public const float WandererChaseChance = 0.55f;

        /// <summary>Chance any profile flaps when it has no other reason to.</summary>
        public const float IdleFlapChance = 0.45f;

        /// <summary>How far above the rider a target must be to force a climb.</summary>
        public const float ClimbThreshold = 0.5f;

        /// <summary>Height a dominator tries to hold above its target.</summary>
        public const float DominatorAdvantage = 1.2f;

        public static Decision Decide(Vector2 self, Vector2 target, float lavaLine, float random01)
        {
            return Decide(self, target, lavaLine, random01, AiProfile.Wanderer);
        }

        public static Decision Decide(Vector2 self, Vector2 target, float lavaLine, float random01,
            AiProfile profile)
        {
            var toTarget = Mathf.Approximately(target.x, self.x)
                ? 0f
                : Mathf.Sign(target.x - self.x);

            var decision = new Decision
            {
                Thrust = profile == AiProfile.Wanderer
                    // A wanderer that always chases is just a hunter, so it only
                    // commits some of the time and otherwise drifts the other way.
                    ? (random01 < WandererChaseChance ? toTarget : -toTarget)
                    : toTarget
            };

            // Survival outranks everything: a rider over the lava climbs even
            // when the player is somewhere far more interesting.
            if (self.y <= lavaLine)
            {
                decision.Flap = true;
                return decision;
            }

            // Any tier contests a lance above its own.
            if (target.y > self.y + ClimbThreshold)
            {
                decision.Flap = true;
                return decision;
            }

            // A dominator is not content with level: it seeks the height that
            // wins the duel before the duel happens.
            if (profile == AiProfile.Dominator && self.y < target.y + DominatorAdvantage)
            {
                decision.Flap = true;
                return decision;
            }

            decision.Flap = random01 < IdleFlapChance;
            return decision;
        }
    }
}
