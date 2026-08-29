# Joust Unity M2 — Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Close the loop and reach arcade parity — unseating produces an egg, eggs hatch into promoted riders, lava kills, players respawn, waves clear and advance, and all three buzzard tiers behave differently.

**Architecture:** Same split as M1. Anything decidable without a frame is a pure static function with headless EditMode tests; anything needing physics is a MonoBehaviour with PlayMode tests. New systems are wired through events the existing components already raise (`Rider.Unseated`, `Egg.Hatched`, `CombatContact.Resolved`), so nothing existing needs rewriting.

**Spec:** `docs/superpowers/specs/2026-08-29-joust-unity-native-design.md`
**Arcade authority:** `docs/superpowers/specs/2026-08-29-arcade-joust-reference.md`

## Global Constraints

All M1 constraints still apply. Additionally:

- **EditMode runs headless** (`-nographics`) and is the default gate. PlayMode
  and screenshots take a graphics device and are batched for when the machine
  is free.
- **Tier behaviour comes from the arcade**: Bounder flies erratically with
  little pursuit, Hunter actively pursues, Shadow Lord is fastest and most
  aggressive. Scores 500 / 750 / 1000.
- **Promotion on hatch**, not a fixed composition list. A wave gets harder
  because of how it is fought.
- **The pterodactyl is time-triggered**, not wave-number triggered: it is the
  arcade's anti-stalling measure. The clone's wave-slot rule is not arcade
  behaviour.

---

### Task 1: Spawning eggs from unseated riders

**Files:**
- Create: `Assets/Scripts/Runtime/World/EggSpawner.cs`
- Test: `Assets/Tests/EditMode/EggSpawnRulesTests.cs`

**Produces:** `Joust.World.EggSpawnRules.ShouldDropEgg(bool wasPlayer)` — only
enemy riders leave eggs; a downed player respawns instead. And
`Joust.World.EggSpawner`, subscribing to `Rider.Unseated` and instantiating an
`Egg` carrying the unseated rider's tier at its position, inheriting its
velocity so the egg is thrown rather than dropped straight down.

- [ ] Write the failing test for `ShouldDropEgg`
- [ ] Run EditMode, confirm red
- [ ] Implement the rule and the spawner
- [ ] Run EditMode, confirm green
- [ ] Commit

### Task 2: Hatching into promoted riders

**Files:**
- Create: `Assets/Scripts/Runtime/Enemies/EnemySpawner.cs`
- Create: `Assets/Scripts/Runtime/Enemies/EnemyDefinition.cs`
- Test: `Assets/Tests/EditMode/EnemyDefinitionTests.cs`

**Produces:** `Joust.Enemies.EnemyDefinition`, a ScriptableObject holding tier,
score, speed multiplier and AI profile; and `Joust.Enemies.EnemySpawner` which
listens for `Egg.Hatched` and builds a rider of the promoted tier. Tier stats
are data, so M4's new enemies are new assets rather than new branches.

- [ ] Write failing tests for tier stat lookup
- [ ] Run EditMode, confirm red
- [ ] Implement definition and spawner
- [ ] Run EditMode, confirm green
- [ ] Commit

### Task 3: Lava death and respawn

**Files:**
- Create: `Assets/Scripts/Runtime/World/LavaField.cs`
- Create: `Assets/Scripts/Runtime/Flow/RespawnService.cs`
- Test: `Assets/Tests/EditMode/RespawnRulesTests.cs`

**Produces:** `Joust.World.LavaField`, killing anything whose Y falls below the
lava surface (y=0 by `ArenaMetrics`); and
`Joust.Flow.RespawnRules.ChooseSpawnPad(IReadOnlyList<Vector2> pads, Vector2 threat)`
returning the pad furthest from the nearest threat, so a player does not respawn
into the rider that just killed them.

- [ ] Write failing tests for pad choice, including the no-threat and
      single-pad cases
- [ ] Run EditMode, confirm red
- [ ] Implement
- [ ] Run EditMode, confirm green
- [ ] Commit

### Task 4: Waves

**Files:**
- Create: `Assets/Scripts/Runtime/Flow/WaveRules.cs`
- Create: `Assets/Scripts/Runtime/Flow/WaveRunner.cs`
- Test: `Assets/Tests/EditMode/WaveRulesTests.cs`

**Produces:** `Joust.Flow.WaveRules.IsWaveClear(int enemies, int eggs)` (both must
be zero — an egg still on the ground is an unspawned enemy),
`WaveRules.OpeningComposition(int wave)` returning the tier list a wave starts
with, and `WaveRules.SurvivalBonus(int wave, bool lostALife)`. `WaveRunner`
drives start, clear detection and advance.

- [ ] Write failing tests: a wave with eggs remaining is not clear; wave 1 opens
      with three bounders; composition grows with wave number; survival bonus is
      3000 only on a survival wave completed without dying
- [ ] Run EditMode, confirm red
- [ ] Implement
- [ ] Run EditMode, confirm green
- [ ] Commit

### Task 5: Hunter and Shadow Lord behaviour

**Files:**
- Modify: `Assets/Scripts/Runtime/Enemies/BuzzardDecision.cs`
- Test: `Assets/Tests/EditMode/TierBehaviourTests.cs`

**Produces:** `BuzzardDecision.Decide` taking an `AiProfile` so the three tiers
differ: a Bounder wanders and rarely commits, a Hunter tracks the player's
position, a Shadow Lord tracks and climbs to stay above. All still pure, all
still dice-injected.

- [ ] Write failing tests distinguishing the three profiles by decision, not by
      statistics
- [ ] Run EditMode, confirm red
- [ ] Implement
- [ ] Run EditMode, confirm green
- [ ] Commit

### Task 6: Wire it into the scene, and gate

**Files:**
- Modify: `Assets/Editor/M1SceneBuilder.cs` (renamed `GameSceneBuilder`)

- [ ] Assemble spawners, lava, wave runner and HUD wave display
- [ ] Run both suites
- [ ] Build the player and capture a screenshot **when the machine is free**
- [ ] User smoke: clear a wave, die to lava, respawn, watch a hatched rider come
      back one tier stronger
- [ ] Commit and publish

---

## Definition of done

- Unseating an enemy drops an egg that hatches one tier stronger if not collected.
- Falling in the lava costs a life; respawn avoids the nearest threat.
- Clearing all enemies and eggs advances the wave.
- The three tiers visibly behave differently.
- Both suites green; user smoke passed.
