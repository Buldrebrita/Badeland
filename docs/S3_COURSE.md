# S3: Floating course, laps and fish

Build the test scene with **Badeland > Create S3 Course Test Scene**, then press Play.

## The test course

A ring of inflatable decks floating on open water, run **counter-clockwise** (to the right from the start). Three laps, then "Finished!" shows (the monster will take over from there). Fall off and you land in the sea: swim to a deck and hop out (Space at the surface). The look follows the reference picture: bright blue sea, blue decks with orange borders, green bumper posts, pink inflatables, beach balls.

| Part | What it does |
|---|---|
| Flag gates | Checkpoints. Run between the two flags. They count at **any height**, so bouncing or jumping over a gate still counts, and running back through one does not. Yellow flags are checkpoints, green flags are the finish line. |
| Right straight | Three pink bounce pads. |
| Top straight | A yellow bar sweeping the whole deck. Jump it or get knocked into the sea. |
| Left straight | No deck: three stepping discs across open water. |
| Beach balls, green posts | Solid soft obstacles on the decks. |

## Fish

Fish leap out of the sea at **random places and random times** (and random species) towards the decks. The pattern is seeded, so in online co-op everyone will see the same fish. Run into a leaping fish, or jump up to it near the top of its arc, to catch it. You hold one at a time and it slips away after about 20 seconds. The text at the top left shows the lap, and the fish and its timer only while you hold one.

Fish areas (`Fish Area ...` objects) have a start zone, a direction, distances, how often a fish leaps (slot length and chance) and a seed. Select one to see its zone as a gizmo. The species assets in `Assets/_Project/Settings/Fish` set what each fish does.

## Per-lap changes (look closely)

Near the start: a **towel** appears on lap 2, **sunglasses** on lap 3, the **lifeguard chair** shifts a little on lap 3, a **cone** is gone on lap 3 and the **sign** turns slightly pink on lap 3. They are set in the `LapChanges` object's list: each entry says from which lap something appears, disappears, moves or changes colour. The changes follow the leading player's lap.

## What to judge

Is the circuit fun three times in a row? Is falling in the sea funny rather than annoying? Do the flag gates always count when you expect? Do random fish feel exciting? Do you notice the changes by lap 3 without being told?

## Not done yet

Throwing fish (needs a second player and a shared clock for the fish), moving platforms and balance beams, a water shader, the finish celebration, the monster, and proper art and UI. Everything here is primitive shapes and flat colours. It is a blockout of the reference look, not final art.
