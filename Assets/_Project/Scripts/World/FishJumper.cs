using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A fish that leaps along an arc from one point to another, again and again, and can be caught by a
    /// player who gets close while it is in the air. Position depends only on the clock, so every player
    /// sees the same fish at the same place, which suits online co-op. A caught fish is gone until its next leap.
    /// </summary>
    public class FishJumper : MonoBehaviour
    {
        public FishSpecies species;
        [Tooltip("Child object holding the fish's look. Switched off while the fish is under water.")]
        public GameObject visual;

        public Vector3 startPoint;
        public Vector3 endPoint;
        [Min(0.1f)] public float arcHeight = 3f;
        [Tooltip("Seconds the fish is in the air.")]
        [Min(0.2f)] public float flightTime = 2f;
        [Tooltip("Seconds from the start of one leap to the start of the next.")]
        [Min(0.5f)] public float period = 6f;
        [Tooltip("Shifts the timing so several fish do not leap together.")]
        public float phase;
        [Min(0.1f)] public float catchRadius = 1.8f;

        int _caughtCycle = -1;

        void Update()
        {
            float t = Time.time + phase;
            int cycle = Mathf.FloorToInt(t / period);
            float inCycle = t - cycle * period;
            bool flying = inCycle < flightTime && cycle != _caughtCycle;

            if (visual != null) visual.SetActive(flying);
            if (!flying) return;

            float u = inCycle / flightTime;
            Vector3 pos = Vector3.Lerp(startPoint, endPoint, u) + Vector3.up * (4f * arcHeight * u * (1f - u));
            transform.position = pos;

            Vector3 dir = endPoint - startPoint;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

            var carriers = FishCarrier.Active;
            for (int i = 0; i < carriers.Count; i++)
            {
                var c = carriers[i];
                if (c.IsHolding) continue; // one fish at a time
                if ((c.transform.position - pos).sqrMagnitude > catchRadius * catchRadius) continue;

                if (c.TryCatch(species))
                {
                    _caughtCycle = cycle;
                    if (visual != null) visual.SetActive(false);
                    return;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(startPoint, 0.3f);
            Gizmos.DrawWireSphere(endPoint, 0.3f);
            Vector3 prev = startPoint;
            for (int i = 1; i <= 16; i++)
            {
                float u = i / 16f;
                Vector3 p = Vector3.Lerp(startPoint, endPoint, u) + Vector3.up * (4f * arcHeight * u * (1f - u));
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
