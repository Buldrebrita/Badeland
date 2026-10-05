# Badeland: Roadmap

**Status:** draft v0.1. Estimates assume roughly 1 to 3 people working full time, and are rough. If it is one person, expect the upper end or beyond. Every phase ends with a **gate**: do not start the next phase until the gate is met.

## Scope decision

Ship **Chapter 1** (the waterpark, the monster fight and the swallow) as a complete game first. Chapter 2 (inside the monster) is the larger, separate block that follows as an update, a sequel or the second half of an Early Access plan. This is an open decision (see DESIGN.md), but planning for it now keeps Chapter 1 achievable and gives it a real ending hook.

## Phases

### Phase 0: Pre-production (now, 2 to 4 weeks)
- Design, tech decision, repo, roadmap, vertical slice spec. **(mostly done)**
- Create the Unity project in the repo.
- **Gate:** Unity project opens, pushes cleanly, and the first gray-box scene runs.

### Phase 1: Prototype (6 to 8 weeks)
Goal: **is it fun?** Gray-box only, no final art.
- Character controller, isometric camera, jump, swim, one slide
- One hidden trap and one treasure
- **Networking spike:** 2 players moving, jumping and interacting online
- Pick the networking library
- **Gate:** friends playtest it and laugh; movement feels good; two players can play together online. Revisit the engine decision here.

### Phase 2: Vertical slice (3 to 5 months)
Goal: a 15 to 20 minute section at near-final quality. See [VERTICAL_SLICE.md](VERTICAL_SLICE.md).
- All 14 slice items, one area with real art and audio direction, basic monster encounter
- 1 to 4 player online
- Performance budget measured
- **Gate:** testers finish it and say they want the next part. Publish a "Coming soon" Steam store page and start collecting wishlists (Valve requires a page to be live for a minimum time before release, so check current Steamworks rules).

### Phase 3: Alpha, Chapter 1 content complete (6 to 9 months)
- All four waterpark areas, all slides and courses, all hidden traps and treasures
- Final monster fight and swallow cinematic, a short teaser of the interior
- Save system, progression, star ranking, cosmetics v1, menus, settings
- Netcode hardened, solo versions of all co-op puzzles
- **Gate:** the whole of Chapter 1 is playable start to finish, solo and in co-op, even if rough.

### Phase 4: Beta (3 to 4 months)
- Content lock, polish, balance, difficulty tuning
- Performance and memory optimisation, graphics settings
- Controller and keyboard/mouse polish, accessibility options, tutorial, credits
- External playtests, crash and bug fixing, a public demo (consider a Steam Next Fest slot, ending the demo when the lights go out)
- **Gate:** no known crashes or progression blockers, playtesters finish without help, performance meets budget.

### Phase 5: Release candidate and launch (1 to 2 months)
- Steamworks integration finished: achievements, cloud saves, Steam Input, rich presence
- Windows build and packaging pipeline, store page assets, trailer
- Final QA pass, day-one patch plan
- **Gate:** clean install on several machines, store page approved.

### Phase 6: Post-launch
- Bug fixes, community feedback, quality-of-life updates
- Start or continue Chapter 2 depending on release model and results

## Ship checklist (from the brief)

Main menu, new game, continue, settings (graphics, audio, controls), keyboard/mouse and controller support, save system, progression, online multiplayer, pause menu, tutorial, credits, accessibility options, resolution/fullscreen/windowed support, performance settings, error handling, Windows build and package process, Steam integration.

Steam-specific systems are built late, except the minimum needed for online play (relay and lobbies), which belongs to the networking spike.

## Risks to the schedule

| Risk | Mitigation |
|---|---|
| Scope grows (two worlds, co-op, high-poly art) | Chapter split, gate every phase, cut features before dates |
| Multiplayer retrofit | Network from the prototype, not after |
| Art becomes the bottleneck | Modular kits, placeholders until fun is proven, consider outsourcing selected art |
| Fun not proven before content is built | Gate 1 and Gate 2 are playtests, not checklists |
| Spoiler problem: the twist is the best marketing and cannot be shown | Sell the waterpark co-op chaos on the store page, tease only that "something is wrong", let the twist be discovered and clipped by players |
