using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Something the lonely narrator thought they saw. A faint figure that fades away as you get close, and is not
    /// there when you look. Needs a transparent material. Decoration only.
    /// </summary>
    public class Mirage : MonoBehaviour
    {
        public Color color = new Color(0.8f, 0.9f, 1f, 0.5f);
        [Tooltip("Gone when you are closer than the first number, fully visible beyond the second.")]
        public Vector2 fadeDistance = new Vector2(3.5f, 10f);

        Renderer[] _renderers;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        void Update()
        {
            float nearest = float.MaxValue;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].IsLocal) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(players[i].transform.position, transform.position));
            }

            float fade = Mathf.InverseLerp(fadeDistance.x, fadeDistance.y, nearest);
            float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f + transform.position.x);
            float alpha = color.a * fade * flicker;

            _block.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
            _block.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));
            foreach (var r in _renderers)
            {
                r.enabled = alpha > 0.02f;
                r.SetPropertyBlock(_block);
            }
        }
    }
}
