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

            PlayerController me = null;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
                if (players[i].IsLocal) { me = players[i]; break; }
            if (me == null) return;

            string lapText = "";
            if (tracker != null)
            {
                lapText = tracker.AllFinished
                    ? "Finished! (the monster comes here later)"
                    : "Lap " + Mathf.Min(tracker.LapOf(me), tracker.totalLaps) + " / " + tracker.totalLaps;
            }
            GUI.Label(new Rect(20, 15, 900, 40), lapText, _style);

            var carriers = FishCarrier.Active;
            FishCarrier mine = null;
            for (int i = 0; i < carriers.Count; i++)
                if (carriers[i].IsLocal) { mine = carriers[i]; break; }

            if (mine != null && mine.IsHolding)
            {
                var c = mine;
                string fishText = c.Held.displayName + "  " + Mathf.CeilToInt(c.TimeLeft) + "s" + (c.IsThrashing ? "  (slipping away!)" : "");
                GUI.Label(new Rect(20, 55, 900, 40), fishText, _style);
            }
        }
    }
}
