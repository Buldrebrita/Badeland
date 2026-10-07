using UnityEngine;

namespace Badeland.World
{
    /// <summary>A body of water that rises and falls with the creature's breath. Put on the water object (with its WaterVolume).</summary>
    public class BreathWater : MonoBehaviour
    {
        [Tooltip("How far the water rises above and falls below its resting level, in metres.")]
        [Min(0f)] public float amplitude = 1.2f;

        Vector3 _home;

        void Awake() => _home = transform.position;

        void Update()
        {
            transform.position = _home + Vector3.up * ((BreathCycle.Level - 0.5f) * 2f * amplitude);
            Physics.SyncTransforms(); // the swimming code reads the water's position straight away
        }
    }
}
