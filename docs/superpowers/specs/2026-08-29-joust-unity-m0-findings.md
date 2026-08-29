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

## F4 — Headless PlayMode tests

## F5 — Arcade flight feel

## F6 — Lance-height resolution and double-resolution hazard

## F7 — Screen wrap

## F8 — Asset pack import and animation

## F9 — Windows player build

## Verdict

## Consequences for the M1 plan
