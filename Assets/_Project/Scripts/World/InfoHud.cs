using UnityEngine;
using UnityEngine.InputSystem;

namespace Badeland.World
{
    /// <summary>
    /// Draws the hints, notes and banners from <see cref="HudHints"/>. A note is shown in a panel you can scroll
    /// (W and S, the arrow keys, the mouse wheel, or the gamepad stick). Throwaway UI, replaced by real UI later.
    /// </summary>
    public class InfoHud : MonoBehaviour
    {
        GUIStyle _hint, _noteTitle, _noteBody, _banner, _footer;
        Vector2 _scroll;
        int _shownSerial = -1;
        float _maxScroll;

        void Update()
        {
            if (!HudHints.NoteVisible) return;

            if (_shownSerial != HudHints.NoteSerial)
            {
                _shownSerial = HudHints.NoteSerial;
                _scroll = Vector2.zero;
            }

            float move = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move -= 1f;
                if (keyboard.pageDownKey.wasPressedThisFrame) _scroll.y += 260f;
                if (keyboard.pageUpKey.wasPressedThisFrame) _scroll.y -= 260f;
            }

            var pad = Gamepad.current;
            if (pad != null) move -= pad.leftStick.ReadValue().y + pad.rightStick.ReadValue().y;

            _scroll.y += move * 650f * Time.unscaledDeltaTime;

            var mouse = Mouse.current;
            if (mouse != null) _scroll.y -= mouse.scroll.ReadValue().y * 0.6f;

            _scroll.y = Mathf.Clamp(_scroll.y, 0f, _maxScroll);
        }

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
                _footer = new GUIStyle(_hint) { fontSize = 20, fontStyle = FontStyle.Normal };
                _footer.normal.textColor = new Color(0.75f, 0.75f, 0.7f);
            }

            string hint = HudHints.Current;
            if (!string.IsNullOrEmpty(hint) && !HudHints.NoteVisible)
                GUI.Label(new Rect(0, Screen.height - 90, Screen.width, 50), hint, _hint);

            if (HudHints.NoteVisible) DrawNote();

            if (!string.IsNullOrEmpty(HudHints.Banner))
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 160), HudHints.Banner, _banner);
        }

        void DrawNote()
        {
            float width = Mathf.Min(Screen.width - 80, 920);
            float height = Mathf.Min(Screen.height * 0.78f, 640);
            var panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.45f, width, height);

            GUI.depth = -20;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 24, panel.y + 12, panel.width - 48, 50), HudHints.NoteTitle, _noteTitle);

            var view = new Rect(panel.x + 30, panel.y + 70, panel.width - 60, panel.height - 130);
            float textWidth = view.width - 24f; // room for the scroll bar
            float textHeight = _noteBody.CalcHeight(new GUIContent(HudHints.NoteBody), textWidth);
            _maxScroll = Mathf.Max(0f, textHeight - view.height + 10f);

            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, textWidth, textHeight + 10f));
            GUI.Label(new Rect(0, 0, textWidth, textHeight), HudHints.NoteBody, _noteBody);
            GUI.EndScrollView();

            string footer = _maxScroll > 1f
                ? "E (gamepad B): close      W / S, mouse wheel or stick: scroll"
                : "E (gamepad B): close";
            GUI.Label(new Rect(panel.x, panel.yMax - 50, panel.width, 40), footer, _footer);
        }
    }
}
