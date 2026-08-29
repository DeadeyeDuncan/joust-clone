# Headless Windows player build.
#
# Uses Start-Process -Wait, because & does not reliably block on this editor
# (M0 finding F3), and gates on the built exe existing rather than on the
# process exit code.

$ErrorActionPreference = 'Stop'
$editor  = 'G:\UnityEditors\6000.5.10f1\Editor\Unity.exe'
$project = Join-Path $PSScriptRoot '..\JoustUnity' | Resolve-Path
$out     = Join-Path $PSScriptRoot '..\artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$log = Join-Path $out 'build-windows.log'
$exe = Join-Path $out 'build\Joust.exe'
if (Test-Path $exe) { Remove-Item $exe -Force }

$proc = Start-Process -FilePath $editor -PassThru -Wait -NoNewWindow -ArgumentList @(
    '-batchmode'
    '-quit'
    '-projectPath', $project
    '-executeMethod', 'Joust.Editor.BuildScript.BuildWindows'
    '-logFile', $log
)

Write-Output "unityExit=$($proc.ExitCode) log=$log"

$result = Select-String -Path $log -Pattern 'build result=' | Select-Object -Last 1
if ($result) { Write-Output $result.Line.Trim() }

if (-not (Test-Path $exe)) {
    Write-Output "FAIL: no player at $exe"
    Select-String -Path $log -Pattern 'error CS|BuildFailedException|Error building' |
        Select-Object -First 5 | ForEach-Object { Write-Output "  $($_.Line.Trim())" }
    exit 2
}

$size = [math]::Round((Get-Item $exe).Length / 1MB, 2)
Write-Output "PASS: player built at $exe (${size} MB)"
exit 0
