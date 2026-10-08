using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A floating log that rolls under your feet. It turns (visibly) and carries anyone standing on top along with its
    /// surface, like a treadmill. Put it on an empty object whose child is the log (a cylinder lying across the route).
    /// </summary>
    public class RollingLog : MonoBehaviour
    {
        [Tooltip("How fast the top of the log moves along the route. Negative carries you backwards.")]
        public float surfaceSpeed = 2.2f;
        public float radius = 0.85f;
        [Tooltip("The part that visibly turns.")]
        public Transform visual;

        Collider[] _colliders;

        void Start() => _colliders = GetComponentsInChildren<Collider>();

        void Update()
        {
            if (visual != null)
                visual.Rotate(Vector3.up, -surfaceSpeed / radius * Mathf.Rad2Deg * Time.deltaTime, Space.Self);

            if (_colliders == null || _colliders.Length == 0) return;
            Bounds b = _colliders[0].bounds;

            Vector3 along = transform.forward; // the route direction
            Vector3 delta = along * (surfaceSpeed * Time.deltaTime);

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsDead) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x - 0.1f || pos.x > b.max.x + 0.1f || pos.z < b.min.z - 0.1f || pos.z > b.max.z + 0.1f) continue;
                if (Mathf.Abs(p.FeetY() - b.max.y) > 0.45f) continue;
                p.Carry(delta);
            }
        }
    }
}
