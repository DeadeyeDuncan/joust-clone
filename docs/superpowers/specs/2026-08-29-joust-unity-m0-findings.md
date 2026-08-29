# Joust Unity M0 — Verification Spike Findings

Date: 2026-08-29
Plan: `docs/superpowers/plans/2026-08-29-joust-unity-m0-spike.md`
Editor: Unity 6000.5.10f1 at `G:\UnityEditors\6000.5.10f1\Editor\Unity.exe`

Every entry below records a command that was actually run, the output that was
actually observed, and a verdict. Claims without an observed command are not
findings.

## F1 — Project creation

**Command**

```
"G:/UnityEditors/6000.5.10f1/Editor/Unity.exe" -createProject "I:/Joust/unity/JoustUnity" \
  -batchmode -quit -nographics -logFile "I:/Joust/unity/artifacts/create.log"
```

**Observed:** exit code 0. `ProjectVersion.txt` reads
`m_EditorVersion: 6000.5.10f1`, `m_EditorVersionWithRevision: 6000.5.10f1 (3bd4f66ad299)`.
Top-level directories created: `Assets`, `Library`, `Logs`, `Packages`,
`ProjectSettings`, `UserSettings`.

**Verdict: HELD.** `-createProject` with `-batchmode -quit -nographics` creates a
complete project non-interactively and exits cleanly. No Unity Hub involvement is
required, which matters because this machine has no Hub configuration on disk —
editors live at `G:\UnityEditors\<version>\`.

**Unplanned finding — the default manifest is barer than the plan assumed.**
`Packages/manifest.json` contains only `com.unity.multiplayer.center` and the
built-in `com.unity.modules.*` set. It does **not** contain
`com.unity.test-framework`, which the plan's Task 3 assumed was present when it
referenced `UnityEngine.TestRunner` and `UnityEditor.TestRunner` from the test
assembly definition. Consequence: Task 3 must add `com.unity.test-framework`
to the manifest before the test assembly can compile. Adding it also brings
`com.unity.ext.nunit`, which supplies `nunit.framework.dll`.

**Verification note.** Project existence was confirmed through PowerShell, not
the Bash tool. On this machine the Bash tool runs inside a virtualized
app-container filesystem, so a process it launches can appear to have written
files that are not on the real disk. Any filesystem claim about a Unity artifact
in this document was checked from PowerShell.

## F2 — URP activation

**Setup.** `com.unity.render-pipelines.universal` pinned to `17.5.0`, the version
this editor bundles, read from
`Editor/Data/Resources/PackageManager/Editor/manifest.json` rather than guessed.
`Assets/Editor/UrpSetup.cs` creates the renderer and pipeline assets and assigns
them.

**Command**

```
"G:/UnityEditors/6000.5.10f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "I:/Joust/unity/JoustUnity" \
  -executeMethod Joust.Editor.UrpSetup.ConfigureUrp \
  -logFile "I:/Joust/unity/artifacts/urp-setup.log"
```

**Observed:** exit code 0, zero `error CS` lines, and the log line

```
URP configured. defaultRenderPipeline=JoustURP (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)
```

**The activation gate, checked four ways:**

| Check | Result |
|---|---|
| `GraphicsSettings.asset` before | `m_CustomRenderPipeline: {fileID: 0}` — Built-in |
| `GraphicsSettings.asset` after | `{fileID: 11400000, guid: 3ef9610330c50e4438a862cd96b190fd, type: 2}` |
| guid matches `JoustURP.asset.meta` | yes — `guid: 3ef9610330c50e4438a862cd96b190fd` |
| `QualitySettings.asset` | all six levels carry `customRenderPipeline` with the same guid |
| URP shader lines in log | 21 |
| Built-in deferred shader lines in log | 0 |

**Verdict: HELD.** The pipeline is genuinely active, not merely installed. The
before-and-after on `m_CustomRenderPipeline` is the evidence that matters: the
package alone left it at `fileID: 0`.

**API note.** `UniversalRenderPipelineAsset.Create(UniversalRendererData)` is
public and works in URP 17.5.0. The planned fallback — `CreateInstance` plus a
`SerializedObject` write to `m_RendererDataList` — was not needed.

**Caveat on the shader-name check.** The 21 URP shader lines in an editor setup
log are weaker evidence than they look; the editor loads URP shaders as soon as
the package resolves, whether or not the pipeline is active. The shader-name
check is only decisive in a *player build* log, where the built player embeds one
shader family or the other. F9 carries the decisive form of this check. The
settings-asset comparison above is what actually proves activation here.

## F3 — Headless EditMode tests

**Setup.** `com.unity.test-framework@1.7.0` added to the manifest (see F1 — it is
not in the default project). `Joust.Runtime` and `Joust.Tests.EditMode` assembly
definitions created, seven tests written against
`Joust.Combat.JoustResolver.Resolve`.

**Command**

```
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

**Red then green.** Before `JoustResolver.cs` existed the run failed with
`error CS0234: The type or namespace name 'Combat' does not exist in the
namespace 'Joust'`. After the implementation landed:

```
platform=EditMode total=7 passed=7 failed=0 result=Passed unityExit=0
PASS
```

Test execution time was 0.022 seconds for the seven cases. The Unity startup and
domain reload around it dominate wall-clock, but the suite itself is effectively
free, so EditMode tests can gate every M1 change.

**Verdict: HELD, but only after the runner was hardened three times.** Headless
EditMode testing works. The naive invocation from the plan did not.

### Three runner hazards, each found by execution

**1. `-runTests` exits 0 when compilation fails.** The first red run printed
`Aborting batchmode due to failure: Scripts have compiler errors` to stdout,
wrote no results XML, and still exited 0. A gate keyed on the process exit code
would have reported a broken build as passing. The exit code is not a usable
signal.

**2. Stale artifacts are read as the current result.** The first hardened wrapper
deleted the stale results XML but not the stale log, then gated on a log grep.
A genuinely passing run was reported as failing, using the *previous* run's
compiler errors. The same defect in the other direction would report a failing
run as passing.

**3. `& $editor ...` does not reliably block.** One invocation returned with
`$LASTEXITCODE` empty and no artifacts on disk; both artifacts appeared a minute
later, written by the Unity process the wrapper had already stopped waiting for.
The wrapper was reading a run that was still in progress.

### The gate that survived

`unity/tools/run-unity-tests.ps1` now:

- deletes both the stale XML and the stale log before running;
- launches via `Start-Process -Wait -PassThru`, which blocks until the editor
  process exits and returns a real exit code;
- treats the **results XML as authoritative** — it must exist, report
  `total > 0`, and report `failed == 0`;
- reads the log only to explain a failure, never to decide one;
- returns distinct exit codes so a failure mode is identifiable without opening
  anything: `2` compile errors, `3` no results file, `4` zero tests, `5` test
  failures.

**Consequence for M1.** Every milestone gate must call this wrapper and check its
exit code, never invoke `Unity.exe -runTests` directly. Any CI or hook that
shells out to Unity and trusts the process exit code is broken by construction on
this editor version.

## F4 — Headless PlayMode tests

**Setup.** `com.unity.inputsystem@1.20.0` added and
`ProjectSettings.asset` `activeInputHandler` changed from `0` (old input manager)
to `1` (new Input System only), edited directly in the settings asset before the
editor was launched, so no interactive restart prompt was involved. A PlayMode
assembly and one physics smoke test were added: a `Rigidbody` with `useGravity`
disabled, driven by `AddForce(ForceMode.Acceleration)` across 30 fixed steps,
asserting the body moved downward.

**Commands**

```
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform PlayMode
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

**Observed**

```
platform=PlayMode total=1 passed=1 failed=0 result=Passed unityExit=0
platform=EditMode total=7 passed=7 failed=0 result=Passed unityExit=0
```

PlayMode test duration 0.628 s; EditMode still 7/7 after the Input System
install, so the package did not break compilation.

**Verdict: HELD, and better than the plan assumed.** Headless PlayMode tests run
on this machine. The wrapper passes `-batchmode` without `-nographics`, which is
what makes it work — a PlayMode run needs a graphics device even when no window
is shown.

**Consequence for M1.** The plan's fallback — gating physics changes on manual
controller smoke because automated PlayMode might be unavailable — is not needed.
M1 can gate flight, landing, wrap and collision behaviour with automated PlayMode
tests. Manual smoke is still required for *feel*, which no assertion captures,
but correctness is automatable.

**Version note.** The plan pinned `com.unity.inputsystem` at `1.14.2`. This
editor declares a minimum of `1.20.0` in
`Editor/Data/Resources/PackageManager/Editor/manifest.json`, so the planned pin
was below the floor and `1.20.0` was used instead. Package versions in the M1
plan should be read from that editor manifest rather than assumed.

## F5 — Arcade flight feel

## F6 — Lance-height resolution and double-resolution hazard

**The hazard.** `OnTriggerEnter` fires on *both* colliders of a pair, so a naive
handler resolves the same duel twice and can kill both riders. The plan proposed
checking this by watching the console for duplicate log lines. That is not
evidence, so it was replaced with automated tests.

**What was built.** `JoustContact` guards resolution with an ownership rule:
of the two participants, only the one with the lower entity id resolves. Three
EditMode tests pin the rule itself; two PlayMode tests drive real physics —
two overlapping trigger colliders with kinematic rigidbodies — and assert the
observable outcome.

**Observed**

```
platform=EditMode total=10 passed=10 failed=0 result=Passed
platform=PlayMode total=3  passed=3  failed=0 result=Passed
```

The decisive assertions: `OverlappingRidersResolveExactlyOnce` asserts
`JoustContact.ResolutionCount == 1` after one overlap, and
`TheHigherLanceIsRecordedAsTheWinner` asserts the rider with the higher lance is
the recorded winner.

**Verdict: HELD.** One overlap produces exactly one resolution, and the
lance-height rule decides it. The design of resolving combat by explicit height
comparison rather than collider geometry works under real physics.

### Unplanned finding — two Unity 6000.5 API breaks, both obsolete-as-ERROR

`Object.GetInstanceID()` does not merely warn in this editor version, it fails
the build:

```
error CS0619: 'Object.GetInstanceID()' is obsolete: 'Use GetEntityId instead.'
```

The obvious fix — casting the replacement to `int` — fails the same way:

```
error CS0619: 'EntityId.implicit operator int(EntityId)' is obsolete:
'EntityId will not be representable by an int in the future.'
```

The resolution was to make the ownership rule generic,
`ShouldResolve<T>(T self, T other) where T : IComparable<T>`, so the runtime
compares `EntityId` values directly while the tests still exercise the rule with
plain ints. **Consequence for M1:** any code ported or generated from
pre-Unity-6 examples that calls `GetInstanceID()` will not compile, and entity
ids must not be stored or compared as `int`.

### Unplanned finding — Unity holds an exclusive project lock

Two Unity invocations against the same project cannot overlap:

```
Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.
```

This was hit by launching a screenshot capture while a test run was still
active. **Consequence for M1:** every Unity invocation must be serialized. A CI
pipeline that runs tests and captures screenshots in parallel against one project
directory will fail intermittently; either serialize the steps or give each a
separate project copy.

## F7 — Screen wrap

**What was built.** `ScreenWrapPrototype` teleports an entity across the arena
bounds and maintains a ghost copy near the seam so the crossing is not visible
as a pop. The wrap arithmetic was pulled out as a pure static method,
`WrapX(float x, float halfWidth)`, so it is testable without physics or a scene.

**Observed**

```
platform=EditMode total=16 passed=16 failed=0 result=Passed unityExit=0
```

Six of those tests cover wrap: positions inside bounds unchanged, both edge
crossings, idempotency (wrapping an already-wrapped value is a no-op), a
position many arena widths away still landing inside bounds, and a zero-width
arena not dividing by zero.

**Verdict: HELD for the arithmetic; the seam visual is still pending.** The
teleport is proven correct by test. Whether the ghost genuinely hides the seam
is a visual judgement that no assertion captures, and is folded into the same
play session as F5.

**Design note carried into M1.** Extracting `WrapX` as a pure function was worth
doing: it turned an untestable `LateUpdate` behaviour into six fast EditMode
tests. The same split — pure arithmetic beside a thin MonoBehaviour that applies
it — is the pattern M1 should follow for movement and combat, and it is what
makes an idiomatic Unity build testable without resorting to slow PlayMode tests
for everything.

## F8 — Asset pack import and animation

### Screenshot capture capability (built alongside)

`Assets/Editor/ScreenshotTool.cs` renders a scene camera to a PNG from the
command line, with `-scene`, `-output`, `-width` and `-height` arguments. It must
run **without** `-nographics`: rendering needs a graphics device even when no
window is shown.

```
Unity.exe -batchmode -quit -projectPath <project>   -executeMethod Joust.Editor.ScreenshotTool.CaptureFromCommandLine   -scene Assets/Scenes/Spike.unity -output <path>.png -width 1280 -height 720
```

**Observed:** `screenshot written to ...m0-spike-scene.png (1280x720)`, and the
image shows the URP sky gradient, a lit capsule and three platforms — correctly
lit and **not magenta**. Magenta is the near-universal signature of a shader that
failed to load, so a correctly lit render is the visual complement to F2: URP is
not merely assigned in settings, it is actually rendering geometry.

Screenshots are written to `unity/artifacts/screenshots/` and published to the
progress site by `unity/tools/build-progress-site.py`.

## F9 — Windows player build

## Verdict

## Consequences for the M1 plan
