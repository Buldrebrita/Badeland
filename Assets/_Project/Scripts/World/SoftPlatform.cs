using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// An inflatable mat that gives under your feet: it sinks when someone stands on it, springs back and bobs, and
    /// bounces a little when someone lands. Put it on an empty object whose children (with colliders) are the mat.
    /// Everyone standing on it is carried along while it moves.
    /// </summary>
    public class SoftPlatform : MonoBehaviour
    {
        [Tooltip("How far it sinks for each person standing on it (two at most count).")]
        public float dip = 0.22f;
        public float stiffness = 38f;
        public float damping = 3.2f;
        [Tooltip("A little downward kick when someone steps on, so it bounces.")]
        public float landKick = 2.2f;
        [Tooltip("How far it drifts up and down all by itself, like something floating on water.")]
        public float idleBob = 0.04f;

        Collider[] _colliders;
        Vector3 _rest;
        float _offset, _velocity, _phase;
        int _lastStanding;

        void Start()
        {
            _colliders = GetComponentsInChildren<Collider>();
            _rest = transform.position;
            _phase = (_rest.x * 0.37f + _rest.z * 0.53f);
        }

        void Update()
        {
            if (_colliders == null || _colliders.Length == 0) return;

            Bounds b = _colliders[0].bounds;
            for (int i = 1; i < _colliders.Length; i++) if (_colliders[i] != null) b.Encapsulate(_colliders[i].bounds);

            int standing = 0;
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.IsEaten || p.IsDead) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x - 0.1f || pos.x > b.max.x + 0.1f || pos.z < b.min.z - 0.1f || pos.z > b.max.z + 0.1f) continue;
                if (Mathf.Abs(p.FeetY() - b.max.y) > 0.5f) continue;
                standing++;
            }

            if (standing > _lastStanding) _velocity -= landKick;
            _lastStanding = standing;

            float target = -dip * Mathf.Min(standing, 2) + idleBob * Mathf.Sin(Time.time * 1.3f + _phase);
            float dt = Time.deltaTime;
            _velocity += (stiffness * (target - _offset) - damping * _velocity) * dt;
            _offset += _velocity * dt;

            Vector3 newPosition = _rest + Vector3.up * _offset;
            Vector3 delta = newPosition - transform.position;

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsDead) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x - 0.1f || pos.x > b.max.x + 0.1f || pos.z < b.min.z - 0.1f || pos.z > b.max.z + 0.1f) continue;
                if (Mathf.Abs(p.FeetY() - b.max.y) > 0.5f) continue;
                p.Carry(delta);
            }

            transform.position = newPosition;
            Physics.SyncTransforms();
        }
    }
}
