using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>Keeps every player (and the camera) inside a rectangle of the world, so nobody wanders off onto the beach or the town.</summary>
    [DefaultExecutionOrder(1000)]
    public class RectBounds : MonoBehaviour
    {
        public float minX = -170f, maxX = 170f, minZ = -28f, maxZ = 190f;
        public float cameraMinY = 0.8f, cameraMaxY = 60f;

        void LateUpdate()
        {
            var players = PlayerController.All;
            bool somebodyInArea = false;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled) continue;
                Vector3 pos = p.transform.position;
                if (pos.z > maxZ + 60f) continue; // far away on purpose (the secret room)
                somebodyInArea = true;
                Vector3 clamped = new Vector3(Mathf.Clamp(pos.x, minX, maxX), pos.y, Mathf.Clamp(pos.z, minZ, maxZ));
                if (clamped != pos) p.Teleport(clamped, p.FacingYaw);
            }

            var cam = Camera.main;
            if (cam != null && somebodyInArea)
            {
                Vector3 c = cam.transform.position;
                Vector3 clamped = new Vector3(Mathf.Clamp(c.x, minX - 4f, maxX + 4f), Mathf.Clamp(c.y, cameraMinY, cameraMaxY), Mathf.Clamp(c.z, minZ - 3f, maxZ + 4f));
                if (clamped != c) cam.transform.position = clamped;
            }
        }
    }
}
