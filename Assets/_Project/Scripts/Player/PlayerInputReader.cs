using UnityEngine;
using UnityEngine.InputSystem;

namespace Badeland.Player
{
    /// <summary>
    /// Polls keyboard and gamepad once per frame, before other scripts run.
    /// Deliberately simple for the S1 prototype. Rebinding and one-device-per-player
    /// (for local players and the networking spike) will move to an InputActions asset later.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool DiveHeld { get; private set; }
        public bool ThrowPressed { get; private set; }
        public bool DropPressed { get; private set; }
        public bool InteractPressed { get; private set; }

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            Vector2 move = Vector2.zero;
            bool jumpDown = false, jumpHeld = false, diveHeld = false, throwDown = false, dropDown = false, interactDown = false;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                jumpDown |= kb.spaceKey.wasPressedThisFrame;
                jumpHeld |= kb.spaceKey.isPressed;
                diveHeld |= kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
                throwDown |= kb.fKey.wasPressedThisFrame;
                dropDown |= kb.qKey.wasPressedThisFrame;
                interactDown |= kb.eKey.wasPressedThisFrame;
            }

            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                jumpDown |= pad.buttonSouth.wasPressedThisFrame;
                jumpHeld |= pad.buttonSouth.isPressed;
                diveHeld |= pad.leftShoulder.isPressed || pad.leftTrigger.isPressed;
                throwDown |= pad.buttonWest.wasPressedThisFrame;
                dropDown |= pad.buttonNorth.wasPressedThisFrame;
                interactDown |= pad.buttonEast.wasPressedThisFrame;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            JumpPressed = jumpDown;
            JumpHeld = jumpHeld;
            DiveHeld = diveHeld;
            ThrowPressed = throwDown;
            DropPressed = dropDown;
            InteractPressed = interactDown;
        }
    }
}
