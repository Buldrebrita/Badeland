using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A sweeping bar. Jump over it or get shoved away with a comedic knock. Deliberately has no collider:
    /// the hit test is done here, so players are knocked back instead of getting stuck against it.
    /// </summary>
    public class RotatingBar : MonoBehaviour
    {
        [Tooltip("Degrees per second. Negative turns the other way.")]
        public float degreesPerSecond = 60f;
        public float knockSpeed = 10f;
        public float knockUp = 6f;
        [Tooltip("Extra reach around the bar to match the player's body.")]
        public float bodyMargin = 0.4f;

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);

            Vector3 half = transform.lossyScale * 0.5f;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                Vector3 local = transform.InverseTransformPoint(p.transform.position);
                Vector3 scaled = Vector3.Scale(local, transform.lossyScale);

                // The player's centre is 1 m above their feet, so a jump clears the bar once the feet are above it.
                if (Mathf.Abs(scaled.x) > half.x + bodyMargin) continue;
                if (Mathf.Abs(scaled.z) > half.z + bodyMargin) continue;
                if (Mathf.Abs(scaled.y) > half.y + 1f) continue;

                Vector3 away = p.transform.position - transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = transform.forward;
                p.Knock(away.normalized * knockSpeed, knockUp);
            }
        }
    }
}
