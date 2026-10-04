# CursorGuard 1.3.0 validation

Focused targets: 36 new cases plus 30 existing settings/hotkey checks.
Full verification: 186 automatic regressions with warnings as errors.
The source ZIP is independently extracted, rebuilt and verified with the full
suite. Previews use actual rendering code and explicitly simulated data.

New coverage includes foreground executable/process/window/region switching,
stabilization, outside cursors, empty lists, active-target removal, duplicates,
exit/restart, read-only single-target migration and preserved preferences, invalid
lists, isolated-file write failure and transaction cleanup, failed-save rollback,
emergency priority, active view/PID icons and real hidden editor handlers.

Existing language labels, settings-intention/hotkey checks and 200% host-size
assertions remain. The localization input assertion checks exact filenames in
the editable target combo, as it previously did in the text box; user names are
never treated as translated captions.

No live game, actual global hotkeys, physical pointer/focus/drag, real monitor
DPI transitions or anti-cheat compatibility test was performed. The original
League natural cursor-escape cause remains unconfirmed.

先完成针对性模拟检查，再做完整验证及源码包独立重建。配置测试仅使用隔离临时
文件，未读写用户正在使用的配置或自启设置；实战及反作弊兼容性仍需单独确认。
