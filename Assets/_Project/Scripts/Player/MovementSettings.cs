using UnityEngine;

namespace Badeland.Player
{
    /// <summary>
    /// Every tunable for ground movement and jumping in one asset, so feel can be tuned
    /// without code (and live in play mode). Create via Assets > Create > Badeland > Movement Settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Badeland/Movement Settings", fileName = "MovementSettings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Run")]
        [Min(0f)] public float maxSpeed = 11f;
        [Min(0f)] public float acceleration = 70f;
        [Min(0f)] public float deceleration = 80f;
        [Tooltip("Degrees per second the character turns to face its movement direction.")]
        [Min(0f)] public float turnRate = 720f;

        [Header("Air")]
        [Range(0f, 1f)] public float airControl = 0.6f;

        [Header("Jump")]
        [Min(0f)] public float maxJumpHeight = 2.2f;
        [Min(0f)] public float minJumpHeight = 0.8f;
        [Tooltip("Multiplier on Physics gravity.")]
        [Min(0.1f)] public float gravityScale = 2.5f;
        [Tooltip("Extra gravity while falling, for a snappier arc.")]
        [Min(1f)] public float fallGravityMultiplier = 1.4f;
        [Tooltip("Seconds after walking off a ledge during which a jump still works.")]
        [Min(0f)] public float coyoteTime = 0.12f;
        [Tooltip("Seconds before landing during which a jump press is remembered.")]
        [Min(0f)] public float jumpBufferTime = 0.15f;
        [Tooltip("Terminal fall speed (positive number).")]
        [Min(1f)] public float maxFallSpeed = 30f;

        [Header("Swim")]
        [Min(0f)] public float swimSpeed = 6f;
        [Min(0f)] public float swimAcceleration = 25f;
        [Min(0f)] public float swimDeceleration = 18f;
        [Tooltip("How deep (metres of the body below the surface) before you start swimming instead of wading.")]
        [Min(0f)] public float swimEnterDepth = 1.2f;
        [Tooltip("Swimming ends again when the water gets shallower than this.")]
        [Min(0f)] public float swimExitDepth = 0.8f;
        [Tooltip("How far the feet hang below the surface when floating. Lower = body sits higher.")]
        [Min(0f)] public float floatDepth = 1.3f;
        [Tooltip("How hard the water pushes you back to the floating level.")]
        [Min(0f)] public float buoyancyStrength = 30f;
        [Tooltip("How quickly bobbing settles. Higher = less bounce.")]
        [Min(0f)] public float buoyancyDamping = 7f;
        [Tooltip("How far below the surface the feet go while diving.")]
        [Min(0f)] public float diveDepth = 3.5f;
        [Tooltip("Max speed going down or up under water.")]
        [Min(0f)] public float diveSpeed = 5f;
        [Tooltip("Height of the hop out of the water when you press jump at the surface.")]
        [Min(0f)] public float surfaceHopHeight = 2.4f;

        public float Gravity => Mathf.Abs(Physics.gravity.y) * gravityScale;
        public float MaxJumpVelocity => Mathf.Sqrt(2f * Gravity * maxJumpHeight);
        public float SurfaceHopVelocity => Mathf.Sqrt(2f * Gravity * surfaceHopHeight);
        public float MinJumpVelocity => Mathf.Sqrt(2f * Gravity * Mathf.Min(minJumpHeight, maxJumpHeight));
    }
}
