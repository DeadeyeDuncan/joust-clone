using System;
using UnityEngine;

namespace Joust.Flow
{
    /// <summary>
    /// Owns the run: score, lives, and the egg-collection chain.
    ///
    /// Deliberately knows nothing about physics, scenes or prefabs, so the whole
    /// of it is exercisable from a bare GameObject in a test.
    /// </summary>
    public class GameDirector : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;

        private int _eggChainIndex;
        private bool _gameOverRaised;

        public int Score { get; private set; }
        public int Lives { get; private set; }

        public event Action GameOver;
        public event Action<int> ScoreChanged;
        public event Action<int> LivesChanged;

        private void Awake() => Lives = startingLives;

        public void AwardKill(EnemyTier tier) => Add(ScoreService.PointsFor(tier));

        /// <summary>Wave bonuses: survival, and the team bonus in two-player.</summary>
        public void AwardBonus(int points) => Add(points);

        /// <summary>
        /// Scores the next egg in the chain. The chain escalates while the player
        /// keeps collecting and resets on death.
        /// </summary>
        public void AwardEgg()
        {
            Add(ScoreService.EggChainValue(_eggChainIndex));
            _eggChainIndex++;
        }

        public void ResetEggChain() => _eggChainIndex = 0;

        public void LoseLife()
        {
            if (Lives <= 0)
            {
                return;
            }

            Lives--;
            ResetEggChain();
            LivesChanged?.Invoke(Lives);

            if (Lives > 0 || _gameOverRaised)
            {
                return;
            }

            _gameOverRaised = true;
            GameOver?.Invoke();
        }

        private void Add(int points)
        {
            var previous = Score;
            Score += points;

            var earned = ScoreService.ExtraLivesEarned(previous, Score);
            if (earned > 0)
            {
                Lives += earned;
                LivesChanged?.Invoke(Lives);
            }

            ScoreChanged?.Invoke(Score);
        }
    }
}
