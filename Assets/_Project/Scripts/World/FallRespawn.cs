using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Safety net: a player who falls out of the world (below <see cref="killLevel"/>) is put back where they last stood
    /// on solid ground. Never a punishment, just a way back.
    /// </summary>
    public class FallRespawn : MonoBehaviour
    {
        public float killLevel = -14f;
        [Tooltip("Where to put someone who has no safe spot yet.")]
        public Transform fallbackPoint;

        readonly Dictionary<PlayerController, Vector3> _lastSafe = new Dictionary<PlayerController, Vector3>();

        void Update()
        {
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled) continue;

                Vector3 pos = p.transform.position;
                if (p.IsGrounded && !p.IsSwimming && pos.y > -1f) _lastSafe[p] = pos;

                if (pos.y < killLevel)
                {
                    Vector3 back = _lastSafe.TryGetValue(p, out var safe) ? safe + Vector3.up * 0.5f
                                 : (fallbackPoint != null ? fallbackPoint.position : Vector3.up * 2f);
                    p.Teleport(back, p.transform.eulerAngles.y);
                }
            }
        }
    }
}
