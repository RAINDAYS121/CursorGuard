# CursorGuard v1.2.3

Copyright (c) 2026 RAINDAYS121. GPL-3.0-only.

Adds persistent Simplified Chinese / English selection throughout owned UI,
menus, help and errors. User program filenames and real window captions retain
their original text. Language saves use the existing interface preferences;
legacy settings retain valid appearance, position and visibility fields, and
failed saves keep the previous language. Windows native file-dialog controls
follow the operating-system language.

Production source, tests and scripts move to src/, tests/ and scripts/ with
matching build, package and CI paths. English README uses real English renderer
previews; Chinese README uses Chinese renderer previews. Protection decisions,
shortcut/target preferences and portable root layout retain existing behavior.

新增简体中文 / English 切换并保存，界面提示及菜单跟随选项，实际程序名不翻译。
源码、测试和脚本分目录，英文说明及预览同步改为英文。

Validation: 132 automatic regressions, including 18 language cases, bilingual
offscreen rendering and an independent rebuild of the final source ZIP.
Physical native dropdown/picker/tray interaction, dragging, actual monitor/DPI,
game/fullscreen/hotkeys and anti-cheat compatibility remain unverified.

Assets: CursorGuard-1.2.3.zip (portable plus complete source),
CursorGuard-1.2.3-source.zip (source only), SHA256SUMS.txt.
Extract into a new folder and exit the older version normally before launching.
Every launch starts paused. Do not overwrite a running folder.
