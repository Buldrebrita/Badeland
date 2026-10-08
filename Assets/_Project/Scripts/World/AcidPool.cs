using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A pool of the creature's digestive fluid. It looks like glowing green water, but anyone who steps in is thrown
    /// back out. Never deadly, just a clear "do not touch" that teaches the player this place can hurt.
    /// </summary>
    public class AcidPool : MonoBehaviour
    {
        public float radius = 3f;
        public float surfaceHeight = 0f;
        public float knockSpeed = 9f;
        public float knockUp = 8f;

        readonly Dictionary<PlayerController, float> _lastHit = new Dictionary<PlayerController, float>();

        void Update()
        {
            var players = PlayerController.All;
            Vector3 c = transform.position;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.Invulnerable) continue;

                Vector3 pos = p.transform.position;
                Vector3 flat = new Vector3(pos.x - c.x, 0f, pos.z - c.z);
                if (flat.magnitude > radius) continue;
                if (p.FeetY() > surfaceHeight + 0.35f) continue; // jumping over it is fine

                if (_lastHit.TryGetValue(p, out float t) && Time.time - t < 1f) continue;
                _lastHit[p] = Time.time;

                if (flat.sqrMagnitude < 0.01f) flat = Vector3.right;
                p.Knock(flat.normalized * knockSpeed, knockUp);
            }
        }
    }
}
