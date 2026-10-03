# CursorGuard v1.2.2

Copyright (c) 2026 RAINDAYS121. GPL-3.0-only.

Improves capsule alpha edges, status spacing, gear drawing, screen-edge settings
placement and scrolling, committed appearance selection and hide-to-tray access.
Main target uses its local executable icon and full-name tooltip, with neutral
fallback. Icons stay in current-target memory only; no icon-cache files or
persistent cache directory are written. Windows Shell caching is not controlled.

设置页采用无箭头的细圆角滚动条，保留更宽命中区域、滚轮、拖动及键盘/焦点
滚入可见逻辑。程序图标不落盘，仅保留当前目标内存位图并及时释放。

Validation: 114 automatic regressions and independent source-package rebuild.
Physical dropdown/picker/tray/scroll/drag interaction, monitor/DPI changes,
actual game/fullscreen/hotkeys and anti-cheat compatibility remain unverified.
No anti-cheat approval or original cursor-escape root cause is claimed.

Assets: CursorGuard-1.2.2.zip (portable plus complete source),
CursorGuard-1.2.2-source.zip (source only), SHA256SUMS.txt.
Verify SHA-256 and extract into a new folder; exit an older version normally
before launching. Launch always starts paused. Do not overwrite a running folder.
