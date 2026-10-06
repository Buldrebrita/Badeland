using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Safety railings around the big platform. They start hidden below the deck and rise at the danger warning,
    /// so nobody can jump into the water while the monster comes. The railings are solid once they are up.
    /// </summary>
    public class RailingRaiser : MonoBehaviour
    {
        public static RailingRaiser Instance { get; private set; }

        [Tooltip("The object holding all the railing pieces. It is moved up when raised.")]
        public Transform root;
        [Tooltip("The solid parts. Switched on only once the railings are up.")]
        public Collider[] colliders;
        public float raiseMeters = 4f;
        public float raiseSeconds = 1.4f;

        Vector3 _down;
        bool _raising;
        double _startTime;

        void Awake()
        {
            Instance = this;
            if (root != null) _down = root.position;
            foreach (var c in colliders)
                if (c != null) c.enabled = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Start raising at the given shared-clock time.</summary>
        public void Raise(double startTime)
        {
            _raising = true;
            _startTime = startTime;
        }

        void Update()
        {
            if (!_raising || root == null) return;

            float k = Mathf.Clamp01((float)((GameClock.Now - _startTime) / raiseSeconds));
            root.position = _down + Vector3.up * (raiseMeters * Mathf.SmoothStep(0f, 1f, k));

            if (k >= 0.7f)
                foreach (var c in colliders)
                    if (c != null) c.enabled = true;
        }
    }
}
