using Badeland.Player;
using Badeland.Systems;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A big inflatable ball on a rope that swings across the lane. Put this on the pivot (the top of the rope); the
    /// ball is a child at the end. Touch it and you are batted away. Its swing depends only on the shared clock.
    /// </summary>
    public class Pendulum : MonoBehaviour
    {
        public Transform ball;
        public float ballRadius = 1.0f;
        public float maxAngle = 50f;
        public float period = 3.2f;
        public float phase = 0f;
        public float knockSpeed = 11f;
        public float knockUp = 6f;

        void Update()
        {
            float angle = maxAngle * Mathf.Sin((float)(GameClock.Now / period) * Mathf.PI * 2f + phase);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (ball == null) return;

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten || p.IsDead) continue;
                Vector3 centre = p.transform.position + Vector3.up * 1f;
                Vector3 away = centre - ball.position;
                if (away.magnitude > ballRadius + 0.6f) continue;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = transform.right;
                p.Knock(away.normalized * knockSpeed, knockUp);
            }
        }
    }
}
