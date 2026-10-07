using UnityEngine;

namespace Badeland.World
{
    /// <summary>Drifts slowly in a wide circle and bobs a little: floating jellyfish and spores. Decoration only.</summary>
    public class Drifter : MonoBehaviour
    {
        public float radius = 6f;
        [Tooltip("Radians per second.")]
        public float speed = 0.15f;
        public float phase;
        public float bobAmplitude = 0.5f;
        public float bobSpeed = 0.9f;

        Vector3 _center;

        void Awake() => _center = transform.position;

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.position = _center + new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(Time.time * bobSpeed + phase * 2f) * bobAmplitude, Mathf.Sin(t) * radius);
        }
    }
}
