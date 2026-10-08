using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A treasure chest hidden somewhere in the world. Walk up and press E to open it. Each chest can be opened once
    /// (per player machine for now) and adds to the shared <see cref="TreasureWallet"/>. The lid swings open.
    /// </summary>
    public class HiddenChest : MonoBehaviour
    {
        static readonly HashSet<string> Opened = new HashSet<string>();

        public string chestId = "chest";
        public int amount = 1;
        public Transform lid;
        public float openRadius = 2.6f;
        [Tooltip("The lid hinge angle when open.")]
        public float openAngle = -110f;

        bool _open;
        float _openAmount;
        float _bannerUntil;

        void Start()
        {
            if (Opened.Contains(chestId)) { _open = true; _openAmount = 1f; ApplyLid(); }
        }

        void Update()
        {
            if (_open)
            {
                if (_openAmount < 1f) { _openAmount = Mathf.MoveTowards(_openAmount, 1f, Time.deltaTime * 2.5f); ApplyLid(); }
                if (_bannerUntil > 0f && Time.time > _bannerUntil)
                {
                    _bannerUntil = 0f;
                    HudHints.Banner = "";
                }
                return;
            }

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsDead || p.IsEaten) continue;
                if ((p.transform.position - transform.position).sqrMagnitude > openRadius * openRadius) continue;

                HudHints.Show("Press E (gamepad: B) to open the chest");
                if (p.InteractPressed)
                {
                    _open = true;
                    Opened.Add(chestId);
                    TreasureWallet.Add(amount);
                    HudHints.Banner = "Treasure found!  (" + TreasureWallet.Count + " collected)";
                    _bannerUntil = Time.time + 3f;
                    return;
                }
            }
        }

        void ApplyLid()
        {
            if (lid != null) lid.localRotation = Quaternion.Euler(openAngle * Mathf.SmoothStep(0f, 1f, _openAmount), 0f, 0f);
        }
    }
}
