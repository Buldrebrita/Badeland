using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A round platform that turns slowly around its middle and carries everyone standing on it round with it. Its angle
    /// depends only on the shared clock, so every player sees it in the same place.
    /// </summary>
    public class SpinningPlatform : MonoBehaviour
    {
        public float degreesPerSecond = 22f;
        public float radius = 6f;

        Quaternion _start;
        float _lastAngle;
        bool _first = true;

        void Start() => _start = transform.rotation;

        void Update()
        {
            float angle = (float)((GameClock.Now * degreesPerSecond) % 360.0);
            if (_first) { _lastAngle = angle; _first = false; }
            float delta = Mathf.DeltaAngle(_lastAngle, angle);
            _lastAngle = angle;

            Vector3 centre = transform.position;
            Quaternion step = Quaternion.AngleAxis(delta, Vector3.up);

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsDead) continue;
                Vector3 offset = p.transform.position - centre;
                if (new Vector2(offset.x, offset.z).magnitude > radius) continue;
                if (Mathf.Abs(p.FeetY() - (centre.y + 0.6f)) > 0.6f) continue; // standing on it, not swimming beside it

                Vector3 rotated = centre + step * offset;
                p.Carry(rotated - p.transform.position);
            }

            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _start;
            Physics.SyncTransforms();
        }
    }
}
