using Joust.Flow;

namespace Joust.World
{
    /// <summary>
    /// The arcade's escalation rule: a defeated rider's egg hatches one tier
    /// stronger than the rider that produced it, capping at Shadow Lord.
    ///
    /// This is what makes a wave get harder because of how it is fought, rather
    /// than because of its number. The pygame clone in this repository spawns
    /// each wave from a fixed composition list and has no such feedback loop;
    /// the arcade is the authority here.
    /// </summary>
    public static class EggRules
    {
        public static EnemyTier Promote(EnemyTier tier) => tier switch
        {
            EnemyTier.Bounder => EnemyTier.Hunter,
            EnemyTier.Hunter => EnemyTier.ShadowLord,
            _ => EnemyTier.ShadowLord
        };
    }
}
