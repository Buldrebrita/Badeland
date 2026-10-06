using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A fish that flies in an arc from a thrower to a target and then disappears. Purely visual: the real
    /// hand-over happens in <see cref="FishCarrier"/> (and over the network). Borrows the look of a jumping fish
    /// so it needs no art of its own.
    /// </summary>
    public class ThrownFishVisual : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        float _time;
        float _duration;

        public static void Spawn(Vector3 from, Vector3 to, Color color, float duration = 0.5f)
        {
            GameObject template = null;
            var jumpers = FishJumper.All;
            for (int i = 0; i < jumpers.Count && template == null; i++)
                template = jumpers[i].visual;
            if (template == null) return;

            var go = Instantiate(template, from, Quaternion.Euler(90f, 0f, 0f));
            go.SetActive(true);
            go.name = "Thrown Fish";

            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }

            var visual = go.AddComponent<ThrownFishVisual>();
            visual._from = from;
            visual._to = to;
            visual._duration = Mathf.Max(0.1f, duration);
        }

        void Update()
        {
            _time += Time.deltaTime;
            float u = _time / _duration;
            if (u >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.Lerp(_from, _to, u) + Vector3.up * (4f * 1.5f * u * (1f - u));
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World); // tumbles in the air
        }
    }
}
