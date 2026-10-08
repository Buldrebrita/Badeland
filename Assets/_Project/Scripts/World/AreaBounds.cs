using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The last line of defence: nobody (and no camera) can be outside the cavern. The outline is the same oval as the
    /// walls. Anyone found outside is put back just inside it.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class AreaBounds : MonoBehaviour
    {
        public float centerX = -1f, halfX = 40f, halfZ = 24f;
        public float margin = 0.8f;
        public float maxHeight = 20f;

        static float Scale(float a) => 1f + 0.05f * Mathf.Sin(3f * a + 1f) + 0.035f * Mathf.Sin(5f * a + 2.5f) + 0.02f * Mathf.Sin(9f * a);

        bool Clamp(ref Vector3 p, float extra)
        {
            float nx = (p.x - centerX) / halfX, nz = p.z / halfZ;
            float rho = Mathf.Sqrt(nx * nx + nz * nz);
            float limit = Scale(Mathf.Atan2(nz, nx)) - (margin + extra) / halfZ;
            if (rho <= limit || rho < 0.0001f) return false;
            float k = limit / rho;
            p.x = centerX + nx * k * halfX;
            p.z = nz * k * halfZ;
            return true;
        }

        void LateUpdate()
        {
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled) continue;
                Vector3 pos = p.transform.position;
                if (Clamp(ref pos, 0f)) p.Teleport(pos, p.FacingYaw);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 c = cam.transform.position;
                bool moved = Clamp(ref c, 0.2f);
                if (c.y > maxHeight) { c.y = maxHeight; moved = true; }
                if (moved) cam.transform.position = c;
            }
        }
    }
}
