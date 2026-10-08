using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A note, carving or sign to read. Press Interact next to it to open it, and again to close it. A long text can be
    /// scrolled. While you read, your character stands still and nothing can hurt it. Story is told like this, in pieces.
    /// </summary>
    public class ReadableNote : MonoBehaviour
    {
        public string title = "A torn page";
        [TextArea(3, 20)] public string text = "";
        [Min(1f)] public float radius = 3f;

        int _openedFrame = -1;

        void Update()
        {
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue;

                // Reading this note: Interact closes it again.
                if (HudHints.IsReader(p) && ReferenceEquals(HudHints.NoteSource, this))
                {
                    if (p.InteractPressed && Time.frameCount != _openedFrame) HudHints.CloseNote();
                    return;
                }

                if (HudHints.NoteVisible) continue; // someone is reading something else
                if ((p.transform.position - transform.position).sqrMagnitude > radius * radius) continue;

                HudHints.Show("Press E (gamepad: B) to read");
                if (p.InteractPressed)
                {
                    HudHints.OpenNote(p, this, title, text);
                    _openedFrame = Time.frameCount;
                    return;
                }
            }
        }
    }
}
