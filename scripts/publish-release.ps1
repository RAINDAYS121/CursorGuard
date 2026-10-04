# SPDX-License-Identifier: GPL-3.0-only
# Copyright (c) 2026 RAINDAYS121.
# Runs only in the specifically authorized hosted publication job.
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:GITHUB_REPOSITORY -ne 'RAINDAYS121/CursorGuard' -or $env:GITHUB_REF -ne 'refs/heads/main') {throw 'Publication requires the approved main-branch hosted job.'}
if($env:GITHUB_SHA -notmatch '^[0-9a-f]{40}$' -or [string]::IsNullOrWhiteSpace($env:RELEASE_TOKEN)) {throw 'Missing hosted commit or token.'}
$publishRoot=Split-Path -Parent $PSScriptRoot
$request=Get-Content -LiteralPath (Join-Path $publishRoot '.github/release-request.json') -Raw | ConvertFrom-Json
if($request.version -ne '1.3.0' -or $request.publish -ne $true) {throw 'No matching explicit release request.'}
$tag='v1.3.0'
$api='https://api.github.com/repos/RAINDAYS121/CursorGuard'
$headers=@{Authorization=('Bearer '+$env:RELEASE_TOKEN);Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2026-03-10'}
function Read-ReleaseApi([string]$path,[bool]$allow404=$false) {
  try {
    $response=Invoke-RestMethod -Uri ($api+$path) -Headers $headers -Method Get
    # Invoke-RestMethod emits a JSON array as one pipeline object. Enumerate
    # explicitly so an empty asset list stays empty for the conflict checks.
    if($response -is [Array]) {foreach($entry in $response) {Write-Output $entry};return}
    return $response
  }
  catch {if($allow404 -and $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {return $null};throw 'GitHub read failed. No speculative write will be attempted.'}
}
function Write-ReleaseApi([string]$path,[string]$method,[object]$body) {
  try {$response=Invoke-WebRequest -Uri ($api+$path) -Headers $headers -Method $method -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 6))) -SkipHttpErrorCheck -TimeoutSec 60}
  catch {throw ('GitHub publication transport failed ('+$method+' '+$path+'). Inspect the remote result before retrying; existing assets are never deleted.')}
  $status=[int]$response.StatusCode
  if($status -lt 200 -or $status -ge 300) {
    $serverMessage='No server message'
    $errorBody=$null
    try {$errorBody=[string]$response.Content | ConvertFrom-Json} catch {}
    if($errorBody.message) {$serverMessage=[string]$errorBody.message}
    foreach($validation in @($errorBody.errors)) {
      $fields=@()
      foreach($key in @('resource','field','code','message')) {if($validation.$key) {$fields+=($key+'='+[string]$validation.$key)}}
      if($fields.Count -gt 0) {$serverMessage+=' ['+($fields -join ', ')+']'}
    }
    $serverMessage=$serverMessage.Replace($env:RELEASE_TOKEN,'[redacted]') -replace '[\r\n\x00-\x1f]',' '
    if($serverMessage.Length -gt 600) {$serverMessage=$serverMessage.Substring(0,600)}
    throw ('GitHub publication request failed ('+$method+' '+$path+', HTTP '+$status+'): '+$serverMessage+'. Inspect the remote result before retrying; existing assets are never deleted.')
  }
  try {return [string]$response.Content | ConvertFrom-Json} catch {throw 'Unexpected GitHub publication response. Inspect the remote result before retrying.'}
}
function Tag-Commit {
  $ref=Read-ReleaseApi ('/git/ref/tags/'+$tag) $true
  if($null -eq $ref) {return $null}
  $obj=$ref.object
  for($depth=0;$depth -lt 8 -and $obj.type -eq 'tag';$depth++) {$obj=(Read-ReleaseApi ('/git/tags/'+$obj.sha)).object}
  if($obj.type -ne 'commit') {throw 'Unsupported existing tag object.'}
  return $obj.sha
}
function Find-Release {
  $found=@()
  for($page=1;$page -le 100;$page++) {
    $batch=@(Read-ReleaseApi ('/releases?per_page=100&page='+$page))
    $found+=@($batch | Where-Object {$_.tag_name -eq $tag -or ($request.resume_empty_draft -and $_.id -eq $request.resume_empty_draft.id -and $_.tag_name -eq $request.resume_empty_draft.previous_tag)})
    if($batch.Count -lt 100) {break}
    if($page -eq 100) {throw 'Release listing limit reached; no speculative creation will be attempted.'}
  }
  if($found.Count -gt 1) {throw 'Multiple releases use this tag; no write will be attempted.'}
  if($found.Count -eq 1) {return $found[0]}
  return $null
}
$releaseDir=Join-Path $publishRoot 'release'
$expectedNames=@('CursorGuard-1.3.0.zip','CursorGuard-1.3.0-source.zip','SHA256SUMS.txt')
$hashes=@{}
$sumLines=@(Get-Content -LiteralPath (Join-Path $releaseDir 'SHA256SUMS.txt'))
if($sumLines.Count -ne 2) {throw 'Unexpected checksum manifest.'}
foreach($line in $sumLines) {
  if($line -notmatch '^([0-9a-f]{64})  (CursorGuard-1\.3\.0(?:-source)?\.zip)$' -or $hashes.ContainsKey($Matches[2])) {throw 'Invalid checksum entry.'}
  $hashes[$Matches[2]]=$Matches[1]
}
foreach($name in $expectedNames) {
  $file=Join-Path $releaseDir $name
  if(-not (Test-Path -LiteralPath $file -PathType Leaf)) {throw 'Missing verified asset.'}
  $actual=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
  if($name -ne 'SHA256SUMS.txt' -and $actual -ne $hashes[$name]) {throw 'Asset checksum mismatch.'}
  $hashes[$name]=$actual
}
$commit=Tag-Commit
if($null -ne $commit -and $commit -ne $env:GITHUB_SHA) {throw 'Existing tag points to a different commit. It will not be changed.'}
$release=Find-Release
if($null -eq $release) {
  # A draft is kept unpublished until all three assets have verified hashes.
  # Read a plain string: Windows PowerShell can serialize Get-Content's
  # attached provider properties as an object rather than a JSON string.
  $notes=[IO.File]::ReadAllText((Join-Path $publishRoot 'RELEASE_NOTES.md'),[Text.Encoding]::UTF8)
  $release=Write-ReleaseApi '/releases' 'Post' @{tag_name=$tag;target_commitish=$env:GITHUB_SHA;name='CursorGuard 1.3.0';body=$notes;draft=$true;prerelease=$false;make_latest='false'}
} elseif($null -eq $commit -and $release.target_commitish -ne $env:GITHUB_SHA) {
  # Continue only the inspected empty draft from the failed approved attempt.
  # Existing tags, published releases and drafts containing assets are protected.
  $resume=$request.resume_empty_draft
  if(-not $release.draft -or $release.id -ne $resume.id -or $release.target_commitish -ne $resume.previous_target -or $resume.previous_target -notmatch '^[0-9a-f]{40}$') {throw 'Existing draft targets a different commit.'}
  $draftAssets=@(Read-ReleaseApi ('/releases/'+$release.id+'/assets?per_page=100'))
  if($draftAssets.Count -ne 0) {throw 'The inspected draft now contains assets; its commit will not be changed.'}
  $release=Write-ReleaseApi ('/releases/'+$release.id) 'Patch' @{tag_name=$tag;target_commitish=$env:GITHUB_SHA;draft=$true}
  if(-not $release.draft -or $release.target_commitish -ne $env:GITHUB_SHA) {throw 'Draft commit update could not be verified.'}
}
if($release.tag_name -ne $tag) {throw 'Unexpected release tag.'}
$initialAssets=@(Read-ReleaseApi ('/releases/'+$release.id+'/assets?per_page=100'))
if(@($initialAssets | Where-Object {$_.name -notin $expectedNames}).Count -gt 0) {throw 'Unexpected existing assets; no replacement or deletion will be attempted.'}
foreach($name in $expectedNames) {
  $assets=@(Read-ReleaseApi ('/releases/'+$release.id+'/assets?per_page=100'))
  $existing=@($assets | Where-Object {$_.name -eq $name})
  $file=Join-Path $releaseDir $name
  $size=(Get-Item -LiteralPath $file).Length
  if($existing.Count -gt 1) {throw 'Duplicate release asset; no replacement will be attempted.'}
  if($existing.Count -eq 1) {
    if($existing[0].state -ne 'uploaded' -or $existing[0].size -ne $size -or $existing[0].digest -ne ('sha256:'+$hashes[$name])) {throw 'Existing asset differs from verified bytes. It will not be deleted or replaced.'}
    continue
  }
  if(-not $release.draft) {throw 'Published release is missing assets; it will not be modified.'}
  $upload='https://uploads.github.com/repos/RAINDAYS121/CursorGuard/releases/'+$release.id+'/assets?name='+[uri]::EscapeDataString($name)
  $contentType=if($name.EndsWith('.zip')) {'application/zip'} else {'text/plain'}
  try {$uploaded=Invoke-RestMethod -Uri $upload -Headers $headers -Method Post -InFile $file -ContentType $contentType}
  catch {throw 'Asset upload failed. Inspect remote assets before retrying; no deletion will be attempted.'}
  if($uploaded.name -ne $name -or $uploaded.size -ne $size -or $uploaded.digest -ne ('sha256:'+$hashes[$name])) {throw 'Uploaded asset hash verification failed; draft remains unpublished.'}
}
if($release.draft) {$release=Write-ReleaseApi ('/releases/'+$release.id) 'Patch' @{tag_name=$tag;target_commitish=$env:GITHUB_SHA;draft=$false;prerelease=$false;make_latest='true'}}
$release=Read-ReleaseApi ('/releases/tags/'+$tag)
if($release.draft -or (Tag-Commit) -ne $env:GITHUB_SHA) {throw 'Final release/commit verification failed.'}
$finalAssets=@(Read-ReleaseApi ('/releases/'+$release.id+'/assets?per_page=100'))
if($finalAssets.Count -ne 3) {throw 'Unexpected published asset count.'}
$downloadDir=Join-Path $env:RUNNER_TEMP ('CursorGuard-1.3.0-download-check-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $downloadDir | Out-Null
foreach($name in $expectedNames) {
  $remote=@($finalAssets | Where-Object {$_.name -eq $name})
  if($remote.Count -ne 1 -or $remote[0].digest -ne ('sha256:'+$hashes[$name])) {throw 'Final release asset metadata differs.'}
  $download=Join-Path $downloadDir $name
  $publicUrl='https://github.com/RAINDAYS121/CursorGuard/releases/download/'+$tag+'/'+[uri]::EscapeDataString($name)
  if($remote[0].browser_download_url -ne $publicUrl) {throw 'Unexpected public download URL.'}
  $downloaded=$false
  for($attempt=0;$attempt -lt 5 -and -not $downloaded;$attempt++) {
    try {Invoke-WebRequest -Uri $publicUrl -OutFile $download -UseBasicParsing -TimeoutSec 120 | Out-Null;$downloaded=$true}
    catch {if($attempt -eq 4) {throw 'Public asset download verification failed.'};Start-Sleep -Seconds 5}
  }
  $downloadHash=(Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant()
  if($downloadHash -ne $hashes[$name] -or (Get-Item -LiteralPath $download).Length -ne $remote[0].size) {throw 'Downloaded bytes differ from the verified release asset.'}
}
Write-Output ('Published '+$release.html_url+' at '+$env:GITHUB_SHA)
Write-Output 'All three public asset downloads match the prepared SHA-256 values.'
Write-Output ($hashes | ConvertTo-Json)
