using UnityEngine;

namespace Badeland.World
{
    /// <summary>Lights that swell and fade with the creature's breath, so the whole place seems to pulse.</summary>
    public class BreathLight : MonoBehaviour
    {
        public Light[] lights;
        [Tooltip("Brightness as a fraction of the normal level, when breathed in and when breathed out.")]
        public float dimmest = 0.55f;
        public float brightest = 1.3f;

        float[] _normal;

        void Awake()
        {
            _normal = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) _normal[i] = lights[i].intensity;
        }

        void Update()
        {
            float k = Mathf.Lerp(dimmest, brightest, BreathCycle.Level);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = _normal[i] * k;
        }
    }
}
