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
        [Min(0f)] public float maxSpeed = 7f;
        [Min(0f)] public float acceleration = 45f;
        [Min(0f)] public float deceleration = 55f;
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

        public float Gravity => Mathf.Abs(Physics.gravity.y) * gravityScale;
        public float MaxJumpVelocity => Mathf.Sqrt(2f * Gravity * maxJumpHeight);
        public float MinJumpVelocity => Mathf.Sqrt(2f * Gravity * Mathf.Min(minJumpHeight, maxJumpHeight));
    }
}
