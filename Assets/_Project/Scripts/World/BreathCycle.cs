using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// The creature breathes. A slow cycle (about 10 seconds in, 10 out) that other things follow: the lake rises and
    /// falls, the lights swell and fade. A faint heartbeat plays. It depends only on the shared clock, so all players
    /// feel the same breath at the same moment. Exactly one in the scene.
    /// </summary>
    public class BreathCycle : MonoBehaviour
    {
        /// <summary>0 = fully breathed in (low), 1 = fully breathed out (high). Smooth.</summary>
        public static float Level { get; private set; } = 0.5f;

        [Tooltip("Seconds for one full breath (in and out).")]
        [Min(2f)] public float period = 20f;
        [Range(0f, 1f)] public float heartbeatVolume = 0.45f;

        void Start()
        {
            if (heartbeatVolume <= 0f) return;

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = MakeHeartbeat();
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = heartbeatVolume;
            source.Play();
        }

        void Update()
        {
            double t = GameClock.Now;
            Level = 0.5f - 0.5f * Mathf.Cos((float)(t / period * Mathf.PI * 2.0));
        }

        // A slow "lub-dub", once every 1.2 seconds. Whole cycles in the clip, so it loops cleanly.
        static AudioClip MakeHeartbeat()
        {
            const int rate = 44100;
            var data = new float[(int)(rate * 1.2f)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                data[i] = Thump(t, 0f, 52f, 0.16f) + 0.7f * Thump(t, 0.3f, 44f, 0.14f);
            }
            var clip = AudioClip.Create("Heartbeat", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Thump(float t, float start, float frequency, float length)
        {
            float local = t - start;
            if (local < 0f || local > length) return 0f;
            float envelope = Mathf.Pow(1f - local / length, 2f);
            return Mathf.Sin(local * frequency * Mathf.PI * 2f) * envelope * 0.8f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Level = 0.5f;
    }
}
