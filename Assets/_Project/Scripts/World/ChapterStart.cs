using Badeland.CameraSystem;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The start of a chapter or area: puts every player at a starting spot, brings back players who were eaten, and
    /// fades in from black with the chapter's title, like waking up. If the scene is opened directly (no players yet),
    /// it creates one so the area can be tested on its own.
    /// </summary>
    public class ChapterStart : MonoBehaviour
    {
        public Transform[] spawnPoints;
        [Tooltip("Used only when the scene is opened directly, with no players already in it.")]
        public GameObject offlinePlayerPrefab;

        public string chapterTitle = "Chapter 2";
        public string chapterSubtitle = "Inside";
        [Min(1f)] public float fadeSeconds = 6f;

        GUIStyle _title, _subtitle;

        void Start()
        {
            ChapterTransition.Done();

            if (PlayerController.All.Count == 0 && offlinePlayerPrefab != null)
                Instantiate(offlinePlayerPrefab);

            var rig = IsoCameraRig.Instance;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (rig != null) rig.AddTarget(p.transform);
                if (!p.IsLocal) continue; // each machine moves its own player; the others follow over the network

                if (rig != null) rig.PrimaryTarget = p.transform;
                if (p.IsEaten) p.Revive(); // the swallowed wake up

                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    Transform spot = spawnPoints[Mathf.Abs(p.NetworkId) % spawnPoints.Length];
                    p.Teleport(spot.position, spot.eulerAngles.y);
                }
            }

            if (rig != null) rig.Snap();
        }

        void OnGUI()
        {
            float t = Time.timeSinceLevelLoad;
            if (t > fadeSeconds + 4f) return;

            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _subtitle = new GUIStyle(_title) { fontSize = 34, fontStyle = FontStyle.Normal };
            }

            GUI.depth = -50;

            // From black: stays dark for a moment, then slowly brightens, as if opening your eyes.
            float dark = 1f - Mathf.Clamp01((t - 1.2f) / fadeSeconds);
            GUI.color = new Color(0f, 0f, 0f, dark);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            // The title appears while it is dark and fades with the light.
            float titleAlpha = Mathf.Clamp01(t / 1.5f) * (1f - Mathf.Clamp01((t - 3.5f) / 2.5f));
            if (titleAlpha > 0f)
            {
                _title.normal.textColor = new Color(1f, 1f, 1f, titleAlpha);
                _subtitle.normal.textColor = new Color(0.8f, 0.95f, 1f, titleAlpha);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 90), chapterTitle, _title);
                GUI.Label(new Rect(0, Screen.height * 0.38f + 85, Screen.width, 50), chapterSubtitle, _subtitle);
            }
        }
    }
}
