using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A gate across the course. A trigger box that <see cref="LapTracker"/> checks players against.
    /// It is a plain bounds check (like water), so it does not depend on physics callbacks.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        BoxCollider _box;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        public bool Contains(Vector3 worldPosition) => _box.bounds.Contains(worldPosition);
    }
}
