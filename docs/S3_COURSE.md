# S3: Course, laps and fish

Build the test scene with **Badeland > Create S3 Course Test Scene**, then press Play.

## The test course

A rectangular circuit with an 8 m wide corridor, run **counter-clockwise** (to the right from the start). Three laps, then "Finished!" shows (the monster will take over from there).

| Part | What it does |
|---|---|
| Yellow gates | Checkpoints. Pass all three in order, then the green finish line, and a lap counts. |
| Right straight | Three pink bounce pads. A **cod** leaps across (faster running). |
| Top straight | A yellow bar sweeping the corridor. Jump it or get knocked back. A **salmon** leaps over it (higher jumps). |
| Left straight | A **clownfish** leaps across (you cannot walk straight). |

## Fish rules in the test

Run into a leaping fish (or jump up to it near the top of its arc) to catch it. You hold one at a time, and it slips away after about 20 seconds (the text at the top left counts down, and warns when it is about to go). Fish effects use the species assets in `Assets/_Project/Settings/Fish`. Change speed, jump, wobble and times there.

## Per-lap changes (look closely)

Near the start area: a **towel** appears on lap 2, **sunglasses** on lap 3, the **lifeguard chair** shifts a little on lap 3, a **cone** is gone on lap 3 and the **sign** turns slightly pink on lap 3. They are set in the `LapChanges` object's list in the inspector: each entry says from which lap something appears, disappears, moves or changes colour. The changes follow the leading player's lap.

## What to judge

Is the circuit fun to run three times? Is chasing fish fun, and do the effects read clearly (cod fast, salmon high, clownfish wobbly)? Do you notice the changes by lap 3 without being told? Are the bounce pads and the bar fair and funny?

## Not done yet

Throwing fish (needs a second player), the swim section, moving platforms and balance beams, the finish celebration, the monster, and proper UI and visuals for everything above.
