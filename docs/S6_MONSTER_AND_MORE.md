# S6: The monster, the secret room, the slide and more

Rebuild the course with **Badeland > Create S3 Course Test Scene** (single player) or **Badeland > Create S4 Network Test Scene** (online). Both now include everything below.

## Testing shortcut

Press **M** to summon the monster straight away (on the host when online). Without it you must finish 3 laps first. To find the trap, press **E** next to the yellow duck. These are the only ways to test quickly.

## The ending: the start/finish platform and the monster

The start and finish are now one big platform south of the bottom straight, with a wide flag gate and a black-and-white checkered finish line across its whole width. The laps start and end here, and so does the monster fight, so nobody has to walk anywhere to meet it.

1. Finish all 3 laps. A big **FINISHED!** banner appears for you the moment you finish (online it also shows how many players are done, "Waiting for the others... 1 / 2 finished").
2. When everyone has finished and stood on the big platform for about 5 seconds, the monster **strikes with no warning**: the light drops, the sea darkens, there is a low rumble and the camera shakes. A giant monster **heaves up out of the sea** beside the platform, with its eyes following the players.
3. For up to **20 seconds** you can run, jump and swim around. Long **tentacles reach in from the water** and loom over a glowing circle on the ground, then slap down:
   - **Slaps** (orange circle): knock you flying into the sea. Funny, not deadly.
   - **Grabs** (red circle, thicker tentacle): the circle follows one player, then locks. If you are inside when it slaps down, you are grabbed and dragged under (eaten). **Run out of the circle** while it is locked and you live on. One player is targeted at a time, in turn, and the grabs get faster and wider.
   - **The final lunge** (a huge red circle from 16.5 s): the monster's head lunges over the platform and its mouth opens. Nobody escapes it.
4. Eaten players disappear and watch their friends (the camera follows whoever is left). When everyone is gone, the screen fades to black and "To be continued..." appears.

Everything follows the shared clock, so online all players see the same attacks at the same moment. Each machine decides only whether its own player is hit.

Tuning is in the `Monster Encounter` object (time on platform, fallback, duration, how far the head rises and lunges). The attack timings are in `MonsterEncounter.BuildPlan` (slap times, grab times, radii and warning times). The monster's parts are in the `Monster Head` object, and the places the tentacles come out of the water are the `Tentacle Base` objects.

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

- **Water:** the sea is a big, solid, bright blue with a new shader (`Assets/_Project/Shaders/BadelandWater.shader`) that draws animated white ripple lines. The course sits on it, and diving hides you under it. If the shader fails to compile, the builder uses a plain blue instead and says so in the Console. The secret room's flood uses a see-through version.
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
