# Badeland: Technical Direction

**Status:** draft v0.1. Decision: **Unity 6 (LTS) with the Universal Render Pipeline (URP)**, C#. Open to revisiting before the prototype is finished (see "What would change this decision").

## 1. Engine decision

| | Unity 6 + URP | Unreal Engine 5 | Godot 4 |
|---|---|---|---|
| Visual ceiling | High, needs more manual work for GI/volumetrics | Highest out of the box (Lumen, Nanite, Niagara, Water) | Good for stylised, weaker for heavy high-poly PBR scenes |
| Multiplayer | Good libraries, you choose and integrate one | Mature built-in replication | Built-in, less proven for physics co-op |
| Small-team productivity | High (C#, fast iteration, small builds) | Lower (C++ or Blueprints, heavy editor, big builds) | High, but smaller ecosystem for this scope |
| Asset formats | Scenes and prefabs are text (YAML), code is C# | Most content is binary assets (.uasset), Blueprints are visual | Text-based |
| Steam path | Well worn | Well worn | Possible, less common |

**Why Unity:**

1. **Finishability.** For a small team, iteration speed beats raw rendering power. C# and fast editor iteration matter more than Lumen.
2. **The look is achievable in URP.** Stylised cartoon with PBR does not need fully dynamic GI. Mostly static levels with baked lighting and light probes get you most of the way.
3. **Workable for AI-assisted development.** I can write and review C#, shaders and text-serialised Unity assets directly. Unreal content is mostly binary and Blueprint-based, which is much harder to build and review through code.
4. **Multiplayer is solvable.** The libraries are mature enough; the real difficulty is design (physics, camera), not the engine.

**What would change this decision:** you already know Unreal well; you want top-tier dynamic lighting and are willing to pay for it in complexity; or the networking spike (below) shows Unity's options cannot give acceptable physics co-op. The decision point is the end of the Prototype phase.

## 2. Stack

| Area | Choice | Notes |
|---|---|---|
| Engine | Unity 6 LTS, URP | Confirm the exact LTS version when the project is created |
| Language | C# | |
| Input | Unity Input System | Keyboard/mouse and gamepad from day one, rebindable |
| Camera | Cinemachine | Target group framing, zone-based overrides |
| Character | Custom kinematic controller | Predictable and easy to network, tuned for game feel |
| Materials | URP Lit (PBR) plus Shader Graph | Stylisation and effects layered on PBR |
| Networking | Decided by a spike in the Prototype phase | Candidates: Netcode for GameObjects, Fish-Net, Photon Fusion, Mirror |
| Transport / lobbies | Steam relay via Steamworks | Avoids port forwarding. Use Steam's test app id (480) during development |
| Audio | Unity audio first, FMOD or Wwise evaluated later | Only if adaptive music needs justify it |
| Version control | Git + Git LFS | Already set up |
| Platforms | Windows first. macOS/Linux and Steam Deck evaluated later | |

## 3. Key technical approaches

### Networking
- **Host-authoritative** (one player is the host). Local character uses client-side prediction so controls feel instant.
- Keep the number of networked rigidbodies small. Prefer **deterministic or scripted** things (slides on splines, doors, mechanisms) over free physics.
- Throwables, pushables and carried objects are the hard cases. Design them with an owner at any moment, and test them early.
- **Build a 2-player spike before any level art.** Retrofitting networking onto a finished single-player game is far more expensive.

### Water
- **Not a fluid simulation.** Buoyancy volumes, a simple shared wave function for the surface height, current volumes, splash effects.
- The same surface-height function drives both the shader and gameplay so visuals and physics agree.
- **Slides are spline-based rails** with speed and banking. Fun, deterministic and cheap to network.
- Water shader: depth fade, foam, refraction, caustics projection, wet-surface blending on nearby materials.

### Camera
- Elevated three-quarter view (roughly 45 to 55 degrees pitch), perspective with a narrow field of view, so depth stays readable.
- Cinemachine target group frames all players. A **leash distance** keeps them within frame.
- Zone volumes override distance, angle and framing (tight areas, big set pieces, cinematics).
- **Occlusion fade:** dithered fade or cutaway shader for geometry between camera and players.
- Split paths: start with leash and short branches. Limited split-screen is a later option, not a starting assumption.

### Rendering
- URP with baked lighting, **Adaptive Probe Volumes** and reflection probes, ambient occlusion, bloom, post-processing.
- "Lights go out" is done by blending lighting scenarios and light intensities, not by needing fully dynamic GI.
- Volumetric effects: faked with fog cards, light shafts and particles first. Re-evaluate if that is not enough.
- Verify exact feature availability against the current Unity docs when the project is created, as URP features change between versions.
- **Budget (to be confirmed in the vertical slice):** 1080p at 60 fps on a mid-range GPU. Steam Deck is a stretch target.

### Assets
- Model in Blender, export FBX/glTF, textures authored for PBR (albedo, normal, metallic/smoothness, AO). Everything large goes through Git LFS.
- Placeholders and gray-box first. Final art only for mechanics proven fun.
- High-poly means detail where it reads from the camera, not everywhere. Use LODs and instancing for repeated inflatables and props.

## 4. Technical risks

| Risk | Why | Mitigation |
|---|---|---|
| Networked physics in water | Four players, buoyancy and throwables must stay in sync | Host authority, few networked rigidbodies, scripted slides, early spike |
| Camera for 4 players | Wide spread, depth judgement, split paths | Leash, zones, target group, playtest early |
| Art volume | Two large worlds in high-poly PBR | Modular kits, strict reuse, chapter split |
| Performance | PBR, high poly, water, up to 4 players | Budgets set in the slice, LODs, profiling every milestone |
| Dynamic lighting expectations | URP is not Lumen | Baked + probes, lighting scenarios, art direction that does not depend on dynamic GI |
