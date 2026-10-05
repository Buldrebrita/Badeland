using System.Collections.Generic;
using UnityEngine;

namespace Badeland.Player
{
    /// <summary>One source's effect on movement (a held fish, a power-up, a slippery floor...).</summary>
    [System.Serializable]
    public struct MovementModifier
    {
        [Tooltip("1 = unchanged. 1.4 = 40% faster.")]
        public float speedMultiplier;
        [Tooltip("1 = unchanged. Scales jump height.")]
        public float jumpMultiplier;
        [Tooltip("Max angle in degrees the walking direction swings left and right. 0 = walks straight.")]
        public float wobbleDegrees;
        [Tooltip("How many full left-right swings per second.")]
        public float wobbleHz;

        public static MovementModifier None => new MovementModifier { speedMultiplier = 1f, jumpMultiplier = 1f };
    }

    /// <summary>
    /// Stack of movement modifiers keyed by source. PlayerController reads the combined values.
    /// Fish, pads, zones and so on add on enter and remove on exit; this class never expires anything itself.
    /// </summary>
    public class MovementModifiers : MonoBehaviour
    {
        readonly Dictionary<object, MovementModifier> _active = new Dictionary<object, MovementModifier>();

        public float SpeedMultiplier { get; private set; } = 1f;
        public float JumpMultiplier { get; private set; } = 1f;
        public float WobbleDegrees { get; private set; }
        public float WobbleHz { get; private set; }

        public void Set(object source, MovementModifier modifier)
        {
            _active[source] = modifier;
            Recalculate();
        }

        public void Remove(object source)
        {
            if (_active.Remove(source)) Recalculate();
        }

        void Recalculate()
        {
            float speed = 1f, jump = 1f, wobble = 0f, hz = 0f;
            foreach (var m in _active.Values)
            {
                speed *= m.speedMultiplier <= 0f ? 1f : m.speedMultiplier;
                jump *= m.jumpMultiplier <= 0f ? 1f : m.jumpMultiplier;
                if (m.wobbleDegrees > wobble) { wobble = m.wobbleDegrees; hz = m.wobbleHz; }
            }
            SpeedMultiplier = speed;
            JumpMultiplier = jump;
            WobbleDegrees = wobble;
            WobbleHz = hz;
        }
    }
}
