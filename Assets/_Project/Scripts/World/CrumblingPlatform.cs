using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A plank that gives way: stand on it for a moment and it shakes, then drops into the water, and comes back up a
    /// few seconds later. Put it on an empty object whose children (with colliders) are the plank. Anyone standing on it
    /// as it falls is carried down with it, then falls into the sea.
    /// </summary>
    public class CrumblingPlatform : MonoBehaviour
    {
        public float standSeconds = 0.55f;
        public float fallSpeed = 9f;
        public float dropDistance = 7f;
        public float downSeconds = 3.2f;
        public float riseSeconds = 1.0f;

        enum State { Idle, Shaking, Falling, Down, Rising }

        Collider[] _colliders;
        Vector3 _rest;
        State _state;
        float _timer, _offset;

        void Start()
        {
            _colliders = GetComponentsInChildren<Collider>();
            _rest = transform.position;
        }

        bool SomeoneOn(out Bounds b)
        {
            b = _colliders[0].bounds;
            for (int i = 1; i < _colliders.Length; i++) if (_colliders[i] != null) b.Encapsulate(_colliders[i].bounds);
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.IsEaten || p.IsDead) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x - 0.1f || pos.x > b.max.x + 0.1f || pos.z < b.min.z - 0.1f || pos.z > b.max.z + 0.1f) continue;
                if (Mathf.Abs(p.FeetY() - b.max.y) > 0.5f) continue;
                return true;
            }
            return false;
        }

        void Update()
        {
            if (_colliders == null || _colliders.Length == 0) return;
            float dt = Time.deltaTime;
            float before = _offset;

            switch (_state)
            {
                case State.Idle:
                    if (SomeoneOn(out _)) { _state = State.Shaking; _timer = 0f; }
                    break;
                case State.Shaking:
                    _timer += dt;
                    _offset = 0.04f * Mathf.Sin(Time.time * 60f);
                    if (_timer >= standSeconds) { _state = State.Falling; _offset = 0f; }
                    break;
                case State.Falling:
                    _offset -= fallSpeed * dt;
                    if (_offset <= -dropDistance) { _offset = -dropDistance; _state = State.Down; _timer = 0f; }
                    break;
                case State.Down:
                    _timer += dt;
                    if (_timer >= downSeconds) { _state = State.Rising; _timer = 0f; }
                    break;
                case State.Rising:
                    _timer += dt;
                    _offset = Mathf.Lerp(-dropDistance, 0f, Mathf.SmoothStep(0f, 1f, _timer / riseSeconds));
                    if (_timer >= riseSeconds) { _offset = 0f; _state = State.Idle; }
                    break;
            }

            Vector3 delta = Vector3.up * (_offset - before);
            if (_state == State.Falling && SomeoneOn(out var bounds))
            {
                var players = PlayerController.All;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (!p.IsLocal || p.IsEaten || p.IsDead) continue;
                    Vector3 pos = p.transform.position;
                    if (pos.x < bounds.min.x - 0.1f || pos.x > bounds.max.x + 0.1f || pos.z < bounds.min.z - 0.1f || pos.z > bounds.max.z + 0.1f) continue;
                    if (Mathf.Abs(p.FeetY() - bounds.max.y) > 0.5f) continue;
                    p.Carry(delta);
                }
            }

            transform.position = _rest + Vector3.up * _offset;
            Physics.SyncTransforms();
        }
    }
}
