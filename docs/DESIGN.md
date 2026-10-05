# Badeland: Design Document

**Status:** draft v0.2. Working title: BADELAND. Source: the project brief. Sibling docs: [TECH.md](TECH.md), [ROADMAP.md](ROADMAP.md), [VERTICAL_SLICE.md](VERTICAL_SLICE.md).

## 1. Vision

A colourful, funny, 1 to 4 player co-op adventure that starts in a giant waterpark and turns, at the end of the first chapter, into something very different. Players fight their way across the park to a floating inflatable platform, the lights go out, an enormous sea monster attacks, and it swallows them. They wake up inside the creature, in a strange fantasy world with a mystery to uncover.

**Target reaction after the first session:** *"What the hell just happened? I need to play the next part."*

## 2. Priorities (in order)

1. Fun gameplay
2. Good controls
3. Readable camera
4. Multiplayer foundation
5. Level design
6. Replayability
7. Art quality
8. Audio
9. Story
10. Polish

Rule of thumb: do not make beautiful assets for a mechanic that has not been proven fun. Placeholders first.

## 3. Structure

The game is split into **chapters**. This keeps scope realistic and lets chapter 1 stand as a complete, shippable experience.

### Chapter 1: The Waterpark

| Area | Mood | Content |
|---|---|---|
| 1. Main Waterpark | Bright, loud, safe | Giant pools, slides, wave pool, kids' play areas, inflatable obstacles, water cannons, bridges, climbing structures |
| 2. The Forgotten Section | Older, quieter, slightly off | Abandoned part of the park, first hints that something is wrong |
| 3. Underground | Industrial, tense | Maintenance tunnels, pipes, machinery, hidden rooms |
| 4. The Final Pool | Calm, then very not calm | Floating inflatable platform, the monster encounter, the swallow |

### Chapter 2: Inside the Monster

A much larger fantasy adventure in a surreal ecosystem (see section 9). Treated as a separate, later production block. Chapter 1 ends on the cliffhanger.

## 4. Core mechanics

- **Movement:** run, variable-height jump, swim (surface and short dives), slide, climb, balance, carry and throw objects, interact.
- **Water:** buoyancy volumes, currents, splashes, wet-surface visuals. Not a fluid simulation.
- **Inflatables:** bouncy, wobbly, squashy surfaces that are a core source of slapstick.
- **Environmental puzzles:** small, readable, usually solvable in under a minute solo.
- **Failure:** comedic and quick. Fall in, get launched, respawn at a nearby checkpoint. No gore, no real death.

## 4b. Laps and catchable fish

### The loop: 2 to 3 laps of the obstacle course

The waterpark course is a **circuit** the party runs two or three times (lap count is a tunable, the slice uses 3) before the monster appears. It must look and feel like a normal, cheerful party game: a race with lap counter, stars and ranking, nothing hinting at the ending.

- Each lap is a chance to do better: faster time, more fish caught, secrets found (see section 7).
- Later laps can add small variations (an extra obstacle, a shortcut opens, fish get faster) so repeats do not feel identical.
- The final lap ends on the floating platform and the party celebration, which is where the monster strikes (section 8).

### Jumping fish

Fish of different species leap out of the water and arc over the obstacles along the course. Players can chase and **catch** them by running into them, jumping at them, or using a net-like grab (final verb to be tuned).

- A player holds **only one fish at a time**. Catching another fish while holding one is not possible until the first is gone (drop it, throw it, or let it escape).
- **Slippery:** the fish wriggles and wants to jump away. After a while (about 20 seconds, tunable per species) it slides out of the player's hands and leaps back into the water. A subtle wriggle and a visible timer cue (the fish thrashes harder) warn the player before it goes.
- **Throwing:** the player can choose to throw the held fish at another player. The target gets that fish's effect (brief if it is a debuff, see co-op note below) and the fish becomes theirs, with its own slip-away timer.
- The player can also drop the fish at any time to shed it.
- Fish are fun targets, not required for completion. Collecting species counts toward collectibles and stars.
- Debuffs are slapstick, never punishing: always short, always funny, and a player can shed them by dropping the fish.

| Fish | Effect | Kind |
|---|---|---|
| Cod | Run faster | Buff |
| Salmon | Jump higher | Buff |
| Pufferfish | Bounce off everything, inflates | Mixed (to design) |
| Eel | Brief electric dash, shocks nearby players | Mixed (to design) |
| Swordfish | Charge forward, knocks obstacles and players | Mixed (to design) |
| Clownfish | Cannot walk straight: steering wobbles side to side | Debuff |
| Flatfish | Heavy and slow, but cannot be knocked back | Debuff (to design) |

Only the cod, salmon and clownfish are decided. The rest are suggestions to try in the prototype. Aim for roughly 6 to 8 species, with at least a third being debuffs.

Design notes:

- Ability variables go in a data asset per species so they are tunable without code (ScriptableObject).
- Readability: every species has a distinct silhouette and colour, and the held fish is visible on the player plus a small icon. Buff and debuff must be clear at a glance.
- Co-op: throwing a bad fish at a teammate is a joke, not a grief, so a hit applies the debuff only briefly and can be opted out of like other player-affecting effects.
- Multiplayer: fish paths are deterministic or host-authoritative so all players see the same fish.
- Solo: works unchanged, fish are a personal power-up.

## 5. Hidden traps and treasure

Core loop: **EXPLORE -> FIND SECRET -> GET TREASURE -> ESCAPE**

Traps are not always obvious. They can be disguised as props, triggered by stepping or interacting, hidden behind decorations or on optional paths, and revealed only after exploring.

Example: a suspicious inflatable object. The player investigates, the floor opens, they fall into a secret room with a treasure. Taking the treasure triggers a mechanism, and now they have to escape.

Trap archetypes to build first (reuse and vary them rather than inventing dozens):

1. Trapdoor / floor drop into a secret room
2. Pressure plate that triggers something elsewhere
3. Decoy prop (looks like a pickup or a toy)
4. Interact-to-trigger (valve, lever, tap)
5. Timed escape (rising water, closing door, deflating floor)

Fairness rules: traps are survivable, always teach through a first harmless instance, never remove progress, and reset or reward quickly. Treasure rooms are optional, never required to finish the level.

## 6. Multiplayer

- Single-player and online co-op, 1 to 4 players.
- **Solo must be fun.** Every co-op mechanic needs a solo-friendly version (for example a weighted object instead of a second player holding a button).
- Co-op mechanics: hold-a-button-while-another-crosses, throwing objects or players to each other, rescuing teammates from traps, two-player mechanisms, push/pull together, boosting each other up climbs, splitting up on branching paths.
- Emergent chaos: players can accidentally trigger traps on friends. It should be funny, not griefing. Rescues are always quick, and players can opt out of player-affecting traps.
- Camera framing for several players is a design constraint, not an afterthought (see TECH.md).

## 7. Replayability

Every level has several reasons to return:

| Playthrough | Goal |
|---|---|
| 1st | Complete the level |
| 2nd | Find hidden treasures |
| 3rd | Find secret rooms |
| 4th | Fastest time |

Ranking: 1 star completed, 2 stars good performance, 3 stars perfect performance. Optional collectibles and secrets per level. Each level should leave the player thinking "I know I missed something."

## 8. The monster encounter (end of Chapter 1)

Beats:

**Sudden, not signposted.** The monster must feel like it comes out of nowhere. Through all the laps the game plays as a normal, fun, bright party game, with no creeping dread, odd lighting or ominous sound beforehand. The change happens very quickly (a few seconds, not a long build-up), which is what makes it land.

1. **Calm.** The party arrives on a large floating inflatable platform in the Final Pool. A moment of celebration.
2. **Wrongness (a few seconds only).** The music cuts or drops out, the lights go down, the water darkens, a low sound from below. No earlier foreshadowing.
3. **Attack.** Tentacles and strikes from beneath the water. Players dodge and fight back with objects collected earlier or found on the platform.
4. **Escalation.** The monster is far bigger than the players. The platform shrinks, tilts or deflates.
5. **Unwinnable.** The fight turns. The monster swallows everyone.
6. **Cut to black.**

Tone: scary for a moment to a child, still inside a colourful adventure. Cartoon-like but intimidating, not extreme horror.

Design notes: the fight is scripted in phases with generous checkpoints; the swallow is a scripted cinematic, not a failure state.

Monster design direction: gigantic aquatic creature, tentacles, huge eyes, giant mouth, bioluminescent details, strange organic textures.

## 9. Inside the monster (Chapter 2)

Not a stomach. A surreal fantasy ecosystem: giant caves, bioluminescent forests, rivers, mountains, floating islands, strange villages, ancient ruins, organic structures, underground oceans, strange creatures, lost human settlements. Mysterious, strange, beautiful, slightly unsettling.

**Questions the player should come to ask, gradually:**

- What is this creature, and how long has it existed?
- Why is there a whole world inside it, and who built these structures?
- Why does it eat people, and for how long has this been happening?
- Who else is trapped here, and has anyone escaped?
- Is the monster actually the villain?

**Reveal rules:** never explain everything up front. Use notes, symbols, environmental storytelling, NPC dialogue, lost equipment, ancient structures, paintings, hidden rooms, remains of earlier expeditions.

Answers to the questions above are an open story decision (see section 13).

## 10. Visual direction

- **High-poly stylised cartoon 3D.** Not low-poly, not realistic. Substantial geometric detail with an appealing stylised look.
- PBR materials and lighting, high-quality shadows, ambient occlusion, reflections, water shaders, wet surfaces, selective subsurface-style effects, volumetric-style effects where they help.
- Characters: cute, expressive, exaggerated proportions, readable from the isometric camera, good up close. No photorealistic humans.
- The waterpark: bright, saturated, energetic, safe, strong colour variation between areas.
- The interior: gradually mysterious, atmospheric, beautiful, slightly unsettling.
- The contrast between the two worlds is the game's identity.
- Reference images are for visual direction only. Original designs only, no copyrighted characters, logos or environments.

## 11. Camera

Fully 3D environments seen from an elevated, angled-down isometric / three-quarter view, not a strict old-school isometric look. Smooth follow, with dynamic adjustment: pull back for big encounters, move closer in tight spaces, change angle for cinematics. Must always keep the player, hazards, platforms, teammates and key interactables visible.

## 12. Audio and progression

**Audio:** the waterpark has happy music, water, slides, family ambience, funny effects, mechanical sounds and cartoon impacts. In the monster sequence the music fades, the water goes quiet, low underwater sounds and distant creature noises build, with large impacts. Inside the monster: echoes, strange creatures, organic sounds, mysterious music.

**Progression:** cosmetics are the primary long-term reward (outfits, hats, backpacks, inflatable gear, emotes, floating mats, trails and effects, collectibles). No pay-to-win.

## 13. Open decisions

- [ ] Final title (the brief mentions another name once; confirm BADELAND)
- [ ] Protagonists: custom characters, fixed cast, or both
- [ ] Answers to the story mystery, and how Chapter 2 ends
- [ ] Release model: Chapter 1 as a full game with a sequel/update, or Early Access
- [ ] Whether co-op uses one shared camera only, or limited split-screen for split paths
- [ ] Single-player companion approach for co-op puzzles
- [x] Lap count: 3
- [x] Held-fish rules: one at a time, slippery (slides out after a while), can be thrown at another player
- [ ] How course variations change per lap
- [ ] Full fish roster and exact slip-away times
- [ ] Whether to leave one very subtle clue in the laps (for example the fish behaving oddly) or none at all
- [ ] Performance targets and minimum PC spec
