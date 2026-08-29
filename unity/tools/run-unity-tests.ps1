param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string]$Platform = 'EditMode'
)

# Two hazards this wrapper exists to defeat, both observed on 6000.5.10f1 (M0 F3):
#
# 1. Unity's -runTests exits 0 even when compilation fails and no test ever runs.
#    The process exit code is NOT a usable gate.
# 2. Stale artifacts. A previous run's XML or log will be read as this run's
#    result unless both are deleted first.
#
# The results XML is the authoritative signal: the test runner writes it on
# completion, so its presence and contents cannot race Unity's log rewriting.
# The log is read only to explain a failure, never to decide one.

$editor  = 'G:\UnityEditors\6000.5.10f1\Editor\Unity.exe'
$project = Join-Path $PSScriptRoot '..\JoustUnity' | Resolve-Path
$out     = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$results = Join-Path $out "tests-$Platform.xml"
$log     = Join-Path $out "tests-$Platform.log"

if (Test-Path $results) { Remove-Item $results -Force }
if (Test-Path $log)     { Remove-Item $log -Force }

# `& $editor ...` was observed returning before Unity had finished writing its
# artifacts, leaving $LASTEXITCODE empty and the wrapper reading a half-finished
# run (M0 finding F3). Start-Process -Wait -PassThru blocks until the editor
# process actually exits and hands back a real exit code.
# EditMode needs no graphics device, so it runs with -nographics and cannot
# steal focus or the GPU from whatever else is running. PlayMode genuinely
# needs a device and will briefly take focus.
$unityArgs = @(
    '-batchmode'
    '-runTests'
    '-projectPath', $project
    '-testPlatform', $Platform
    '-testResults', $results
    '-logFile', $log
)
if ($Platform -eq 'EditMode') { $unityArgs += '-nographics' }

$proc = Start-Process -FilePath $editor -PassThru -Wait -NoNewWindow -ArgumentList $unityArgs
$unityExit = $proc.ExitCode

if (-not (Test-Path $results)) {
    Write-Output "FAIL: no results file at $results (unityExit=$unityExit). Tests did not run."
    $compileErrors = @(Select-String -Path $log -Pattern 'error CS' -ErrorAction SilentlyContinue)
    if ($compileErrors.Count -gt 0) {
        Write-Output "Diagnosis: $($compileErrors.Count) compiler error line(s):"
        $compileErrors | Select-Object -First 5 | ForEach-Object { Write-Output "  $($_.Line.Trim())" }
        exit 2
    }
    exit 3
}

$root = ([xml](Get-Content $results -Raw)).DocumentElement
$total  = [int]$root.total
$passed = [int]$root.passed
$failed = [int]$root.failed

Write-Output "platform=$Platform total=$total passed=$passed failed=$failed result=$($root.result) unityExit=$unityExit"
Write-Output "results=$results log=$log"

if ($total -le 0) {
    Write-Output 'FAIL: results file reports zero tests.'
    exit 4
}

if ($failed -gt 0) {
    Write-Output 'FAIL: one or more tests failed:'
    ([xml](Get-Content $results -Raw)).SelectNodes('//test-case') |
        Where-Object { $_.result -ne 'Passed' } |
        ForEach-Object { Write-Output "  $($_.name): $($_.result)" }
    exit 5
}

Write-Output 'PASS'
exit 0
