# S2: Swimming

Build the test scene with **Badeland > Create S2 Swimming Test Scene**, then press Play. Walk north (up the screen) to the pool and jump in.

## Controls in water

| Action | Keyboard | Gamepad |
|---|---|---|
| Swim | WASD / arrows | Left stick |
| Dive (hold) | Left Ctrl or C | Left shoulder or left trigger |
| Hop out of the water | Space at the surface | A / South |

Release dive to float back up. Swimming starts when the water reaches about waist height and ends when it gets shallow again, so walking in from the steps wades first, then swims.

## What to judge (the S2 test)

Is swimming fun, not fiddly? Is the transition into the water smooth? Is diving and coming back up clear? Is getting out easy, from the steps and by hopping onto the edge?

All values are in the `MovementSettings` asset under "Swim" (swim speed, how deep before swimming, float height, bounce, dive depth, hop height). Existing assets pick up the new fields with their defaults.

## How it works

- `WaterVolume` goes on a cube. Its trigger collider is the water and the top face is the surface. It is a box check, not a fluid simulation.
- `PlayerController` switches between walking and swimming. While swimming there is no gravity. A spring pulls the body to a floating depth, or a deeper dive depth while dive is held.
- `PlayerController.EnteredWater` (with the impact speed) and `ExitedWater` events are where splash effects and sounds will plug in.

## Not done yet

Splash and ripple visuals and sounds, a water shader (the surface is a plain transparent blue box), currents, a swim animation, and a breath limit for diving. The character's pivot must be at the centre of its body for the water checks to work (true for the capsule in the test scene).
