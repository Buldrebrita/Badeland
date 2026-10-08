using UnityEngine;

namespace Badeland.World
{
    /// <summary>A spectator who hops and waves their arms. Arms are two child pivots that are raised and waved.</summary>
    public class Cheerer : MonoBehaviour
    {
        public Transform leftArm, rightArm;
        public float hopHeight = 0.22f;
        public float speed = 4f;

        Vector3 _home;
        float _phase, _speedScale;

        void Start()
        {
            _home = transform.position;
            _phase = Random.value * 10f;
            _speedScale = 0.8f + Random.value * 0.5f;
        }

        void Update()
        {
            float t = Time.time * speed * _speedScale + _phase;
            transform.position = _home + Vector3.up * (Mathf.Abs(Mathf.Sin(t)) * hopHeight);
            float wave = 140f + 25f * Mathf.Sin(t * 1.7f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, -wave);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, wave);
        }
    }
}
