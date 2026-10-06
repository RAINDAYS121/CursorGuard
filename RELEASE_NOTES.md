# CursorGuard v1.3.1

Remember the last explicit protection-switch choice: enabled restores enabled;
an explicit pause stays paused. New installs and legacy configurations missing
ProtectionEnabled default to paused. After upgrading, enable once to save the
choice. Normal exit keeps it; settings suspension and ordinary foreground loss
do not overwrite it.

Restoration only arms the guard: ready hotkeys/DPI, a valid eligible foreground
window, cursor inside and 100 ms stable identity/bounds are still required.
Waiting is shown as waiting, not protected. Emergency release and protection
faults disarm restart restoration. If a config write fails, the runtime stays
paused and reports that a restart may retain the prior on-disk choice.

Targets, custom shortcuts, name/icon display and separate language/theme/status
bar preferences remain supported. Windows startup entries are not changed.
The existing verified GitHub release workflow publishes this version without expanding permissions.

新增保护开关记忆，正常退出及开机恢复上次主动选择。旧配置第一次升级仍暂停，
请开启一次保存。设置编辑和切屏不改意图；急停与保护异常优先，恢复仍经过前台、
区域、鼠标位置和稳定性校验。未修改 Windows 自启项，公开发布状态以 GitHub Releases 为准。

Validation: 28 new intent cases and 16 existing focused regressions (44 total);
214 full checks and an independent final-source-ZIP rebuild. Fake backends,
isolated configuration fixtures and hidden/offscreen UI only. No live game,
real global hotkeys/input/focus/DPI or anti-cheat validation.

Files: CursorGuard-1.3.1.zip, CursorGuard-1.3.1-source.zip, SHA256SUMS.txt.
Extract into a new folder; exit the old instance normally before opening this
version. Keep the existing configured startup location in mind when installing;
this task does not rewrite startup entries or replace the running executable.
