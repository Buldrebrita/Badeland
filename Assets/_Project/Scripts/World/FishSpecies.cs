using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Data for one kind of fish. New species are new assets, not new code.
    /// Create via Assets > Create > Badeland > Fish Species.
    /// </summary>
    [CreateAssetMenu(menuName = "Badeland/Fish Species", fileName = "Fish_New")]
    public class FishSpecies : ScriptableObject
    {
        public string displayName = "Cod";
        public Color color = Color.white;
        public Sprite icon;

        [Tooltip("Marks the fish as a bad one for UI and for how long it lasts when thrown at someone.")]
        public bool isDebuff;

        [Header("Effect while held")]
        public MovementModifier effect = MovementModifier.None;

        [Header("Slippery")]
        [Tooltip("Seconds until the fish wriggles free of the holder's hands.")]
        [Min(1f)] public float holdSeconds = 20f;
        [Tooltip("Seconds a debuff fish stays on a player it was thrown at. Buffs keep their normal time.")]
        [Min(1f)] public float thrownDebuffSeconds = 6f;
        [Tooltip("Fraction of remaining time at which the fish starts thrashing as a warning.")]
        [Range(0f, 1f)] public float warnFraction = 0.25f;
    }
}
