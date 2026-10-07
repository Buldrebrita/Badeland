using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The way on out of an area. When every player is inside the trigger box, a message shows. For now it marks the
    /// end of what is built; later it loads the next area. Needs a BoxCollider.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class AreaExit : MonoBehaviour
    {
        [TextArea] public string message = "The end of this area. More to come...";
        [Tooltip("If set, this scene is loaded when everyone is here. Leave empty for now.")]
        public string nextScene = "";

        BoxCollider _box;
        bool _done;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        void Update()
        {
            if (_done) return;

            Bounds b = _box.bounds;
            int inside = 0, total = 0;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].IsEaten) continue;
                total++;
                if (b.Contains(players[i].transform.position)) inside++;
            }

            if (total > 0 && inside == total)
            {
                _done = true;
                HudHints.Banner = message;
                if (!string.IsNullOrEmpty(nextScene)) ChapterTransition.Go(nextScene);
            }
            else if (inside > 0)
            {
                HudHints.Show("Everyone needs to be here to go on  (" + inside + " / " + total + ")");
            }
        }
    }
}
