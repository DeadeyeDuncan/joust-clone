using UnityEngine;

namespace Joust.Core
{
    /// <summary>
    /// Every number that decides how the game feels, in one inspector-editable
    /// asset so it can be tuned during play rather than recompiled.
    ///
    /// These values were play-tested and accepted on 2026-08-29, after the M0
    /// baseline was judged too floaty: gravity was raised from 24 to 36 and the
    /// flap lowered from 9 to 8.5, which shortens the arc per flap from roughly
    /// 1.7 units to 1.0 and forces the constant flapping the arcade runs on.
    /// Air drag went from 0.6 to 1.4 so a glide bleeds speed instead of coasting.
    /// </summary>
    [CreateAssetMenu(menuName = "Joust/Tuning Profile", fileName = "TuningProfile")]
    public class TuningProfile : ScriptableObject
    {
        [Header("Flight")]
        [Tooltip("Downward acceleration in units per second squared.")]
        public float gravity = 36f;

        [Tooltip("Vertical velocity a flap SETS, not adds. Setting is what keeps repeated flaps controllable.")]
        public float flapImpulse = 8.5f;

        [Tooltip("Horizontal acceleration while thrusting, in units per second squared.")]
        public float thrustAcceleration = 20f;

        [Tooltip("Horizontal speed cap in units per second.")]
        public float maxHorizontalSpeed = 12f;

        [Tooltip("Horizontal decay per second while airborne and not thrusting.")]
        public float airDrag = 1.4f;

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
