# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 RAINDAYS121.
$ErrorActionPreference='Stop'
$verifyRoot=$PSScriptRoot
$verifyExe=Join-Path $verifyRoot 'CursorGuard.exe'
if(-not (Test-Path -LiteralPath $verifyExe)) {throw 'Run build.ps1 first.'}
$testOutput=Join-Path $verifyRoot 'test-results.json'
$testProcess=Start-Process -FilePath $verifyExe -ArgumentList ('--self-test "'+$testOutput+'"') -WindowStyle Hidden -Wait -PassThru
if($testProcess.ExitCode -ne 0) {throw 'Simulation tests failed.'}
$report=Get-Content -LiteralPath $testOutput -Raw -Encoding UTF8 | ConvertFrom-Json
if($report.failed -ne 0) {throw 'Report contains failed tests.'}
$previewOutput=Join-Path $verifyRoot 'previews'
$previewProcess=Start-Process -FilePath $verifyExe -ArgumentList ('--render-preview "'+$previewOutput+'"') -WindowStyle Hidden -Wait -PassThru
if($previewProcess.ExitCode -ne 0) {throw 'Offscreen preview generation failed.'}
$verification=[ordered]@{version='1.2.2';simulation_passed=$report.passed;simulation_failed=$report.failed;exe_sha256=(Get-FileHash -LiteralPath $verifyExe -Algorithm SHA256).Hash;mode='simulation_and_offscreen_only';native_window_handle_created_in_tests=$false;real_game_tested=$false;real_drag_focus_tested=$false;real_monitor_dpi_tested=$false;anticheat_compatibility_confirmed=$false}
$verification | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $verifyRoot 'verification.json') -Encoding UTF8
Write-Output ('Passed '+$report.passed+' simulations and generated previews. No normal app launch or game input.')
