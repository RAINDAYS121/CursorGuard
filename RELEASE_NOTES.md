# CursorGuard v1.3.0

Copyright (c) 2026 RAINDAYS121. GPL-3.0-only.

Add multiple targets using + in Settings: select a visible running program,
choose an EXE without launching it, or enter a filename. Use − to remove the
selected entry. Names are deduplicated without case sensitivity.

Only the eligible actual foreground program from the list is protected.
Switching executable, process, window or client bounds releases the old owned
constraint and requires fresh valid bounds stable for 100 ms. Empty lists stay
empty. Main/floating names and icons identify the active target; idle snapshots
clear stale identity. Icons use read-only active-PID metadata and stay in memory.

Single-target version-1 settings migrate to a version-2 list, retaining keys,
startup and display mode. Loading does not rewrite the original file; a successful
explicit save persists the list. Existing interface preferences are preserved.
Older versions cannot read version-2 target settings.

Settings enable intention, pause/emergency priority, 中文 / English, themes and
icon/name display remain. Application and Windows login always start paused.

新增多程序列表，设置中“+”添加、“−”移除，按程序文件名去重。仅识别实际前台目标，
切换后先释放旧区域并重新等待稳定。旧单程序配置迁移为列表，保留其他偏好；
读取不改写文件，成功保存后才更新。界面显示真实活动程序，空闲时清除旧名称图标。

Validation: 186 automatic regressions, including 36 new multiple-target cases,
bilingual offscreen previews and an independent source-ZIP rebuild. Focused
target/settings and localization checks precede the final full suite. Existing
200% runner/window-size regressions and assertions remain. No live game, actual
global hotkeys, physical focus/drag or anti-cheat compatibility validation.

Assets: CursorGuard-1.3.0.zip, CursorGuard-1.3.0-source.zip, SHA256SUMS.txt.
Extract into a new folder; exit an older copy normally before launching.
Do not overwrite a running folder. Previous versions remain available.
