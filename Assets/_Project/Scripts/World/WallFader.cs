using Badeland.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.World
{
    /// <summary>
    /// Lets the walls be real, tall walls without hiding the players from the isometric camera. When this wall piece
    /// is between the camera and a player, it stops being drawn (it still blocks movement, and still casts its shadow).
    /// </summary>
    public class WallFader : MonoBehaviour
    {
        Renderer _own;
        Renderer[] _all;
        bool _hidden;

        void Awake()
        {
            _own = GetComponent<Renderer>();
            _all = GetComponentsInChildren<Renderer>();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || _own == null) return;

            Vector3 camPos = cam.transform.position;
            Bounds b = _own.bounds;
            b.Expand(1.5f);

            bool blocking = false;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count && !blocking; i++)
            {
                var p = players[i];
                if (p.IsEaten) continue;
                Vector3 target = p.transform.position + Vector3.up * 1f;
                Vector3 to = target - camPos;
                float dist = to.magnitude;
                if (dist < 0.1f) continue;
                var ray = new Ray(camPos, to / dist);
                if (b.IntersectRay(ray, out float hit) && hit < dist - 0.3f) blocking = true;
            }

            if (blocking == _hidden) return;
            _hidden = blocking;
            var mode = blocking ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            for (int i = 0; i < _all.Length; i++)
                if (_all[i] != null) _all[i].shadowCastingMode = mode;
        }
    }
}
