using Badeland.Player;
using UnityEngine;

namespace Badeland.World
{
    /// <summary>
    /// A floor plate that is pressed while a player stands on it, or a <see cref="Carryable"/> rests on it. Needs a
    /// BoxCollider (set as a trigger). A gate (<see cref="SlidingGate"/>) listens to it. This is the hold-a-button
    /// puzzle: a friend stands on the plate while you go through, or alone you leave a heavy stone on it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PressurePlate : MonoBehaviour
    {
        public Renderer visual;
        public Color offColor = new Color(0.35f, 0.3f, 0.4f);
        public Color onColor = new Color(0.4f, 1f, 0.6f);

        public bool IsPressed { get; private set; }

        BoxCollider _box;
        MaterialPropertyBlock _block;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
            _block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (_box == null) Awake();
            if (_block == null) _block = new MaterialPropertyBlock();
            Bounds b = _box.bounds;
            bool pressed = false;

            var players = PlayerController.All;
            for (int i = 0; i < players.Count && !pressed; i++)
            {
                var p = players[i];
                if (p.IsEaten) continue;
                Vector3 pos = p.transform.position;
                if (pos.x < b.min.x || pos.x > b.max.x || pos.z < b.min.z || pos.z > b.max.z) continue;
                if (p.FeetY() > b.max.y + 0.4f || p.FeetY() < b.min.y - 0.6f) continue;
                pressed = true;
            }

            var carryables = Carryable.All;
            for (int i = 0; i < carryables.Count && !pressed; i++)
            {
                var c = carryables[i];
                if (c.IsHeld) continue;
                Vector3 pos = c.transform.position;
                if (pos.x < b.min.x || pos.x > b.max.x || pos.z < b.min.z || pos.z > b.max.z) continue;
                if (pos.y > b.max.y + 1.6f || pos.y < b.min.y - 0.6f) continue;
                pressed = true;
            }

            IsPressed = pressed;

            if (visual != null)
            {
                Color c = pressed ? onColor : offColor;
                _block.SetColor("_BaseColor", c);
                _block.SetColor("_Color", c);
                _block.SetColor("_EmissionColor", pressed ? onColor * 0.6f : Color.black);
                visual.SetPropertyBlock(_block);
            }
        }
    }
}
