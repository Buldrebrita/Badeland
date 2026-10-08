# Badeland

*(working title)*

A colourful, funny, 1 to 4 player co-op adventure for PC. It starts in a giant waterpark full of slides, inflatable obstacle courses, hidden traps and treasures. At the end, the lights go out, a sea monster rises from the water and swallows everyone. They wake up inside it, in a strange world with a mystery to solve.

> Story and monster: [docs/STORY.md](docs/STORY.md) · Design: [docs/DESIGN.md](docs/DESIGN.md) · Tech: [docs/TECH.md](docs/TECH.md) · Roadmap: [docs/ROADMAP.md](docs/ROADMAP.md) · First playable: [docs/VERTICAL_SLICE.md](docs/VERTICAL_SLICE.md)

## At a glance

| | |
|---|---|
| Genre | 3D co-op action-platformer / adventure, waterpark levels then a fantasy world |
| Players | Single-player and online co-op, 1 to 4 |
| Camera | Elevated isometric / three-quarter view in full 3D |
| Look | High-poly stylised cartoon, PBR materials and rendering |
| Tone | Funny and colourful, then mysterious and slightly scary |
| Engine | Unity (Unity 6 LTS, Universal Render Pipeline) |
| Target | PC first (Steam), other storefronts later |

## Getting started

### 1. Install the tools

- **Unity Hub** and the **latest Unity 6 LTS** editor (include the Windows/Mac/Linux Build Support modules you need).
- **Git** and **Git LFS**. Run `git lfs install` once per machine, *before* cloning.

### 2. Clone

```bash
git lfs install
git clone https://github.com/Buldrebrita/badeland.git
cd badeland
```

### 3. Create the Unity project in this folder

The repo contains the folder structure and git configuration, but not the Unity-generated project files (`ProjectSettings/`, `Packages/`), because those must be created by the editor.

1. In Unity Hub choose **Add > Add project from disk** and select the cloned `badeland` folder. If Hub does not accept it, create a new **3D (URP)** project in a temporary folder and copy its `ProjectSettings/` and `Packages/` folders into the repo.
2. Open the project once so Unity generates the missing files and `.meta` files.
3. Commit `ProjectSettings/`, `Packages/manifest.json`, `Packages/packages-lock.json` and all `.meta` files. **Never commit `Library/`**; the `.gitignore` already excludes it.
4. In **Edit > Project Settings > Editor** set **Version Control** to *Visible Meta Files* and **Asset Serialization** to *Force Text*.

## Repository layout

```
Assets/_Project/
  Art/            Characters, Environment, Props, Materials, Textures, VFX, UI
  Audio/          Music, SFX, Voice
  Animation/
  Prefabs/        Characters, Environment, Interactables, Hazards
  Scenes/         Bootstrap, Hub, Levels
  Scripts/        Player, Camera, World, Systems, UI, Steam
  Settings/       URP assets, input actions, project settings assets
  Shaders/
  Tests/          EditMode, PlayMode
docs/             Design documents
steam_build/      Steamworks build scripts (never commit credentials)
```

Third-party assets from the Asset Store or elsewhere go in `Assets/ThirdParty/`, outside `_Project`, so our work stays separate.

## Conventions

- **Git LFS** handles models, textures, audio and video (see `.gitattributes`). Do not commit large binaries outside LFS.
- **Scenes and prefabs** are text-serialized. To avoid merge conflicts, only one person should edit a given scene at a time; prefer prefabs and additive scenes for shared work.
- **Naming:** `PascalCase` for scripts, prefabs and scenes. Prefix by type where helpful (`M_` materials, `T_` textures, `SM_` static meshes, `SK_` skeletal meshes, `A_` animations).
- **Branches:** work on feature branches and open pull requests into `main`.

## Releasing on Steam (later)

Planned: Steamworks account and app, Steamworks.NET (or Facepunch.Steamworks) integration in `Scripts/Steam`, and a build script in `steam_build/`. Never commit `steam_appid.txt` with a real id or any account credentials.

## Status

Pre-production, with a working prototype (Unity 6, gray-box). Built and play-tested so far:

- Character controller and isometric camera (S1), swimming and diving (S2)
- A floating obstacle course on the sea with flag-gate laps, bounce pads, a rotating bar and stepping discs, run 3 times (S3)
- Fish that leap at random places and times, can be caught for buffs and debuffs, and thrown at other players (S3)
- Subtle per-lap changes in the park (S3)
- Two-player online test with Netcode for GameObjects (S4/S5): synced movement, a shared clock, host-decided fish catches, thrown fish

Also built (untested until you try it): the bridge and big platform, the monster fight and swallow (S6), the hidden trap and treasure room, a water slide, a moving ferry and a windmill, a cartoon water shader and a nicer lighting look. See [docs/S6_MONSTER_AND_MORE.md](docs/S6_MONSTER_AND_MORE.md). Chapter 2 has begun: after the monster eats the party, everyone wakes up inside it (see [docs/CHAPTER2.md](docs/CHAPTER2.md)). Not built yet: the rest of Chapter 2, and all final art and audio. See [docs/S1_SETUP.md](docs/S1_SETUP.md), [docs/S2_SWIMMING.md](docs/S2_SWIMMING.md), [docs/S3_COURSE.md](docs/S3_COURSE.md) and [docs/S4_NETWORK.md](docs/S4_NETWORK.md) for how to try each part.
