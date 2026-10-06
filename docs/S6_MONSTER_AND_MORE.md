# S6: The monster, the secret room, the slide and more

Rebuild the course with **Badeland > Create S3 Course Test Scene** (single player) or **Badeland > Create S4 Network Test Scene** (online). Both now include everything below.

## Testing shortcut

Press **M** to summon the monster straight away (on the host when online). Without it you must finish 3 laps first. To find the trap, press **E** next to the yellow duck. These are the only ways to test quickly.

## The ending: bridge, platform, monster

1. Finish all 3 laps (online: every player). The text at the bottom says "All laps done! Cross the new bridge to the big platform." and a **pink bridge inflates** from the bottom deck to the big platform in the middle of the sea.
2. When everyone has been on the platform for about 3 seconds (or 40 seconds after the laps if someone never gets there), the monster **strikes with no warning**: the light drops, the sea darkens, there is a low rumble and the camera shakes.
3. For up to **20 seconds** you can run, jump and swim around. The monster attacks:
   - **Tentacle slams** (orange circle, then a tentacle crashes down): knocks you flying into the sea. Funny, not deadly.
   - **Swallows** (red circle that follows one player, then locks, then the mouth rises): if you are inside when it closes, you are eaten. **Run out of the circle** while it is locked and you live on. One player is targeted at a time, in turn. They get faster and wider.
   - **The final swallow** (a huge red circle that starts at 16.5 s): nobody escapes it.
4. Eaten players disappear and watch their friends (the camera follows whoever is left). When everyone is gone, the screen fades to black and "To be continued..." appears.

Everything follows the shared clock, so online all players see the same attacks at the same moment. Each machine decides only whether its own player is hit.

Tuning is in the `Monster Encounter` object (time on platform, fallback, duration). The attack timings are in `MonsterEncounter.BuildPlan` (slam times, swallow times, radii and warning times).

## Hidden trap and treasure

On the top straight, west side, a suspicious **yellow duck** sits on a trap door. Press **E** next to it:

1. The floor drops away and anyone on it falls into the **secret room** (a closed room far off to the side). Friends can set it off for each other.
2. Take the **gold treasure** (it counts at once: "Treasure: 1" at the top left).
3. Taking it starts the escape: the room **floods** over about 14 seconds. Reach the **cyan exit pad** and you are bounced back out, or if the water fills the room you are flushed out anyway.

The trap door closes again after a few seconds. The room is optional and nothing is lost if you fail.

## Water slide

At the top-west corner, stairs lead up to a pink tower. Step into the slide's mouth on top of the tower:

- You are carried down a curving slide above the stepping discs. You **speed up** on the way down and can **steer left and right** inside the lane.
- At the end you are thrown into the sea near the bottom deck. Swim over and hop out (Space at the surface).

## More obstacles

- **Ferry:** the bottom straight has a gap crossed on a moving orange platform. Hop on, it carries you across.
- **Windmill:** a spinning red cross on the right straight, between two bounce pads. Time it, or jump the low arm.
- **Hoops, drones and balls:** big hoops to run through, drones hovering over the park, beach balls bobbing in the sea.

## The look

- **Water:** the sea uses a new shader (`Assets/_Project/Shaders/BadelandWater.shader`) with animated white ripple lines that deepen to blue at low angles. If the shader fails to compile, the builder uses a plain blue instead and says so in the Console.
- **Lighting:** warm sun with soft shadows, bright ambient light, and post-processing (a little bloom, richer colours, a soft vignette) in `Assets/_Project/Settings/PostProcessing.asset`.
- Everything is still primitive shapes and flat colours. Rounded inflatable models, real textures and animation are art work for later. This step only makes the structure and the mood.

## Controls (all of them)

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Jump (hold for higher) | Space | A |
| Dive (in water) | Left Ctrl or C | Left shoulder / trigger |
| Throw fish at a player | F | X |
| Drop fish | Q | Y |
| Interact (the trap) | E | B |
| Summon the monster (testing) | M | |

## Not done yet

Real audio and music (only a placeholder rumble), the monster's final animation, a restart, a proper lobby, art for everything, and the inside of the monster (Chapter 2).
