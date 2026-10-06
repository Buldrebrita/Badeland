using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Makes fish leap out of the water at random places and random times, and lets a player catch one by
    /// getting close while it is in the air. Time is cut into slots; each slot gets its own random plan
    /// (does a fish leap, when, from where, which way, how high, which species). The plan only depends on the
    /// seed and the slot number, so every player sees the same fish at the same place, which suits online co-op
    /// (once the clock is shared). A caught fish is gone for the rest of its leap.
    /// </summary>
    public class FishJumper : MonoBehaviour
    {
        [Tooltip("Species that can leap here. One is picked at random each time.")]
        public FishSpecies[] speciesPool;
        [Tooltip("Child object holding the fish's look. Switched off while the fish is under water.")]
        public GameObject visual;

        [Header("Where fish leap from")]
        public Vector3 zoneCenter;
        [Tooltip("Size of the area (x and z) where leaps start. Use 0 in a direction for a line.")]
        public Vector3 zoneSize = new Vector3(1f, 0f, 16f);
        [Tooltip("Direction of the leap in degrees. 0 = +Z, 90 = +X, 180 = -Z, 270 = -X.")]
        public float headingDegrees;
        [Range(0f, 90f)] public float headingSpread = 35f;

        [Header("The leap")]
        [Min(0.5f)] public float minDistance = 6f;
        [Min(0.5f)] public float maxDistance = 11f;
        [Min(0.5f)] public float minArcHeight = 2.5f;
        [Min(0.5f)] public float maxArcHeight = 4.5f;
        [Min(0.2f)] public float flightTime = 2f;

        [Header("How often")]
        [Tooltip("Time is cut into slots of this many seconds. At most one leap per slot, at a random moment in it.")]
        [Min(1f)] public float slotSeconds = 8f;
        [Tooltip("Chance that a slot has a leap at all.")]
        [Range(0f, 1f)] public float leapChance = 0.85f;
        [Tooltip("Different seeds give different fish patterns. Use a different one on each fish area.")]
        public int seed = 1;

        [Header("Catching")]
        [Min(0.1f)] public float catchRadius = 1.8f;

        struct Leap
        {
            public bool happens;
            public float offset;
            public Vector3 start;
            public Vector3 end;
            public float arc;
            public FishSpecies species;
        }

        int _slot = int.MinValue;
        int _caughtSlot = -1;
        int _tintedSlot = -1;
        Leap _leap;
        Renderer _renderer;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (visual != null) _renderer = visual.GetComponentInChildren<Renderer>(true);
        }

        void Update()
        {
            if (speciesPool == null || speciesPool.Length == 0) return;

            float now = Time.time;
            int slot = Mathf.FloorToInt(now / slotSeconds);
            if (slot != _slot)
            {
                _slot = slot;
                _leap = Plan(slot);
            }

            float u = (now - slot * slotSeconds - _leap.offset) / flightTime;
            bool flying = _leap.happens && _caughtSlot != slot && u >= 0f && u <= 1f;

            if (visual != null) visual.SetActive(flying);
            if (!flying) return;

            Vector3 pos = Vector3.Lerp(_leap.start, _leap.end, u) + Vector3.up * (4f * _leap.arc * u * (1f - u));
            transform.position = pos;

            Vector3 dir = _leap.end - _leap.start;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);

            if (_tintedSlot != slot && _renderer != null)
            {
                _tintedSlot = slot;
                _block.SetColor("_BaseColor", _leap.species.color);
                _block.SetColor("_Color", _leap.species.color);
                _renderer.SetPropertyBlock(_block);
            }

            var carriers = FishCarrier.Active;
            for (int i = 0; i < carriers.Count; i++)
            {
                var c = carriers[i];
                if (c.IsHolding) continue; // one fish at a time
                if ((c.transform.position - pos).sqrMagnitude > catchRadius * catchRadius) continue;

                if (c.TryCatch(_leap.species))
                {
                    _caughtSlot = slot;
                    if (visual != null) visual.SetActive(false);
                    return;
                }
            }
        }

        Leap Plan(int slot)
        {
            // Same seed + same slot always gives the same plan, on every machine.
            var rng = new System.Random(unchecked(seed * 73856093 ^ slot * 19349663));

            var leap = new Leap();
            leap.happens = rng.NextDouble() < leapChance;
            leap.offset = Between(rng, 0f, Mathf.Max(0f, slotSeconds - flightTime));

            leap.start = zoneCenter + new Vector3(
                Between(rng, -zoneSize.x * 0.5f, zoneSize.x * 0.5f), 0f,
                Between(rng, -zoneSize.z * 0.5f, zoneSize.z * 0.5f));

            float yaw = (headingDegrees + Between(rng, -headingSpread, headingSpread)) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            leap.end = leap.start + dir * Between(rng, minDistance, maxDistance);

            leap.arc = Between(rng, minArcHeight, maxArcHeight);
            leap.species = speciesPool[rng.Next(speciesPool.Length)];
            return leap;
        }

        static float Between(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(zoneCenter, new Vector3(Mathf.Max(zoneSize.x, 0.2f), 0.2f, Mathf.Max(zoneSize.z, 0.2f)));
            float yaw = headingDegrees * Mathf.Deg2Rad;
            Gizmos.DrawRay(zoneCenter, new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * maxDistance);
        }
    }
}
