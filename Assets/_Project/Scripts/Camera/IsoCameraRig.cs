using System.Collections.Generic;
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
        [Range(30f, 70f)] [SerializeField] float pitch = 50f;
        [SerializeField] float yaw = 45f;

        [Header("Distance")]
        [Min(1f)] [SerializeField] float baseDistance = 14f;
        [Tooltip("Extra distance per metre the targets are spread apart.")]
        [Min(0f)] [SerializeField] float spreadZoom = 0.9f;
        [Min(1f)] [SerializeField] float maxDistance = 30f;

        [Header("Follow")]
        [Tooltip("Seconds to catch up. Smaller is tighter.")]
        [Min(0.01f)] [SerializeField] float followTime = 0.25f;
        [Tooltip("Aim slightly above the targets' feet.")]
        [SerializeField] float focusHeight = 1f;

        Vector3 _focusVelocity;
        float _distanceVelocity;
        Vector3 _focus;
        float _distance;
        bool _initialised;

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
            transform.SetPositionAndRotation(_focus - rot * Vector3.forward * _distance, rot);
        }

        bool ComputeFraming(out Vector3 center, out float spread)
        {
            center = Vector3.zero;
            spread = 0f;
            int count = 0;
            Vector3 min = Vector3.zero, max = Vector3.zero;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
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
