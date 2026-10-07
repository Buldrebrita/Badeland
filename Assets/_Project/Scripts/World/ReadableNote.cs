using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>A note left by someone earlier. Press Interact next to it to read it. Story is told like this: in pieces.</summary>
    public class ReadableNote : MonoBehaviour
    {
        public string title = "A torn page";
        [TextArea(3, 10)] public string text = "";
        [Min(1f)] public float radius = 3f;
        [Tooltip("Seconds the note stays on screen.")]
        [Min(2f)] public float readSeconds = 14f;

        void Update()
        {
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue;
                if ((p.transform.position - transform.position).sqrMagnitude > radius * radius) continue;

                HudHints.Show("Press E (gamepad: B) to read");
                if (p.InteractPressed)
                {
                    HudHints.NoteTitle = title;
                    HudHints.NoteBody = text;
                    HudHints.NoteUntil = Time.time + readSeconds;
                }
            }
        }
    }
}
