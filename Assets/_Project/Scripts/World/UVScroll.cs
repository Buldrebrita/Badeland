using UnityEngine;

namespace Badeland.World
{
    /// <summary>Slides a material's texture along, so a waterfall seems to pour. Works on its own copy of the material.</summary>
    public class UVScroll : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0f, -1.3f);

        Material _material;

        void Start() => _material = GetComponent<Renderer>().material;

        void Update()
        {
            if (_material != null) _material.mainTextureOffset += speed * Time.deltaTime;
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
