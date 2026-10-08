using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>A huge eye set in the wall that slowly turns to watch the nearest player.</summary>
    public class EyeWatcher : MonoBehaviour
    {
        public float turnSpeed = 1.5f;

        void Update()
        {
            var players = PlayerController.All;
            Transform best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsEaten) continue;
                float d = (players[i].transform.position - transform.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = players[i].transform; }
            }
            if (best == null) return;

            Vector3 dir = best.position + Vector3.up - transform.position;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
        }
    }
}
