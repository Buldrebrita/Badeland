using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>Inflatable bounce pad. Launches any player who lands on it. Uses the trigger box's top as the surface.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BouncePad : MonoBehaviour
    {
        [Tooltip("Upward launch speed. About 14 gives a hop of roughly 4 m with the default gravity.")]
        public float bounceVelocity = 16f;

        BoxCollider _box;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        void Update()
        {
            var b = _box.bounds;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x || pos.x > b.max.x || pos.z < b.min.z || pos.z > b.max.z) continue;
                if (p.FeetY() > b.max.y + 0.2f || p.Velocity.y > 2f) continue; // above it, or already going up

                p.Launch(bounceVelocity);
            }
        }
    }
}
