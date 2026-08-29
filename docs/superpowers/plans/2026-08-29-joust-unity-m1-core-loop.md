# Joust Unity M1 — Core Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A playable core loop — fly an ostrich around the arena, joust a buzzard rider by lance height, unseat it into an egg, collect the egg, die and respawn, all scored on a HUD.

**Architecture:** Unity-idiomatic MonoBehaviours and ScriptableObjects over a thin layer of pure static logic. Anything decidable without a frame — joust resolution, scoring, wrap arithmetic, spawn selection — is a pure function with EditMode tests; anything that needs physics is a MonoBehaviour with a PlayMode test. Tuning lives in a `TuningProfile` asset so feel is adjustable in the inspector during play.

**Tech Stack:** Unity 6000.5.10f1, URP 17.5.0, Input System 1.20.0, Test Framework 1.7.0, Mono scripting backend.

**Spec:** `docs/superpowers/specs/2026-08-29-joust-unity-native-design.md`
**Arcade authority:** `docs/superpowers/specs/2026-08-29-arcade-joust-reference.md`
**Spike evidence:** `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

## Global Constraints

These are carried from the M0 findings and are not optional.

- **Never gate on a Unity process exit code.** Run tests through
  `unity/tools/run-unity-tests.ps1`, which gates on the results XML.
  `Unity.exe -runTests` exits 0 when compilation fails.
- **Serialize every Unity invocation.** Unity holds an exclusive project lock;
  a test run and a screenshot capture cannot overlap.
- **Unity is Y-up.** The higher lance has the **greater** Y. The pygame
  reference is Y-down; do not copy its comparison direction.
- **Scene scale is 20 logical pixels per world unit.** The arena is 32 units
  wide, a mount is 2.0 x 1.6 units, the lava surface is y=0. Use the
  `WorldX` / `WorldY` / `Units` helpers.
- **`Object.GetInstanceID()` and `EntityId`'s implicit int conversion are
  obsolete-as-ERROR** in Unity 6000.5. Compare `EntityId` values directly.
- **Package versions come from the editor manifest** at
  `Editor/Data/Resources/PackageManager/Editor/manifest.json`, never guessed.
- **`VolumeProfile.Add<T>()` must be called with `overrides:true`**, and each
  overridden field needs `overrideState = true`, or values are silently dropped.
- **Pipeline activation is proven only** by a non-zero `m_CustomRenderPipeline`
  guid in `ProjectSettings/GraphicsSettings.asset`. Shader-name counts in a log
  prove nothing: a URP player still compiles `Hidden/Internal-Deferred*`.
- **Arcade values win over the pygame clone.** Bounder 500, Hunter 750,
  **Shadow Lord 1000** (the clone's 1500 is wrong). Eggs 250/500/750/1000.
  Three lives, extra life every 20,000.
- Scripting backend is **Mono2x** until "Windows Build Support (IL2CPP)" is
  installed through the Hub.

---

## File Structure

| Path | Responsibility |
|---|---|
| `Assets/Scripts/Runtime/Core/Tuning.cs` | `TuningProfile` ScriptableObject: all movement and combat numbers |
| `Assets/Scripts/Runtime/Core/ArenaMetrics.cs` | Pure logical-pixel ↔ world-unit conversion, shared by runtime and editor |
| `Assets/Scripts/Runtime/Movement/RiderMotor.cs` | Rigidbody flight and ground handling, driven by a `TuningProfile` |
| `Assets/Scripts/Runtime/Movement/PlayerInput.cs` | Input System bindings → motor commands |
| `Assets/Scripts/Runtime/Combat/JoustResolver.cs` | Existing pure resolver (from M0), unchanged |
| `Assets/Scripts/Runtime/Combat/Rider.cs` | Identity, lance transform, mounted state, death |
| `Assets/Scripts/Runtime/Combat/CombatContact.cs` | Trigger handling and single-owner resolution |
| `Assets/Scripts/Runtime/Enemies/BuzzardAI.cs` | Bounder-tier pursuit and flap decisions |
| `Assets/Scripts/Runtime/World/Egg.cs` | Egg fall, rest, hatch timer, collection |
| `Assets/Scripts/Runtime/World/ScreenWrap.cs` | Production wrap, from the M0 prototype |
| `Assets/Scripts/Runtime/Flow/ScoreService.cs` | Pure scoring: tiers, egg chain, extra lives |
| `Assets/Scripts/Runtime/Flow/GameDirector.cs` | Lives, respawn, wave start/clear |
| `Assets/Scripts/UI/HudController.cs` | UI Toolkit HUD: score, lives, wave |
| `Assets/Tests/EditMode/*` | Pure-logic tests |
| `Assets/Tests/PlayMode/*` | Physics and integration tests |

---

### Task 1: Arena metrics and tuning profile

**Files:**
- Create: `Assets/Scripts/Runtime/Core/ArenaMetrics.cs`
- Create: `Assets/Scripts/Runtime/Core/Tuning.cs`
- Test: `Assets/Tests/EditMode/ArenaMetricsTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `Joust.Core.ArenaMetrics` with `const float PixelsPerUnit = 20f`,
  `static float WorldX(float px)`, `static float WorldY(float px)`,
  `static float Units(float px)`, `static float ArenaHalfWidth`; and
  `Joust.Core.TuningProfile`, a ScriptableObject with public fields
  `gravity`, `flapImpulse`, `thrustAcceleration`, `maxHorizontalSpeed`,
  `airDrag`, `groundSkidDeceleration`, `tieBandUnits`.

- [ ] **Step 1: Write the failing test**

```csharp
using Joust.Core;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    public class ArenaMetricsTests
    {
        [Test]
        public void ArenaIsThirtyTwoUnitsWide()
        {
            Assert.AreEqual(16f, ArenaMetrics.ArenaHalfWidth, 1e-4f);
        }

        [Test]
        public void LeftEdgePixelMapsToLeftEdgeUnit()
        {
            Assert.AreEqual(-16f, ArenaMetrics.WorldX(0f), 1e-4f);
        }

        [Test]
        public void CentrePixelMapsToOrigin()
        {
            Assert.AreEqual(0f, ArenaMetrics.WorldX(320f), 1e-4f);
        }

        [Test]
        public void LavaLineMapsToZero()
        {
            Assert.AreEqual(0f, ArenaMetrics.WorldY(344f), 1e-4f);
        }

        [Test]
        public void HigherOnScreenIsGreaterWorldY()
        {
            Assert.Greater(ArenaMetrics.WorldY(64f), ArenaMetrics.WorldY(300f));
        }

        [Test]
        public void MountFootprintIsTwoByOnePointSix()
        {
            Assert.AreEqual(2.0f, ArenaMetrics.Units(40f), 1e-4f);
            Assert.AreEqual(1.6f, ArenaMetrics.Units(32f), 1e-4f);
        }
    }
}
```

- [ ] **Step 2: Run and confirm it fails**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: exit code 2, compiler errors naming `ArenaMetrics`.

- [ ] **Step 3: Write the implementation**

```csharp
namespace Joust.Core
{
    /// <summary>
    /// Conversion between the original game's logical pixel space (640x360,
    /// y down, lava at 344) and Unity world units (y up, lava at 0).
    /// </summary>
    public static class ArenaMetrics
    {
        public const float PixelsPerUnit = 20f;
        public const float LogicalWidth = 640f;
        public const float LogicalLavaY = 344f;

        public static float ArenaHalfWidth => LogicalWidth / 2f / PixelsPerUnit;

        public static float WorldX(float pixelX) => (pixelX - LogicalWidth / 2f) / PixelsPerUnit;

        public static float WorldY(float pixelY) => (LogicalLavaY - pixelY) / PixelsPerUnit;

        public static float Units(float pixels) => pixels / PixelsPerUnit;
    }
}
```

```csharp
using UnityEngine;

namespace Joust.Core
{
    [CreateAssetMenu(menuName = "Joust/Tuning Profile", fileName = "TuningProfile")]
    public class TuningProfile : ScriptableObject
    {
        [Header("Flight")]
        public float gravity = 24f;
        public float flapImpulse = 9f;
        public float thrustAcceleration = 17f;
        public float maxHorizontalSpeed = 12f;
        public float airDrag = 0.6f;

        [Header("Ground")]
        public float groundSkidDeceleration = 27f;

        [Header("Combat")]
        public float tieBandUnits = 0.2f;
    }
}
```

- [ ] **Step 4: Run and confirm it passes**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: `PASS`, exit 0, and the total up by 6 from M0's 16.

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: arena metrics and tuning profile"
```

---

### Task 2: Scoring, to arcade values

**Files:**
- Create: `Assets/Scripts/Runtime/Flow/ScoreService.cs`
- Test: `Assets/Tests/EditMode/ScoreServiceTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `Joust.Flow.EnemyTier` (enum: `Bounder`, `Hunter`, `ShadowLord`),
  `Joust.Flow.ScoreService` with `static int PointsFor(EnemyTier tier)`,
  `static int EggChainValue(int chainIndex)`,
  `static int ExtraLivesEarned(int previousScore, int newScore)`.

Arcade values, per the reference doc: Bounder 500, Hunter 750, Shadow Lord
**1000**. The pygame clone's 1500 for Shadow Lord is wrong and must not be
copied. Egg chain 250/500/750/1000, capping at 1000. Extra life every 20,000.

- [ ] **Step 1: Write the failing test**

```csharp
using Joust.Flow;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    public class ScoreServiceTests
    {
        [Test]
        public void TierValuesMatchTheArcade()
        {
            Assert.AreEqual(500, ScoreService.PointsFor(EnemyTier.Bounder));
            Assert.AreEqual(750, ScoreService.PointsFor(EnemyTier.Hunter));
            Assert.AreEqual(1000, ScoreService.PointsFor(EnemyTier.ShadowLord));
        }

        [Test]
        public void EggChainEscalatesThenCaps()
        {
            Assert.AreEqual(250, ScoreService.EggChainValue(0));
            Assert.AreEqual(500, ScoreService.EggChainValue(1));
            Assert.AreEqual(750, ScoreService.EggChainValue(2));
            Assert.AreEqual(1000, ScoreService.EggChainValue(3));
            Assert.AreEqual(1000, ScoreService.EggChainValue(9));
        }

        [Test]
        public void ExtraLifeEveryTwentyThousand()
        {
            Assert.AreEqual(0, ScoreService.ExtraLivesEarned(0, 19999));
            Assert.AreEqual(1, ScoreService.ExtraLivesEarned(0, 20000));
            Assert.AreEqual(1, ScoreService.ExtraLivesEarned(19000, 21000));
            Assert.AreEqual(0, ScoreService.ExtraLivesEarned(21000, 22000));
        }

        [Test]
        public void CrossingTwoThresholdsAtOnceAwardsTwo()
        {
            Assert.AreEqual(2, ScoreService.ExtraLivesEarned(0, 41000));
        }
    }
}
```

- [ ] **Step 2: Run and confirm it fails**

Expected: exit 2, compiler errors naming `ScoreService`.

- [ ] **Step 3: Write the implementation**

```csharp
namespace Joust.Flow
{
    public enum EnemyTier
    {
        Bounder,
        Hunter,
        ShadowLord
    }

    /// <summary>
    /// Scoring, to the 1982 arcade's values. Note the Shadow Lord is worth 1000,
    /// not the 1500 used by the pygame clone in this repository.
    /// </summary>
    public static class ScoreService
    {
        public const int ExtraLifeEvery = 20000;

        private static readonly int[] EggChain = { 250, 500, 750, 1000 };

        public static int PointsFor(EnemyTier tier) => tier switch
        {
            EnemyTier.Bounder => 500,
            EnemyTier.Hunter => 750,
            EnemyTier.ShadowLord => 1000,
            _ => 0
        };

        public static int EggChainValue(int chainIndex)
        {
            if (chainIndex < 0)
            {
                return 0;
            }

            return chainIndex >= EggChain.Length ? EggChain[EggChain.Length - 1] : EggChain[chainIndex];
        }

        public static int ExtraLivesEarned(int previousScore, int newScore)
        {
            return newScore / ExtraLifeEvery - previousScore / ExtraLifeEvery;
        }
    }
}
```

- [ ] **Step 4: Run and confirm it passes**

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: scoring to arcade values"
```

---

### Task 3: Rider motor

**Files:**
- Create: `Assets/Scripts/Runtime/Movement/RiderMotor.cs`
- Test: `Assets/Tests/PlayMode/RiderMotorTests.cs`

**Interfaces:**
- Consumes: `Joust.Core.TuningProfile`.
- Produces: `Joust.Movement.RiderMotor`, a `MonoBehaviour` with
  `void Configure(TuningProfile profile)`, `void SetThrust(float direction)`
  (-1, 0 or 1), `void Flap()`, `bool Grounded { get; }`,
  `Vector2 Velocity { get; }`.

Physics rules: `useGravity` false with gravity applied in `FixedUpdate`;
constraints `FreezePositionZ | FreezeRotation`; interpolation on. Flap sets
vertical velocity outright rather than adding force, so repeated flaps do not
accumulate unbounded climb — this is what makes arcade flap feel controllable.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections;
using Joust.Core;
using Joust.Movement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    public class RiderMotorTests
    {
        private static RiderMotor Spawn()
        {
            var go = new GameObject("rider");
            go.AddComponent<Rigidbody>();
            var motor = go.AddComponent<RiderMotor>();
            var profile = ScriptableObject.CreateInstance<TuningProfile>();
            motor.Configure(profile);
            return motor;
        }

        [UnityTest]
        public IEnumerator FallsUnderGravityWhenAirborne()
        {
            var motor = Spawn();
            var startY = motor.transform.position.y;
            for (var i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.Less(motor.transform.position.y, startY);
            Object.Destroy(motor.gameObject);
        }

        [UnityTest]
        public IEnumerator FlapProducesUpwardVelocity()
        {
            var motor = Spawn();
            motor.Flap();
            yield return new WaitForFixedUpdate();
            Assert.Greater(motor.Velocity.y, 0f);
            Object.Destroy(motor.gameObject);
        }

        [UnityTest]
        public IEnumerator RepeatedFlapsDoNotAccumulateSpeed()
        {
            var motor = Spawn();
            motor.Flap();
            yield return new WaitForFixedUpdate();
            var first = motor.Velocity.y;
            motor.Flap();
            motor.Flap();
            yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(motor.Velocity.y, first + 0.01f);
            Object.Destroy(motor.gameObject);
        }

        [UnityTest]
        public IEnumerator ThrustIsClampedToMaxSpeed()
        {
            var motor = Spawn();
            motor.SetThrust(1f);
            for (var i = 0; i < 200; i++) yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(Mathf.Abs(motor.Velocity.x), 12.01f);
            Object.Destroy(motor.gameObject);
        }

        [UnityTest]
        public IEnumerator MovementStaysOnThePlane()
        {
            var motor = Spawn();
            motor.SetThrust(1f);
            motor.Flap();
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, motor.transform.position.z, 1e-3f);
            Object.Destroy(motor.gameObject);
        }
    }
}
```

- [ ] **Step 2: Run PlayMode and confirm it fails**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform PlayMode
```

- [ ] **Step 3: Write the implementation**

```csharp
using Joust.Core;
using UnityEngine;

namespace Joust.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class RiderMotor : MonoBehaviour
    {
        [SerializeField] private TuningProfile profile;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody _body;
        private float _thrust;
        private bool _flapQueued;

        public bool Grounded { get; private set; }

        public Vector2 Velocity => new Vector2(_body.linearVelocity.x, _body.linearVelocity.y);

        public void Configure(TuningProfile tuning) => profile = tuning;

        public void SetThrust(float direction) => _thrust = Mathf.Clamp(direction, -1f, 1f);

        public void Flap() => _flapQueued = true;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            if (profile == null)
            {
                return;
            }

            Grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayers);

            var velocity = _body.linearVelocity;

            if (_flapQueued)
            {
                // Set, not add: repeated flaps must not accumulate climb.
                velocity.y = profile.flapImpulse;
                Grounded = false;
                _flapQueued = false;
            }

            if (Mathf.Abs(_thrust) > 0.01f)
            {
                velocity.x += _thrust * profile.thrustAcceleration * Time.fixedDeltaTime;
            }
            else
            {
                var decel = Grounded ? profile.groundSkidDeceleration : profile.airDrag;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, decel * Time.fixedDeltaTime);
            }

            velocity.x = Mathf.Clamp(velocity.x, -profile.maxHorizontalSpeed, profile.maxHorizontalSpeed);

            if (!Grounded)
            {
                velocity.y -= profile.gravity * Time.fixedDeltaTime;
            }
            else if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            _body.linearVelocity = velocity;
        }
    }
}
```

- [ ] **Step 4: Run PlayMode and confirm it passes**

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: rider motor driven by tuning profile"
```

---

### Task 4: Rider identity and combat contact

**Files:**
- Create: `Assets/Scripts/Runtime/Combat/Rider.cs`
- Create: `Assets/Scripts/Runtime/Combat/CombatContact.cs`
- Test: `Assets/Tests/PlayMode/CombatContactTests.cs`

**Interfaces:**
- Consumes: `Joust.Combat.JoustResolver`, `Joust.Combat.JoustOutcome` (M0).
- Produces: `Joust.Combat.Rider` with `Transform Lance`, `bool Mounted`,
  `EnemyTier Tier`, `event Action<Rider> Unseated`, `void Unseat()`; and
  `Joust.Combat.CombatContact` raising `event Action<Rider, Rider> Resolved`
  (winner, loser) exactly once per overlapping pair.

Ownership guard, from M0: only the participant with the lower `EntityId`
resolves. Do not call `GetInstanceID()`; it is obsolete-as-error.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections;
using Joust.Combat;
using Joust.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    public class CombatContactTests
    {
        private static Rider Spawn(string name, Vector3 position, float lanceHeight)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 1f;
            collider.isTrigger = true;

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var lance = new GameObject("lance").transform;
            lance.SetParent(go.transform, false);
            lance.localPosition = new Vector3(0f, lanceHeight, 0f);

            var rider = go.AddComponent<Rider>();
            rider.Configure(lance, EnemyTier.Bounder);
            go.AddComponent<CombatContact>().Configure(rider, 0.2f);
            return rider;
        }

        [UnityTest]
        public IEnumerator HigherLanceUnseatsLower()
        {
            var high = Spawn("high", Vector3.zero, 1.5f);
            var low = Spawn("low", new Vector3(0.3f, 0f, 0f), 0.2f);

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(high.Mounted, "the higher rider should stay mounted");
            Assert.IsFalse(low.Mounted, "the lower rider should be unseated");

            Object.Destroy(high.gameObject);
            Object.Destroy(low.gameObject);
        }

        [UnityTest]
        public IEnumerator EqualHeightsUnseatNobody()
        {
            var a = Spawn("a", Vector3.zero, 1f);
            var b = Spawn("b", new Vector3(0.3f, 0f, 0f), 1.05f);

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(a.Mounted);
            Assert.IsTrue(b.Mounted);

            Object.Destroy(a.gameObject);
            Object.Destroy(b.gameObject);
        }
    }
}
```

- [ ] **Step 2: Run PlayMode and confirm it fails**

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using Joust.Flow;
using UnityEngine;

namespace Joust.Combat
{
    public class Rider : MonoBehaviour
    {
        [SerializeField] private Transform lance;
        [SerializeField] private EnemyTier tier = EnemyTier.Bounder;

        public Transform Lance => lance;
        public EnemyTier Tier => tier;
        public bool Mounted { get; private set; } = true;

        public event Action<Rider> Unseated;

        public void Configure(Transform lanceTransform, EnemyTier riderTier)
        {
            lance = lanceTransform;
            tier = riderTier;
        }

        public void Unseat()
        {
            if (!Mounted)
            {
                return;
            }

            Mounted = false;
            Unseated?.Invoke(this);
        }
    }
}
```

```csharp
using System;
using UnityEngine;

namespace Joust.Combat
{
    [RequireComponent(typeof(Collider))]
    public class CombatContact : MonoBehaviour
    {
        [SerializeField] private Rider rider;
        [SerializeField] private float tieBand = 0.2f;

        public event Action<Rider, Rider> Resolved;

        public void Configure(Rider owner, float tieBandUnits)
        {
            rider = owner;
            tieBand = tieBandUnits;
        }

        /// <summary>
        /// OnTriggerEnter fires on both colliders, so exactly one participant
        /// owns the resolution. Compared as EntityId: GetInstanceID() and
        /// EntityId's int conversion are both obsolete-as-error in Unity 6000.5.
        /// </summary>
        public static bool ShouldResolve<T>(T self, T other) where T : IComparable<T>
        {
            return self.CompareTo(other) < 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            var opponent = other.GetComponentInParent<CombatContact>();
            if (opponent == null || opponent == this || rider == null || opponent.rider == null)
            {
                return;
            }

            if (!ShouldResolve(GetEntityId(), opponent.GetEntityId()))
            {
                return;
            }

            var outcome = JoustResolver.Resolve(
                rider.Lance.position.y, opponent.rider.Lance.position.y, tieBand);

            switch (outcome)
            {
                case JoustOutcome.AWins:
                    opponent.rider.Unseat();
                    Resolved?.Invoke(rider, opponent.rider);
                    break;
                case JoustOutcome.BWins:
                    rider.Unseat();
                    Resolved?.Invoke(opponent.rider, rider);
                    break;
                default:
                    // Tie: both bounce, neither is unseated.
                    break;
            }
        }
    }
}
```

- [ ] **Step 4: Run PlayMode and confirm it passes**

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: rider identity and single-owner combat contact"
```

---

### Task 5: Eggs, with arcade promotion

**Files:**
- Create: `Assets/Scripts/Runtime/World/Egg.cs`
- Create: `Assets/Scripts/Runtime/World/EggRules.cs`
- Test: `Assets/Tests/EditMode/EggRulesTests.cs`

**Interfaces:**
- Consumes: `Joust.Flow.EnemyTier`.
- Produces: `Joust.World.EggRules.Promote(EnemyTier tier)` returning the tier a
  hatching egg produces, and `Joust.World.Egg`, a `MonoBehaviour` with
  `float HatchSeconds`, `event Action<Egg> Hatched`, `void Collect()`.

**This is the arcade behaviour the pygame clone lacks.** A defeated rider's egg
hatches one tier stronger: Bounder → Hunter → Shadow Lord → Shadow Lord. Wave
difficulty therefore responds to how the player fights, rather than following a
fixed composition list.

- [ ] **Step 1: Write the failing test**

```csharp
using Joust.Flow;
using Joust.World;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    public class EggRulesTests
    {
        [Test]
        public void BounderPromotesToHunter()
        {
            Assert.AreEqual(EnemyTier.Hunter, EggRules.Promote(EnemyTier.Bounder));
        }

        [Test]
        public void HunterPromotesToShadowLord()
        {
            Assert.AreEqual(EnemyTier.ShadowLord, EggRules.Promote(EnemyTier.Hunter));
        }

        [Test]
        public void ShadowLordStaysShadowLord()
        {
            Assert.AreEqual(EnemyTier.ShadowLord, EggRules.Promote(EnemyTier.ShadowLord));
        }
    }
}
```

- [ ] **Step 2: Run EditMode and confirm it fails**

- [ ] **Step 3: Write the implementation**

```csharp
using Joust.Flow;

namespace Joust.World
{
    /// <summary>
    /// Arcade rule: a hatching egg produces a rider one tier stronger than the
    /// one that laid it, capping at Shadow Lord. This makes the difficulty curve
    /// respond to play, and is the behaviour the pygame clone in this repository
    /// does not have.
    /// </summary>
    public static class EggRules
    {
        public static EnemyTier Promote(EnemyTier tier) => tier switch
        {
            EnemyTier.Bounder => EnemyTier.Hunter,
            EnemyTier.Hunter => EnemyTier.ShadowLord,
            _ => EnemyTier.ShadowLord
        };
    }
}
```

```csharp
using System;
using Joust.Flow;
using UnityEngine;

namespace Joust.World
{
    [RequireComponent(typeof(Rigidbody))]
    public class Egg : MonoBehaviour
    {
        [SerializeField] private float hatchSeconds = 8f;

        private float _age;
        private bool _collected;

        public EnemyTier Tier { get; private set; }

        public event Action<Egg> Hatched;
        public event Action<Egg> Collected;

        public void Configure(EnemyTier layingTier) => Tier = layingTier;

        public void Collect()
        {
            if (_collected)
            {
                return;
            }

            _collected = true;
            Collected?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (_collected)
            {
                return;
            }

            _age += Time.deltaTime;
            if (_age < hatchSeconds)
            {
                return;
            }

            Hatched?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 4: Run EditMode and confirm it passes**

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: eggs with arcade tier promotion"
```

---

### Task 6: Bounder AI

**Files:**
- Create: `Assets/Scripts/Runtime/Enemies/BuzzardAI.cs`
- Test: `Assets/Tests/EditMode/BuzzardDecisionTests.cs`

**Interfaces:**
- Consumes: `Joust.Movement.RiderMotor`.
- Produces: `Joust.Enemies.BuzzardDecision`, a pure static decision function
  `static Decision Decide(Vector2 self, Vector2 target, float lavaLine, float random01)`
  returning a struct with `float Thrust` and `bool Flap`; and
  `Joust.Enemies.BuzzardAI`, the MonoBehaviour that applies it.

Splitting the decision out as a pure function is deliberate: it makes AI
behaviour testable without a scene, which is the M0 lesson about pure functions
beside thin MonoBehaviours.

- [ ] **Step 1: Write the failing test**

```csharp
using Joust.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Joust.Tests.EditMode
{
    public class BuzzardDecisionTests
    {
        [Test]
        public void FlapsWhenBelowTheLavaLine()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 0.5f), new Vector2(0f, 8f), 1.5f, 0.9f);
            Assert.IsTrue(d.Flap, "a rider below the lava line must always flap");
        }

        [Test]
        public void ThrustsTowardTheTarget()
        {
            var right = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(6f, 5f), 1.5f, 0.5f);
            Assert.Greater(right.Thrust, 0f);

            var left = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(-6f, 5f), 1.5f, 0.5f);
            Assert.Less(left.Thrust, 0f);
        }

        [Test]
        public void FlapsWhenTheTargetIsAbove()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 3f), new Vector2(0f, 9f), 1.5f, 0.1f);
            Assert.IsTrue(d.Flap);
        }

        [Test]
        public void DoesNotAlwaysFlapWhenLevelWithTheTarget()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(3f, 5f), 1.5f, 0.99f);
            Assert.IsFalse(d.Flap);
        }
    }
}
```

- [ ] **Step 2: Run EditMode and confirm it fails**

- [ ] **Step 3: Write the implementation**

```csharp
using UnityEngine;

namespace Joust.Enemies
{
    public struct Decision
    {
        public float Thrust;
        public bool Flap;
    }

    /// <summary>
    /// Bounder-tier decision making, as a pure function so it can be tested
    /// without a scene.
    /// </summary>
    public static class BuzzardDecision
    {
        private const float FlapChanceWhenLevel = 0.45f;

        public static Decision Decide(Vector2 self, Vector2 target, float lavaLine, float random01)
        {
            var decision = new Decision
            {
                Thrust = Mathf.Approximately(target.x, self.x) ? 0f : Mathf.Sign(target.x - self.x)
            };

            if (self.y <= lavaLine)
            {
                decision.Flap = true;
                return decision;
            }

            if (target.y > self.y + 0.5f)
            {
                decision.Flap = true;
                return decision;
            }

            decision.Flap = random01 < FlapChanceWhenLevel;
            return decision;
        }
    }
}
```

```csharp
using Joust.Core;
using Joust.Movement;
using UnityEngine;

namespace Joust.Enemies
{
    [RequireComponent(typeof(RiderMotor))]
    public class BuzzardAI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float decisionInterval = 0.25f;

        private RiderMotor _motor;
        private float _nextDecision;

        public void Configure(Transform pursue) => target = pursue;

        private void Awake() => _motor = GetComponent<RiderMotor>();

        private void Update()
        {
            if (target == null || Time.time < _nextDecision)
            {
                return;
            }

            _nextDecision = Time.time + decisionInterval;

            var decision = BuzzardDecision.Decide(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(target.position.x, target.position.y),
                ArenaMetrics.Units(60f),
                Random.value);

            _motor.SetThrust(decision.Thrust);
            if (decision.Flap)
            {
                _motor.Flap();
            }
        }
    }
}
```

- [ ] **Step 4: Run EditMode and confirm it passes**

- [ ] **Step 5: Commit**

```bash
git add unity/JoustUnity
git commit -m "feat: bounder AI with a pure decision function"
```

---

### Task 7: Game director, HUD, and the playable loop

**Files:**
- Create: `Assets/Scripts/Runtime/Flow/GameDirector.cs`
- Create: `Assets/Scripts/UI/HudController.cs`
- Create: `Assets/UI/Hud.uxml`, `Assets/UI/Hud.uss`
- Create: `Assets/Editor/M1SceneBuilder.cs`
- Test: `Assets/Tests/PlayMode/GameLoopTests.cs`

**Interfaces:**
- Consumes: everything above.
- Produces: `Joust.Flow.GameDirector` with `int Score`, `int Lives`,
  `void AwardKill(EnemyTier tier)`, `void AwardEgg()`, `void LoseLife()`,
  `event Action GameOver`; and `Joust.Editor.M1SceneBuilder.BuildM1Scene()`
  producing `Assets/Scenes/Game.unity` with the gritty arena, one player, one
  bounder, and the HUD.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections;
using Joust.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    public class GameLoopTests
    {
        [UnityTest]
        public IEnumerator KillsAndEggsAccumulateScore()
        {
            var director = new GameObject("director").AddComponent<GameDirector>();
            yield return null;

            director.AwardKill(EnemyTier.Bounder);
            director.AwardEgg();

            Assert.AreEqual(750, director.Score);
            Object.Destroy(director.gameObject);
        }

        [UnityTest]
        public IEnumerator EggChainEscalatesWithinAWave()
        {
            var director = new GameObject("director").AddComponent<GameDirector>();
            yield return null;

            director.AwardEgg();
            director.AwardEgg();

            Assert.AreEqual(750, director.Score, "250 then 500");
            Object.Destroy(director.gameObject);
        }

        [UnityTest]
        public IEnumerator LivesRunOutAndRaiseGameOver()
        {
            var director = new GameObject("director").AddComponent<GameDirector>();
            yield return null;

            var over = false;
            director.GameOver += () => over = true;

            director.LoseLife();
            director.LoseLife();
            director.LoseLife();

            Assert.AreEqual(0, director.Lives);
            Assert.IsTrue(over);
            Object.Destroy(director.gameObject);
        }

        [UnityTest]
        public IEnumerator CrossingTwentyThousandAwardsALife()
        {
            var director = new GameObject("director").AddComponent<GameDirector>();
            yield return null;

            for (var i = 0; i < 40; i++) director.AwardKill(EnemyTier.ShadowLord);

            Assert.AreEqual(40000, director.Score);
            Assert.AreEqual(5, director.Lives, "three starting lives plus two awarded");
            Object.Destroy(director.gameObject);
        }
    }
}
```

- [ ] **Step 2: Run PlayMode and confirm it fails**

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using UnityEngine;

namespace Joust.Flow
{
    public class GameDirector : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;

        private int _eggChainIndex;

        public int Score { get; private set; }
        public int Lives { get; private set; }

        public event Action GameOver;
        public event Action<int> ScoreChanged;
        public event Action<int> LivesChanged;

        private void Awake()
        {
            Lives = startingLives;
        }

        public void AwardKill(EnemyTier tier) => Add(ScoreService.PointsFor(tier));

        public void AwardEgg()
        {
            Add(ScoreService.EggChainValue(_eggChainIndex));
            _eggChainIndex++;
        }

        /// <summary>The egg chain resets when the player lands or dies.</summary>
        public void ResetEggChain() => _eggChainIndex = 0;

        public void LoseLife()
        {
            if (Lives <= 0)
            {
                return;
            }

            Lives--;
            ResetEggChain();
            LivesChanged?.Invoke(Lives);

            if (Lives == 0)
            {
                GameOver?.Invoke();
            }
        }

        private void Add(int points)
        {
            var previous = Score;
            Score += points;
            Lives += ScoreService.ExtraLivesEarned(previous, Score);
            ScoreChanged?.Invoke(Score);
            LivesChanged?.Invoke(Lives);
        }
    }
}
```

The HUD is a UI Toolkit document showing score, lives and wave, bound to
`GameDirector`'s events. `M1SceneBuilder` assembles the gritty arena from
`GrittyArenaBuilder`'s helpers plus a player rider, one bounder, and the HUD.

- [ ] **Step 4: Run both suites and confirm they pass**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform PlayMode
```

- [ ] **Step 5: Build the scene and capture a screenshot**

Run the scene builder, then the screenshot tool, **serially** — Unity holds an
exclusive project lock.

- [ ] **Step 6: Build the player and smoke it**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/build-windows.ps1
```

Then ask the user to play it: fly, joust the bounder, collect the egg, die three
times. This is the M1 gate and cannot be automated.

- [ ] **Step 7: Publish and commit**

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/publish-progress.ps1
git add unity/JoustUnity site
git commit -m "feat: playable core loop with HUD"
```

---

## Definition of done

- EditMode and PlayMode suites green through `run-unity-tests.ps1`.
- A Windows player builds and the user confirms the loop plays: fly, joust,
  unseat, collect, die, respawn, game over.
- Flight tuning accepted by the user and saved into the `TuningProfile` asset
  (this also closes M0's F5).
- Score values match the arcade reference, including Shadow Lord at 1000.
- Eggs promote on hatch.
- Progress site updated with an M1 screenshot.


---

## Known defects at the end of Task 7

Recorded rather than left for someone to rediscover. Neither blocks play; both
belong to M3 (presentation).

**1. The bounder renders cream despite a red material.**
`bounder_plumage.mat` carries `_BaseColor 0.66, 0.15, 0.11` and the game scene
references its guid three times (mount, head, lance shaft), verified by reading
the scene file. It still renders the same near-white as the player. Three
hypotheses were tested and eliminated:

- *Material asset collapse* — disproved: the two plumage materials exist as
  separate assets with distinct guids and distinct reference counts.
- *Saturated key light washing colour out* — this genuinely caused an earlier
  all-blue frame, but the light is now near-white at moderate intensity.
- *Bloom blowout* — halving bloom intensity and raising its threshold changed
  the frame not at all.

Cause unknown. Next thing to try: read the renderer's material binding at
runtime rather than from the scene file, since a serialized reference and a
live binding are not the same claim.

**2. No post-processing in the built scene.**
`Assets/Settings/GrittyVolume.asset` does not exist after a scene build, so the
Volume component has no profile and the colour grading, vignette, film grain and
tonemapping authored in M0 are not running. The screenshots throughout M0 and M1
therefore show raw lit output, not the intended grade.

This also means F8's screenshots in the M0 findings are weaker evidence for the
art direction than they appear: they show the lighting and materials, but never
the post stack.
