using System.Collections.Generic;
using Joust.Core;
using UnityEngine;

namespace Joust.Flow
{
    /// <summary>
    /// Death and rebirth, as pure rules.
    ///
    /// Kept out of any MonoBehaviour so the choice of spawn pad can be tested
    /// headlessly, without a scene or a physics step.
    /// </summary>
    public static class RespawnRules
    {
        /// <summary>
        /// The lava surface is y=0 by the arena mapping, so anything below it is
        /// in the lava.
        /// </summary>
        public static bool IsBelowLava(float worldY) => worldY < 0f;

        /// <summary>
        /// Picks the pad furthest from the nearest threat.
        ///
        /// A fixed spawn point, or a random one, drops the player back on top of
        /// whatever just killed them often enough to feel stolen rather than
        /// lost. With no threat on the board, the first pad is as good as any.
        /// </summary>
        public static Vector2 ChooseSpawnPad(IReadOnlyList<Vector2> pads, Vector2? threat)
        {
            if (pads == null || pads.Count == 0)
            {
                return Vector2.zero;
            }

            if (threat == null || pads.Count == 1)
            {
                return pads[0];
            }

            var best = pads[0];
            var bestDistance = float.NegativeInfinity;

            foreach (var pad in pads)
            {
                var distance = (pad - threat.Value).sqrMagnitude;
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = pad;
                }
            }

            return best;
        }

        /// <summary>
        /// The nearest threat to a point, or null when the board is clear.
        /// </summary>
        public static Vector2? NearestThreat(IReadOnlyList<Vector2> threats, Vector2 from)
        {
            if (threats == null || threats.Count == 0)
            {
                return null;
            }

            var nearest = threats[0];
            var nearestDistance = (nearest - from).sqrMagnitude;

            for (var i = 1; i < threats.Count; i++)
            {
                var distance = (threats[i] - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = threats[i];
                }
            }

            return nearest;
        }

        /// <summary>
        /// The spawn pads of the original layout: the tops of the platforms the
        /// arcade starts riders on, in logical pixels converted to world space.
        /// </summary>
        public static IReadOnlyList<Vector2> DefaultPads() => new[]
        {
            new Vector2(ArenaMetrics.WorldX(320f), ArenaMetrics.WorldY(64f)),
            new Vector2(ArenaMetrics.WorldX(72f), ArenaMetrics.WorldY(120f)),
            new Vector2(ArenaMetrics.WorldX(568f), ArenaMetrics.WorldY(120f)),
            new Vector2(ArenaMetrics.WorldX(156f), ArenaMetrics.WorldY(300f)),
        };
    }
}
