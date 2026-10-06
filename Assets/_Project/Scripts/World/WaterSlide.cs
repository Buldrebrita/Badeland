using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A water slide that follows a smooth curve through a list of points. Step into the mouth at the top and you
    /// are carried down: you speed up, can steer left and right inside the lane, and are thrown off the end into
    /// the water. The ride is local to each player (their own machine moves them), like the rest of the movement.
    /// </summary>
    public class WaterSlide : MonoBehaviour
    {
        [Tooltip("Points the slide passes through, from the top to the end. At least 4.")]
        public Transform[] points;
        [Min(0.5f)] public float laneHalfWidth = 1.6f;
        [Min(0.1f)] public float steerSpeed = 4.5f;
        [Min(0f)] public float startSpeed = 7f;
        [Min(1f)] public float maxSpeed = 22f;
        [Tooltip("Extra speed per second on top of what the slope gives.")]
        [Min(0f)] public float acceleration = 5f;
        [Tooltip("How high the player's centre rides above the track.")]
        public float riderHeight = 1f;
        [Tooltip("Step within this distance of the top of the slide to start the ride.")]
        [Min(0.5f)] public float entryRadius = 1.6f;
        [Tooltip("Top speed when thrown off the end.")]
        [Min(1f)] public float releaseSpeed = 12f;

        class Rider
        {
            public PlayerController player;
            public float distance;
            public float speed;
            public float offset;
        }

        readonly List<Rider> _riders = new List<Rider>();
        readonly List<Vector3> _samples = new List<Vector3>();
        readonly List<float> _cumulative = new List<float>();

        public float Length { get; private set; }

        void Awake() => Rebuild();

        /// <summary>Recalculate the curve from the points. Called automatically; also used by the scene builder.</summary>
        public void Rebuild()
        {
            _samples.Clear();
            _cumulative.Clear();
            Length = 0f;
            if (points == null || points.Length < 2) return;

            const int perSegment = 24;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 p0 = points[Mathf.Max(i - 1, 0)].position;
                Vector3 p1 = points[i].position;
                Vector3 p2 = points[i + 1].position;
                Vector3 p3 = points[Mathf.Min(i + 2, points.Length - 1)].position;

                for (int k = 0; k < perSegment; k++)
                    _samples.Add(CatmullRom(p0, p1, p2, p3, k / (float)perSegment));
            }
            _samples.Add(points[points.Length - 1].position);

            _cumulative.Add(0f);
            for (int i = 1; i < _samples.Count; i++)
            {
                Length += Vector3.Distance(_samples[i - 1], _samples[i]);
                _cumulative.Add(Length);
            }
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Position and direction of travel at a distance along the slide.</summary>
        public void SampleAt(float distance, out Vector3 position, out Vector3 tangent)
        {
            if (_samples.Count < 2)
            {
                position = transform.position;
                tangent = Vector3.forward;
                return;
            }

            distance = Mathf.Clamp(distance, 0f, Length);
            int i = 1;
            while (i < _cumulative.Count - 1 && _cumulative[i] < distance) i++;

            float segment = _cumulative[i] - _cumulative[i - 1];
            float t = segment > 0.0001f ? (distance - _cumulative[i - 1]) / segment : 0f;
            position = Vector3.Lerp(_samples[i - 1], _samples[i], t);
            tangent = (_samples[i] - _samples[i - 1]).normalized;
        }

        void Update()
        {
            if (_samples.Count < 2) return;
            float dt = Time.deltaTime;

            // Players stepping into the mouth start riding.
            Vector3 start = _samples[0];
            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsExternallyControlled) continue;

                Vector3 d = p.transform.position - start;
                Vector2 flat = new Vector2(d.x, d.z);
                if (flat.magnitude > entryRadius || Mathf.Abs(p.transform.position.y - (start.y + riderHeight)) > 1.8f) continue;

                p.BeginExternalControl();
                _riders.Add(new Rider { player = p, distance = 0f, speed = startSpeed, offset = 0f });
            }

            // Riders slide along the curve.
            for (int i = _riders.Count - 1; i >= 0; i--)
            {
                var r = _riders[i];
                if (r.player == null || r.player.IsEaten)
                {
                    _riders.RemoveAt(i);
                    continue;
                }

                SampleAt(r.distance, out Vector3 pos, out Vector3 tangent);

                // Downhill is faster, uphill is slower.
                r.speed = Mathf.Clamp(r.speed + (acceleration - tangent.y * 25f) * dt, startSpeed * 0.6f, maxSpeed);
                r.distance += r.speed * dt;

                Vector3 right = Vector3.Cross(Vector3.up, new Vector3(tangent.x, 0f, tangent.z)).normalized;
                float steer = Vector3.Dot(r.player.WorldMoveDirection(), right);
                r.offset = Mathf.Clamp(r.offset + steer * steerSpeed * dt, -laneHalfWidth, laneHalfWidth);

                if (r.distance >= Length)
                {
                    // Off the end: thrown into the water.
                    Vector3 flatTangent = new Vector3(tangent.x, 0f, tangent.z).normalized;
                    r.player.EndExternalControl(flatTangent * Mathf.Min(r.speed, releaseSpeed) + Vector3.up * 2f);
                    _riders.RemoveAt(i);
                    continue;
                }

                Vector3 flatDir = new Vector3(tangent.x, 0f, tangent.z);
                float yaw = flatDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(flatDir).eulerAngles.y : 0f;
                r.player.SetExternalPose(pos + right * r.offset + Vector3.up * riderHeight, yaw);
            }
        }

        void OnDrawGizmos()
        {
            if (points == null || points.Length < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < points.Length - 1; i++)
                if (points[i] != null && points[i + 1] != null) Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
}
