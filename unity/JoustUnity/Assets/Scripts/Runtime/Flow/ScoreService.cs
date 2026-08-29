namespace Joust.Flow
{
    /// <summary>
    /// The three buzzard riders, weakest first. A defeated rider's egg hatches
    /// one tier stronger; see <see cref="Joust.World.EggRules"/>.
    /// </summary>
    public enum EnemyTier
    {
        Bounder,
        Hunter,
        ShadowLord
    }

    /// <summary>
    /// Scoring, to the values of the 1982 Williams arcade machine.
    ///
    /// Note the Shadow Lord is worth 1000. The pygame clone in this repository
    /// pays 1500, which is wrong; the arcade is the authority for this rebuild.
    /// See docs/superpowers/specs/2026-08-29-arcade-joust-reference.md.
    /// </summary>
    public static class ScoreService
    {
        public const int ExtraLifeEvery = 20000;

        /// <summary>
        /// Points for successive eggs collected without touching down. The
        /// chain holds at its top value rather than resetting.
        /// </summary>
        private static readonly int[] EggChain = { 250, 500, 750, 1000 };

        public static int PointsFor(EnemyTier tier) => tier switch
        {
            EnemyTier.Bounder => 500,
            EnemyTier.Hunter => 750,
            EnemyTier.ShadowLord => 1000,
            _ => 0
        };

        public static int EggChainValue(int chainIndex)
        {
            if (chainIndex < 0)
            {
                return 0;
            }

            return chainIndex >= EggChain.Length
                ? EggChain[EggChain.Length - 1]
                : EggChain[chainIndex];
        }

        /// <summary>
        /// Extra lives earned by crossing multiples of 20,000. Computed from the
        /// score either side of an award rather than tracked as state, so a
        /// single large award correctly grants more than one life.
        /// </summary>
        public static int ExtraLivesEarned(int previousScore, int newScore)
        {
            return newScore / ExtraLifeEvery - previousScore / ExtraLifeEvery;
        }
    }
}
