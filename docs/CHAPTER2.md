# Chapter 2: Inside

**Status:** draft v0.1. The first area exists as a playable stub. Everything marked *proposal* is a creative first pass for you to change.

Source: DESIGN.md section 9. Not a stomach: a surreal, beautiful, slightly unsettling fantasy ecosystem inside the creature. Mysterious, never gory.

## The big idea

You are not just *inside* a monster, you are inside something **alive and listening**. The whole place breathes. The clues say you were not the first, and that the creature is not what you think. The funny waterpark world has to echo in the new one: old bits of it keep turning up, which tells you that others came in through the water before you.

## How you get here

The monster eats everyone, the screen fades to black ("To be continued..."), and a few seconds later the party wakes up on a shore inside, under a "Chapter 2: Inside" title. Online, the host loads the scene for everyone. Swallowed players come back to life.

## Rules of this world (proposal)

1. **It breathes.** About 20 seconds per full breath (10 in, 10 out). The lake rises and falls, the light swells and fades, a slow heartbeat plays. Later areas use the same clock: tides open and close passages, and big breaths blow the spores around.
2. **It is quiet.** Old carvings say "DO NOT BE LOUD. IT HEARS THE RIVER." Later this can become a stealth-like mechanic (some things only wake when you splash or shout), but the tone stays mysterious, not horror.
3. **Light is life.** The glowing plants are the only real light. Lights in the world react to you and to each other.
4. **Echoes of above.** Waterpark objects keep turning up (a lifebuoy, a "Splash Zone!" sign, a flat pink inflatable). It is the same kind of place, a long time ago, and other people came here the same way.
5. **Solo works.** Every co-op puzzle has a solo way (the heavy stone for the plate puzzle).

## The areas (proposal)

| # | Area | Mood | Idea |
|---|---|---|---|
| 1 | **The Waking Shore** *(built, first pass)* | Dark, calm, strange | Wake up, glowing grove, hold-a-button gate, ruins with notes, the breathing lake, a great door in the far wall |
| 2 | The Ribs | Vast, pale, windy | Climbing the giant ribcage. Wind from each breath sweeps you off unless you time it |
| 3 | The Spore Forest | Beautiful, glowing | Bioluminescent forest. Spores drift with the breath and light up hidden paths |
| 4 | The Warm River | Fast, funny, wet | A river that flows with the breathing (slides again, with a big current). A callback to the waterpark |
| 5 | The Stone Village | Eerie, quiet | The people who built here. Their houses, their carvings, the last survivors' camp |
| 6 | The Heart | Huge, rhythmic | The centre. A heartbeat you can feel. The truth about the creature |

## Area 1: The Waking Shore (what exists now)

- **Wake-up:** fade in from black with the chapter title. Players stand on a pale, warm shore.
- **The cavern:** fleshy walls, huge ribs overhead, glowing mushrooms in three colours, drifting jellyfish, fog.
- **The breathing lake:** the water rises and falls about a metre every 10 seconds. A rotting dock leads into it.
- **The bone gate puzzle:** the gate sinks while the plate in the north is held down. With friends, someone stands on it. Alone, you carry the heavy glowing stone onto it (press E to pick up and put down).
- **The shrine:** a circle of standing stones around an altar, forty-one folded robes with bowls, and three carvings that tell why people once came here willingly.
- **Seven notes (press E to read, E to close, W/S or mouse wheel to scroll):** the diary of Marit and the narrator. While you read, your character stands still and nothing can hurt you.
- **Compact and enclosed:** a living cavern with glowing veins, pulsing organs and an unbroken wall all around (you cannot fall out of the world). If you somehow fall, you are put back where you last stood.
- **The great door** in the east wall. When everyone is at it, a message says it leads deeper, and that this is the end of what is built.

## Story (decided so far)

The full direction is in [STORY.md](STORY.md). What this first area already tells, in pieces:

- **The shrine of the Tide-Keepers.** Forty-one folded robes with offering bowls, a circle of standing stones, an altar, and three carvings. A very long time ago people came to the creature (the **Bearer**) *on purpose*, believing it came from a sacred place where the sea was born. They went to be kept, not eaten, so that the world it carries would not end.
- **Marit and the narrator.** A diary in seven pages spread along the route. Marit loses her mind inside, wants out, and runs off into the lake. The narrator, alone, becomes lonely, talks to the mushrooms and sees Marit everywhere (faint figures that fade when you walk up to them), and finally believes the great door will let them walk out. It does not lead out. It leads deeper.
- **Outside things.** A lifebuoy, a "Splash Zone!" sign and a flat pink inflatable lie in the sand. Others came in through the water, and from the waterpark.

## Open decisions

Decided by the project owner (see STORY.md): the creature is an ancient living world, not a villain; it consumes things to repair itself; many people have come before, some by choice; the waterpark is connected; there are three endings.

Still open:

- [ ] What is making the creature sick, and what is the ancient danger at the bottom of the sea (the secret ending)?
- [ ] What happened to Marit: found later (changed? alive?), or never?
- [ ] Who are the NPCs of the first living community, and what do they want?
- [ ] How the stealth-like "do not be loud" idea works, if at all.
- [ ] How Chapter 2 ends, and how the endings branch.

## What is not built yet

Areas 2 to 6, NPCs and dialogue, any threat inside, a current in the river, real art and audio (the heartbeat is a generated placeholder), and putting the carried stone over the network (it works on the machine of whoever carries it).

## Room 2: The Sunken Harbour (first draft)

Menu: `Badeland > Create Chapter 2 - Room 2 (The Sunken Harbour)`. Room 1's great door leads here.
A bigger cavern with a lagoon, a wrecked ship (gangplank, deck, button B), an empty lantern village on a terrace
(button A, heavy stone, the shell that wakes the first checkpoint), a waterfall with a plunge pool, acid pools, and a
stone gate that opens only while both buttons are held. Three hidden chests. One narrator note. The second checkpoint
(beyond the gate) is already awake. Shared cavern code lives in `ChapterTwoBuilder` (room 2 is a partial of it).
