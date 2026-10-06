using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Red flashing alarm beacons around the big platform. Off until the danger warning, then they pulse.
    /// The monster encounter turns the <see cref="Intensity"/> up and down.
    /// </summary>
    public class AlarmLights : MonoBehaviour
    {
        public static AlarmLights Instance { get; private set; }

        public Light[] lights;
        public Renderer[] beacons;
        public float maxLightIntensity = 14f;
        [Tooltip("Flashes per second.")]
        public float flashRate = 1.6f;

        /// <summary>0 = off, 1 = full alarm.</summary>
        public float Intensity { get; set; }

        MaterialPropertyBlock _block;

        void Awake()
        {
            Instance = this;
            _block = new MaterialPropertyBlock();
            SetLights(0f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * flashRate * Mathf.PI * 2f);
            SetLights(Intensity * (0.15f + 0.85f * pulse));
        }

        void SetLights(float level)
        {
            foreach (var l in lights)
            {
                if (l == null) continue;
                l.enabled = level > 0.01f;
                l.intensity = maxLightIntensity * level;
            }

            Color glow = Color.Lerp(new Color(0.25f, 0.02f, 0.02f), new Color(1f, 0.1f, 0.05f), level);
            _block.SetColor("_BaseColor", glow);
            _block.SetColor("_Color", glow);
            foreach (var b in beacons)
                if (b != null) b.SetPropertyBlock(_block);
        }
    }
}
