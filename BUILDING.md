# Build and test / 构建与测试

## 环境 / Requirements

Windows 10/11 with an existing .NET Framework 4.x runtime and compiler. PowerShell is used for the scripts. The build checks `Framework64\v4.0.30319\csc.exe`, then the 32-bit compiler. It installs nothing, uses no NuGet packages, and does not change execution policy or machine settings.

需要 Windows 10/11 已有的 .NET Framework 4.x 运行时及编译器。脚本不安装依赖、不下载软件、不修改执行策略。

## Commands / 命令

From the extracted project root / 在项目根目录执行：

```powershell
.\build.ps1
.\verify.ps1
```

The build uses warning level 4 and treats warnings as errors. `verify.ps1` runs the compiled program only in `--self-test` and `--render-preview` modes, using hidden subprocesses. It never starts the guard worker, registers real hotkeys, calls native ClipCursor or shows a normal app window. Results are written to `test-results.json`; generated images go to `previews`.

构建把警告当作错误。验证只运行模拟测试和离屏绘制模式，不启用鼠标约束或注册真实热键，不显示正常窗口。

Individual commands / 单独运行：

```powershell
.\CursorGuard.exe --self-test "$PWD\test-results.json"
.\CursorGuard.exe --render-preview "$PWD\previews"
```

Tests use fake cursor/hotkey/startup backends and unique temporary files inside the chosen output directory. A window-policy test constructs a hidden form without creating its native handle, inspects effective styles, and exercises its mouse-activation response in memory. It is not a physical focus test.

Version 1.2.2 runs 114 checks. Managed hover events and synthetic movement traces verify paint/layout and state logic without physical mouse input; repeated/cancelled appearance commits and settings placement at four edges/scales are included. Adaptive-name checks cover short and long names, retained click areas, stable width across status changes, target-triggered floating resizing, and 125%/150%/200% offscreen scaling. Preview filenames `preview-name-*` and `preview-floating-name-*` show simulated League of Legends, Paint and a long target basename. Approved review boards are under `previews/review`.

1.2.2 运行 114 项检查；托管悬停事件、合成移动轨迹、四边设置定位与重复外观选择不创建原生窗口，不发送真实鼠标/键盘输入。图标资源测试只读本工具的 EXE。源码包解压后使用同一套构建与验证命令。

预览共享真实客户端绘制代码，状态和程序选择条目为明确标注的模拟数据。输入框、下拉框在离屏模式中绘制代表性文本和边框，实际 Windows 原生控件边缘可能不同。没有抓取用户当前桌面或运行中窗口标题。微渐变和边缘高光使用实际绘制；主界面和悬浮条的外缘由独立的每像素透明窗口合成，原生交互控件保留在内侧窗口区域。离屏位图验证不证明用户桌面的合成效果。

The manifest requests `asInvoker` and PerMonitorV2/PerMonitor awareness. The runtime also checks DPI readiness before allowing protection. There is no elevation fallback. Icons under `assets/icons` are original source graphics; the build embeds `CursorGuard.ico` when present.

## Native rendering probe / 原生绘制检查

```powershell
.\CursorGuard.exe --native-render-probe "$PWD\native-probe"
```

This optional mode creates only its own NOACTIVATE windows outside the current virtual desktop. It reads public foreground/clip metadata, records alpha/region/styles, reconstructs PrintWindow clients with the applied alpha bitmap, and checks programmatic placement/visibility. It does not start the worker, write effective preferences, register shortcuts, move the pointer or send input. It requires access to the interactive desktop; it is separate from verify.ps1 and is not a real drag/dropdown/tray test.

此模式只创建桌面范围外的自身窗口，验证合成、布局和程序化显隐；不改变当前配置，不启动保护后端，不模拟鼠标或键盘。截图重建原生客户端与透明边框，并非用户桌面截屏。

## Packaging / 打包

```powershell
.\package.ps1
```

Run build.ps1 and verify.ps1 first. Packaging rejects a failed test report, mismatched verified executable hash or wrong version. The script builds a portable ZIP including executable, complete source, docs and icons, plus a source-only ZIP. It uses an explicit allowlist, rejects symlinks/reparse points, verifies ZIP contents and writes SHA-256 checksums next to the ZIPs. Personal configurations, diagnostics, upload helpers, credentials and workspace history are excluded. The script does not upload, create a repository, push or publish a release.

发行包可以解压后直接运行，源码包含完整对应源码及构建脚本。打包不访问个人配置或发布账户，不执行公开发布。

## Manual checks / 待人工检查

Follow [docs/VALIDATION.md](docs/VALIDATION.md). Run interactive checks after finishing the current game, with the old tool exited normally. Do not confuse successful simulation with real game or anti-cheat validation.
