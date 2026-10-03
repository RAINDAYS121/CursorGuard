# CursorGuard v1.2.4

Copyright (c) 2026 RAINDAYS121. GPL-3.0-only.

Fixes the enable switch turning off when settings open. Editing releases the
cursor while retaining the prior intention. Done or Back restores that intention,
then requires a fresh valid foreground target, an inside cursor and stable bounds.
Explicit pause, emergency release, quit, failed release, registration or save
errors cancel restoration. Application and Windows login still start paused.

Language choices are always 中文 / English. New Target display settings offer
Program icon (default) or Program name in both bars, with compact layouts and
unchanged protection-state text. Names retain their original text. Icons remain
memory-only; legacy files lacking the display field retain other preferences.

修复进入设置时开关丢失：编辑释放鼠标，完成或返回恢复原意图，仍需有效前台及稳定区域。
主动暂停、紧急释放、退出或错误优先。语言选项固定为“中文 / English”。
新增主界面和悬浮条“程序图标 / 程序名称”，默认图标；保留实际状态与程序本名。

Validation: 150 automatic regressions, including 16 new settings/display cases,
bilingual offscreen rendering and an independent rebuild of the source ZIP.
No live game, actual hotkeys, physical pointer/focus, monitor DPI transitions or
anti-cheat compatibility validation was performed during the ongoing game.

Assets: CursorGuard-1.2.4.zip, CursorGuard-1.2.4-source.zip, SHA256SUMS.txt.
Extract into a new folder; exit the older version normally before launching.
Do not overwrite a running folder. The published v1.2.3 remains available.
