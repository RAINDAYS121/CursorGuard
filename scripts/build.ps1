# SPDX-License-Identifier: GPL-3.0-only
$ErrorActionPreference = 'Stop'
# Copyright (c) 2026 RAINDAYS121.
$buildRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw '未找到已有的 .NET Framework C# 编译器；本脚本不会安装依赖。' }
$sourceFiles=@(Get-ChildItem -LiteralPath (Join-Path $buildRoot 'src'),(Join-Path $buildRoot 'tests') -Filter '*.cs' -File | Sort-Object FullName | ForEach-Object {$_.FullName})
$iconOptions=@()
$iconFile=Join-Path $buildRoot 'assets\icons\CursorGuard.ico'
if(Test-Path -LiteralPath $iconFile) {$iconOptions=@('/win32icon:'+$iconFile)}
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 /warnaserror+ /utf8output /codepage:65001 `
  "/out:$buildRoot\CursorGuard.exe" "/win32manifest:$buildRoot\src\app.manifest" `
  @iconOptions `
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll `
  @sourceFiles
if ($LASTEXITCODE -ne 0) { throw "编译失败：$LASTEXITCODE" }
Write-Output '编译完成。未启动工具，未启用鼠标约束。'
