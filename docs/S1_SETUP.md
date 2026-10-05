# S1 Setup: first playable gray-box

How to get the prototype scripts running once the Unity project exists (see README, "Create the Unity project").

## Packages

Install **Input System** from Package Manager. When Unity asks to enable the new input backend, accept (it restarts the editor). Set *Active Input Handling* to **Input System Package (New)** or **Both**.

## Scene

1. New scene in `Assets/_Project/Scenes/Levels/Graybox_S1`.
2. **Ground:** a large cube or plane. Add a few boxes at different heights to test jumping.
3. **Player:**
   - Empty or capsule GameObject named `Player` at (0, 1, 0).
   - Add `PlayerController`. Unity adds `CharacterController` and `PlayerInputReader` for you.
   - Add `MovementModifiers`.
   - Create a movement settings asset: *Assets > Create > Badeland > Movement Settings*, then drag it into `PlayerController`. Defaults are a good start.
4. **Camera:** put an empty `CameraRig` in the scene with `IsoCameraRig`, add the player to **Targets**, and parent the Main Camera to the rig (zero its local position and rotation). The rig positions itself, so just make sure `Camera.main` is that camera.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Jump (hold for higher) | Space | A / South |
| Throw fish at a player | F | X / West |
| Drop fish | Q | Y / North |
| Interact | E | B / East |

## What to judge (the S1 test)

Run, stop, turn, jump short and tall, jump just after leaving a ledge, press jump just before landing. Tune everything in the `MovementSettings` asset (live in play mode, then copy values). Does it feel good in the first minute?

## Fish (S3b, early look)

Create species assets via *Assets > Create > Badeland > Fish Species*:

| Fish | Settings |
|---|---|
| Cod | speedMultiplier 1.4 |
| Salmon | jumpMultiplier 1.6 |
| Clownfish | isDebuff on, wobbleDegrees 60, wobbleHz 1.2 |

Add `FishCarrier` to the player and call `TryCatch(species)` (from a test button or a fish trigger) to try the effects. Needs at least two players with `FishCarrier` to test throwing.

## Not done yet

Swimming (S2), the fish that jump in the world and catching them, lap tracking and per-lap changes, networking (S5), tests (no Unity editor was available when this was written, so none of this has been compiled yet. Expect small fixes on first open).
