using UnityEngine;

namespace Badeland.World
{
    /// <summary>Spins an object around an axis. Decoration only (drone rotors, the treasure).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 180f;

        void Update() => transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
