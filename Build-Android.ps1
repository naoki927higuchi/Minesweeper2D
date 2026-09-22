[CmdletBinding()]
param([string]$UnityEditor)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$editorVersion = [regex]::Match((Get-Content "$PSScriptRoot/ProjectSettings/ProjectVersion.txt" -Raw), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $UnityEditor) { $UnityEditor = "$env:ProgramFiles/Unity/Hub/Editor/$editorVersion/Editor/Unity.exe" }
$android = Join-Path (Split-Path $UnityEditor) 'Data/PlaybackEngines/AndroidPlayer'
$keytool = Join-Path $android 'OpenJDK/bin/keytool.exe'
if (-not (Test-Path $keytool)) { throw 'Install Android Build Support with SDK, NDK and OpenJDK in Unity Hub.' }
$version = (Get-Content "$PSScriptRoot/VERSION" -Raw).Trim()
$output = "$PSScriptRoot/bin/Android/Release-$version/Minesweeper-$version.apk"
if (Test-Path $output) { throw "Release APK already exists: $output" }
$local = "$PSScriptRoot/.local/android-signing"
New-Item -ItemType Directory -Force $local | Out-Null
$secretPath = "$local/password.dpapi"
$keyPath = "$local/minesweeper.keystore"
if (-not (Test-Path $secretPath)) {
    if (Test-Path $keyPath) { throw 'Existing key has no password file. Restore signing credentials.' }
    $bytes = New-Object byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $secret = ConvertTo-SecureString ([Convert]::ToBase64String($bytes)) -AsPlainText -Force
    $secret | ConvertFrom-SecureString | Set-Content $secretPath
}
$secure = Get-Content $secretPath -Raw | ConvertTo-SecureString
$env:MINESWEEPER_KEY_PASSWORD = [Net.NetworkCredential]::new('', $secure).Password
$env:MINESWEEPER_KEYSTORE = $keyPath
try {
    if (-not (Test-Path $keyPath)) {
        & $keytool -genkeypair -keystore $keyPath -storetype JKS -alias minesweeper -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=Minesweeper, O=FieldNotes' -storepass:env MINESWEEPER_KEY_PASSWORD -keypass:env MINESWEEPER_KEY_PASSWORD
        if ($LASTEXITCODE -ne 0) { throw 'Release signing key generation failed.' }
    }
    New-Item -ItemType Directory -Force "$PSScriptRoot/Builds/Logs" | Out-Null
    $log = "$PSScriptRoot/Builds/Logs/android-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')).log"
    & $UnityEditor -batchmode -quit -nographics -projectPath $PSScriptRoot -buildTarget Android -executeMethod Minesweeper.Editor.AndroidBuild.BuildRelease -logFile $log | Out-Host
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $output)) { throw "Android build failed. See $log" }
    & "$PSScriptRoot/Verify-Android.ps1" -Apk $output -AndroidRoot $android
    if (-not $?) { throw 'APK verification failed.' }
    Write-Output $output
} finally {
    Remove-Item Env:MINESWEEPER_KEY_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:MINESWEEPER_KEYSTORE -ErrorAction SilentlyContinue
}
