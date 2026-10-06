# S6: The monster, the secret room, the slide and more

Rebuild the course with **Badeland > Create S3 Course Test Scene** (single player) or **Badeland > Create S4 Network Test Scene** (online). Both now include everything below.

## Testing shortcuts

- **M**: runs the whole ending now (the danger alarm, then the monster). Use it on the big platform.
- **N**: the monster at once, with no alarm.
- **E** next to the yellow duck: the trap.

Without these you must finish 3 laps first.

## The ending: alarm, then the monster

The start and finish are one big platform south of the bottom straight, with a wide flag gate and a black-and-white checkered finish line. The laps start and end here, and so does the fight, so nobody has to walk anywhere.

1. **Finish all 3 laps.** A big **FINISHED!** banner appears for you the moment you finish (online it also shows "Waiting for the others... 1 / 2 finished").
2. **The danger alarm (about 7 seconds)**, once everyone has stood on the platform for about 3 seconds:
   - The sea gets **rougher**: the ripples speed up and the whole sea heaves up and down.
   - **Red alarm lights** flash on poles around the platform, the sun turns red, and a **siren** sounds.
   - A **voice** says "Danger! Do not go in the water!" (twice), and the same text flashes on screen.
   - **Railings rise** around the platform, so nobody can jump into the water.
3. **The strike.** The light drops and a giant sea monster heaves up out of the water beside the platform, on a long thick neck, with its eyes following the players.
4. **For up to 20 seconds you can run and jump around.** The monster:
   - **Eats:** it rears back, opens its jaws and its eyes **lock on one player**. Then its head **suddenly leaps forward** and snaps. Anyone in its jaws is eaten. There is no circle on the ground. The warning is the rearing and the stare: watch the eyes, and when it pulls back, move. The leap aims where you stood a moment before.
   - **Slaps:** in between, long tentacles reach in from the sea and loom over an orange circle, then slap down, knocking you flying across the platform (the railings keep you out of the water).
   - **The last lunge** at exactly 20 seconds takes everyone left. It always happens.
5. Eaten players disappear and watch their friends. When everyone is gone, the screen fades to black and "To be continued..." appears.

Everything follows the shared clock, so online all players see the same thing at the same moment. Each machine decides only whether its own player is hit.

**The voice.** If you give the `Monster Encounter` object a recorded **Voice Clip**, it plays that. If not, on Windows it uses the computer's own voice as a placeholder. On other systems it stays silent, and the on-screen text still shows. A real voice recording replaces it later.

**Tuning** is in the `Monster Encounter` object: time on the platform, the length of the alarm, the fight length, how far the jaws reach. The timing of the lunges and slaps is in `MonsterEncounter.BuildPlan`.

## Hidden trap and treasure

On the top straight, west side, a suspicious **yellow duck** sits on a trap door. Press **E** next to it:

1. The floor drops away and anyone on it falls into the **secret room** (a closed room far off to the side). Friends can set it off for each other.
2. Take the **gold treasure** (it counts at once: "Treasure: 1" at the top left).
3. Taking it starts the escape: the room **floods** over about 14 seconds. Reach the **cyan exit pad** and you are bounced back out, or if the water fills the room you are flushed out anyway.

The trap door closes again after a few seconds. The room is optional and nothing is lost if you fail.

## Water slide

At the top-west corner, a long gentle **ramp** (just run up it) leads to a pink tower. Step into the slide's mouth on top of the tower:

- You are carried down a curving slide above the stepping discs. You **speed up** on the way down and can **steer left and right** inside the lane.
- At the end you are thrown into the sea near the bottom deck. Swim over and hop out (Space at the surface).

## More obstacles

- **Ferry:** the bottom straight has a gap crossed on a moving orange platform. Hop on, it carries you across.
- **Windmill:** a spinning red cross on the right straight, between two bounce pads. Time it, or jump the low arm.
- **Beach balls:** big soft balls to dodge on the decks, and some bobbing in the sea.

## The look

- **Water:** the sea is a big, solid, bright blue with a generated white-ripple texture (`Assets/_Project/Art/Textures/SeaRipples.png`) that drifts across it. It uses the render pipeline's own default material, so it always draws. Every deck has a rim of white foam where it meets the sea, so it looks like it floats in the water. The secret room's flood is a see-through blue.
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
| Run the ending now (testing) | M (alarm, then the monster), N (monster at once) | |

## Not done yet

Real audio and music (only a placeholder rumble), the monster's final animation, a restart, a proper lobby, art for everything, and the inside of the monster (Chapter 2).
