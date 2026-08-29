# Joust Unity M0 — Verification Spike Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove every engine assumption behind the Joust Unity rebuild by execution — URP actually rendering, headless tests runnable, arcade flight feeling right, lance-height combat resolving once per pair, screen wrap invisible, an asset pack importing and animating, and a Windows player building — and record the results in a findings document.

**Architecture:** A new Unity 6 project at `unity/JoustUnity`, configured from the command line and by editor scripts rather than by hand, so every configuration step is diffable and repeatable. Gameplay code written in this milestone is prototype-grade and explicitly labelled throwaway, except the pure-logic combat resolver and the project/CI scaffolding, which carry forward into M1.

**Tech Stack:** Unity 6000.5.10f1 (`G:\UnityEditors\6000.5.10f1\Editor\Unity.exe`), URP, Unity Test Framework (NUnit), Unity Input System, IL2CPP Windows standalone.

**Spec:** `docs/superpowers/specs/2026-08-29-joust-unity-native-design.md`

## Global Constraints

- Unity editor version is exactly `6000.5.10f1`. Editor path: `G:\UnityEditors\6000.5.10f1\Editor\Unity.exe`.
- Unity project root is `unity/JoustUnity/` inside this repository. The Python implementation in `joust/`, `tools/`, `tests/` and `assets/` is never modified by this plan.
- Gameplay space is 2.5D: movement is confined to the XY plane, Z is used only for visual depth.
- Unity is Y-up, whereas the Python reference is Y-down. In all Unity code the **higher** lance is the **greater** Y. Do not copy the Python comparison direction.
- The worker never launches the Unity editor, `Unity.exe`, or a built player, in any form, not once, not to check. Authoring only. Every Unity invocation in this plan is run by the controller, who records the real output. See `AUTHORING-ONLY` notes on each task.
- A render pipeline package being present in `Packages/manifest.json` does not mean URP is active. Activation is proven only by a non-zero `m_CustomRenderPipeline` in `ProjectSettings/GraphicsSettings.asset` plus `Universal Render Pipeline/` shader names in a build log.
- `VolumeProfile.Add<T>()` defaults to `overrides:false` and silently discards values. Any override added in code sets `overrideState = true` explicitly.
- Every prototype script created in this milestone starts with the comment line `// SPIKE: throwaway, not carried into M1.` Exceptions, which are production code from the start: `JoustResolver.cs`, the assembly definitions, the build and setup editor scripts, `.gitignore` and `.gitattributes`.
- Findings are appended to `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md` as each task completes. A finding records the command run, the observed output, and the verdict.

---

## File Structure

| Path | Responsibility |
|---|---|
| `.gitignore` (modified) | Unity ignore rules appended to the existing Python rules |
| `.gitattributes` (create) | Line-ending and merge rules for Unity YAML assets |
| `unity/JoustUnity/` | The Unity project root, created by the Unity CLI |
| `unity/JoustUnity/Assets/Editor/Joust.Editor.asmdef` | Editor-only assembly for setup and build scripts |
| `unity/JoustUnity/Assets/Editor/UrpSetup.cs` | Creates the URP pipeline asset and assigns it to Graphics and Quality settings |
| `unity/JoustUnity/Assets/Editor/BuildScript.cs` | Headless Windows player build entry point |
| `unity/JoustUnity/Assets/Scripts/Runtime/Joust.Runtime.asmdef` | Runtime gameplay assembly |
| `unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustResolver.cs` | Pure lance-height resolution. Production code |
| `unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustContact.cs` | Prototype trigger handler proving one resolution per pair |
| `unity/JoustUnity/Assets/Scripts/Runtime/Movement/FlightPrototype.cs` | Prototype arcade flight controller |
| `unity/JoustUnity/Assets/Scripts/Runtime/World/ScreenWrapPrototype.cs` | Prototype wrap-and-ghost renderer |
| `unity/JoustUnity/Assets/Tests/EditMode/Joust.Tests.EditMode.asmdef` | EditMode test assembly |
| `unity/JoustUnity/Assets/Tests/EditMode/JoustResolverTests.cs` | Tests for the resolver |
| `unity/JoustUnity/Assets/Art/ATTRIBUTION.md` | Licence record for every imported asset pack |
| `unity/tools/run-unity-tests.ps1` | Controller-run wrapper for the headless test CLI |
| `unity/tools/build-windows.ps1` | Controller-run wrapper for the headless player build |
| `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md` | The deliverable of this milestone |

---

### Task 1: Repository scaffolding and Unity project creation

**Files:**
- Modify: `.gitignore`
- Create: `.gitattributes`
- Create: `unity/JoustUnity/` (by CLI)
- Create: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: nothing.
- Produces: the Unity project root path `unity/JoustUnity`, used by every later task; the findings document that every later task appends to.

`AUTHORING-ONLY`: the worker writes the ignore, attributes and findings files. The controller runs the `Unity.exe -createProject` command and reports the result back.

- [ ] **Step 1: Append Unity rules to `.gitignore`**

Append exactly this block to the existing `.gitignore`:

```gitignore

# --- Unity ---
unity/JoustUnity/[Ll]ibrary/
unity/JoustUnity/[Tt]emp/
unity/JoustUnity/[Oo]bj/
unity/JoustUnity/[Bb]uild/
unity/JoustUnity/[Bb]uilds/
unity/JoustUnity/[Ll]ogs/
unity/JoustUnity/[Uu]ser[Ss]ettings/
unity/JoustUnity/[Mm]emoryCaptures/
unity/JoustUnity/[Rr]ecordings/
unity/JoustUnity/.vsconfig
unity/JoustUnity/*.csproj
unity/JoustUnity/*.sln
unity/JoustUnity/*.unityproj
unity/JoustUnity/*.pidb
unity/JoustUnity/*.booproj
unity/JoustUnity/*.svd
unity/JoustUnity/*.apk
unity/JoustUnity/*.aab
unity/JoustUnity/*.unitypackage
unity/JoustUnity/crashlytics-build.properties
unity/artifacts/
```

- [ ] **Step 2: Create `.gitattributes`**

```gitattributes
* text=auto

*.cs text diff=csharp
*.shader text
*.hlsl text
*.uxml text
*.uss text
*.asmdef text
*.json text
*.md text

*.unity merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf
*.asset merge=unityyamlmerge eol=lf
*.mat merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf
*.meta merge=unityyamlmerge eol=lf

*.png binary
*.jpg binary
*.wav binary
*.fbx binary
*.glb binary
*.dll binary
```

- [ ] **Step 3: Create the findings document skeleton**

Create `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`:

```markdown
# Joust Unity M0 — Verification Spike Findings

Date: 2026-08-29
Plan: `docs/superpowers/plans/2026-08-29-joust-unity-m0-spike.md`
Editor: Unity 6000.5.10f1 at `G:\UnityEditors\6000.5.10f1\Editor\Unity.exe`

Every entry below records a command that was actually run, the output that was
actually observed, and a verdict. Claims without an observed command are not
findings.

## F1 — Project creation

## F2 — URP activation

## F3 — Headless EditMode tests

## F4 — Headless PlayMode tests

## F5 — Arcade flight feel

## F6 — Lance-height resolution and double-resolution hazard

## F7 — Screen wrap

## F8 — Asset pack import and animation

## F9 — Windows player build

## Verdict

## Consequences for the M1 plan
```

- [ ] **Step 4: CONTROLLER — create the Unity project**

Run:

```bash
"G:/UnityEditors/6000.5.10f1/Editor/Unity.exe" -createProject "I:/Joust/unity/JoustUnity" -batchmode -quit -nographics -logFile "I:/Joust/unity/artifacts/create.log"
```

Expected: exit code 0, and `unity/JoustUnity/Assets`, `unity/JoustUnity/Packages/manifest.json`, `unity/JoustUnity/ProjectSettings/ProjectVersion.txt` all exist. Record the exit code and the contents of `ProjectVersion.txt` under **F1**.

- [ ] **Step 5: CONTROLLER — verify the editor version stamp**

Run:

```bash
cat unity/JoustUnity/ProjectSettings/ProjectVersion.txt
```

Expected: `m_EditorVersion: 6000.5.10f1`. If it differs, stop — the wrong editor created the project.

- [ ] **Step 6: Commit**

```bash
git add .gitignore .gitattributes unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "chore: scaffold Unity 6 project for the native rebuild spike"
```

---

### Task 2: URP installation and proven activation

**Files:**
- Modify: `unity/JoustUnity/Packages/manifest.json`
- Create: `unity/JoustUnity/Assets/Editor/Joust.Editor.asmdef`
- Create: `unity/JoustUnity/Assets/Editor/UrpSetup.cs`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: the project root from Task 1.
- Produces: `Joust.Editor.UrpSetup.ConfigureUrp()`, a static parameterless method invocable by `-executeMethod`, which creates `Assets/Settings/JoustURP.asset` and `Assets/Settings/JoustURP_Renderer.asset` and assigns the pipeline to both `GraphicsSettings.defaultRenderPipeline` and every quality level.

`AUTHORING-ONLY`: the worker writes the manifest edit and both C# files. The controller runs `-executeMethod` and the verification greps.

- [ ] **Step 1: Add the URP package to the manifest**

In `unity/JoustUnity/Packages/manifest.json`, add to the `dependencies` object:

```json
"com.unity.render-pipelines.universal": "17.5.0",
```

- [ ] **Step 2: Create the editor assembly definition**

`unity/JoustUnity/Assets/Editor/Joust.Editor.asmdef`:

```json
{
  "name": "Joust.Editor",
  "rootNamespace": "Joust.Editor",
  "references": ["Unity.RenderPipelines.Universal.Runtime"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 3: Write the URP setup script**

`unity/JoustUnity/Assets/Editor/UrpSetup.cs`:

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Joust.Editor
{
    /// <summary>
    /// Creates the project URP assets and, critically, ACTIVATES the pipeline.
    /// A package in manifest.json proves only that assemblies exist; rendering
    /// through URP requires GraphicsSettings.defaultRenderPipeline to be set.
    /// </summary>
    public static class UrpSetup
    {
        private const string SettingsDir = "Assets/Settings";
        private const string RendererPath = SettingsDir + "/JoustURP_Renderer.asset";
        private const string PipelinePath = SettingsDir + "/JoustURP.asset";

        [MenuItem("Joust/Configure URP")]
        public static void ConfigureUrp()
        {
            Directory.CreateDirectory(SettingsDir);

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"URP configured. defaultRenderPipeline={GraphicsSettings.defaultRenderPipeline}");
        }
    }
}
```

- [ ] **Step 4: CONTROLLER — run the setup method**

Run:

```bash
"G:/UnityEditors/6000.5.10f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "I:/Joust/unity/JoustUnity" -executeMethod Joust.Editor.UrpSetup.ConfigureUrp -logFile "I:/Joust/unity/artifacts/urp-setup.log"
```

Expected: exit code 0, and the log contains `URP configured. defaultRenderPipeline=JoustURP`.

- [ ] **Step 5: CONTROLLER — prove activation in the settings asset**

Run:

```bash
grep -A3 "m_CustomRenderPipeline" unity/JoustUnity/ProjectSettings/GraphicsSettings.asset
```

Expected: a `fileID` that is not `0` and a `guid` that matches `unity/JoustUnity/Assets/Settings/JoustURP.asset.meta`. A `fileID: 0` means the Built-in pipeline is still active and this task has failed regardless of what the log said.

- [ ] **Step 6: CONTROLLER — prove activation in a compiled shader list**

Run:

```bash
grep -c "Universal Render Pipeline/" unity/artifacts/urp-setup.log
```

Expected: at least one match. Record both greps verbatim under **F2**, then also record whether `Standard` (the Built-in family) appears.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "build: install URP and prove pipeline activation"
```

---

### Task 3: Headless EditMode tests and the lance resolver

**Files:**
- Create: `unity/JoustUnity/Assets/Scripts/Runtime/Joust.Runtime.asmdef`
- Create: `unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustResolver.cs`
- Create: `unity/JoustUnity/Assets/Tests/EditMode/Joust.Tests.EditMode.asmdef`
- Create: `unity/JoustUnity/Assets/Tests/EditMode/JoustResolverTests.cs`
- Create: `unity/tools/run-unity-tests.ps1`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: the configured project from Task 2.
- Produces: `Joust.Combat.JoustOutcome` (enum: `AWins`, `BWins`, `Tie`) and `Joust.Combat.JoustResolver.Resolve(float aLanceY, float bLanceY, float tieBand)` returning `JoustOutcome`. Task 6 consumes both. Also produces `unity/tools/run-unity-tests.ps1`, the test command used by every later task.

`AUTHORING-ONLY`: the worker writes all files including the PowerShell wrapper, and must not execute the wrapper. The controller runs it and records the real result.

- [ ] **Step 1: Write the failing test**

`unity/JoustUnity/Assets/Tests/EditMode/JoustResolverTests.cs`:

```csharp
using Joust.Combat;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    public class JoustResolverTests
    {
        // Unity is Y-up: the greater Y is the higher lance and wins.
        [Test]
        public void HigherLanceWins()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(10f, 4f, 0.5f));
        }

        [Test]
        public void LowerLanceLoses()
        {
            Assert.AreEqual(JoustOutcome.BWins, JoustResolver.Resolve(4f, 10f, 0.5f));
        }

        [Test]
        public void EqualHeightsTie()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceInsideTieBandTies()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7.4f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceExactlyOnTieBandTies()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7.5f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceJustOutsideTieBandWins()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(7.6f, 7f, 0.5f));
        }

        [Test]
        public void NegativeCoordinatesCompareByHeightNotMagnitude()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(-2f, -9f, 0.5f));
        }
    }
}
```

- [ ] **Step 2: Create the two assembly definitions**

`unity/JoustUnity/Assets/Scripts/Runtime/Joust.Runtime.asmdef`:

```json
{
  "name": "Joust.Runtime",
  "rootNamespace": "Joust",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`unity/JoustUnity/Assets/Tests/EditMode/Joust.Tests.EditMode.asmdef`:

```json
{
  "name": "Joust.Tests.EditMode",
  "rootNamespace": "Joust.Tests.EditMode",
  "references": ["Joust.Runtime", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 3: Write the test runner wrapper**

`unity/tools/run-unity-tests.ps1`:

```powershell
param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode'
)

$editor  = 'G:\UnityEditors\6000.5.10f1\Editor\Unity.exe'
$project = Join-Path $PSScriptRoot '..\JoustUnity' | Resolve-Path
$out     = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$results = Join-Path $out "tests-$Platform.xml"
$log     = Join-Path $out "tests-$Platform.log"

& $editor -batchmode -runTests -projectPath $project `
    -testPlatform $Platform -testResults $results -logFile $log
$code = $LASTEXITCODE

Write-Output "exit=$code results=$results log=$log"
exit $code
```

- [ ] **Step 4: CONTROLLER — run the tests and confirm they FAIL**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: a non-zero exit code, and `unity/artifacts/tests-EditMode.log` containing a compile error naming `JoustResolver` as missing. This is the red state.

- [ ] **Step 5: Write the minimal implementation**

`unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustResolver.cs`:

```csharp
using System;

namespace Joust.Combat
{
    public enum JoustOutcome
    {
        AWins,
        BWins,
        Tie
    }

    /// <summary>
    /// Decides a lance duel purely from lance heights. Unity is Y-up, so the
    /// greater Y is the higher lance and wins. Collider geometry never decides
    /// the winner; this comparison does.
    /// </summary>
    public static class JoustResolver
    {
        public static JoustOutcome Resolve(float aLanceY, float bLanceY, float tieBand)
        {
            if (Math.Abs(aLanceY - bLanceY) <= tieBand)
            {
                return JoustOutcome.Tie;
            }

            return aLanceY > bLanceY ? JoustOutcome.AWins : JoustOutcome.BWins;
        }
    }
}
```

- [ ] **Step 6: CONTROLLER — run the tests and confirm they PASS**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: exit code 0, and `unity/artifacts/tests-EditMode.xml` reporting `total="7" passed="7" failed="0"`. Record the exit code, the run duration from the log, and the results summary under **F3**. If the headless runner cannot run at all, record that failure — it is the single most important finding in this milestone, because it decides whether M1 can be test-gated.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity unity/tools docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "test: headless EditMode runner and lance-height resolver"
```

---

### Task 4: Headless PlayMode probe and Input System install

**Files:**
- Modify: `unity/JoustUnity/Packages/manifest.json`
- Create: `unity/JoustUnity/Assets/Tests/PlayMode/Joust.Tests.PlayMode.asmdef`
- Create: `unity/JoustUnity/Assets/Tests/PlayMode/PlayModeSmokeTests.cs`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: `unity/tools/run-unity-tests.ps1` from Task 3.
- Produces: a proven or disproven answer to whether PlayMode tests run headless, which determines how M1 gates physics changes. Produces the Input System package at a pinned version for Task 5.

`AUTHORING-ONLY`: worker writes files only; controller runs the probe.

- [ ] **Step 1: Add the Input System package**

In `unity/JoustUnity/Packages/manifest.json` `dependencies`, add:

```json
"com.unity.inputsystem": "1.14.2",
```

- [ ] **Step 2: Set the active input handling to the new system**

In `unity/JoustUnity/ProjectSettings/ProjectSettings.asset`, set:

```yaml
  activeInputHandler: 1
```

Value `1` means the new Input System only. If the key is absent, add it at the same indentation as its neighbouring keys.

- [ ] **Step 3: Create the PlayMode test assembly**

`unity/JoustUnity/Assets/Tests/PlayMode/Joust.Tests.PlayMode.asmdef`:

```json
{
  "name": "Joust.Tests.PlayMode",
  "rootNamespace": "Joust.Tests.PlayMode",
  "references": ["Joust.Runtime", "UnityEngine.TestRunner"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 4: Write a physics smoke test**

`unity/JoustUnity/Assets/Tests/PlayMode/PlayModeSmokeTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator RigidbodyWithCustomGravityFalls()
        {
            var go = new GameObject("faller");
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            var startY = go.transform.position.y;
            for (var i = 0; i < 30; i++)
            {
                body.AddForce(new Vector3(0f, -30f, 0f), ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }

            Assert.Less(go.transform.position.y, startY, "custom gravity did not move the body downward");
            Object.Destroy(go);
        }
    }
}
```

- [ ] **Step 5: CONTROLLER — run the PlayMode probe**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform PlayMode
```

Expected, if headless PlayMode works: exit code 0 with `total="1" passed="1"`. A failure here is an acceptable outcome of a spike — record exactly how it fails (licence, graphics device, or timeout) under **F4**, and note that M1 must then gate physics by controller smoke instead of automated PlayMode tests.

- [ ] **Step 6: CONTROLLER — re-run the EditMode suite**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: still exit code 0 with 7 passing. This confirms the Input System install did not break compilation.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "test: probe headless PlayMode and install the Input System"
```

---

### Task 5: Arcade flight prototype and feel smoke

**Files:**
- Create: `unity/JoustUnity/Assets/Scripts/Runtime/Movement/FlightPrototype.cs`
- Create: `unity/JoustUnity/Assets/Editor/SpikeSceneBuilder.cs`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: the Input System from Task 4.
- Produces: `Joust.Movement.FlightPrototype`, a `MonoBehaviour` with public serialized fields `gravity`, `flapImpulse`, `thrustAcceleration`, `maxHorizontalSpeed`, `airDrag`, `groundSkidDeceleration`; and `Joust.Editor.SpikeSceneBuilder.BuildSpikeScene()`, a static parameterless method that generates `Assets/Scenes/Spike.unity` containing a camera, a directional light, three box platforms, and one player capsule carrying `FlightPrototype`. Tasks 6 and 7 add to the same scene through the same builder.

`AUTHORING-ONLY`: worker writes both scripts. The controller runs the scene builder, opens the editor, plays it, and judges the feel. Feel is a human verdict and cannot be delegated.

- [ ] **Step 1: Update both assembly definitions for their new dependencies**

`FlightPrototype` uses the Input System, and `SpikeSceneBuilder` uses runtime
types, so two references must be added before either file will compile. Neither
reference could be added earlier, because the assemblies they name did not exist
yet.

In `unity/JoustUnity/Assets/Scripts/Runtime/Joust.Runtime.asmdef`, change the
references line to:

```json
  "references": ["Unity.InputSystem"],
```

In `unity/JoustUnity/Assets/Editor/Joust.Editor.asmdef`, change the references
line to:

```json
  "references": ["Unity.RenderPipelines.Universal.Runtime", "Joust.Runtime"],
```

- [ ] **Step 2: Write the flight prototype**

`unity/JoustUnity/Assets/Scripts/Runtime/Movement/FlightPrototype.cs`:

```csharp
// SPIKE: throwaway, not carried into M1.
using UnityEngine;
using UnityEngine.InputSystem;

namespace Joust.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightPrototype : MonoBehaviour
    {
        [SerializeField] private float gravity = 24f;
        [SerializeField] private float flapImpulse = 9f;
        [SerializeField] private float thrustAcceleration = 17f;
        [SerializeField] private float maxHorizontalSpeed = 12f;
        [SerializeField] private float airDrag = 0.6f;
        [SerializeField] private float groundSkidDeceleration = 27f;
        [SerializeField] private float groundCheckDistance = 0.6f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody _body;
        private float _moveInput;
        private bool _flapQueued;
        private bool _grounded;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            _moveInput = 0f;
            if (keyboard.leftArrowKey.isPressed)
            {
                _moveInput -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                _moveInput += 1f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _flapQueued = true;
            }
        }

        private void FixedUpdate()
        {
            _grounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                groundCheckDistance,
                groundLayers);

            var velocity = _body.linearVelocity;

            if (_flapQueued)
            {
                velocity.y = flapImpulse;
                _grounded = false;
                _flapQueued = false;
            }

            if (Mathf.Abs(_moveInput) > 0.01f)
            {
                velocity.x += _moveInput * thrustAcceleration * Time.fixedDeltaTime;
            }
            else if (_grounded)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, groundSkidDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, airDrag * Time.fixedDeltaTime);
            }

            velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);

            if (!_grounded)
            {
                velocity.y -= gravity * Time.fixedDeltaTime;
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

- [ ] **Step 3: Write the spike scene builder**

`unity/JoustUnity/Assets/Editor/SpikeSceneBuilder.cs`:

```csharp
using System.IO;
using Joust.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Joust.Editor
{
    public static class SpikeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Spike.unity";

        [MenuItem("Joust/Build Spike Scene")]
        public static void BuildSpikeScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 6f, -22f);
                camera.transform.rotation = Quaternion.identity;
                camera.fieldOfView = 35f;
            }

            CreatePlatform("platform_left", new Vector3(-9f, 0f, 0f), new Vector3(7f, 1f, 3f));
            CreatePlatform("platform_mid", new Vector3(0f, 5f, 0f), new Vector3(6f, 1f, 3f));
            CreatePlatform("platform_right", new Vector3(9f, 0f, 0f), new Vector3(7f, 1f, 3f));

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "player";
            player.transform.position = new Vector3(0f, 9f, 0f);
            player.AddComponent<Rigidbody>();
            player.AddComponent<FlightPrototype>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Spike scene written to {ScenePath}");
        }

        private static void CreatePlatform(string name, Vector3 position, Vector3 scale)
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.position = position;
            platform.transform.localScale = scale;
        }
    }
}
```

- [ ] **Step 4: CONTROLLER — generate the scene**

Run:

```bash
"G:/UnityEditors/6000.5.10f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath "I:/Joust/unity/JoustUnity" -executeMethod Joust.Editor.SpikeSceneBuilder.BuildSpikeScene -logFile "I:/Joust/unity/artifacts/spike-scene.log"
```

Expected: exit code 0 and `unity/JoustUnity/Assets/Scenes/Spike.unity` on disk.

- [ ] **Step 5: CONTROLLER — user feel smoke**

Open the editor, load `Assets/Scenes/Spike.unity`, enter play mode, and fly with the arrow keys and space bar. Ask the user directly: does the flap arc, the fall rate, and the horizontal glide feel like Joust? Tune the six serialized fields live in the inspector until the answer is yes, then record the accepted values under **F5**. Those values seed the `TuningProfile` asset in M1.

- [ ] **Step 6: Persist the accepted tuning values**

Write the accepted values back into the `[SerializeField]` defaults in `FlightPrototype.cs` so the scene and the source agree.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "feat: arcade flight prototype and spike scene builder"
```

---

### Task 6: Lance-height contact resolution and the double-resolution hazard

**Files:**
- Create: `unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustContact.cs`
- Modify: `unity/JoustUnity/Assets/Tests/EditMode/JoustResolverTests.cs`
- Modify: `unity/JoustUnity/Assets/Editor/SpikeSceneBuilder.cs`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: `Joust.Combat.JoustResolver.Resolve(float, float, float)` and `Joust.Combat.JoustOutcome` from Task 3.
- Produces: `Joust.Combat.JoustContact`, a `MonoBehaviour` exposing `[SerializeField] private Transform lance`, `[SerializeField] private float tieBand`, and `public static int ResolutionCount` — a counter the controller reads to prove that one overlap between two riders resolves exactly once, not once per participant.

The hazard this task exists to catch: `OnTriggerEnter` fires on **both** colliders, so a naive handler resolves the same duel twice and can kill both riders. The guard is that only the participant with the smaller `GetInstanceID()` performs the resolution.

`AUTHORING-ONLY`: worker writes the script, the added test, and the scene-builder change. Controller runs the tests and the scene.

- [ ] **Step 1: Write the failing test for the ordering guard**

Append to `unity/JoustUnity/Assets/Tests/EditMode/JoustResolverTests.cs`, inside the same namespace:

```csharp
    public class JoustOwnershipTests
    {
        [Test]
        public void OnlyTheLowerInstanceIdResolves()
        {
            Assert.IsTrue(Joust.Combat.JoustContact.ShouldResolve(10, 20));
            Assert.IsFalse(Joust.Combat.JoustContact.ShouldResolve(20, 10));
        }

        [Test]
        public void SelfContactNeverResolves()
        {
            Assert.IsFalse(Joust.Combat.JoustContact.ShouldResolve(10, 10));
        }
    }
```

- [ ] **Step 2: CONTROLLER — run the tests and confirm they FAIL**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: non-zero exit, compile error naming `JoustContact`.

- [ ] **Step 3: Write the contact handler**

`unity/JoustUnity/Assets/Scripts/Runtime/Combat/JoustContact.cs`:

```csharp
// SPIKE: throwaway, not carried into M1.
using UnityEngine;

namespace Joust.Combat
{
    [RequireComponent(typeof(Collider))]
    public class JoustContact : MonoBehaviour
    {
        [SerializeField] private Transform lance;
        [SerializeField] private float tieBand = 0.5f;

        public static int ResolutionCount;

        /// <summary>
        /// OnTriggerEnter fires on both colliders of a pair. Exactly one of the
        /// two must own the resolution, or the duel resolves twice and can kill
        /// both riders. Ownership goes to the smaller instance id.
        /// </summary>
        public static bool ShouldResolve(int selfInstanceId, int otherInstanceId)
        {
            return selfInstanceId < otherInstanceId;
        }

        private void OnTriggerEnter(Collider other)
        {
            var opponent = other.GetComponentInParent<JoustContact>();
            if (opponent == null)
            {
                return;
            }

            if (!ShouldResolve(GetInstanceID(), opponent.GetInstanceID()))
            {
                return;
            }

            var outcome = JoustResolver.Resolve(
                lance.position.y,
                opponent.lance.position.y,
                tieBand);

            ResolutionCount++;
            Debug.Log($"joust resolved: {outcome} (count={ResolutionCount})");
        }
    }
}
```

- [ ] **Step 4: CONTROLLER — run the tests and confirm they PASS**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/run-unity-tests.ps1 -Platform EditMode
```

Expected: exit code 0, `total="9" passed="9" failed="0"`.

- [ ] **Step 5: Add two duelling riders to the spike scene**

In `SpikeSceneBuilder.BuildSpikeScene`, after the player is created, add:

```csharp
            CreateRider("rider_high", new Vector3(-4f, 7.5f, 0f));
            CreateRider("rider_low", new Vector3(4f, 6.0f, 0f));
```

and add this method to the class:

```csharp
        private static void CreateRider(string name, Vector3 position)
        {
            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = name;
            rider.transform.position = position;

            var collider = rider.GetComponent<Collider>();
            collider.isTrigger = true;

            var lance = new GameObject("lance").transform;
            lance.SetParent(rider.transform, false);
            lance.localPosition = new Vector3(0f, 1.2f, 0f);

            var contact = rider.AddComponent<Joust.Combat.JoustContact>();
            var serialized = new UnityEditor.SerializedObject(contact);
            serialized.FindProperty("lance").objectReferenceValue = lance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
```

- [ ] **Step 6: CONTROLLER — rebuild the scene and observe one resolution**

Run the scene builder command from Task 5 Step 4, then open the editor, play the scene, and drive the two riders into each other. Expected: exactly one `joust resolved:` line per collision in the console, with `count` incrementing by one, and the outcome naming the rider whose lance is higher. Record the console lines under **F6**. Two lines per collision means the ownership guard failed and M1 must not proceed on this design.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "feat: lance-height contact resolution with single-owner guard"
```

---

### Task 7: Screen wrap and the seam ghost

**Files:**
- Create: `unity/JoustUnity/Assets/Scripts/Runtime/World/ScreenWrapPrototype.cs`
- Modify: `unity/JoustUnity/Assets/Editor/SpikeSceneBuilder.cs`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: `Joust.Movement.FlightPrototype` from Task 5, on the same GameObject.
- Produces: `Joust.World.ScreenWrapPrototype` with public serialized field `halfWidth`, which teleports the transform across the arena bounds and maintains a ghost copy near the seam.

`AUTHORING-ONLY`: worker writes the script and the builder change; controller plays it and judges whether the seam is visible.

- [ ] **Step 1: Write the wrap component**

`unity/JoustUnity/Assets/Scripts/Runtime/World/ScreenWrapPrototype.cs`:

```csharp
// SPIKE: throwaway, not carried into M1.
using UnityEngine;

namespace Joust.World
{
    public class ScreenWrapPrototype : MonoBehaviour
    {
        [SerializeField] private float halfWidth = 16f;
        [SerializeField] private float ghostMargin = 4f;

        private Transform _ghost;

        private void Start()
        {
            var ghost = Instantiate(gameObject, transform.position, transform.rotation);
            ghost.name = $"{name}_ghost";

            foreach (var behaviour in ghost.GetComponents<MonoBehaviour>())
            {
                Destroy(behaviour);
            }

            var body = ghost.GetComponent<Rigidbody>();
            if (body != null)
            {
                Destroy(body);
            }

            var collider = ghost.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _ghost = ghost.transform;
        }

        private void LateUpdate()
        {
            var position = transform.position;
            var width = halfWidth * 2f;

            if (position.x > halfWidth)
            {
                position.x -= width;
                transform.position = position;
            }
            else if (position.x < -halfWidth)
            {
                position.x += width;
                transform.position = position;
            }

            if (_ghost == null)
            {
                return;
            }

            var distanceToEdge = halfWidth - Mathf.Abs(position.x);
            if (distanceToEdge > ghostMargin)
            {
                _ghost.gameObject.SetActive(false);
                return;
            }

            _ghost.gameObject.SetActive(true);
            var offset = position.x > 0f ? -width : width;
            _ghost.position = new Vector3(position.x + offset, position.y, position.z);
            _ghost.rotation = transform.rotation;
        }
    }
}
```

- [ ] **Step 2: Attach it in the scene builder**

In `SpikeSceneBuilder.BuildSpikeScene`, after `player.AddComponent<FlightPrototype>();` add:

```csharp
            player.AddComponent<Joust.World.ScreenWrapPrototype>();
```

- [ ] **Step 3: CONTROLLER — rebuild the scene and fly through the seam**

Run the scene builder command from Task 5 Step 4, then play and fly off both edges at full horizontal speed. Expected: the player reappears on the opposite side with no visible pop, no one-frame gap, and no duplicate that lingers on screen after the crossing. Record the verdict and any visible artefact under **F7**.

- [ ] **Step 4: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "feat: screen wrap with seam ghost"
```

---

### Task 8: Asset pack import and animation

**Files:**
- Create: `unity/JoustUnity/Assets/Art/ATTRIBUTION.md`
- Create: `unity/JoustUnity/Assets/Art/` model and animation assets (imported)
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: the URP project from Task 2, since imported materials must survive the pipeline.
- Produces: a proven import recipe — scale factor, pivot handling, material upgrade path, Animator setup — recorded in the findings and reused for every pack in M3 and M4.

`AUTHORING-ONLY`: the worker writes `ATTRIBUTION.md` from the pack metadata the controller supplies, and never downloads anything. The controller asks the user to download the pack, then imports it in the editor.

- [ ] **Step 1: CONTROLLER — request the asset pack from the user**

Ask the user to download one animated low-poly bird model, CC0 licensed, from Quaternius (the Ultimate Animated Animals pack) in FBX or glTF form, and to say where it landed on disk. Do not download it automatically.

- [ ] **Step 2: CONTROLLER — import and normalise**

Copy the model into `unity/JoustUnity/Assets/Art/Birds/`. In the import settings, set the scale factor so the bird measures roughly 1.2 units from beak to tail, confirm the pivot sits at the body centre, and set the material creation mode so materials import as URP-lit rather than Built-in Standard. Record the exact scale factor and any material fix-up needed.

- [ ] **Step 3: CONTROLLER — verify materials render under URP**

Place the bird in `Assets/Scenes/Spike.unity` and confirm it is lit and not magenta. Magenta means a Built-in shader survived the import and the material needs upgrading through `Window > Rendering > Render Pipeline Converter`.

- [ ] **Step 4: CONTROLLER — verify the flap animation plays**

Create an Animator controller with the pack flap clip as the default state, assign it to the bird, and play the scene. Expected: the wings animate on loop. Record the clip names available in the pack, because those names decide the M1 Animator state machine.

- [ ] **Step 5: Write the attribution record**

`unity/JoustUnity/Assets/Art/ATTRIBUTION.md`:

```markdown
# Art attribution

Every third-party asset in this project is recorded here with its source and
licence. An asset with no entry here must not ship.

| Asset path | Pack | Author | Licence | Source URL |
|---|---|---|---|---|
| `Art/Birds/` | Ultimate Animated Animals | Quaternius | CC0 | https://quaternius.com/ |
```

Fill the row from the actual pack the user downloaded, correcting the pack name, author and URL if they differ.

- [ ] **Step 6: Commit**

```bash
git add unity/JoustUnity docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "feat: import and verify a CC0 animated bird pack under URP"
```

---

### Task 9: Windows player build and findings verdict

**Files:**
- Create: `unity/JoustUnity/Assets/Editor/BuildScript.cs`
- Create: `unity/tools/build-windows.ps1`
- Modify: `docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md`

**Interfaces:**
- Consumes: everything above.
- Produces: `Joust.Editor.BuildScript.BuildWindows()`, a static parameterless method invocable by `-executeMethod`, writing to `unity/artifacts/build/Joust.exe`; and the completed findings document that the M1 plan is written against.

`AUTHORING-ONLY`: worker writes both files; controller runs the build and the smoke.

- [ ] **Step 1: Write the build script**

`unity/JoustUnity/Assets/Editor/BuildScript.cs`:

```csharp
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Joust.Editor
{
    public static class BuildScript
    {
        [MenuItem("Joust/Build Windows Player")]
        public static void BuildWindows()
        {
            var output = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "artifacts", "build", "Joust.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Spike.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"build result={summary.result} size={summary.totalSize} errors={summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
```

- [ ] **Step 2: Write the build wrapper**

`unity/tools/build-windows.ps1`:

```powershell
$editor  = 'G:\UnityEditors\6000.5.10f1\Editor\Unity.exe'
$project = Join-Path $PSScriptRoot '..\JoustUnity' | Resolve-Path
$out     = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$log = Join-Path $out 'build-windows.log'

& $editor -batchmode -quit -nographics -projectPath $project `
    -executeMethod Joust.Editor.BuildScript.BuildWindows -logFile $log
$code = $LASTEXITCODE

Write-Output "exit=$code log=$log"
exit $code
```

- [ ] **Step 3: CONTROLLER — build the player**

Run:

```bash
powershell -ExecutionPolicy Bypass -File unity/tools/build-windows.ps1
```

Expected: exit code 0, a log line reading `build result=Succeeded`, and `unity/artifacts/build/Joust.exe` on disk. If IL2CPP fails for a missing module, record the exact error and retry once with `ScriptingImplementation.Mono2x`, recording that the project is Mono-only until the module is installed.

- [ ] **Step 4: CONTROLLER — prove URP shipped in the player**

Run:

```bash
grep -c "Universal Render Pipeline/" unity/artifacts/build-windows.log
```

Expected: at least one match, and no `Standard` deferred shader family from the Built-in pipeline. This is the second half of the activation gate: URP active in the editor does not prove URP active in a player build.

- [ ] **Step 5: CONTROLLER — smoke the built player**

Ask the user to run `unity/artifacts/build/Joust.exe`, fly around, and confirm it renders and controls as it did in the editor. Record the verdict under **F9**.

- [ ] **Step 6: Write the verdict and consequences**

Complete the two closing sections of the findings document. The verdict states, for each of the nine findings, whether the assumption held. The consequences section states what the M1 plan must change as a result — in particular whether physics can be gated by automated PlayMode tests or must be gated by controller smoke, and whether the build is IL2CPP or Mono.

- [ ] **Step 7: Commit**

```bash
git add unity/JoustUnity unity/tools docs/superpowers/specs/2026-08-29-joust-unity-m0-findings.md
git commit -m "build: headless Windows player build and M0 findings verdict"
```

---

## Definition of done

- All nine findings recorded with real commands and real observed output.
- EditMode suite green from the command line, exit code 0.
- A Windows player built and smoked by the user.
- URP proven active in both the editor settings and a player build log.
- The verdict and consequences sections written, so the M1 plan can be authored against evidence rather than assumption.
