using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The start of an area, or a checkpoint. A checkpoint is a grey stone altar with a carving of an item on top; it
    /// comes alive once the party finds that item and lays it on the altar. After that, a living player reaching it
    /// revives every ghost in the party, and it is where everyone restarts after a total wipe.
    /// </summary>
    public class ReviveZone : MonoBehaviour
    {
        public float radius = 5f;
        [Tooltip("Where the revived appear.")]
        public Transform respawn;
        [Tooltip("A living player reaching an activated checkpoint revives all ghosts. Off for the start of the area.")]
        public bool livingPlayersRevive = true;
        public string label = "Checkpoint";
        public Light beacon;

        [Header("Altar")]
        [Tooltip("Which QuestItem activates this checkpoint. Empty: active from the start.")]
        public string requiredItemId = "";
        [Tooltip("Where the item is laid.")]
        public Transform altarTop;

        /// <summary>Where everyone restarts after a total wipe: the latest checkpoint reached, or the start of the area.</summary>
        public static ReviveZone Last { get; private set; }

        public bool Activated { get; private set; }
        float _bannerTimer;
        float _beaconBase;

        void Awake()
        {
            if (!livingPlayersRevive) Last = this; // the start of the area, until a checkpoint is activated
        }

        void Start()
        {
            if (respawn == null) respawn = transform;
            if (beacon != null) _beaconBase = beacon.intensity;
            if (string.IsNullOrEmpty(requiredItemId)) Activated = true;
        }

        bool Inside(PlayerController p)
        {
            Vector3 d = p.transform.position - transform.position;
            return new Vector2(d.x, d.z).magnitude < radius && Mathf.Abs(d.y) < 6f;
        }

        void Update()
        {
            var players = PlayerController.All;

            if (!Activated && altarTop != null) CheckAltar(players);

            bool livingInside = false;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.IsDead || p.IsEaten || !Inside(p)) continue;
                livingInside = true;
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
                HudHints.Show(label + " restored");
            }
            else if (!Activated && altarTop != null)
            {
                for (int i = 0; i < players.Count; i++)
                    if (players[i].IsLocal && !players[i].IsDead && (players[i].transform.position - altarTop.position).sqrMagnitude < 36f)
                    { HudHints.Show("A stone altar. The carving on top shows what it wants."); break; }
            }

            if (beacon != null) beacon.intensity = Activated ? _beaconBase : 0f;
            if (!Activated) return;

            Last = this;

            for (int i = 0; i < players.Count; i++)
            {
                var g = players[i];
                if (!g.IsLocal || !g.IsDead || g.IsDying) continue; // each machine revives its own ghost
                if (!(livingPlayersRevive && livingInside)) continue; // friends bring ghosts back by reaching an active checkpoint

                float angle = Mathf.Abs(g.NetworkId) * 1.7f;
                Vector3 spot = respawn.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.6f;
                g.Resurrect(spot, respawn.eulerAngles.y);
            }
        }

        // The item must be put down (not carried) on the altar.
        void CheckAltar(System.Collections.Generic.IReadOnlyList<PlayerController> players)
        {
            var items = QuestItem.All;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.itemId != requiredItemId) continue;

                var carry = item.GetComponent<Carryable>();
                if (carry != null && carry.IsHeld) continue;

                Vector3 d = item.transform.position - altarTop.position;
                if (new Vector2(d.x, d.z).magnitude > 1.6f || Mathf.Abs(d.y) > 1.5f) continue;

                Activated = true;
                Last = this;
                item.transform.SetPositionAndRotation(altarTop.position, altarTop.rotation);
                if (carry != null) carry.Locked = true; // it stays on the altar
                _bannerTimer = 3.5f;
                return;
            }
        }
    }
}
