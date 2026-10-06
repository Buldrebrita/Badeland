using System;
using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// Lets a player hold exactly one fish. The fish applies its effect while held, wriggles harder
    /// as its time runs out, then slides out of the player's hands. The player can also drop it or
    /// throw it at another player, who then holds it with a fresh (for debuffs, shorter) timer.
    /// </summary>
    [RequireComponent(typeof(MovementModifiers))]
    public class FishCarrier : MonoBehaviour
    {
        static readonly List<FishCarrier> All = new List<FishCarrier>();
        /// <summary>Every active fish carrier (one per player).</summary>
        public static IReadOnlyList<FishCarrier> Active => All;

        [SerializeField] PlayerInputReader input;
        [Tooltip("Direction fish are thrown in. Defaults to this transform.")]
        [SerializeField] Transform aim;
        [SerializeField] float throwRange = 12f;
        [Tooltip("Max angle from the aim direction at which a player can be targeted.")]
        [Range(5f, 90f)] [SerializeField] float throwArc = 40f;

        MovementModifiers _modifiers;

        public FishSpecies Held { get; private set; }
        public float TimeLeft { get; private set; }
        public bool IsHolding => Held != null;
        /// <summary>1 = just caught, 0 = about to slip away. For UI.</summary>
        public float NormalizedTimeLeft => Held == null ? 0f : Mathf.Clamp01(TimeLeft / Held.holdSeconds);
        /// <summary>True once the fish starts thrashing as a warning. Drive animation and UI from this.</summary>
        public bool IsThrashing => Held != null && TimeLeft <= Held.holdSeconds * Held.warnFraction;

        public event Action<FishSpecies> Caught;
        /// <summary>Fired when the fish leaves for any reason (slipped, dropped, thrown).</summary>
        public event Action<FishSpecies, FishLossReason> Lost;

        void Awake()
        {
            _modifiers = GetComponent<MovementModifiers>();
            if (aim == null) aim = transform;
            if (input == null) input = GetComponent<PlayerInputReader>();
        }

        void OnEnable() => All.Add(this);

        void OnDisable()
        {
            All.Remove(this);
            if (Held != null) Release(FishLossReason.Dropped);
        }

        void Update()
        {
            if (Held == null)
            {
                return;
            }

            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f)
            {
                Release(FishLossReason.Slipped);
                return;
            }

            if (input == null) return;
            if (input.DropPressed) Release(FishLossReason.Dropped);
            else if (input.ThrowPressed) ThrowAtNearest();
        }

        /// <summary>Called by a fish in the world when this player touches or grabs it. One fish at a time.</summary>
        public bool TryCatch(FishSpecies species)
        {
            if (Held != null || species == null) return false;
            Receive(species, species.holdSeconds);
            return true;
        }

        void Receive(FishSpecies species, float seconds)
        {
            Held = species;
            TimeLeft = seconds;
            _modifiers.Set(this, species.effect);
            Caught?.Invoke(species);
        }

        void Release(FishLossReason reason)
        {
            var species = Held;
            Held = null;
            TimeLeft = 0f;
            _modifiers.Remove(this);
            Lost?.Invoke(species, reason);
        }

        void ThrowAtNearest()
        {
            FishCarrier target = FindThrowTarget();
            if (target == null)
            {
                // Nobody to hit: the fish flops away. A thrown projectile can replace this later.
                Release(FishLossReason.Dropped);
                return;
            }

            var species = Held;
            float seconds = species.isDebuff
                ? Mathf.Min(species.thrownDebuffSeconds, species.holdSeconds)
                : species.holdSeconds;

            Release(FishLossReason.Thrown);
            target.Receive(species, seconds);
        }

        FishCarrier FindThrowTarget()
        {
            Vector3 forward = Vector3.ProjectOnPlane(aim.forward, Vector3.up).normalized;
            FishCarrier best = null;
            float bestScore = float.MaxValue;

            foreach (var other in All)
            {
                // One fish at a time: only players with empty hands can be hit.
                if (other == this || other.IsHolding) continue;

                Vector3 to = other.transform.position - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > throwRange || dist < 0.01f) continue;
                if (Vector3.Angle(forward, to) > throwArc) continue;

                float score = dist * (1f + Vector3.Angle(forward, to) / throwArc);
                if (score < bestScore) { bestScore = score; best = other; }
            }

            return best;
        }
    }

    public enum FishLossReason { Slipped, Dropped, Thrown }
}
