using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>Throwaway on-screen text for testing: lap, finish message and the held fish. Replaced by real UI later.</summary>
    public class RaceHud : MonoBehaviour
    {
        public LapTracker tracker;

        GUIStyle _style;

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
                _style.normal.textColor = Color.white;
            }

            var players = PlayerController.All;
            if (players.Count == 0) return;

            string lapText = "";
            if (tracker != null)
            {
                lapText = tracker.AllFinished
                    ? "Finished! (the monster comes here later)"
                    : "Lap " + Mathf.Min(tracker.LapOf(players[0]), tracker.totalLaps) + " / " + tracker.totalLaps;
            }
            GUI.Label(new Rect(20, 15, 900, 40), lapText, _style);

            var carriers = FishCarrier.Active;
            if (carriers.Count > 0)
            {
                var c = carriers[0];
                string fishText = c.IsHolding
                    ? c.Held.displayName + "  " + Mathf.CeilToInt(c.TimeLeft) + "s" + (c.IsThrashing ? "  (slipping away!)" : "")
                    : "No fish";
                GUI.Label(new Rect(20, 55, 900, 40), fishText, _style);
            }
        }
    }
}
