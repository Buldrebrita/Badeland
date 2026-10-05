# Badeland: Vertical Slice

**Status:** draft v0.1. The first playable. Placeholders and gray-box first, final-quality art only after the mechanics prove fun.

## Goal

A 15 to 20 minute section that takes a player (solo or in a group of up to four) from the start of a small waterpark area, through the core mechanics, to the floating platform, the first monster attack and the swallow. It must show the whole game's promise in miniature, including the shift from funny to scary.

**Success criteria**

- Testers laugh at least a few times without being prompted.
- Controls feel good in the first minute, with keyboard/mouse and with a gamepad.
- The camera never hides the player, a hazard or the next objective.
- Nobody gets stuck for more than a couple of minutes without a hint.
- After the cut to black, testers say they want to see what is next.
- Runs at 60 fps at 1080p on the target mid-range GPU (to be confirmed).

## Contents (the 14 original items plus 15 and 16)

| # | Item | Design | Gray-box version first |
|---|---|---|---|
| 1 | Small waterpark environment | One compact area: start plaza, pool, course, slide, finish pool | Blocked-out shapes with simple colours |
| 2 | Playable character | One cute, readable, expressive character | Capsule with face and wobble |
| 3 | Basic movement | Run, acceleration, turning, footsteps | Tuned variables, no animation polish |
| 4 | Isometric camera | Three-quarter view, smooth follow, occlusion fade, zone overrides | Cinemachine with fixed pitch |
| 5 | Swimming | Surface swim, short dive, enter/exit transitions, buoyancy | Water plane with buoyancy volume |
| 6 | Jumping | Variable height, coyote time, jump buffering, forgiving ledges | Same |
| 7 | One water slide | Spline-based, steering within the lane, speed boost, splash finish | One spline with a ride mover |
| 8 | One obstacle course | Inflatable bounce pads, rotating bars, moving platforms, balance beam | Boxes and cylinders |
| 9 | One hidden trap | Suspicious inflatable object, floor opens into a secret room | Trigger volume plus animated floor |
| 10 | One treasure | In the secret room, triggers an escape mechanism when taken | Placeholder pickup, counter UI |
| 11 | One simple puzzle | Hold-a-button gate: a second player holds it, solo uses a heavy object | Pressure plate and door |
| 12 | One finish area | Celebration pool with stars shown, results screen | Simple UI |
| 13 | The floating platform | Large inflatable platform on dark-able water, the arena | Platform on a water plane |
| 15 | Lap loop | Obstacle course run 3 times (tunable) with lap counter and subtle per-lap differences ("wait, was that there before?") driven by a per-lap data asset | Counter and a loop trigger |
| 16 | Catchable fish | 3 species (cod buff, salmon buff, clownfish debuff) jumping over the obstacles, caught and held (one at a time) for an effect, slips away after a while, can be thrown at another player | Capsule fish on spline arcs, effect via data asset |
| 14 | Basic monster encounter | Tentacle attack patterns, throwable objects, scripted swallow, cut to black | Capsule tentacles, scripted camera |

## Flow

1. **Start plaza.** Cheerful. Short tutorial: move, jump, interact, throw.
2. **Obstacle course, 3 laps.** Fish jump over the obstacles, players catch them for buffs and debuffs (cod: faster, salmon: higher jump, clownfish: cannot walk straight). Bounce pads, rotating bars, moving platforms. A suspicious inflatable object sits slightly off the path.
3. **Hidden trap and treasure.** Investigate the object, the floor opens, fall into a secret room. Take the treasure, the room starts to close or flood, escape.
4. **Puzzle.** A gate that needs a button held. Solo: push a heavy object onto the plate. Co-op: a teammate stands on it.
5. **Slide.** A long, fun, loud slide with a splash finish into the final pool.
6. **Finish area.** Rank stars for the run, collectibles counted. Everything feels great.
7. **Floating platform.** The party swims or hops to the big inflatable. A short celebration, then the music thins out.
8. **Monster encounter** (below).
9. **Cut to black.**

## Monster encounter spec (slice version)

- **Phase 1 (sudden):** the game is cheerful right up to this moment, with no earlier warning. Music cuts, lights drop, water darkens, a low rumble, within a few seconds.
- **Phase 2 (tentacle attacks):** 2 to 3 telegraphed patterns, for example a slam on a marked circle, a sweeping arm to jump over, a grab attempt on a player that teammates can break. Each telegraph is visible a beat before the hit.
- **Phase 3 (counterattack):** players pick up and throw objects (balls, floats, cannon-launched things) at weak points. Hits hurt the monster enough to feel like progress.
- **Phase 4 (escalation):** the monster rises, the platform tilts or deflates, patterns speed up.
- **Phase 5 (swallow):** scripted. All players are scooped up. Camera pulls back, the mouth closes, cut to black. No fail state before this point beyond a quick respawn on the platform.
- **After the cut:** a title card or short "to be continued" line is enough for the slice. The interior is a stub.

Tone check: scary-for-a-moment, not horror. Hits knock players into the water with a comedic splash, they climb back on.

## Controls to tune (expose as variables from day one)

Max speed, acceleration, deceleration, turn rate, jump height (min and max), gravity scale, coyote time, jump buffer window, air control, swim speed, dive depth, buoyancy strength, slide speed and lane steering, camera pitch, distance and follow damping.

## Out of scope for the slice

Final cosmetics, the full save system, accessibility options beyond basic rebinding, localisation, Steam achievements, anything inside the monster, more than one area, final audio mix.

## Build order (small, testable stages)

Each stage ends with something you can play and judge.

| Stage | Build | Test |
|---|---|---|
| S0 | Create the Unity project, check the repo workflow | Opens and pushes cleanly |
| S1 | Character controller and isometric camera in a gray-box scene | Does moving and jumping feel good? |
| S2 | Water, swimming, enter/exit, buoyancy | Is swimming fun, not fiddly? |
| S3 | Slide and obstacle course, lap loop | Is the course fun to replay three times? |
| S3b | Catchable fish and their effects | Is chasing fish fun, and are buff/debuff clear? |
| S4 | Hidden trap, treasure, escape, simple puzzle | Do testers find the trap and laugh? |
| S5 | **Networking spike:** two players online through all of the above | Can two friends play together? |
| S6 | Finish area, floating platform, monster v0 with patterns | Is the fight readable and tense? |
| S7 | Swallow cinematic, lighting and audio shift, cut to black | Does the shift land? |
| S8 | Art and audio pass on the slice, performance pass, playtest | Do testers want the next part? |

**Next step:** S0 and S1. The prototype scripts (player controller, isometric camera) can be written as soon as the Unity project exists in the repo.
