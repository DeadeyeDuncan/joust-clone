using UnityEngine;

namespace Joust.Core
{
    /// <summary>
    /// Every number that decides how the game feels, in one inspector-editable
    /// asset so it can be tuned during play rather than recompiled.
    ///
    /// The defaults are the provisional baseline accepted at the end of M0: they
    /// are flyable, not yet judged. M1's definition of done is the user accepting
    /// tuned values here, with an enemy to fight and a death to avoid.
    /// </summary>
    [CreateAssetMenu(menuName = "Joust/Tuning Profile", fileName = "TuningProfile")]
    public class TuningProfile : ScriptableObject
    {
        [Header("Flight")]
        [Tooltip("Downward acceleration in units per second squared.")]
        public float gravity = 24f;

        [Tooltip("Vertical velocity a flap SETS, not adds. Setting is what keeps repeated flaps controllable.")]
        public float flapImpulse = 9f;

        [Tooltip("Horizontal acceleration while thrusting, in units per second squared.")]
        public float thrustAcceleration = 17f;

        [Tooltip("Horizontal speed cap in units per second.")]
        public float maxHorizontalSpeed = 12f;

        [Tooltip("Horizontal decay per second while airborne and not thrusting.")]
        public float airDrag = 0.6f;

        [Header("Ground")]
        [Tooltip("Horizontal decay per second while grounded and not thrusting.")]
        public float groundSkidDeceleration = 27f;

        [Header("Combat")]
        [Tooltip("Lance height difference within which a joust is a tie and both riders bounce.")]
        public float tieBandUnits = 0.2f;

        [Tooltip("Horizontal speed imparted to each rider by a tied joust.")]
        public float bounceSpeed = 3f;

        [Header("Lives")]
        public int startingLives = 3;
    }
}
