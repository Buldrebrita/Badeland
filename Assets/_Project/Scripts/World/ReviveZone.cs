using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A place where the dead come back: the start of an area, or a checkpoint. A ghost that floats into the zone is
    /// revived there. At a checkpoint, a living player reaching it also revives every ghost in the party (so friends
    /// can bring each other back by making progress). At the start, only a ghost that makes its way back revives.
    /// </summary>
    public class ReviveZone : MonoBehaviour
    {
        public float radius = 5f;
        [Tooltip("Where the revived appear.")]
        public Transform respawn;
        [Tooltip("A living player reaching this zone revives all ghosts (checkpoints). Off for the start of the area.")]
        public bool livingPlayersRevive = true;
        public string label = "Checkpoint";
        public Light beacon;

        public bool Activated { get; private set; }
        float _bannerTimer;
        float _beaconBase;

        void Start()
        {
            if (respawn == null) respawn = transform;
            if (beacon != null) _beaconBase = beacon.intensity;
        }

        bool Inside(PlayerController p)
        {
            Vector3 d = p.transform.position - transform.position;
            return new Vector2(d.x, d.z).magnitude < radius && Mathf.Abs(d.y) < 6f;
        }

        void Update()
        {
            var players = PlayerController.All;

            bool livingInside = false;
            bool localLivingInside = false;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.IsDead || p.IsEaten || !Inside(p)) continue;
                livingInside = true;
                if (p.IsLocal) localLivingInside = true;
            }

            if (livingPlayersRevive && livingInside && !Activated)
            {
                Activated = true;
                if (localLivingInside) _bannerTimer = 3f;
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
                HudHints.Show(label + " reached");
            }

            if (beacon != null) beacon.intensity = Activated ? _beaconBase * 2.5f : _beaconBase;

            for (int i = 0; i < players.Count; i++)
            {
                var g = players[i];
                if (!g.IsLocal || !g.IsDead) continue; // each machine revives its own ghost
                if (!(Inside(g) || (livingPlayersRevive && livingInside))) continue;

                float angle = Mathf.Abs(g.NetworkId) * 1.7f;
                Vector3 spot = respawn.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.6f;
                g.Resurrect(spot, respawn.eulerAngles.y);
            }
        }
    }
}
