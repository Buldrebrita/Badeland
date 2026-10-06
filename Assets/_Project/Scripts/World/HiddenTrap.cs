using System;
using System.Collections.Generic;
using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A disguised trap: a suspicious inflatable sitting on a trap door. Press Interact next to it and the
    /// floor drops away. Anyone standing on it falls into the secret room (<see cref="SecretRoom"/>). Friends
    /// can set it off for each other, which is the point. The door closes again after a few seconds.
    /// Fairness rules from DESIGN.md: survivable, optional, never removes progress.
    /// </summary>
    public class HiddenTrap : MonoBehaviour
    {
        static readonly List<HiddenTrap> AllTraps = new List<HiddenTrap>();

        /// <summary>Set by the networking layer. When set, opening the trap is a request the host answers.</summary>
        public static Action<int> TriggerRequested;

        static string _prompt = "";
        static int _promptFrame = -10;

        /// <summary>Hint text for the on-screen display ("Press E to investigate"). Empty when nothing is near.</summary>
        public static string PromptText => _promptFrame >= Time.frameCount - 1 ? _prompt : "";

        public int id = 1;
        [Tooltip("The suspicious object. Press Interact within the radius to set the trap off.")]
        public Transform suspicious;
        [Tooltip("The part of the floor that drops away. Needs a BoxCollider.")]
        public Transform panel;
        public SecretRoom room;
        [Min(1f)] public float interactRadius = 3f;
        [Min(1f)] public float openSeconds = 6f;

        const float DropAnimationSeconds = 0.3f;
        const float DropDistance = 1.6f;

        bool _open;
        double _openedAt;
        Vector3 _panelStart;
        Bounds _panelBounds;

        void Awake()
        {
            AllTraps.Add(this);
            if (panel != null)
            {
                _panelStart = panel.position;
                var box = panel.GetComponent<BoxCollider>();
                _panelBounds = box != null ? box.bounds : new Bounds(panel.position, panel.lossyScale);
            }
        }

        void OnDestroy() => AllTraps.Remove(this);

        public static void OpenById(int trapId, double time)
        {
            for (int i = 0; i < AllTraps.Count; i++)
                if (AllTraps[i].id == trapId) AllTraps[i].OpenAt(time);
        }

        public void OpenAt(double time)
        {
            if (_open) return;
            _open = true;
            _openedAt = time;
        }

        void Update()
        {
            double now = GameClock.Now;

            if (!_open)
            {
                var players = PlayerController.All;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten || suspicious == null) continue;
                    if ((p.transform.position - suspicious.position).sqrMagnitude > interactRadius * interactRadius) continue;

                    _prompt = "Press E (gamepad: B) to investigate";
                    _promptFrame = Time.frameCount;
                    if (p.InteractPressed)
                    {
                        if (TriggerRequested != null) TriggerRequested(id);
                        else OpenAt(now);
                    }
                }
            }

            if (!_open || panel == null) return;

            double open = now - _openedAt;
            if (open < 0) return; // timestamp slightly in the future

            if (open < DropAnimationSeconds)
            {
                panel.gameObject.SetActive(true);
                panel.position = _panelStart + Vector3.down * (DropDistance * (float)(open / DropAnimationSeconds));
            }
            else
            {
                panel.gameObject.SetActive(false);
                if (suspicious != null) suspicious.gameObject.SetActive(false);
            }

            // Anyone who has dropped through the hole ends up in the secret room.
            if (open > DropAnimationSeconds * 0.5)
            {
                var players = PlayerController.All;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled || room == null) continue;

                    Vector3 pos = p.transform.position;
                    if (pos.x < _panelBounds.min.x || pos.x > _panelBounds.max.x || pos.z < _panelBounds.min.z || pos.z > _panelBounds.max.z) continue;
                    float deckTop = _panelStart.y + panel.lossyScale.y * 0.5f;
                    if (p.FeetY() < deckTop - 0.5f) room.DropIn(p); // they have dropped through the hole
                }
            }

            if (open >= openSeconds)
            {
                // The floor closes up again, and the suspicious object is back.
                _open = false;
                panel.position = _panelStart;
                panel.gameObject.SetActive(true);
                if (suspicious != null) suspicious.gameObject.SetActive(true);
            }
        }
    }
}
