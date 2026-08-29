# Arcade Joust (Williams, 1982) — Reference

Date: 2026-08-29
Status: living reference; consulted by every milestone

**The 1982 Williams arcade machine is the authority for this rebuild.** The
pygame implementation in this repository (`joust/`) is a *clone*, and where the
clone and the arcade disagree, the arcade wins unless a deliberate,
recorded decision says otherwise.

This document records what the arcade actually does, and flags every place the
clone diverges. It is deliberately explicit about confidence, because arcade
documentation is secondary-source and inconsistent: facts are marked
**confirmed** (agreed across sources) or **unverified** (single source, or
inferred).

---

## 1. Core rule

Both the player and the enemy knights ride flying mounts and carry a lance.
Flight is achieved by repeatedly pressing a flap button; there is no held-thrust
climb. When two riders collide, **the higher rider wins** and unseats the lower
one. A collision at equal height bounces both riders apart with neither
unseated. *(confirmed)*

The player rides an ostrich; player two rides a stork. Enemy knights ride
buzzards. *(confirmed)*

## 2. Enemies and scoring

| Enemy | Colour | Points | Notes |
|---|---|---|---|
| Bounder | red | 500 | Weakest; flies erratically with little pursuit |
| Hunter | grey / silver | 750 | Actively pursues the player |
| Shadow Lord | blue | **1000** | Fastest and most aggressive |
| Pterodactyl | — | 1000 | Killed only by a lance hit to the beak |

*(confirmed for Bounder and Hunter; Shadow Lord value is the significant
divergence below.)*

**Promotion on respawn.** A defeated rider becomes an egg; when that egg hatches,
the replacement is one tier stronger than the rider that produced it. A Bounder
becomes a Hunter, a Hunter becomes a Shadow Lord, and a Shadow Lord stays a
Shadow Lord. Waves therefore escalate in composition as the player fights them,
not only by wave number. *(confirmed)*

**Eggs.** An unseated rider falls as an egg. Collecting an egg before it hatches
scores an escalating chain — 250, 500, 750, then 1000 for successive eggs
collected without landing — and denies the enemy a respawn. An uncollected egg
hatches into a hatchling, which waits briefly before a buzzard arrives to mount
it and rejoin the wave. *(chain values unverified in exact escalation rule;
behaviour confirmed)*

**Pterodactyl.** Appears when a wave has run too long, as an anti-stalling
measure. It hunts the player and is invulnerable except to a lance strike
directly on its beak. *(confirmed)*

**Lava Troll.** A hand reaches up out of the lava at the bottom of the screen and
grabs any rider flying too low, dragging them down. Flapping hard can break the
grip; failing to escape is fatal. *(confirmed)*

## 3. Wave structure

| Wave type | Behaviour | Bonus |
|---|---|---|
| Normal | Clear all buzzard riders | — |
| Survival | Complete without losing a life | 3000 |
| Egg | Collect all eggs before they hatch | — |
| Gladiator | Two-player only; first player to unseat the other | 3000 |

*(confirmed for Survival and Gladiator bonuses at 3000 points)*

**Lives.** Three to start, with an extra life every 20,000 points. *(confirmed)*

**Terrain erosion.** Platforms progressively disappear as waves advance, shrinking
the safe area and forcing more time in the air over the lava. *(behaviour
confirmed; exact per-wave order unverified)*

## 4. Two-player

Two players play simultaneously and may cooperate or attack each other. Unseating
the other player scores points, and the Gladiator wave explicitly rewards it.
Team-play bonuses reward not attacking each other on the relevant wave.
*(confirmed in outline; exact team-wave rules unverified)*

---

## 5. Divergences in the pygame clone

Checked against `joust/config.py` and `joust/waves.py` at commit `dd6afc8`.

| Item | Arcade | Clone | Action for the Unity rebuild |
|---|---|---|---|
| Shadow Lord points | **1000** | `SCORE_TIERS = (500, 750, 1500)` → **1500** | **Use 1000.** The clone inflates the top tier by 50%. |
| Bounder / Hunter points | 500 / 750 | 500 / 750 | Matches; keep |
| Pterodactyl points | 1000 | `SCORE_PTERO = 1000` | Matches; keep |
| Survival bonus | 3000 | `BONUS_SURVIVAL = 3000` | Matches; keep |
| Gladiator / team bonus | 3000 | `BONUS_TEAM = 3000` | Matches; keep |
| Extra life | every 20,000 | `EXTRA_LIFE_EVERY = 20000` | Matches; keep |
| Starting lives | 3 | `START_LIVES = 3` | Matches; keep |
| Egg chain | 250/500/750/1000 | `EGG_CHAIN = (250, 500, 750, 1000)` | Matches; keep |
| Wave-type schedule | emergent from the machine's rules | fixed arithmetic slots (`SURVIVAL_WAVE_FIRST = 5`, step 5, etc.) | The clone's schedule is an invention that approximates the arcade. Keep as the starting point, flag as **not arcade-derived**, and revisit if a better source is found. |
| Pterodactyl trigger | elapsed time within a wave | `PTERO_WAVE_FIRST = 11` — a *wave-number* slot, plus a timer | The arcade uses time-in-wave as an anti-stall measure, not a wave slot. **Prefer the timer rule**; a wave-number gate changes the game's pacing pressure. |
| Rider promotion on hatch | Bounder → Hunter → Shadow Lord | clone spawns from a per-wave composition list | **Adopt arcade promotion.** This is a substantive gameplay difference: it makes the difficulty curve responsive to play rather than scripted. |

### The two that matter most

1. **Shadow Lord scoring.** A straightforward numeric error to correct.
2. **Egg promotion.** The arcade's escalation is driven by what the player kills,
   so a wave gets harder because of how it is fought. The clone's fixed
   composition list loses that feedback loop. The Unity rebuild should implement
   promotion.

## 6. Open questions for a future pass

- Exact per-wave platform erosion order.
- Exact pterodactyl spawn timing and whether it re-spawns on a fixed interval.
- The precise rule governing which wave numbers are Survival, Egg and Gladiator
  waves, if the arcade uses a rule at all rather than a table.
- Buzzard count per wave and how it scales.

These are marked unverified above rather than guessed. Resolving them wants a
primary source: the arcade ROM disassembly, an operator manual, or frame-accurate
recorded play.

## Sources

- [Joust (video game) — Ultimate Pop Culture Wiki](https://ultimatepopculture.fandom.com/wiki/Joust_(video_game))
- [Joust — Codex Gamicus](https://gamicus.fandom.com/wiki/Joust)
- [Joust — arcade-history](https://www.arcade-history.com/game/1228/joust)
- [Joust — Museum of the Game](https://www.arcade-museum.com/Videogame/joust)
- [Joust — MobyGames](https://www.mobygames.com/game/4059/joust/)
