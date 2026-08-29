using System;
using System.Collections.Generic;
using System.Linq;
using Joust.Combat;
using Joust.Core;
using Joust.Enemies;
using Joust.World;
using UnityEngine;

namespace Joust.Flow
{
    /// <summary>
    /// Runs the game loop: start a wave, watch it empty, advance.
    ///
    /// Holds no rules of its own — <see cref="WaveRules"/> decides what a wave
    /// contains and when it is over, <see cref="RespawnRules"/> decides where a
    /// death sends you. This only wires the events together and owns the scene
    /// objects, so the decisions stay headlessly testable.
    /// </summary>
    public class WaveRunner : MonoBehaviour
    {
        [SerializeField] private RiderFactory factory;
        [SerializeField] private EggSpawner eggSpawner;
        [SerializeField] private LavaField lava;
        [SerializeField] private GameDirector director;
        [SerializeField] private float waveStartDelay = 1.5f;
        [SerializeField] private float respawnDelay = 1.2f;

        private readonly List<Rider> _enemies = new();
        private readonly List<Egg> _eggs = new();

        private Rider _player;
        private int _wave;
        private bool _lostALifeThisWave;
        private float _nextCheck;
        private bool _running;

        public int Wave => _wave;

        public event Action<int> WaveStarted;

        public void Configure(RiderFactory riderFactory, EggSpawner eggs, LavaField lavaField,
            GameDirector gameDirector)
        {
            factory = riderFactory;
            eggSpawner = eggs;
            lava = lavaField;
            director = gameDirector;
        }

        private void Start()
        {
            if (factory == null || eggSpawner == null || director == null)
            {
                Debug.LogError("WaveRunner is missing a dependency; not starting");
                return;
            }

            eggSpawner.EggSpawned += OnEggSpawned;
            eggSpawner.PlayerDown += OnPlayerDown;

            if (lava != null)
            {
                lava.RiderConsumed += OnRiderConsumed;
                lava.EggConsumed += OnEggConsumed;
            }

            SpawnPlayer();
            StartWave(1);
            _running = true;
        }

        private void OnDestroy()
        {
            if (eggSpawner != null)
            {
                eggSpawner.EggSpawned -= OnEggSpawned;
                eggSpawner.PlayerDown -= OnPlayerDown;
            }

            if (lava != null)
            {
                lava.RiderConsumed -= OnRiderConsumed;
                lava.EggConsumed -= OnEggConsumed;
            }
        }

        private void Update()
        {
            if (!_running || Time.time < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.time + 0.25f;

            _enemies.RemoveAll(e => e == null || !e.Mounted);
            _eggs.RemoveAll(e => e == null);

            if (!WaveRules.IsWaveClear(_enemies.Count, _eggs.Count))
            {
                return;
            }

            // Stop checking while the next wave is pending, or the clear
            // condition stays true and fires every quarter second.
            _running = false;
            AdvanceWave();
        }

        private void StartWave(int wave)
        {
            _wave = wave;
            _lostALifeThisWave = false;
            _enemies.Clear();

            var pads = RespawnRules.DefaultPads();
            var composition = WaveRules.OpeningComposition(wave);

            for (var i = 0; i < composition.Count; i++)
            {
                // Spread the wave across the pads rather than stacking it, so a
                // player is not immediately surrounded on spawn.
                var pad = pads[(i + 1) % pads.Count];
                var jitter = new Vector3(
                    ArenaMetrics.Units(UnityEngine.Random.Range(-30f, 30f)),
                    ArenaMetrics.Units(UnityEngine.Random.Range(10f, 40f)), 0f);

                var enemy = factory.CreateEnemy(composition[i], new Vector3(pad.x, pad.y, 0f) + jitter);
                Register(enemy);
            }

            WaveStarted?.Invoke(wave);
        }

        /// <summary>
        /// Tracks an enemy and scores it when unseated. Unseating is the scoring
        /// event in the arcade; the egg it leaves behind is a separate concern
        /// owned by the spawner.
        /// </summary>
        private void Register(Rider enemy)
        {
            eggSpawner.Watch(enemy);
            enemy.Unseated += OnEnemyUnseated;
            _enemies.Add(enemy);
        }

        private void OnEnemyUnseated(Rider enemy)
        {
            enemy.Unseated -= OnEnemyUnseated;
            _enemies.Remove(enemy);
            director.AwardKill(enemy.Tier);

            // The rider is gone; its egg now carries the threat.
            DestroyRider(enemy, 0.1f);
        }

        /// <summary>
        /// BuzzardAI requires RiderMotor, so tearing a rider down can try to
        /// destroy the motor first and log an error. Strip the AI first, and
        /// stop it steering a corpse in the meantime.
        /// </summary>
        private static void DestroyRider(Rider rider, float delay = 0f)
        {
            if (rider == null)
            {
                return;
            }

            var ai = rider.GetComponent<BuzzardAI>();
            if (ai != null)
            {
                Destroy(ai);
            }

            Destroy(rider.gameObject, delay);
        }

        private void SpawnPlayer()
        {
            var threats = _enemies.Where(e => e != null)
                .Select(e => new Vector2(e.transform.position.x, e.transform.position.y))
                .ToList();

            var pads = RespawnRules.DefaultPads();
            var spawn = RespawnRules.ChooseSpawnPad(pads,
                RespawnRules.NearestThreat(threats, pads.Count > 0 ? pads[0] : Vector2.zero));

            _player = factory.CreatePlayer(new Vector3(spawn.x, spawn.y + ArenaMetrics.Units(20f), 0f));
            _player.gameObject.AddComponent<Joust.Player.PlayerInput>();
            eggSpawner.Watch(_player);
        }

        private void OnEggSpawned(Egg egg)
        {
            _eggs.Add(egg);
            egg.Hatched += OnEggHatched;
            egg.Collected += OnEggCollected;
        }

        private void OnEggHatched(Egg egg, EnemyTier promotedTier)
        {
            _eggs.Remove(egg);

            var enemy = factory.CreateEnemy(promotedTier, egg.transform.position);
            Register(enemy);
        }

        private void OnEggCollected(Egg egg)
        {
            _eggs.Remove(egg);
            director.AwardEgg();
        }

        private void OnEggConsumed(Egg egg)
        {
            _eggs.Remove(egg);
            Destroy(egg.gameObject);
        }

        private void OnRiderConsumed(Rider rider)
        {
            if (rider.IsPlayer)
            {
                OnPlayerDown(rider);
                return;
            }

            if (_enemies.Remove(rider))
            {
                director.AwardKill(rider.Tier);
            }

            DestroyRider(rider);
        }

        private void OnPlayerDown(Rider rider)
        {
            _lostALifeThisWave = true;
            director.LoseLife();

            DestroyRider(rider);

            if (director.Lives > 0)
            {
                Invoke(nameof(SpawnPlayer), respawnDelay);
            }
            else
            {
                _running = false;
            }
        }

        private void AdvanceWave()
        {
            var bonus = WaveRules.SurvivalBonus(_wave, _lostALifeThisWave);
            if (bonus > 0)
            {
                director.AwardBonus(bonus);
            }

            Invoke(nameof(StartNextWave), waveStartDelay);
        }

        private void StartNextWave()
        {
            StartWave(_wave + 1);
            _running = true;
        }
    }
}
