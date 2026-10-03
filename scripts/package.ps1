# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 RAINDAYS121.
$ErrorActionPreference='Stop'
$packageRoot=[IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$releaseExe=Join-Path $packageRoot 'CursorGuard.exe'
$tests=Get-Content -LiteralPath (Join-Path $packageRoot 'test-results.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$verified=Get-Content -LiteralPath (Join-Path $packageRoot 'verification.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($tests.failed -ne 0 -or $verified.simulation_failed -ne 0 -or $tests.passed -ne $verified.simulation_passed) {throw 'Run build.ps1 and verify.ps1 successfully before packaging.'}
if((Get-FileHash -LiteralPath $releaseExe -Algorithm SHA256).Hash -ne $verified.exe_sha256) {throw 'Executable changed after verification. Run verify.ps1 again.'}
if([Diagnostics.FileVersionInfo]::GetVersionInfo($releaseExe).FileVersion -ne '1.2.4.0') {throw 'Unexpected executable version.'}
$releaseRoot=Join-Path $packageRoot 'release'
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$rootNames=@('CursorGuard.exe.config','README.md','README.en.md','BUILDING.md','CHANGELOG.md','CONTRIBUTING.md','SECURITY.md','THIRD_PARTY_NOTICES.md','LICENSE','.gitignore','RELEASE_NOTES.md','docs\VERIFICATION-1.2.3.json','docs\VERIFICATION-1.2.4.json','.github\release-request.json')
$sourceFiles=[System.Collections.Generic.List[string]]::new()
foreach($name in $rootNames) {$sourceFiles.Add((Join-Path $packageRoot $name))}
foreach($subdir in @('src','tests','scripts','docs','.github','assets','previews')) {Get-ChildItem -LiteralPath (Join-Path $packageRoot $subdir) -Recurse -File | Where-Object {$_.Extension -in @('.cs','.ps1','.manifest','.md','.ico','.svg','.png','.yml','.yaml')} | ForEach-Object {$sourceFiles.Add($_.FullName)}}
foreach($file in $sourceFiles) {if(-not (Test-Path -LiteralPath $file -PathType Leaf)) {throw ('Missing required source: '+[IO.Path]::GetFileName($file))};if(([IO.File]::GetAttributes($file) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {throw 'Reparse point in package input.'}}
function Write-ReleaseZip([string]$name,[string]$prefix,[string[]]$files) {
  $zipPath=Join-Path $releaseRoot $name
  $stream=[IO.File]::Open($zipPath,[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
  try {$zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true);try {foreach($file in $files) {$full=[IO.Path]::GetFullPath($file);if(-not $full.StartsWith($packageRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Input escaped project root.'};$relative=$full.Substring($packageRoot.Length+1).Replace('\','/');[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$full,$prefix+'/'+$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null}} finally {$zip.Dispose()}} finally {$stream.Dispose()}
  $read=[IO.Compression.ZipFile]::OpenRead($zipPath)
  try {if($read.Entries.Count -ne $files.Count) {throw 'ZIP entry count mismatch.'};if(@($read.Entries | Where-Object {$_.FullName -match '(^|/)(settings\.json|floating\.json|\.library[^/]*|\.git/)' -or $_.FullName.Contains('..')}).Count -gt 0) {throw 'Unexpected private input in ZIP.'}} finally {$read.Dispose()}
  return $zipPath
}
$portable=[string[]]$sourceFiles.ToArray()+@((Join-Path $packageRoot 'CursorGuard.exe'),(Join-Path $packageRoot 'test-results.json'),(Join-Path $packageRoot 'verification.json'))
$portablePath=Write-ReleaseZip 'CursorGuard-1.2.4.zip' 'CursorGuard-1.2.4' $portable
$sourcePath=Write-ReleaseZip 'CursorGuard-1.2.4-source.zip' 'CursorGuard-1.2.4-source' $sourceFiles.ToArray()
$sumLines=@($portablePath,$sourcePath) | ForEach-Object {(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($_)}
$sumLines | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ASCII
Write-Output $sumLines
