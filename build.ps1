# SPDX-License-Identifier: GPL-3.0-only
$ErrorActionPreference = 'Stop'
# Copyright (c) 2026 RAINDAYS121.
$buildRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw '未找到已有的 .NET Framework C# 编译器；本脚本不会安装依赖。' }
$iconOptions=@()
$iconFile=Join-Path $buildRoot 'assets\icons\CursorGuard.ico'
if(Test-Path -LiteralPath $iconFile) {$iconOptions=@('/win32icon:'+$iconFile)}
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 /warnaserror+ /utf8output /codepage:65001 `
  "/out:$buildRoot\CursorGuard.exe" "/win32manifest:$buildRoot\app.manifest" `
  @iconOptions `
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll `
  "$buildRoot\LoLMouseGuard.cs" "$buildRoot\Settings.cs" "$buildRoot\Runtime.cs" "$buildRoot\Panel.cs" "$buildRoot\SettingsTests.cs" "$buildRoot\Floating.cs" "$buildRoot\FloatingTests.cs" "$buildRoot\TrayIcons.cs" "$buildRoot\Appearance.cs" "$buildRoot\ProgramPicker.cs" "$buildRoot\LayeredFrame.cs" "$buildRoot\WindowPlacement.cs" "$buildRoot\UiRegressionTests.cs" "$buildRoot\NativeRenderProbe.cs" "$buildRoot\ReviewBoards.cs" "$buildRoot\ProgramIcon.cs" "$buildRoot\ProgramIconTests.cs" "$buildRoot\SettingsScroll.cs" "$buildRoot\SettingsScrollTests.cs"
if ($LASTEXITCODE -ne 0) { throw "编译失败：$LASTEXITCODE" }
Write-Output '编译完成。未启动工具，未启用鼠标约束。'
