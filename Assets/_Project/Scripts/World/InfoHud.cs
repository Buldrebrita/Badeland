using UnityEngine;

namespace Badeland.World
{
    /// <summary>Draws the hints, notes and banners from <see cref="HudHints"/>. Throwaway UI, replaced by real UI later.</summary>
    public class InfoHud : MonoBehaviour
    {
        GUIStyle _hint, _noteTitle, _noteBody, _banner;

        void OnGUI()
        {
            if (_hint == null)
            {
                _hint = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _hint.normal.textColor = Color.white;
                _noteTitle = new GUIStyle(_hint) { fontSize = 30 };
                _noteTitle.normal.textColor = new Color(1f, 0.9f, 0.6f);
                _noteBody = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true, alignment = TextAnchor.UpperLeft };
                _noteBody.normal.textColor = new Color(0.95f, 0.93f, 0.85f);
                _banner = new GUIStyle(_hint) { fontSize = 34 };
                _banner.normal.textColor = new Color(0.8f, 0.95f, 1f);
            }

            string hint = HudHints.Current;
            if (!string.IsNullOrEmpty(hint))
                GUI.Label(new Rect(0, Screen.height - 90, Screen.width, 50), hint, _hint);

            if (HudHints.NoteVisible)
            {
                float width = Mathf.Min(Screen.width - 80, 900);
                var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.12f, width, 330);
                GUI.color = new Color(0f, 0f, 0f, 0.78f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 24, panel.y + 12, panel.width - 48, 50), HudHints.NoteTitle, _noteTitle);
                GUI.Label(new Rect(panel.x + 30, panel.y + 70, panel.width - 60, panel.height - 80), HudHints.NoteBody, _noteBody);
            }

            if (!string.IsNullOrEmpty(HudHints.Banner))
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 120), HudHints.Banner, _banner);
        }
    }
}
