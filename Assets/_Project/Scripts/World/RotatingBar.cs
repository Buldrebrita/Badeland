using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A spinning hazard: a sweeping bar, or (turned on its side) a windmill. Jump it or get shoved away with a
    /// comedic knock. Deliberately has no collider: the hit test is done here, so players are knocked back instead of
    /// getting stuck against it. The angle depends only on the shared clock, so every player sees it in the same place.
    /// </summary>
    public class RotatingBar : MonoBehaviour
    {
        [Tooltip("Degrees per second. Negative turns the other way.")]
        public float degreesPerSecond = 60f;
        [Tooltip("The world axis it spins around. Up = a sweeping bar. Along the lane = a windmill.")]
        public Vector3 rotationAxis = Vector3.up;
        [Tooltip("Parts that hurt. Leave empty to use this object itself.")]
        public Transform[] hitBoxes;
        public float knockSpeed = 10f;
        public float knockUp = 6f;
        [Tooltip("Extra reach around the bar to match the player's body.")]
        public float bodyMargin = 0.4f;

        Quaternion _startRotation;

        void Awake()
        {
            _startRotation = transform.rotation;
            if (hitBoxes == null || hitBoxes.Length == 0) hitBoxes = new[] { transform };
        }

        void Update()
        {
            Vector3 axis = rotationAxis.sqrMagnitude < 0.001f ? Vector3.up : rotationAxis.normalized;
            float angle = (float)((GameClock.Now * degreesPerSecond) % 360.0);
            transform.rotation = Quaternion.AngleAxis(angle, axis) * _startRotation;

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue; // each player's own machine decides their own knocks

                for (int b = 0; b < hitBoxes.Length; b++)
                {
                    var box = hitBoxes[b];
                    Vector3 half = box.lossyScale * 0.5f;
                    Vector3 local = box.InverseTransformPoint(p.transform.position);
                    Vector3 scaled = Vector3.Scale(local, box.lossyScale);

                    // The player's centre is 1 m above their feet, so a jump clears a flat bar once the feet are above it.
                    if (Mathf.Abs(scaled.x) > half.x + bodyMargin) continue;
                    if (Mathf.Abs(scaled.z) > half.z + bodyMargin) continue;
                    if (Mathf.Abs(scaled.y) > half.y + 1f) continue;

                    Vector3 away = p.transform.position - transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = box.forward;
                    p.Knock(away.normalized * knockSpeed, knockUp);
                    break;
                }
            }
        }
    }
}
