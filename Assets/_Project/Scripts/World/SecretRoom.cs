using System;
using System.Collections.Generic;
using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The room below a <see cref="HiddenTrap"/>: players drop in, find the treasure, and taking it starts the
    /// escape: the room floods. Reach the exit pad before it is full (or get flushed out anyway, which is
    /// funny, not a failure). The treasure counts as soon as it is taken. Fairness: optional, survivable, no progress lost.
    /// </summary>
    public class SecretRoom : MonoBehaviour
    {
        static readonly List<SecretRoom> AllRooms = new List<SecretRoom>();

        /// <summary>Set by the networking layer. When set, taking the treasure is a request the host answers.</summary>
        public static Action<int> TreasureRequested;

        public int id = 1;
        [Tooltip("Where players appear, above the floor, so they visibly fall in.")]
        public Transform dropPoint;
        public Transform treasure;
        [Tooltip("Stepping on this gets you out.")]
        public Transform exitPad;
        [Tooltip("Where you come back out (on the course, near the trap).")]
        public Transform returnPoint;
        [Tooltip("The rising water: a box with a WaterVolume. Its top face is the surface.")]
        public Transform flood;
        public Vector3 roomCenter;
        public Vector3 roomSize = new Vector3(16f, 12f, 16f);
        [Min(0.5f)] public float treasureRadius = 1.6f;
        [Min(1f)] public float floodRiseMeters = 3.6f;
        [Min(1f)] public float floodSeconds = 14f;

        bool _treasureTaken;
        double _floodStart;
        Vector3 _floodBase;
        Bounds _exitBounds;
        Bounds _room;

        void Awake()
        {
            AllRooms.Add(this);
            if (flood != null) _floodBase = flood.position;
            _room = new Bounds(roomCenter, roomSize);
            if (exitPad != null)
            {
                var box = exitPad.GetComponent<BoxCollider>();
                _exitBounds = box != null ? box.bounds : new Bounds(exitPad.position, exitPad.lossyScale);
                _exitBounds.Expand(new Vector3(0f, 3f, 0f));
            }
        }

        void OnDestroy() => AllRooms.Remove(this);

        public static void ConfirmById(int roomId, double time, bool byLocalPlayer)
        {
            for (int i = 0; i < AllRooms.Count; i++)
                if (AllRooms[i].id == roomId) AllRooms[i].ConfirmTreasure(time, byLocalPlayer);
        }

        public void DropIn(PlayerController player)
        {
            if (dropPoint == null) return;
            player.Teleport(dropPoint.position, 0f);
        }

        /// <summary>The treasure was taken (by us or by a friend). The escape begins for everyone.</summary>
        public void ConfirmTreasure(double time, bool byLocalPlayer)
        {
            if (_treasureTaken) return;
            _treasureTaken = true;
            _floodStart = time;
            if (treasure != null) treasure.gameObject.SetActive(false);
            if (byLocalPlayer) TreasureWallet.Add(1);
        }

        void Update()
        {
            double now = GameClock.Now;
            var players = PlayerController.All;

            // Taking the treasure.
            if (!_treasureTaken && treasure != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten) continue;
                    if ((p.transform.position - treasure.position).sqrMagnitude > treasureRadius * treasureRadius) continue;

                    if (TreasureRequested != null) TreasureRequested(id);
                    else ConfirmTreasure(now, true);
                    break;
                }
            }

            // The flood rises once the treasure is gone.
            float fill = 0f;
            if (_treasureTaken && flood != null)
            {
                fill = Mathf.Clamp01((float)((now - _floodStart) / floodSeconds));
                flood.position = _floodBase + Vector3.up * (floodRiseMeters * fill);
                Physics.SyncTransforms();
            }

            // Getting out: the exit pad, or being flushed out when the room is full.
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled) continue;
                if (!_room.Contains(p.transform.position)) continue;

                bool onPad = exitPad != null && _exitBounds.Contains(p.transform.position);
                if (onPad || (_treasureTaken && fill >= 1f))
                    Leave(p);
            }
        }

        void Leave(PlayerController player)
        {
            if (returnPoint == null) return;
            player.Teleport(returnPoint.position, 0f);
            player.Launch(14f); // pops you back out with a bounce
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(roomCenter, roomSize);
        }
    }
}
