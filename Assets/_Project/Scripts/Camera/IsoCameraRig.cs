using System.Collections.Generic;
using Badeland.Player;
using Badeland.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Badeland.CameraSystem
{
    public enum CameraMode
    {
        /// <summary>Behind the character, looking slightly down, a little over the shoulder.</summary>
        Behind,
        /// <summary>Through the character's own eyes.</summary>
        Eyes,
    }

    /// <summary>
    /// The player camera. Every player picks their own view (V on keyboard, press the right stick on a gamepad):
    /// behind the character on a slight angle, or through the character's eyes. The mouse or the right stick turns
    /// the view; movement is always relative to where the camera looks. (The class keeps its old name so existing
    /// scenes keep working; the fixed isometric view is gone.)
    /// </summary>
    public class IsoCameraRig : MonoBehaviour
    {
        [SerializeField] List<Transform> targets = new List<Transform>();

        [Header("Behind")]
        [SerializeField] float behindDistance = 6.5f;
        [SerializeField] float behindPitch = 16f;
        [SerializeField] float shoulderOffset = 0.6f;
        [SerializeField] float headHeight = 1.6f;

        [Header("Looking")]
        [Tooltip("Degrees per mouse pixel.")]
        [SerializeField] float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick.")]
        [SerializeField] float stickSensitivity = 150f;

        /// <summary>The view this machine's player chose. Kept between scenes.</summary>
        public static CameraMode Mode { get; set; } = CameraMode.Behind;

        /// <summary>The camera rig in the scene (there is one). Players add themselves to it when they spawn.</summary>
        public static IsoCameraRig Instance { get; private set; }

        /// <summary>The player this machine controls (or the friend a ghost is following). The camera is on them.</summary>
        public Transform PrimaryTarget { get; set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SetBodyHidden(false);
            if (Cursor.lockState == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        float _yaw, _pitch;
        Vector3 _pivot, _pivotVelocity;
        bool _initialised;
        Transform _lastTarget;
        CameraMode _lastMode = (CameraMode)(-1);
        bool _cursorFree;

        Renderer[] _hidden;
        float _shakeTime, _shakeDuration, _shakeMagnitude;

        /// <summary>Shake the camera (monster slams, big moments).</summary>
        public void Shake(float magnitude, float seconds)
        {
            if (magnitude >= _shakeMagnitude * (_shakeDuration > 0f ? _shakeTime / _shakeDuration : 0f))
            {
                _shakeMagnitude = magnitude;
                _shakeDuration = Mathf.Max(0.01f, seconds);
                _shakeTime = _shakeDuration;
            }
        }

        /// <summary>Jump straight to the player instead of gliding (after a teleport).</summary>
        public void Snap() => _initialised = false;

        public void AddTarget(Transform t) { if (t != null && !targets.Contains(t)) targets.Add(t); }
        public void RemoveTarget(Transform t) { targets.Remove(t); }

        void LateUpdate()
        {
            Transform primary = PrimaryTarget;
            for (int i = 0; i < targets.Count && primary == null; i++) primary = targets[i];
            if (primary == null) return;

            var player = primary.GetComponent<PlayerController>();

            // A swallowed player watches through a friend's view instead.
            if (player != null && player.IsEaten)
                for (int i = 0; i < targets.Count; i++)
                {
                    var other = targets[i] != null ? targets[i].GetComponent<PlayerController>() : null;
                    if (other != null && !other.IsEaten) { primary = targets[i]; player = other; break; }
                }

            if (primary != _lastTarget)
            {
                _lastTarget = primary;
                _yaw = player != null ? player.FacingYaw : primary.eulerAngles.y;
                _initialised = false;
                SetBodyHidden(false);
            }
            if (Mode != _lastMode)
            {
                _lastMode = Mode;
                _pitch = Mode == CameraMode.Behind ? behindPitch : 0f;
                SetBodyHidden(false);
            }

            HandleInput();

            float eye = player != null ? player.EyeHeight : headHeight;
            Vector3 head = primary.position + Vector3.up * (Mode == CameraMode.Eyes ? eye : headHeight);

            if (!_initialised) { _pivot = head; _pivotVelocity = Vector3.zero; _initialised = true; }
            else _pivot = Vector3.SmoothDamp(_pivot, head, ref _pivotVelocity, Mode == CameraMode.Eyes ? 0.02f : 0.07f);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 position;

            if (Mode == CameraMode.Eyes)
            {
                position = _pivot + rot * Vector3.forward * 0.2f;
                SetBodyHidden(true, primary);
                if (player != null && player.IsLocal && !player.IsDead) player.SetFacing(_yaw);
            }
            else
            {
                Vector3 back = rot * Vector3.back;
                Vector3 side = rot * Vector3.right * shoulderOffset;
                Vector3 origin = _pivot + side;
                float dist = behindDistance;
                if (Physics.SphereCast(origin, 0.3f, back, out var hit, behindDistance, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<PlayerController>() == null)
                    dist = Mathf.Max(0.6f, hit.distance - 0.1f);
                position = origin + back * dist;
            }

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float fade = Mathf.Clamp01(_shakeTime / _shakeDuration);
                position += Random.insideUnitSphere * (_shakeMagnitude * fade);
            }

            // Dizzy after a hit: the view rolls from side to side and wobbles a little, fading out over three seconds.
            if (player != null && player.Dizziness > 0f)
            {
                float d = player.Dizziness;
                rot = rot * Quaternion.Euler(Mathf.Sin(Time.time * 5.3f) * 2.5f * d, 0f, Mathf.Sin(Time.time * 3.1f) * 11f * d);
            }

            transform.SetPositionAndRotation(position, rot);
        }

        void HandleInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            if ((keyboard != null && keyboard.vKey.wasPressedThisFrame) || (pad != null && pad.rightStickButton.wasPressedThisFrame))
                Mode = Mode == CameraMode.Behind ? CameraMode.Eyes : CameraMode.Behind;

            // The mouse is captured while playing. Escape lets go of it; clicking takes it back.
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) _cursorFree = true;
            if (_cursorFree && mouse != null && mouse.leftButton.wasPressedThisFrame) _cursorFree = false;
            bool lockCursor = !_cursorFree;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;

            if (HudHints.NoteVisible) return; // reading: the view stays still

            Vector2 look = Vector2.zero;
            if (mouse != null && lockCursor) look += mouse.delta.ReadValue() * mouseSensitivity;
            if (pad != null) look += pad.rightStick.ReadValue() * stickSensitivity * Time.unscaledDeltaTime;

            _yaw += look.x;
            float min = Mode == CameraMode.Eyes ? -80f : -15f;
            float max = Mode == CameraMode.Eyes ? 80f : 60f;
            _pitch = Mathf.Clamp(_pitch - look.y, min, max);
        }

        // In first person the player's own body would fill the view, so it is not drawn (its shadow still is).
        void SetBodyHidden(bool hidden, Transform owner = null)
        {
            if (hidden)
            {
                if (_hidden != null || owner == null) return;
                _hidden = owner.GetComponentsInChildren<Renderer>();
                foreach (var r in _hidden) if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else if (_hidden != null)
            {
                foreach (var r in _hidden) if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                _hidden = null;
            }
        }
    }
}
