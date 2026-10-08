using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The start of an area, or a checkpoint. A living player reaching a checkpoint revives every ghost in the party
    /// (friends bring each other back by making progress), and the latest one reached is where everyone restarts after
    /// a total wipe.
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

        /// <summary>Where everyone restarts after a total wipe: the latest checkpoint reached, or the start of the area.</summary>
        public static ReviveZone Last { get; private set; }

        public bool Activated { get; private set; }
        float _bannerTimer;
        float _beaconBase;

        void Awake()
        {
            if (!livingPlayersRevive) Last = this; // the start of the area, until a checkpoint is reached
        }

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
                Last = this;
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
                if (!(livingPlayersRevive && livingInside)) continue; // friends bring ghosts back by reaching a checkpoint

                float angle = Mathf.Abs(g.NetworkId) * 1.7f;
                Vector3 spot = respawn.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.6f;
                g.Resurrect(spot, respawn.eulerAngles.y);
            }
        }
    }
}
