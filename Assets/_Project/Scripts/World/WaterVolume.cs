using System.Collections.Generic;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A box of water. The top of the box is the surface. Players check whether they are inside it;
    /// there is no fluid simulation (see TECH.md). Add to a cube, which gets a trigger BoxCollider.
    /// Later, a shared wave function can be added to <see cref="SurfaceYAt"/> so gameplay and the water
    /// shader agree on surface height.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolume : MonoBehaviour
    {
        static readonly List<WaterVolume> All = new List<WaterVolume>();

        BoxCollider _box;

        void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true; // water must never block movement
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>World-space height of the surface at the given position.</summary>
        public float SurfaceYAt(Vector3 worldPosition) => _box.bounds.max.y;

        /// <summary>True if the point lies inside any water volume (horizontally in the box, and below its surface).</summary>
        public static bool TryFind(Vector3 worldPosition, out WaterVolume volume, out float surfaceY)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var b = All[i]._box.bounds;
                if (worldPosition.x < b.min.x || worldPosition.x > b.max.x) continue;
                if (worldPosition.z < b.min.z || worldPosition.z > b.max.z) continue;
                if (worldPosition.y < b.min.y || worldPosition.y > b.max.y) continue;

                volume = All[i];
                surfaceY = volume.SurfaceYAt(worldPosition);
                return true;
            }

            volume = null;
            surfaceY = 0f;
            return false;
        }
    }
}
