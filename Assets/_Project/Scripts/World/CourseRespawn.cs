using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Fall in the water and you are put back at the start of the obstacle you fell from (the furthest one you have
    /// reached). It waits a couple of seconds first, so swimming out to a hidden chest on a little islet still works.
    /// </summary>
    public class CourseRespawn : MonoBehaviour
    {
        // One entry per section of the course, in order.
        public Vector3[] centers;
        public Vector3[] directions;
        public float[] halfLengths;
        public float[] halfWidths;
        public Vector3[] spawnPoints;

        [Min(0.5f)] public float swimSecondsBeforeReturn = 2.2f;
        public float maxZ = 90f; // beyond this is somewhere else on purpose (the secret room)

        readonly Dictionary<PlayerController, int> _progress = new Dictionary<PlayerController, int>();
        readonly Dictionary<PlayerController, float> _swimming = new Dictionary<PlayerController, float>();

        void Update()
        {
            if (MonsterEncounter.Started) return; // the finish area belongs to the monster now

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsDead || p.IsExternallyControlled) continue;
                Vector3 pos = p.transform.position;
                if (pos.z > maxZ) continue;

                _progress.TryGetValue(p, out int progress);

                // Which section is the player standing on? (Only counts further along the course.)
                if (p.IsGrounded && !p.IsSwimming)
                    for (int s = progress; s < centers.Length; s++)
                    {
                        Vector3 d = pos - centers[s];
                        float along = Vector3.Dot(d, directions[s]);
                        float across = Vector3.Dot(d, Vector3.Cross(Vector3.up, directions[s]));
                        if (Mathf.Abs(along) <= halfLengths[s] + 0.5f && Mathf.Abs(across) <= halfWidths[s] + 0.5f && pos.y > 0.4f)
                        {
                            progress = s;
                            break;
                        }
                    }
                _progress[p] = progress;

                if (p.IsSwimming)
                {
                    _swimming.TryGetValue(p, out float t);
                    t += Time.deltaTime;
                    _swimming[p] = t;
                    if (t >= swimSecondsBeforeReturn && centers.Length > 0)
                    {
                        _swimming[p] = 0f;
                        Vector3 yawDir = directions[progress];
                        p.Teleport(spawnPoints[progress], Mathf.Atan2(yawDir.x, yawDir.z) * Mathf.Rad2Deg);
                    }
                }
                else _swimming[p] = 0f;
            }
        }
    }
}
