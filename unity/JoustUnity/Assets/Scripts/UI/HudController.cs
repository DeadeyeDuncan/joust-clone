using Joust.Flow;
using UnityEngine;
using UnityEngine.UIElements;

namespace Joust.UI
{
    /// <summary>
    /// Binds the HUD document to <see cref="GameDirector"/> events.
    ///
    /// Event-driven rather than polled in Update: the score changes a handful of
    /// times a run, and rebuilding text every frame is wasted work.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        [SerializeField] private GameDirector director;

        private Label _score;
        private Label _lives;
        private Label _wave;
        private Label _banner;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _score = root.Q<Label>("score-value");
            _lives = root.Q<Label>("lives-value");
            _wave = root.Q<Label>("wave-value");
            _banner = root.Q<Label>("banner");

            if (director == null)
            {
                director = FindFirstObjectByType<GameDirector>();
            }

            if (director == null)
            {
                return;
            }

            director.ScoreChanged += OnScoreChanged;
            director.LivesChanged += OnLivesChanged;
            director.GameOver += OnGameOver;

            OnScoreChanged(director.Score);
            OnLivesChanged(director.Lives);
            SetBanner(string.Empty);
        }

        private void OnDisable()
        {
            if (director == null)
            {
                return;
            }

            director.ScoreChanged -= OnScoreChanged;
            director.LivesChanged -= OnLivesChanged;
            director.GameOver -= OnGameOver;
        }

        public void SetWave(int wave)
        {
            if (_wave != null)
            {
                _wave.text = wave.ToString();
            }
        }

        public void SetBanner(string text)
        {
            if (_banner != null)
            {
                _banner.text = text;
            }
        }

        private void OnScoreChanged(int score)
        {
            if (_score != null)
            {
                _score.text = score.ToString();
            }
        }

        private void OnLivesChanged(int lives)
        {
            if (_lives != null)
            {
                _lives.text = lives.ToString();
            }
        }

        private void OnGameOver() => SetBanner("GAME OVER");
    }
}
