using System;
using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Counts laps of the circuit. Players must pass the checkpoints in order, and the last checkpoint is the
    /// finish line. Everyone starts lap 1 without having to cross the line first. After the last lap a player
    /// is finished, and when all players are finished <see cref="AllPlayersFinished"/> fires. That is the
    /// moment the floating platform and the monster take over.
    ///
    /// <see cref="WorldLap"/> is the highest lap anyone has reached. The subtle per-lap changes in the park
    /// (<see cref="LapChanges"/>) follow it, so the party sees them as soon as the first player starts a new lap.
    /// </summary>
    public class LapTracker : MonoBehaviour
    {
        [Tooltip("In course order. The last one is the finish line.")]
        public Checkpoint[] checkpoints;
        [Min(1)] public int totalLaps = 3;

        class Progress
        {
            public int lap = 1;
            public int next;
            public bool finished;
            public float lapStartTime;
            public Vector3 lastPosition;
        }

        readonly Dictionary<PlayerController, Progress> _progress = new Dictionary<PlayerController, Progress>();

        public int WorldLap { get; private set; } = 1;
        public bool AllFinished { get; private set; }

        public event Action<int> WorldLapChanged;
        /// <summary>Player, the lap just completed, and how long it took in seconds.</summary>
        public event Action<PlayerController, int, float> PlayerLapCompleted;
        public event Action<PlayerController> PlayerFinished;
        public event Action AllPlayersFinished;

        public int LapOf(PlayerController player) =>
            _progress.TryGetValue(player, out var p) ? p.lap : 1;

        public bool HasFinished(PlayerController player) =>
            _progress.TryGetValue(player, out var p) && p.finished;

        void Start() => WorldLapChanged?.Invoke(WorldLap);

        void Update()
        {
            if (checkpoints == null || checkpoints.Length == 0) return;

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Vector3 current = player.transform.position;

                if (!_progress.TryGetValue(player, out var progress))
                {
                    _progress[player] = new Progress { lapStartTime = Time.time, lastPosition = current };
                    continue;
                }

                Vector3 last = progress.lastPosition;
                progress.lastPosition = current;

                if (progress.finished) continue;
                // The gate counts when the player's path since last frame crossed it (at any height).
                if (!checkpoints[progress.next].Crossed(last, current)) continue;

                progress.next++;
                if (progress.next < checkpoints.Length) continue;

                // Crossed the finish line with every checkpoint passed: a lap is done.
                PlayerLapCompleted?.Invoke(player, progress.lap, Time.time - progress.lapStartTime);
                progress.next = 0;
                progress.lapStartTime = Time.time;

                if (progress.lap >= totalLaps)
                {
                    progress.finished = true;
                    PlayerFinished?.Invoke(player);
                    CheckAllFinished();
                }
                else
                {
                    progress.lap++;
                    if (progress.lap > WorldLap)
                    {
                        WorldLap = progress.lap;
                        WorldLapChanged?.Invoke(WorldLap);
                    }
                }
            }
        }

        void CheckAllFinished()
        {
            if (AllFinished || _progress.Count < PlayerController.All.Count) return;
            foreach (var p in _progress.Values)
                if (!p.finished) return;

            AllFinished = true;
            AllPlayersFinished?.Invoke();
        }
    }
}
