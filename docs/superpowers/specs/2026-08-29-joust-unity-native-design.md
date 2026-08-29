# Joust Unity — Native 3D Rebuild (design)

Date: 2026-08-29
Status: approved design, pre-plan
Reference implementation: the pygame-ce Joust clone in this repo (`joust/`, `tools/`, `tests/`)

## 1. Purpose

Rebuild the Joust clone as a native Unity 6 game with full 3D graphics, keeping
the arcade rules of Joust and extending them with new content. The Python
version is retained untouched as a reference implementation and playable
artifact; it is not ported line by line.

## 2. Decisions

| Decision | Choice | Why |
|---|---|---|
| Engine | Unity 6000.5.10f1 (installed) | Already on the machine, C#-native, URP available |
| Pipeline | URP 3D | Unity's unified pipeline; HDRP is in maintenance |
| Visual target | Full 3D models and lighting | User choice |
| Gameplay space | 2.5D — movement locked to the XY plane, 3D art and camera | Preserves the lance-height duel; depth used for parallax only |
| Build style | Fresh Unity-idiomatic build | User choice: MonoBehaviours, prefabs, ScriptableObjects, Animator, physics. Python is reference, not source |
| Feel | Tuned in-engine by playtest | Not transcribed from the Python constants |
| Art source | Free and CC0 asset packs, imported and wired | No hand-modelling capability in this pipeline |
| Scope | Parity plus new content | User choice |
| Repo | `unity/JoustUnity/` inside this repo | Keeps the reference implementation adjacent; no remote coupling |

### Rejected alternatives

- **Deterministic C# simulation with Unity as renderer.** Highest feel fidelity
  and fastest tests, but rejected by the user in favour of an idiomatic native
  build.
- **Hybrid custom kinematics with Unity colliders.** Marginal gain, and it adds a
  physics-tick versus simulation-tick ordering hazard.
- **True 3D free-flight arena.** Rejected: the lance-height rule, the AI, screen
  wrap and the camera would all be new design, making it a different game.

## 3. Architecture

```
unity/JoustUnity/
  Assets/
    Scenes/    Boot.unity  Title.unity  Game.unity
    Scripts/
      Runtime/                      (asmdef Joust.Runtime)
        Movement/   FlightController.cs  GroundController.cs
        Combat/     Lance.cs  JoustResolver.cs
        Enemies/    BuzzardAI.cs  BossAI.cs  PterodactylAI.cs  LavaTroll.cs
                    ShieldBearerAI.cs  BomberAI.cs  SplitterAI.cs
        Flow/       GameDirector.cs  WaveRunner.cs  ScoreService.cs  SaveService.cs
        World/      ArenaBuilder.cs  Platform.cs  ScreenWrap.cs
        PowerUps/   PowerUpService.cs  PowerUpEffect.cs
      UI/                           (asmdef Joust.UI, UI Toolkit)
    Data/     TuningProfile, EnemyDef, WaveDef, ArenaDef, PowerUpDef (ScriptableObjects)
    Prefabs/  Art/  Audio/  Settings/
  Tests/EditMode  Tests/PlayMode
```

### Movement

A `Rigidbody` with `useGravity` disabled, interpolation enabled, and constraints
freezing Z position and all rotation. Gravity, flap impulse, horizontal thrust,
drag and ground skid are applied in `FixedUpdate` from a `TuningProfile`
ScriptableObject, editable in the inspector during play. Grounded state comes
from contact with platform colliders.

### Combat

Mount colliders are triggers. On overlap, `JoustResolver` compares the world Y of
the two `Lance` transforms: the higher lance wins, and within a tie band both
riders bounce apart with a horizontal impulse and a small upward nudge. Collision
geometry never decides the winner — the height comparison is explicit.

Unmounted riders and hatchlings are collected on contact rather than jousted.
Eggs bounce off platforms with damping, come to rest, and hatch on a timer; a
hatchling waits, then a buzzard mounts it and rejoins the wave.

### Screen wrap

A `ScreenWrap` component teleports entities at the horizontal bounds of the arena
and renders a ghost copy near the seam so the transition is not visible. It is
applied only to entities that wrap.

### Data-driven content

Enemies, waves, arenas and power-ups are ScriptableObjects. Adding an enemy is a
data asset plus an AI component, not a branch in a scheduler. This is what makes
the new-content milestone affordable.

### Rendering

URP 3D. A side-on camera with slight perspective; entities occupy depth lanes for
parallax without affecting gameplay. Emissive lava with real point lights, and
particle effects for flap, impact, hatch and burn. A Volume profile carries
bloom, vignette and colour grading.

Two verification gates, both earned in prior Unity work in this environment:

1. A URP package present in `manifest.json` does not mean URP is rendering.
   `ProjectSettings/GraphicsSettings.asset` must carry a non-zero
   `m_CustomRenderPipeline` pointing at the pipeline asset of this project, and
   the build log must name `Universal Render Pipeline/...` shaders rather than the
   Built-in family.
2. `VolumeProfile.Add<T>()` defaults to `overrides:false`, which silently discards
   every value written to the component. Every override added in code sets
   `overrideState` explicitly.

### Audio

Unity `AudioSource` through an `AudioMixer`. The 15 generated `.wav` files in
`assets/sfx/` are the starter kit; `tools/gen_sfx.py` remains the source of truth
for regenerating them.

### Persistence

Hi-score and settings as JSON in `Application.persistentDataPath`.

## 4. Content

### Parity content

Three buzzard tiers (bounder, hunter, shadow lord) with the scoring ladder; eggs
with a collection chain bonus, hatching and re-mounting; the pterodactyl; the
lava troll; platform erosion across waves; the wave types (normal, survival, egg,
pterodactyl, and the two-player team wave); extra lives at a score threshold;
local two-player including player-versus-player jousting; hi-score persistence;
and the title, attract, pause and game-over states.

### Boss wave — The Skylord

Every tenth wave, in a dedicated open arena, so the parity wave-type slots stay
untouched. An oversized armoured mount taking three lance hits, each hit
staggering it and stripping armour. Its cycle is cruise, telegraph (a screech and
wing flare of roughly 0.8 seconds), dive along the Y of the player, then recover.
It is vulnerable only during recovery, and spawns two hunter adds per phase. Its
death drops a guaranteed power-up and a large egg worth a chained bonus.

### Arena variants

An `ArenaDef` holds a platform list, spawn pads and a hazard set. Four arenas:

- **Classic** — the reference layout.
- **Spires** — tall narrow pillars, favouring vertical duels.
- **Bridges** — long spans that burn through and collapse mid-wave.
- **Islands** — two platforms that drift horizontally.

Arenas rotate per five-wave set, and lava erosion applies within a set.

### New enemies

- **Shield-bearer** — frontal lance contact is blocked with a clang; it must be
  hit from behind or from directly above.
- **Bomber** — flies high and drops falling fireballs that kill on contact and
  scorch platforms.
- **Splitter** — on defeat it splits once into two smaller, faster, lower-value
  bounders.

### Power-ups

Dropped by collected eggs at roughly one in five, and guaranteed from the boss.
Each lasts fifteen seconds, shown by a HUD timer ring.

- **Swift Mount** — 35 percent higher maximum speed.
- **Double Flap** — stronger flap impulse and a faster climb.
- **Long Lance** — extended reach; wins ties instead of bouncing.
- **Aegis** — absorbs one lethal hit and shatters visibly.

## 5. Art pipeline

Free and CC0 low-poly packs, downloaded by the user and wired up here. The
primary targets are Quaternius (animated animals for the mounts and buzzards,
medieval and knight packs for the riders), Kenney (nature and castle kits for
platforms and props, CC0), and Poly Pizza for one-off props. Each asset is
imported, its scale and pivot normalised, and given a shared URP-lit material
pass so the palette stays consistent; an Animator then drives `flap`, `glide`,
`run` and `brake` states from the movement controller. Assets with no pack match
— the claw of the lava troll, the eggs, the armour of the Skylord — are built
from primitives, which suits the art style.

Licences for every imported pack are recorded in
`unity/JoustUnity/Assets/Art/ATTRIBUTION.md`.

## 6. Testing

Pure-logic components — wave scheduling, scoring and bonuses, joust resolution,
arena selection, power-up expiry — are written as plain static C# and covered by
EditMode tests, which run headless in seconds. Physics and feel get a thin
PlayMode smoke set: the player can flap, land, wrap, joust and die. The Python
`tests/` directory is a checklist of behaviours to cover, not a numerical
specification to match.

Each milestone gates on EditMode tests passing, a successful player build, and a
user smoke test. A full build and test smoke runs before every commit.

## 7. Milestones

| # | Milestone | Done when |
|---|---|---|
| M0 | Verification spike | URP proven active by settings and build log; an asset pack imports and animates; flight feels right in hand; trigger-based lance resolution fires correctly. Output: a findings document. No planning proceeds on unverified engine claims. |
| M1 | Core loop | Flight, one arena, one buzzard tier, joust resolution, death and respawn, score and lives HUD |
| M2 | Parity | Three tiers, eggs and hatching, pterodactyl, lava troll, wave schedule, local two-player, hi-score, title, attract and pause |
| M3 | Presentation | URP lighting and VFX pass, camera juice, audio mix, animation polish |
| M4 | New content | Arenas, three new enemies, power-ups, the Skylord boss |
| M5 | Ship | IL2CPP Windows build, user smoke test |

## 8. Open items for the plan

- The exact asset packs are chosen during M0, once import behaviour is confirmed.
- Input uses the Unity Input System, with keyboard and gamepad bindings mirroring
  the control scheme of the reference implementation.
- Whether attract mode replays a recorded demo or runs AI against AI is decided in
  M2; AI against AI is the cheaper default.
