using System;
using System.Collections.Generic;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>One subtle change to the park, tied to lap numbers.</summary>
    [Serializable]
    public class LapChange
    {
        public GameObject target;
        [Tooltip("Visible from this lap on. 1 = always there.")]
        public int showFromLap = 1;
        [Tooltip("Gone from this lap on. 0 = never disappears.")]
        public int hideFromLap;
        [Tooltip("Shifted by the offset from this lap on. 0 = never moves.")]
        public int moveFromLap;
        public Vector3 moveOffset;
        [Tooltip("Recoloured from this lap on. 0 = never.")]
        public int tintFromLap;
        public Color tintColor = Color.red;
    }

    /// <summary>
    /// Makes the park a little different each lap, so players wonder "wait, was that there before?"
    /// Driven by data: add entries in the inspector, no code needed. It only reads the lap number, so
    /// all players see the same thing. Changes should stay small, deniable and harmless (see DESIGN.md 4b).
    /// </summary>
    public class LapChanges : MonoBehaviour
    {
        public LapTracker tracker;
        public List<LapChange> entries = new List<LapChange>();

        Vector3[] _origins;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            _origins = new Vector3[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].target != null) _origins[i] = entries[i].target.transform.position;
        }

        void OnEnable()
        {
            if (tracker != null) tracker.WorldLapChanged += Apply;
        }

        void OnDisable()
        {
            if (tracker != null) tracker.WorldLapChanged -= Apply;
        }

        void Start() => Apply(tracker != null ? tracker.WorldLap : 1);

        public void Apply(int lap)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.target == null) continue;

                bool visible = lap >= e.showFromLap && (e.hideFromLap <= 0 || lap < e.hideFromLap);
                e.target.SetActive(visible);

                bool moved = e.moveFromLap > 0 && lap >= e.moveFromLap;
                e.target.transform.position = moved ? _origins[i] + e.moveOffset : _origins[i];

                if (e.tintFromLap > 0)
                {
                    var r = e.target.GetComponentInChildren<Renderer>(true);
                    if (r == null) continue;

                    if (lap >= e.tintFromLap)
                    {
                        _block.SetColor("_BaseColor", e.tintColor);
                        _block.SetColor("_Color", e.tintColor);
                        r.SetPropertyBlock(_block);
                    }
                    else
                    {
                        r.SetPropertyBlock(null);
                    }
                }
            }
        }
    }
}
