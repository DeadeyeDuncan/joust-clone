using Joust.Flow;
using UnityEngine;

namespace Joust.Enemies
{
    /// <summary>
    /// How a tier behaves in the air.
    ///
    /// From the arcade: a Bounder flies erratically with little pursuit, a
    /// Hunter actively pursues, and a Shadow Lord is the fastest and most
    /// aggressive.
    /// </summary>
    public enum AiProfile
    {
        Wanderer,
        Pursuer,
        Dominator
    }

    /// <summary>
    /// Everything that distinguishes one enemy from another, as data.
    ///
    /// Made a ScriptableObject so M4's new enemies are new assets rather than
    /// new branches in the spawner, with <see cref="Default"/> supplying the
    /// three arcade tiers without needing assets to exist first.
    /// </summary>
    [CreateAssetMenu(menuName = "Joust/Enemy Definition", fileName = "EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyTier tier = EnemyTier.Bounder;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private AiProfile profile = AiProfile.Wanderer;
        [SerializeField] private Color colour = Color.red;
        [SerializeField] private string mountModel = "Vulture";

        public EnemyTier Tier => tier;
        public float SpeedMultiplier => speedMultiplier;
        public AiProfile Profile => profile;
        public Color Colour => colour;
        public string MountModel => mountModel;

        /// <summary>
        /// Score comes from ScoreService rather than being stored, so a tier's
        /// value cannot drift from the arcade reference by being edited in one
        /// place and not the other.
        /// </summary>
        public int Points => ScoreService.PointsFor(tier);

        public static EnemyDefinition Default(EnemyTier tier)
        {
            var definition = CreateInstance<EnemyDefinition>();
            definition.tier = tier;

            switch (tier)
            {
                case EnemyTier.Bounder:
                    definition.speedMultiplier = 1f;
                    definition.profile = AiProfile.Wanderer;
                    definition.colour = new Color(0.68f, 0.14f, 0.10f);
                    break;

                case EnemyTier.Hunter:
                    definition.speedMultiplier = 1.18f;
                    definition.profile = AiProfile.Pursuer;
                    definition.colour = new Color(0.72f, 0.72f, 0.76f);
                    break;

                default:
                    definition.speedMultiplier = 1.4f;
                    definition.profile = AiProfile.Dominator;
                    definition.colour = new Color(0.24f, 0.34f, 0.78f);
                    break;
            }

            definition.mountModel = "Vulture";
            return definition;
        }
    }
}
