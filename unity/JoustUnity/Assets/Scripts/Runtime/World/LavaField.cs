using System;
using Joust.Combat;
using Joust.Flow;
using UnityEngine;

namespace Joust.World
{
    /// <summary>
    /// Kills anything that falls into the lava.
    ///
    /// Checks height against the arena mapping rather than using a trigger
    /// volume: the lava surface is y=0 by definition, and a height test cannot
    /// be tunnelled through by a fast-moving rider the way a thin trigger can.
    /// </summary>
    public class LavaField : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 0.1f;

        private float _nextCheck;

        /// <summary>Raised as (rider) when a rider is claimed by the lava.</summary>
        public event Action<Rider> RiderConsumed;

        /// <summary>Raised as (egg) when an egg is claimed by the lava.</summary>
        public event Action<Egg> EggConsumed;

        private void Update()
        {
            if (Time.time < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.time + checkInterval;

            foreach (var rider in FindObjectsByType<Rider>(FindObjectsSortMode.None))
            {
                if (RespawnRules.IsBelowLava(rider.transform.position.y))
                {
                    RiderConsumed?.Invoke(rider);
                }
            }

            foreach (var egg in FindObjectsByType<Egg>(FindObjectsSortMode.None))
            {
                if (RespawnRules.IsBelowLava(egg.transform.position.y))
                {
                    EggConsumed?.Invoke(egg);
                }
            }
        }
    }
}
