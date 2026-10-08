using System;
using System.Collections.Generic;
using Badeland.CameraSystem;
using Badeland.World;
using UnityEngine;

namespace Badeland.Player
{
    /// <summary>
    /// Kinematic third-person controller: camera-relative run with acceleration, variable-height
    /// jump with coyote time and jump buffering, plus swimming in <see cref="WaterVolume"/>s.
    /// No Rigidbody, so it stays predictable and easy to network later. All tuning lives in
    /// <see cref="MovementSettings"/>; temporary effects (fish, pads) arrive through
    /// <see cref="MovementModifiers"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public partial class PlayerController : MonoBehaviour
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
        float _noSwimTimer;

        static readonly List<PlayerController> AllPlayers = new List<PlayerController>();
        /// <summary>Every active player. Course pieces (laps, bounce pads, fish) look players up here.</summary>
        public static IReadOnlyList<PlayerController> All => AllPlayers;

        float _knockTimer;

        /// <summary>False for other players' avatars in an online game. They are moved by the network, not by this script.</summary>
        public bool IsLocal { get; set; } = true;

        /// <summary>Identifies the player on the network (the client id). 0 offline.</summary>
        public int NetworkId { get; set; }

        /// <summary>True once the monster has swallowed this player. They no longer move or show.</summary>
        public bool IsEaten { get; private set; }

        /// <summary>True while something else (a water slide) is moving the player.</summary>
        public bool IsExternallyControlled { get; private set; }

        /// <summary>
        /// While true the player ignores the movement, jump and dive controls (reading a note, a cutscene). They still
        /// stand on the ground and can still press Interact.
        /// </summary>
        public bool InputLocked { get; set; }

        /// <summary>While true, nothing can hurt the player: no knocks, and the monster cannot eat them (except a forced eat).</summary>
        public bool Invulnerable { get; set; }

        Vector2 MoveInput => InputLocked ? Vector2.zero : _input.Move;
        bool JumpPressedNow => !InputLocked && _input.JumpPressed;

        /// <summary>Fired once when this player is swallowed.</summary>
        public event Action Swallowed;

        /// <summary>Fired when a swallowed player comes back (waking up inside the monster).</summary>
        public event Action Revived;

        public bool IsGrounded { get; private set; }
        public bool IsSwimming { get; private set; }
        /// <summary>Metres of the body below the water surface (0 when not in water).</summary>
        public float WaterDepth { get; private set; }
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;

        /// <summary>Fired when the player enters deep water. The argument is the downward speed on impact.
        /// Hook up splash effects and sounds to this.</summary>
        public event Action<float> EnteredWater;
        public event Action ExitedWater;

        void Awake()
        {
            AllPlayers.Add(this); // in Awake, so remote players (whose controller is switched off) are still listed
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _modifiers = GetComponent<MovementModifiers>();
            if (visual == null) visual = transform;
        }

        void OnDestroy() => AllPlayers.Remove(this);

        void Update()
        {
            if (settings == null || IsEaten || IsExternallyControlled) return;
            float dt = Time.deltaTime;
            if (IsDead) { UpdateGhost(dt); return; }
            _knockTimer -= dt;

            UpdateWaterState(dt);

            if (IsSwimming)
            {
                UpdateHorizontal(dt, settings.swimSpeed, settings.swimAcceleration, settings.swimDeceleration, 1f);
                UpdateSwimVertical(dt);
            }
            else
            {
                UpdateHorizontal(dt, settings.maxSpeed, settings.acceleration, settings.deceleration,
                    IsGrounded ? 1f : settings.airControl);
                UpdateVertical(dt);
            }

            _cc.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            IsGrounded = _cc.isGrounded;
        }

        // ---------------------------------------------------------------- water

        public float FeetY() => transform.position.y + _cc.center.y - _cc.height * 0.5f;

        void UpdateWaterState(float dt)
        {
            _noSwimTimer -= dt;

            bool inWater = WaterVolume.TryFind(transform.position, out WaterVolume volume, out float surfaceY);
            WaterDepth = inWater ? Mathf.Max(0f, surfaceY - FeetY()) : 0f;

            if (!IsSwimming)
            {
                if (inWater && _noSwimTimer <= 0f && WaterDepth >= settings.swimEnterDepth)
                {
                    IsSwimming = true;
                    _jumping = false;
                    _jumpBufferTimer = 0f;
                    float impact = Mathf.Max(0f, -_verticalVelocity);
                    _verticalVelocity *= 0.35f; // the water catches you
                    EnteredWater?.Invoke(impact);
                }
            }
            else if (!inWater || WaterDepth < settings.swimExitDepth)
            {
                IsSwimming = false;
                ExitedWater?.Invoke();
            }
        }

        void UpdateSwimVertical(float dt)
        {
            // Hop out of the water: jump at the surface to leap onto the pool edge.
            if (JumpPressedNow && WaterDepth <= settings.floatDepth + 0.5f)
            {
                _verticalVelocity = settings.SurfaceHopVelocity;
                _noSwimTimer = 0.35f; // do not re-enter the water on the way up
                IsSwimming = false;
                ExitedWater?.Invoke();
                return;
            }

            if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = 0f; // resting on the pool floor

            // Spring-damper towards the target depth: floating by default, deeper while diving.
            WaterVolume.TryFind(transform.position, out _, out float surfaceY);
            float targetDepth = (!InputLocked && _input.DiveHeld) ? settings.diveDepth : settings.floatDepth;
            float targetFeetY = surfaceY - targetDepth;

            float accel = settings.buoyancyStrength * (targetFeetY - FeetY())
                          - settings.buoyancyDamping * _verticalVelocity;
            _verticalVelocity += accel * dt;
            _verticalVelocity = Mathf.Clamp(_verticalVelocity, -settings.diveSpeed, settings.diveSpeed);
        }

        // ---------------------------------------------------------------- ground and air

        void UpdateHorizontal(float dt, float maxSpeed, float acceleration, float deceleration, float control)
        {
            Vector3 wish = CameraRelative(MoveInput);

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

            Vector3 target = wish * (maxSpeed * speedMult);
            float rate = (wish.sqrMagnitude > 0.0001f ? acceleration : deceleration) * control;

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
            _jumpBufferTimer = JumpPressedNow ? settings.jumpBufferTime : _jumpBufferTimer - dt;

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
            if (_jumping && (InputLocked || !_input.JumpHeld) && _verticalVelocity > settings.MinJumpVelocity)
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

        /// <summary>The direction the player's stick points, relative to the camera, on the ground plane.</summary>
        public Vector3 WorldMoveDirection() => CameraRelative(MoveInput);

        public bool InteractPressed => !IsDead && _input != null && _input.InteractPressed;

        /// <summary>The monster swallowed this player: hide them and switch their movement off.</summary>
        public void Eat(bool force = false)
        {
            if (IsEaten) return;
            if (Invulnerable && !force) return;
            if (IsDead)
            {
                if (!force) return;
                ClearGhostLook();
                IsDead = false;
            }
            IsEaten = true;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            if (_cc != null) _cc.enabled = false;
            Swallowed?.Invoke();
        }

        /// <summary>Bring a swallowed player back: visible again and able to move.</summary>
        public void Revive()
        {
            if (!IsEaten) return;
            IsEaten = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            if (_cc != null && IsLocal && !IsExternallyControlled) _cc.enabled = true; // other players' avatars are moved by the network
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            Revived?.Invoke();
        }

        /// <summary>Instantly move the player somewhere else (trap doors, secret room exits).</summary>
        public void Teleport(Vector3 position, float yawDegrees)
        {
            bool wasEnabled = _cc.enabled;
            _cc.enabled = false;
            transform.position = position;
            visual.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            _cc.enabled = wasEnabled && !IsExternallyControlled && !IsEaten;

            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            IsSwimming = false;
            IsGrounded = false;
            _noSwimTimer = 0.3f;
            if (IsLocal && IsoCameraRig.Instance != null) IsoCameraRig.Instance.Snap();
        }

        /// <summary>Move with something the player stands on (a moving platform).</summary>
        public void Carry(Vector3 delta)
        {
            if (!IsLocal || IsEaten || IsExternallyControlled || !_cc.enabled) return;
            _cc.Move(delta);
        }

        /// <summary>A slide (or similar) takes over: normal movement and collision are switched off.</summary>
        public void BeginExternalControl()
        {
            IsExternallyControlled = true;
            _cc.enabled = false;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            IsSwimming = false;
            _jumping = false;
        }

        public void SetExternalPose(Vector3 position, float yawDegrees)
        {
            transform.position = position;
            visual.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        /// <summary>Hand control back, carrying on with the given velocity (so a slide can throw you into the sea).</summary>
        public void EndExternalControl(Vector3 velocity)
        {
            IsExternallyControlled = false;
            _cc.enabled = !IsEaten;
            _horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            _verticalVelocity = velocity.y;
            IsGrounded = false;
            _noSwimTimer = 0.2f;
        }

        /// <summary>Hit by an obstacle: shoved sideways and up. Ignored for a moment after a hit so it cannot repeat every frame.</summary>
        public void Knock(Vector3 horizontalVelocity, float upwardVelocity)
        {
            if (_knockTimer > 0f || Invulnerable || IsDead) return;
            _knockTimer = 0.6f;
            horizontalVelocity.y = 0f;
            _horizontalVelocity = horizontalVelocity;
            Launch(upwardVelocity);
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
