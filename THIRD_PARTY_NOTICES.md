# Dependencies, assets and licensing / 依赖、素材与版权

| Component | Source / handling |
| --- | --- |
| CursorGuard code and original mouse/lock icons | GPL-3.0-only; complete project source and editable SVGs are included. Project copyright: (c) 2026 RAINDAYS121, as confirmed by the maintainer. |
| .NET Framework / WinForms / GDI+ / System.Web.Extensions | Microsoft system runtime and build references, not redistributed in either package; no NuGet packages. Their existing vendor terms remain applicable. |
| Win32 and DWM APIs | Windows system interfaces; no Windows DLLs, driver binaries or SDK headers are bundled. |
| Fonts | Requests installed Microsoft YaHei UI for Chinese and Segoe UI for Latin text/digits; falls back to the system interface font if unavailable. No font files are downloaded or bundled. |
| GNU GPLv3 license document | Copyright 2007 Free Software Foundation, Inc. The license text permits verbatim copying. Retrieved from the official SPDX license-list-data repository after the GNU text endpoint timed out: https://github.com/spdx/license-list-data/blob/main/text/GPL-3.0-only.txt |
| Visual reference screenshot | Used only to understand capsule geometry; its bytes and the depicted third-party symbols/text are not redistributed. |

No copied AutoCursorLock code, assets or binaries are used. Transfer helpers, browser profiles, raw diagnostics, local cleanup scripts and effective personal preferences are excluded. A sanitized verification summary and synthetic interface previews are included. GitHub CI references actions/checkout remotely at a pinned commit; it is not bundled into the application.

`LICENSE` 中的 FSF 署名是许可证文本版权，并非本项目作者署名。项目采用 GPLv3（仅第 3 版）；项目署名为 RAINDAYS121（2026），目标仓库为 RAINDAYS121/CursorGuard。
