using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A pool of the creature's digestive acid. Anyone who steps in starts to be etched away: their colour turns sickly
    /// and eaten, bubbles boil around them, and if they stay in too long they dissolve and come back at the last safe
    /// spot they stood on. A heavy stone dropped in is etched away too, and returns to where it started.
    /// </summary>
    public class AcidPool : MonoBehaviour
    {
        public float radius = 3f;
        public float surfaceHeight = 0f;
        [Tooltip("Seconds in the acid before a player dissolves.")]
        public float dissolveSeconds = 1.8f;

        class Victim
        {
            public Renderer[] renderers;
            public Material[] materials;
            public Color[] originals;
            public float exposure;
            public Vector3 lastSafe;
            public bool hasSafe;
            public bool etched;
        }

        static readonly Color Etched = new Color(0.2f, 0.28f, 0.05f);

        readonly Dictionary<PlayerController, Victim> _victims = new Dictionary<PlayerController, Victim>();
        readonly Dictionary<Carryable, Vector3> _homes = new Dictionary<Carryable, Vector3>();
        readonly Dictionary<Carryable, Vector3> _scales = new Dictionary<Carryable, Vector3>();
        readonly Dictionary<Carryable, float> _stoneTime = new Dictionary<Carryable, float>();
        Transform[] _bubbles;
        float[] _bubblePhase;
        Material _bubbleMaterial;

        void Start()
        {
            var carryables = Carryable.All;
            for (int i = 0; i < carryables.Count; i++) { _homes[carryables[i]] = carryables[i].transform.position; _scales[carryables[i]] = carryables[i].transform.localScale; }

            var renderer = GetComponent<Renderer>();
            _bubbleMaterial = renderer != null ? renderer.sharedMaterial : null;

            _bubbles = new Transform[14];
            _bubblePhase = new float[_bubbles.Length];
            for (int i = 0; i < _bubbles.Length; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.name = "Acid Bubble";
                Destroy(b.GetComponent<Collider>());
                if (_bubbleMaterial != null) b.GetComponent<Renderer>().sharedMaterial = _bubbleMaterial;
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b.transform.SetParent(transform.parent, true);
                _bubbles[i] = b.transform;
                _bubblePhase[i] = Random.value;
            }
        }

        void Update()
        {
            Vector3 c = transform.position;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.IsEaten) continue;
                if (!_victims.TryGetValue(p, out var v)) { v = Capture(p); _victims[p] = v; }

                Vector3 pos = p.transform.position;
                Vector3 flat = new Vector3(pos.x - c.x, 0f, pos.z - c.z);
                bool inAcid = flat.magnitude < radius && p.FeetY() < surfaceHeight + 0.35f;

                if (!inAcid && flat.magnitude > radius + 1.5f && p.IsGrounded && !p.IsSwimming)
                {
                    v.lastSafe = pos; v.hasSafe = true;
                }

                v.exposure = Mathf.Clamp01(v.exposure + (inAcid ? 1f / dissolveSeconds : -1.5f / dissolveSeconds) * Time.deltaTime);
                if (v.exposure > 0f || v.etched)
                {
                    ApplyEtch(v, v.exposure); // only while this pool is involved, so pools never undo each other
                    v.etched = v.exposure > 0f;
                }

                if (p.IsLocal && v.exposure >= 1f)
                {
                    Vector3 back = v.hasSafe ? v.lastSafe + Vector3.up * 0.3f : c + (flat.sqrMagnitude < 0.01f ? Vector3.right : flat.normalized) * (radius + 3f) + Vector3.up * 2f;
                    p.Teleport(back, p.transform.eulerAngles.y);
                    v.exposure = 0f;
                    ApplyEtch(v, 0f);
                }
            }

            // Heavy stones dropped in are eaten away and come back where they began.
            var carryables = Carryable.All;
            for (int i = 0; i < carryables.Count; i++)
            {
                var s = carryables[i];
                if (!_homes.ContainsKey(s)) { _homes[s] = s.transform.position; _scales[s] = s.transform.localScale; }
                Vector3 sp = s.transform.position;
                bool inside = !s.IsHeld && new Vector2(sp.x - c.x, sp.z - c.z).magnitude < radius && sp.y < surfaceHeight + 1f;
                if (!inside) { _stoneTime.Remove(s); continue; }

                _stoneTime.TryGetValue(s, out float t);
                t += Time.deltaTime;
                _stoneTime[s] = t;
                float k = Mathf.Clamp01(1f - t / 2f);
                s.transform.localScale = _scales[s] * Mathf.Max(0.05f, k);
                if (t >= 2f)
                {
                    s.transform.position = _homes[s];
                    s.transform.localScale = _scales[s];
                    _stoneTime.Remove(s);
                }
            }

            AnimateBubbles(c);
        }

        static Victim Capture(PlayerController p)
        {
            var v = new Victim { renderers = p.GetComponentsInChildren<Renderer>() };
            v.materials = new Material[v.renderers.Length];
            v.originals = new Color[v.renderers.Length];
            for (int i = 0; i < v.renderers.Length; i++)
            {
                v.materials[i] = v.renderers[i].material; // an own copy, so nothing else changes colour
                v.originals[i] = ColorOf(v.materials[i]);
            }
            return v;
        }

        static string ColorProperty(Material m) => m.HasProperty("_BaseColor") ? "_BaseColor" : m.HasProperty("_Color") ? "_Color" : null;

        static Color ColorOf(Material m)
        {
            string prop = ColorProperty(m);
            return prop != null ? m.GetColor(prop) : Color.white;
        }

        static void ApplyEtch(Victim v, float amount)
        {
            for (int i = 0; i < v.materials.Length; i++)
            {
                if (v.materials[i] == null) continue;
                string prop = ColorProperty(v.materials[i]);
                if (prop == null) continue;
                float flicker = amount > 0f ? 0.8f + 0.2f * Mathf.Sin(Time.time * 30f + i * 1.7f) : 1f; // sizzling, pitted
                v.materials[i].SetColor(prop, Color.Lerp(v.originals[i], Etched * flicker, amount));
            }
        }

        void AnimateBubbles(Vector3 c)
        {
            if (_bubbles == null) return;

            // Boiling bubbles on the surface; more of them gather around anyone who is in the acid.
            PlayerController target = null;
            foreach (var kv in _victims)
                if (kv.Value.exposure > 0.01f) { target = kv.Key; break; }

            for (int i = 0; i < _bubbles.Length; i++)
            {
                _bubblePhase[i] += Time.deltaTime * (0.6f + (i % 4) * 0.2f);
                if (_bubblePhase[i] > 1f) _bubblePhase[i] -= 1f;
                float t = _bubblePhase[i];

                float angle = i * 2.4f + Mathf.Floor(Time.time * 0.3f + i) * 1.7f;
                float dist = radius * 0.8f * Mathf.Abs(Mathf.Sin(i * 1.9f + Mathf.Floor(Time.time * 0.3f + i)));
                Vector3 centre = c;
                if (target != null && i % 2 == 0) { centre = target.transform.position; centre.y = surfaceHeight; dist = 0.7f; }

                float size = Mathf.Sin(t * Mathf.PI) * (target != null && i % 2 == 0 ? 0.45f : 0.35f);
                _bubbles[i].position = new Vector3(centre.x + Mathf.Cos(angle) * dist, surfaceHeight + 0.05f + t * 0.35f, centre.z + Mathf.Sin(angle) * dist);
                _bubbles[i].localScale = Vector3.one * size;
            }
        }

        void OnDestroy()
        {
            foreach (var v in _victims.Values)
                foreach (var m in v.materials) if (m != null) Destroy(m);
        }
    }
}
