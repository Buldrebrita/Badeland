using UnityEngine;

namespace Badeland.World
{
    /// <summary>A gate that slides open while every one of its plates is pressed, and closes again when one is let go.</summary>
    public class SlidingGate : MonoBehaviour
    {
        public PressurePlate[] plates;
        [Tooltip("How far the gate moves when open.")]
        public Vector3 openOffset = new Vector3(0f, 13f, 0f);
        [Min(0.5f)] public float speed = 5f;

        Vector3 _closed;

        void Awake() => _closed = transform.position;

        void Update()
        {
            bool open = plates != null && plates.Length > 0;
            if (plates != null)
                foreach (var plate in plates)
                    if (plate == null || !plate.IsPressed) open = false;

            Vector3 target = open ? _closed + openOffset : _closed;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            Physics.SyncTransforms();
        }
    }
}
