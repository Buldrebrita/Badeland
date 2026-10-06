using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.CameraSystem
{
    /// <summary>
    /// Elevated three-quarter follow camera that frames one or more players.
    /// Fixed yaw and pitch (so controls stay predictable), smooth follow, and a distance that
    /// grows with how far apart the targets are. S1 version without Cinemachine; the zone
    /// overrides and occlusion fade from TECH.md come later.
    /// </summary>
    public class IsoCameraRig : MonoBehaviour
    {
        [SerializeField] List<Transform> targets = new List<Transform>();

        [Header("Angle")]
        [Range(20f, 70f)] [SerializeField] float pitch = 30f;
        [SerializeField] float yaw = 45f;

        [Header("Distance")]
        [Min(1f)] [SerializeField] float baseDistance = 18f;
        [Tooltip("Extra distance per metre the targets are spread apart.")]
        [Min(0f)] [SerializeField] float spreadZoom = 0.9f;
        [Min(1f)] [SerializeField] float maxDistance = 30f;

        [Header("Follow")]
        [Tooltip("Seconds to catch up. Smaller is tighter.")]
        [Min(0.01f)] [SerializeField] float followTime = 0.25f;
        [Tooltip("Aim slightly above the targets' feet.")]
        [SerializeField] float focusHeight = 1f;

        [Header("Framing")]
        [Tooltip("Players further than this from the main player are left out of the framing (for example someone in a secret room).")]
        [Min(5f)] [SerializeField] float maxFramingDistance = 60f;

        /// <summary>The camera rig in the scene (there is one). Players add themselves to it when they spawn.</summary>
        public static IsoCameraRig Instance { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Vector3 _focusVelocity;
        float _distanceVelocity;
        Vector3 _focus;
        float _distance;
        bool _initialised;

        /// <summary>The player this machine controls. Framing is centred around them. Defaults to the first target.</summary>
        public Transform PrimaryTarget { get; set; }

        float _shakeTime;
        float _shakeDuration;
        float _shakeMagnitude;

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

        /// <summary>Jump straight to the framing instead of gliding (after a teleport).</summary>
        public void Snap() => _initialised = false;

        public void AddTarget(Transform t) { if (t != null && !targets.Contains(t)) targets.Add(t); }
        public void RemoveTarget(Transform t) { targets.Remove(t); }

        void LateUpdate()
        {
            if (!ComputeFraming(out Vector3 center, out float spread)) return;

            Vector3 focusTarget = center + Vector3.up * focusHeight;
            float distanceTarget = Mathf.Min(baseDistance + spread * spreadZoom, maxDistance);

            if (!_initialised)
            {
                _focus = focusTarget;
                _distance = distanceTarget;
                _initialised = true;
            }
            else
            {
                _focus = Vector3.SmoothDamp(_focus, focusTarget, ref _focusVelocity, followTime);
                _distance = Mathf.SmoothDamp(_distance, distanceTarget, ref _distanceVelocity, followTime * 1.5f);
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = _focus - rot * Vector3.forward * _distance;

            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;
                float fade = Mathf.Clamp01(_shakeTime / _shakeDuration);
                position += Random.insideUnitSphere * (_shakeMagnitude * fade);
            }

            transform.SetPositionAndRotation(position, rot);
        }

        bool ComputeFraming(out Vector3 center, out float spread)
        {
            center = Vector3.zero;
            spread = 0f;
            int count = 0;
            Vector3 min = Vector3.zero, max = Vector3.zero;

            Transform primary = PrimaryTarget;
            if (primary == null)
                for (int i = 0; i < targets.Count && primary == null; i++) primary = targets[i];

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;

                // Leave out players who have been swallowed, and players far away (a different room).
                var player = targets[i].GetComponent<PlayerController>();
                if (player != null && player.IsEaten) continue;
                if (primary != null && (targets[i].position - primary.position).sqrMagnitude > maxFramingDistance * maxFramingDistance) continue;

                Vector3 p = targets[i].position;
                if (count == 0) { min = max = p; }
                else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                count++;
            }

            if (count == 0) return false;
            center = (min + max) * 0.5f;
            Vector3 size = max - min;
            spread = new Vector2(size.x, size.z).magnitude;
            return true;
        }
    }
}
