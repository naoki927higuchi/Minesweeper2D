[CmdletBinding()]
param([string]$UnityEditor, [string]$Version)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$versionFile = Join-Path $PSScriptRoot 'ProjectSettings\ProjectVersion.txt'
$editorVersion = [regex]::Match((Get-Content -LiteralPath $versionFile -Raw), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $editorVersion) { throw 'ProjectVersion.txt does not specify a Unity Editor version.' }
if (-not $UnityEditor) {
    $UnityEditor = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity $editorVersion was not found. Pass -UnityEditor with the full path to Unity.exe."
}
if (-not $Version) { $Version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim() }
if ($Version -notmatch '\A\d+\.\d+\.\d+(?:-[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)*)?\z') {
    throw 'Version must be MAJOR.MINOR.PATCH, optionally with a suffix such as -rc.1.'
}
$lockPath = Join-Path $PSScriptRoot 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try {
        $lockProbe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        $lockProbe.Dispose()
    } catch { throw 'This project is open in Unity. Use Minesweeper > Build Release, or close the Editor before running this script.' }
}
$logDirectory = Join-Path $PSScriptRoot 'Builds\Logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$runId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$buildLog = Join-Path $logDirectory "$runId.log"
Write-Host "Building Minesweeper $Version with Unity $editorVersion"
Write-Host "Log: $buildLog"
& $UnityEditor -batchmode -quit -nographics -projectPath $PSScriptRoot -buildTarget Win64 -executeMethod Minesweeper.Editor.WindowsBuild.BuildRelease -releaseVersion $Version -logFile $buildLog | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Unity build failed (exit $LASTEXITCODE). See $buildLog" }
$releaseLine = Get-Content -LiteralPath $buildLog | Where-Object { $_ -match '^RELEASE_EXE=' } | Select-Object -Last 1
if (-not $releaseLine) { throw "Unity did not report a release EXE. See $buildLog" }
$exePath = $releaseLine.Substring('RELEASE_EXE='.Length).Trim()
if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) { throw 'Reported EXE does not exist.' }
Write-Output $exePath
