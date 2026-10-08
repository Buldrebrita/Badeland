using UnityEngine;

namespace Badeland.World
{
    /// <summary>Swells and shrinks slightly with the creature's breath: the living walls and organs of the place.</summary>
    public class BreathScale : MonoBehaviour
    {
        [Tooltip("How much it grows and shrinks, as a fraction (0.05 = 5%).")]
        [Range(0f, 0.3f)] public float amount = 0.05f;

        Vector3 _home;

        void Awake() => _home = transform.localScale;

        void Update() => transform.localScale = _home * (1f + (BreathCycle.Level - 0.5f) * 2f * amount);
    }
}
