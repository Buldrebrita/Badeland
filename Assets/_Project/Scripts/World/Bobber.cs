using UnityEngine;

namespace Badeland.World
{
    /// <summary>Gently bobs an object up and down, like something floating or hovering. Decoration only.</summary>
    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.25f;
        public float speed = 1.2f;
        public float phase;

        Vector3 _start;

        void Awake() => _start = transform.position;

        void Update()
        {
            transform.position = _start + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * amplitude);
        }
    }
}
