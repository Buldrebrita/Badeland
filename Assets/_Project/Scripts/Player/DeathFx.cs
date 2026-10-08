using UnityEngine;

namespace Badeland.Player
{
    /// <summary>Plays the death animation on a player. Separate from PlayerController so it also runs for other players' avatars online.</summary>
    public class DeathFx : MonoBehaviour
    {
        PlayerController _player;

        void Awake() => _player = GetComponent<PlayerController>();

        void Update()
        {
            if (_player != null) _player.TickDeath(Time.deltaTime);
        }
    }
}
