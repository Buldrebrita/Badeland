using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Makes the sea move. The ripple texture always drifts slowly; when <see cref="Roughness"/> goes up (the danger
    /// alarm, the monster) it drifts faster and the whole sea heaves up and down. The swimming code follows the
    /// moving surface automatically. Put on the sea object, next to its WaterVolume.
    /// </summary>
    public class SeaMotion : MonoBehaviour
    {
        public static SeaMotion Instance { get; private set; }

        /// <summary>0 = calm, 1 = stormy. Set by the monster encounter.</summary>
        public float Roughness { get; set; }

        [Tooltip("Texture drift speed when calm and when stormy (in texture tiles per second).")]
        public float calmSpeed = 0.02f;
        public float stormySpeed = 0.12f;
        [Tooltip("How far the sea heaves up and down when stormy, in metres.")]
        public float swayMeters = 0.35f;

        Renderer _renderer;
        Material _material;
        string _textureProperty;
        Vector3 _home;
        Vector2 _offset;

        void Awake()
        {
            Instance = this;
            _home = transform.position;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _material = _renderer.material; // our own copy of the material
            if (_material != null) _textureProperty = _material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float speed = Mathf.Lerp(calmSpeed, stormySpeed, Roughness);
            _offset += new Vector2(speed, speed * 0.6f) * Time.deltaTime;
            if (_material != null && _textureProperty != null && _material.HasProperty(_textureProperty)) _material.SetTextureOffset(_textureProperty, _offset);

            float t = (float)GameClock.Now;
            float sway = Mathf.Sin(t * 1.7f) * 0.6f + Mathf.Sin(t * 2.9f + 1f) * 0.4f;
            transform.position = _home + Vector3.up * (sway * swayMeters * Roughness);
            if (Roughness > 0.001f) Physics.SyncTransforms();
        }
    }
}
