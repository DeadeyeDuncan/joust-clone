using System.Collections.Generic;

namespace Joust.Flow
{
    /// <summary>
    /// When a wave ends, what the next one opens with, and what it pays.
    ///
    /// The arcade's exact wave table is not documented in any source found so
    /// far (see the arcade reference doc), so the schedule here is an explicit
    /// approximation: it reproduces the behaviours that are confirmed — waves
    /// grow, tiers escalate, survival waves pay 3000 — without pretending to
    /// reproduce a table nobody has.
    /// </summary>
    public static class WaveRules
    {
        public const int SurvivalBonusPoints = 3000;

        /// <summary>Survival waves, from the reference: every fifth wave.</summary>
        public const int SurvivalEvery = 5;

        /// <summary>Cap on simultaneous enemies, so the arena stays playable.</summary>
        public const int MaxEnemies = 8;

        /// <summary>
        /// A wave is over only when both the enemies and the eggs are gone. An
        /// egg still on the ground is an enemy that has not hatched yet, and
        /// clearing on enemy count alone would end the wave while the board is
        /// still live.
        /// </summary>
        public static bool IsWaveClear(int enemies, int eggs) => enemies <= 0 && eggs <= 0;

        public static bool IsSurvivalWave(int wave) => wave > 0 && wave % SurvivalEvery == 0;

        public static int SurvivalBonus(int wave, bool lostALife)
        {
            return IsSurvivalWave(wave) && !lostALife ? SurvivalBonusPoints : 0;
        }

        /// <summary>
        /// The tiers a wave starts with. Promotion on hatch does the rest of the
        /// escalation, so this only has to set the opening pressure.
        /// </summary>
        public static IReadOnlyList<EnemyTier> OpeningComposition(int wave)
        {
            var count = System.Math.Min(MaxEnemies, 2 + (wave + 1) / 2);
            var composition = new List<EnemyTier>(count);

            // Shadow lords arrive later than hunters, and never make up the whole
            // wave: the tier mix widens rather than simply shifting upward.
            var lords = wave >= 9 ? System.Math.Min(2, (wave - 7) / 4) : 0;
            var hunters = wave >= 4 ? System.Math.Min(count - lords, (wave - 2) / 2) : 0;
            var bounders = count - lords - hunters;

            for (var i = 0; i < bounders; i++) composition.Add(EnemyTier.Bounder);
            for (var i = 0; i < hunters; i++) composition.Add(EnemyTier.Hunter);
            for (var i = 0; i < lords; i++) composition.Add(EnemyTier.ShadowLord);

            return composition;
        }
    }
}
