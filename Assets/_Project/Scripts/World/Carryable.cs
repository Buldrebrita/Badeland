using System.Collections.Generic;
using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A heavy object you can pick up with Interact and put down again. It slows you down while you carry it. It can
    /// hold a <see cref="PressurePlate"/> down, which is how a solo player does a puzzle that two players would do by
    /// standing on the plate. (Not yet shared over the network: it works on the machine of the player who carries it.)
    /// </summary>
    public class Carryable : MonoBehaviour
    {
        static readonly List<Carryable> AllCarryables = new List<Carryable>();
        public static IReadOnlyList<Carryable> All => AllCarryables;

        [Min(0.5f)] public float pickupRadius = 2.4f;
        [Range(0.3f, 1f)] public float carrySpeed = 0.72f;
        [Range(0.3f, 1f)] public float carryJump = 0.8f;
        [Min(0.1f)] public float radius = 0.7f;

        public bool IsHeld => _holder != null;

        PlayerController _holder;
        Collider _collider;
        MovementModifiers _modifiers;

        void Awake()
        {
            AllCarryables.Add(this);
            _collider = GetComponent<Collider>();
        }

        void OnDestroy()
        {
            AllCarryables.Remove(this);
            Release();
        }

        void Update()
        {
            if (_holder != null)
            {
                transform.position = _holder.transform.position + Vector3.up * 1.7f;
                if (_holder.IsDead || _holder.InteractPressed) Drop();
                return;
            }

            var players = PlayerController.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsLocal || p.IsEaten) continue;
                if ((p.transform.position - transform.position).sqrMagnitude > pickupRadius * pickupRadius) continue;

                HudHints.Show("Press E (gamepad: B) to pick up");
                if (p.InteractPressed && !HoldingSomething(p))
                {
                    PickUp(p);
                    return;
                }
            }
        }

        static bool HoldingSomething(PlayerController p)
        {
            foreach (var c in AllCarryables)
                if (c._holder == p) return true;
            return false;
        }

        void PickUp(PlayerController p)
        {
            _holder = p;
            if (_collider != null) _collider.enabled = false; // it must not push its carrier around
            _modifiers = p.GetComponent<MovementModifiers>();
            if (_modifiers != null)
                _modifiers.Set(this, new MovementModifier { speedMultiplier = carrySpeed, jumpMultiplier = carryJump });
        }

        void Drop()
        {
            var holder = _holder;
            Release();

            // Put it down just in front of the player, on the ground.
            Vector3 spot = holder.transform.position + holder.transform.forward * 1.4f;
            spot.y = holder.FeetY() + radius;
            if (Physics.Raycast(spot + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                spot.y = hit.point.y + radius + 0.1f; // rest on the ground below, even on uneven floor
            transform.position = spot;
        }

        void Release()
        {
            if (_modifiers != null) _modifiers.Remove(this);
            _modifiers = null;
            _holder = null;
            if (_collider != null) _collider.enabled = true;
        }
    }
}
