# Badeland

*(working title)*

A funny, all-ages 3D action-platformer with an isometric camera. Explore a small semi-open hub world, enter themed levels, and collect what you need to reach one big final goal. The comedy comes from slapstick physics and characters, not violence.

> Storyline, characters and setting are still to be decided. See [docs/DESIGN.md](docs/DESIGN.md).

## At a glance

| | |
|---|---|
| Genre | 3D action-platformer, hub world + levels |
| Camera | Isometric (fixed angle) |
| Look | High-poly, cartoon style, PBR materials and rendering |
| Tone | Funny, slapstick, suitable for all ages |
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

Project skeleton only. No gameplay yet.
