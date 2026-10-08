using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// When every player is dead, cut to black and start everyone again at the last checkpoint (or the start of the
    /// area). Created automatically the first time someone dies. Every machine runs it for its own players.
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        static DeathScreen _instance;

        enum Phase { Idle, FadeOut, Hold, FadeIn }
        Phase _phase;
        float _time;
        Texture2D _black;
        GUIStyle _style;
        string _message = "";

        public static void Ensure()
        {
            if (_instance != null) return;
            _instance = new GameObject("Death Screen").AddComponent<DeathScreen>();
        }

        void Awake()
        {
            _instance = this;
            _black = new Texture2D(1, 1);
            _black.SetPixel(0, 0, Color.black);
            _black.Apply();
        }

        void Update()
        {
            var all = PlayerController.All;
            switch (_phase)
            {
                case Phase.Idle:
                    if (EveryoneDead(all))
                    {
                        _message = all.Count > 1 ? "Everyone fell." : "You died.";
                        _phase = Phase.FadeOut;
                        _time = 0f;
                    }
                    break;
                case Phase.FadeOut:
                    _time += Time.deltaTime;
                    if (_time >= 1f) { _phase = Phase.Hold; _time = 0f; Restart(all); }
                    break;
                case Phase.Hold:
                    _time += Time.deltaTime;
                    if (_time >= 0.7f) { _phase = Phase.FadeIn; _time = 0f; }
                    break;
                case Phase.FadeIn:
                    _time += Time.deltaTime;
                    if (_time >= 1f) _phase = Phase.Idle;
                    break;
            }
        }

        static bool EveryoneDead(System.Collections.Generic.IReadOnlyList<PlayerController> all)
        {
            bool any = false;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p.IsEaten) continue;
                if (!p.IsDead || p.IsDying) return false; // someone is alive, or still falling
                any = true;
            }
            return any;
        }

        static void Restart(System.Collections.Generic.IReadOnlyList<PlayerController> all)
        {
            var zone = ReviveZone.Last;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (!p.IsLocal || !p.IsDead) continue; // each machine restarts its own players

                Vector3 spot = p.transform.position + Vector3.up * 0.5f;
                float yaw = p.transform.eulerAngles.y;
                if (zone != null)
                {
                    float angle = Mathf.Abs(p.NetworkId) * 1.7f;
                    spot = zone.respawn.position + new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)) * 1.6f;
                    yaw = zone.respawn.eulerAngles.y;
                }
                p.Resurrect(spot, yaw);
            }
        }

        void OnGUI()
        {
            float alpha = _phase == Phase.FadeOut ? Mathf.Clamp01(_time) : _phase == Phase.Hold ? 1f : _phase == Phase.FadeIn ? 1f - Mathf.Clamp01(_time) : 0f;
            if (alpha <= 0.001f) return;

            GUI.depth = -1000;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _black);

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 36, fontStyle = FontStyle.Bold };
                _style.normal.textColor = Color.white;
            }
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), _message, _style);
            GUI.color = Color.white;
        }
    }
}
