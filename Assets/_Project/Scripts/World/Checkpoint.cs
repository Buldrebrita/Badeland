using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A flag gate across the course: two flags a set distance apart, centred on this object. It counts when a
    /// player's path crosses the line between the flags, in the gate's forward direction, at ANY height. So
    /// bouncing or jumping over it still counts, and going back through it does not. The object's forward
    /// (blue arrow) is the direction of travel. <see cref="LapTracker"/> checks players against it.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [Min(1f)] public float width = 8f;
        [Tooltip("Extra room outside the flags that still counts, so near misses feel fair.")]
        [Min(0f)] public float forgiveness = 0.5f;

        /// <summary>True if moving from <paramref name="from"/> to <paramref name="to"/> crossed the gate going forward.</summary>
        public bool Crossed(Vector3 from, Vector3 to)
        {
            Vector3 center = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float before = Vector3.Dot(from - center, forward);
            float after = Vector3.Dot(to - center, forward);
            if (before >= 0f || after < 0f) return false; // not a forward crossing

            float t = before / (before - after);
            Vector3 hit = Vector3.Lerp(from, to, t);
            return Mathf.Abs(Vector3.Dot(hit - center, right)) <= width * 0.5f + forgiveness;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector3 right = Vector3.Cross(Vector3.up, transform.forward);
            Gizmos.DrawLine(transform.position - right * width * 0.5f, transform.position + right * width * 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
