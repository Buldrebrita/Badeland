# Badeland: Design Document

**Status:** draft v0.1. Working title. Storyline, protagonist and setting are intentionally undecided (see [Open decisions](#open-decisions)).

## 1. Vision

A funny, welcoming 3D action-platformer for all ages. The player explores a small semi-open hub world, enters themed levels, collects what each one rewards, and uses those rewards to reach a single, visible final goal. The game should make people laugh out loud and want to show others what just happened.

## 2. Design pillars

1. **Funny first.** Humor comes from physics, animation, reactions and characters. If a moment isn't funny or charming, we cut or rework it.
2. **Playful, not punishing.** Failure is slapstick, not violence. No gore, no death, and a short, quick return to play after a mishap.
3. **Great movement.** Moving the character around should feel good on its own. Everything else is built on that.
4. **Readable at a glance.** The isometric view must always show where the player is, what is dangerous and where to go next.
5. **All ages.** Easy to pick up, with depth for players who want a challenge. Content is family-friendly.

## 3. Structure

### Hub world
- Small, semi-open, and explorable in any order.
- The **final goal is visible from the start**, so players always know what they're working toward.
- Holds the entrances to each level, friendly characters, small secrets and light puzzles.
- Gates on the path to the goal open as the player collects level rewards.

### Levels
- Target for the first full version: **4 to 6 themed levels**, each short enough to finish in 10 to 20 minutes.
- Each level has one **signature gimmick**, a few set pieces, hazards and an optional secret.
- Each level ends with a **reward** that counts toward opening the way to the goal.

### Goal
- One final area or event, unlocked when enough rewards are collected.
- Ends with a short, memorable finale.

## 4. Core gameplay

- Third-person character control relative to the isometric camera.
- Core moves (to be tuned): run, jump (with variable height), a signature action, and interact.
- Game-feel details: coyote time, jump buffering, forgiving ledges, generous camera.
- Physics-driven slapstick: bouncy landings, topples, launches, things reacting to the player.
- Hazards and enemies are comedic obstacles that knock the player back, not kill them.
- Checkpoints are frequent; "failing" returns the player to the last one with a funny animation.

## 5. Camera and presentation

- Fixed-angle **isometric** camera (orthographic or narrow-FOV perspective, decided in prototype).
- Camera follows the player with smoothing and gentle look-ahead.
- Occlusion handling: fade or cut away geometry that hides the player (shader-based).
- Optional limited camera rotation (for example 90-degree steps) to fix hidden spots. To be tested.

## 6. Art direction

- **High-poly, cartoon style** with **PBR** materials and rendering.
- Stylization comes from exaggerated proportions, chunky shapes, bright palettes and expressive animation, with physically based lighting underneath.
- Strong silhouettes and clear color coding for gameplay (safe, dangerous, interactive).
- Expressive, squash-and-stretch character animation.
- Performance budget to be set after the first vertical-slice scene.

## 7. Audio direction

- Music that is bouncy and varied per level.
- Exaggerated, funny sound effects. Sound carries a lot of the comedy.
- Optional voiced or gibberish characters and a narrator. To be decided with the storyline.

## 8. Technical plan

| Area | Plan |
|---|---|
| Engine | Unity 6 LTS, Universal Render Pipeline (URP) |
| Language | C# |
| Input | Unity Input System (keyboard/mouse and gamepad) |
| Physics | Unity physics, with custom character controller for movement feel |
| Camera | Cinemachine |
| Materials | URP Lit (PBR) plus Shader Graph for stylization and occlusion fade |
| Platforms | PC (Windows) first for Steam; macOS/Linux and consoles evaluated later |
| Store integration | Steamworks (achievements, cloud saves, Steam Deck support) |
| Version control | Git with Git LFS |

## 9. Milestones

1. **Prototype:** player controller and isometric camera in a gray-box test scene. Prove the movement feels good.
2. **Vertical slice:** one polished hub area and one complete level with final art, audio, hazards and the reward loop.
3. **Content:** remaining levels, full hub, the final goal and finale.
4. **Polish and release prep:** playtests, performance, accessibility options, Steam store page, trailer, achievements, release build.

## 10. Accessibility and audience

- Remappable controls, gamepad support, adjustable camera and difficulty options.
- Subtitles and clear visual cues for important sounds.
- Avoid flashing effects. Offer a reduced-motion option.
- Rating target: suitable for all ages.

## Open decisions

- [ ] **Storyline, protagonist and setting** (to be provided by the project owner).
- [ ] Final game title.
- [ ] Signature action for the character.
- [ ] Number of levels and each level's theme and gimmick.
- [ ] Camera rotation: fixed, or limited steps.
- [ ] Single player only, or local co-op.
- [ ] Performance targets and minimum PC spec.
