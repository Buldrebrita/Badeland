using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A deck that glides back and forth between two points and carries players standing on it. Its position
    /// depends only on the shared clock, so everyone sees it in the same place. Needs a BoxCollider.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        [Tooltip("Seconds for a full trip there and back.")]
        [Min(1f)] public float period = 7f;

        BoxCollider _box;

        void Awake() => _box = GetComponent<BoxCollider>();

        void Update()
        {
            // Smooth ease at both ends.
            float u = (Mathf.Sin((float)(GameClock.Now / period) * Mathf.PI * 2f) + 1f) * 0.5f;
            Vector3 target = Vector3.Lerp(pointA, pointB, u);
            Vector3 delta = target - transform.position;

            var b = _box.bounds;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal) continue;

                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x - 0.2f || pos.x > b.max.x + 0.2f || pos.z < b.min.z - 0.2f || pos.z > b.max.z + 0.2f) continue;
                if (Mathf.Abs(p.FeetY() - b.max.y) > 0.4f) continue; // standing on top, not beside or under it

                p.Carry(delta);
            }

            transform.position = target;
            Physics.SyncTransforms(); // let the character controller see the new position straight away
        }
    }
}
