# Build and test / 构建与测试

Windows 10/11 with an existing .NET Framework 4.x runtime/compiler and PowerShell. The scripts check the existing 64-bit compiler, then the 32-bit compiler. They install nothing, use no NuGet packages and change no execution policy.

## Layout / 目录

- `src/`: production C#, application manifest and optional rendering diagnostics.
- `tests/`: fake-backend and hidden-control regression checks.
- `scripts/`: build, verify and package commands.
- `assets/`: original icon source graphics and ICO resources.
- `previews/en/`, `previews/zh-CN/`: actual application renderer previews with simulated data.
- `docs/`, `.github/`: validation, architecture and repository workflow files.

The executable and runtime configuration remain at the extracted project root. Moving source files does not change portable startup or the legacy preference directory.

## Commands / 命令

From the project root:

```powershell
.\scripts\build.ps1
.\scripts\verify.ps1
.\scripts\package.ps1
```

Build uses warning level 4 with warnings as errors. Verify launches hidden subprocesses only in `--self-test`, `--render-preview` and `--render-preview-en` modes. It does not create the guard worker, register real hotkeys, write a cursor constraint, show the app or send input. Output is `test-results.json`, `verification.json` and ignored `artifacts/validation-previews/`. Verification does not overwrite the committed illustrative previews.

1.2.4 runs 150 checks, including 18 language tests and 16 settings/display regressions. They cover preference roundtrip, migration of absent/unknown language values, successful and failed saves, dropdown commitment, state/target preservation, English conflicts/errors, and both themes at 100%, 125%, 150% and 200%. Language test files are unique temporary fixtures inside the chosen test-output directory, removed on completion. They never read or write the user's effective preferences.

验证仅使用模拟后端、隐藏托管控件、独立临时配置和离屏绘制。不会启动正常窗口、保护线程、真实快捷键或鼠标操作。当前用户的配置和运行目录保持不变。

To regenerate curated previews, launch these modes in hidden subprocesses:

```powershell
$previewExe=Join-Path $PWD 'CursorGuard.exe'
Start-Process -FilePath $previewExe -ArgumentList ('--render-preview "'+$PWD+'\previews\zh-CN"') -WindowStyle Hidden -Wait
Start-Process -FilePath $previewExe -ArgumentList ('--render-preview-en "'+$PWD+'\previews\en"') -WindowStyle Hidden -Wait
```

These modes use the same captions, layout and drawing code as the app, with explicitly simulated state/program data. Textbox and dropdown borders/text are representative offscreen drawings; Windows native popup borders can differ. No image editing, personal window-title enumeration or desktop capture is used. Installed Microsoft YaHei UI and Segoe UI have system-font fallbacks; no fonts are bundled.

## Packaging / 打包

Packaging requires a passing report, the verified executable hash and version 1.2.4.0. It includes complete corresponding source, scripts, docs, original icon assets and bilingual previews in both ZIPs. The portable ZIP also includes the executable and sanitized test/verification reports. An explicit input allowlist excludes personal preferences, raw diagnostics, credentials, helpers and workspace history. Inputs cannot be reparse points or escape the project root. SHA256SUMS.txt accompanies the ZIPs. This script performs no upload or publication.

For an independent check, extract `release/CursorGuard-1.2.4-source.zip` into a new folder and run its `scripts/build.ps1` and `scripts/verify.ps1`. The ordinary GitHub workflow repeats this check with `contents: read`. Binary equality across different compiler/OS environments is not claimed.

## Optional native diagnostic / 可选原生检查

`CursorGuard.exe --native-render-probe <output>` is separate from verification. It creates its own NOACTIVATE windows outside the virtual desktop and reads public foreground/clip metadata, using PrintWindow and alpha surfaces to inspect its own rendering. It requires interactive desktop access and is not part of this revision's automatic validation. It never starts the guard worker or sends physical input. Run only when explicitly intended; raw host metadata is excluded from public packages.

## Manual validation / 人工检查

Follow [docs/VALIDATION.md](docs/VALIDATION.md) after finishing the game and exiting the older tool normally. Check language restoration, native dropdown/file-picker/tray interaction, dragging, real DPI changes and game/fullscreen behavior separately. Simulations do not establish game or anti-cheat compatibility.

## Authorized release workflow

The dedicated release workflow accepts only main-branch pushes changing
`.github/release-request.json` or manual dispatch on main. It has no PR trigger.
Preparation keeps `contents: read`, builds and tests, packages and independently
rebuilds the corresponding source ZIP. Only the separate publish job has
`contents: write`, using its temporary GITHUB_TOKEN without stored credentials.
It publishes v1.2.4 for the exact run commit after verifying the three assets,
then downloads each public attachment and checks SHA-256. Mismatching existing
tags/assets are refused, never replaced or deleted. Ordinary CI stays read-only.

## License and distribution / 许可与发行

Project code and original icons are GPL-3.0-only; see the full [LICENSE](LICENSE)
and [third-party notices](THIRD_PARTY_NOTICES.md). Copyright © 2026 RAINDAYS121.
The repository is [RAINDAYS121/CursorGuard](https://github.com/RAINDAYS121/CursorGuard).
Portable packages include complete corresponding source; source-only packages
and SHA-256 checksums accompany them. GitHub Releases records publication status.
Detailed test scope and limits are in [docs/VALIDATION.md](docs/VALIDATION.md).

项目代码和原创图标采用 GPLv3（仅第 3 版），版权归 RAINDAYS121（2026）。
完整许可及第三方说明见上方链接；发行包附对应源码和 SHA-256，详细验证范围见验证文档。
