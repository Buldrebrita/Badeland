using UnityEngine;

namespace Badeland.Player
{
    /// <summary>
    /// Kinematic third-person controller: camera-relative run with acceleration, variable-height
    /// jump with coyote time and jump buffering. No Rigidbody, so it stays predictable and easy to
    /// network later. All tuning lives in <see cref="MovementSettings"/>; temporary effects
    /// (fish, pads) arrive through <see cref="MovementModifiers"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] MovementSettings settings;
        [Tooltip("Movement is relative to this transform's yaw. Falls back to Camera.main.")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("Visual root that turns to face the move direction. Defaults to this transform.")]
        [SerializeField] Transform visual;

        CharacterController _cc;
        PlayerInputReader _input;
        MovementModifiers _modifiers;

        Vector3 _horizontalVelocity;
        float _verticalVelocity;
        float _coyoteTimer;
        float _jumpBufferTimer;
        bool _jumping;
        float _wobblePhase;

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _modifiers = GetComponent<MovementModifiers>();
            if (visual == null) visual = transform;
        }

        void Update()
        {
            if (settings == null) return;
            float dt = Time.deltaTime;

            UpdateHorizontal(dt);
            UpdateVertical(dt);

            _cc.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            IsGrounded = _cc.isGrounded;
        }

        void UpdateHorizontal(float dt)
        {
            Vector3 wish = CameraRelative(_input.Move);

            float speedMult = 1f;
            if (_modifiers != null)
            {
                speedMult = _modifiers.SpeedMultiplier;
                if (_modifiers.WobbleDegrees > 0f && wish.sqrMagnitude > 0.0001f)
                {
                    // Clownfish effect: the walking direction swings left and right.
                    _wobblePhase += dt * _modifiers.WobbleHz * Mathf.PI * 2f;
                    float angle = Mathf.Sin(_wobblePhase) * _modifiers.WobbleDegrees;
                    wish = Quaternion.AngleAxis(angle, Vector3.up) * wish;
                }
            }

            Vector3 target = wish * (settings.maxSpeed * speedMult);
            float rate = wish.sqrMagnitude > 0.0001f ? settings.acceleration : settings.deceleration;
            if (!IsGrounded) rate *= settings.airControl;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);

            if (wish.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(wish, Vector3.up);
                visual.rotation = Quaternion.RotateTowards(visual.rotation, look, settings.turnRate * dt);
            }
        }

        void UpdateVertical(float dt)
        {
            // Timers
            _coyoteTimer = IsGrounded ? settings.coyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer = _input.JumpPressed ? settings.jumpBufferTime : _jumpBufferTimer - dt;

            if (IsGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f; // keep pressed to the ground on slopes
                _jumping = false;
            }

            // Jump (buffered press + coyote ground)
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                float jumpMult = _modifiers != null ? _modifiers.JumpMultiplier : 1f;
                // Height scales linearly with the multiplier, so velocity scales with its square root.
                _verticalVelocity = settings.MaxJumpVelocity * Mathf.Sqrt(jumpMult);
                _jumping = true;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                IsGrounded = false;
            }

            // Variable height: letting go early cuts the rise short.
            if (_jumping && !_input.JumpHeld && _verticalVelocity > settings.MinJumpVelocity)
                _verticalVelocity = settings.MinJumpVelocity;

            float g = settings.Gravity * (_verticalVelocity < 0f ? settings.fallGravityMultiplier : 1f);
            _verticalVelocity = Mathf.Max(_verticalVelocity - g * dt, -settings.maxFallSpeed);
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

            Transform cam = cameraTransform != null
                ? cameraTransform
                : (Camera.main != null ? Camera.main.transform : null);

            Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up) : Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f) forward = cam != null ? cam.up : Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            return forward * input.y + right * input.x;
        }

        /// <summary>Launch the player upward, for bounce pads and the like.</summary>
        public void Launch(float upwardVelocity)
        {
            _verticalVelocity = upwardVelocity;
            _jumping = false; // bounces are not cut short by releasing jump
            IsGrounded = false;
        }
    }
}
