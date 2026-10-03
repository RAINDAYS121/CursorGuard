# Validation / 验证说明

Version 1.2.4 passes 150 automatic regressions with warnings treated as errors.
The final corresponding source ZIP is independently extracted, rebuilt and
verified with the same suite. See [VALIDATION-1.2.4.md](VALIDATION-1.2.4.md).

Existing fake cursor/hotkey/startup/store tests cover foreground/stability and
release decisions, rollback, target matching, icon lifetime, managed hover,
settings placement and scrollbar models. Eighteen additional language checks
cover both languages, saved preference migration/roundtrip, failed-save rollback,
dropdown commitment, raw user titles, localized error codes and four scales.
The renderer generates both languages using actual UI code and simulated data.

This revision's verification uses hidden managed controls and offscreen bitmaps.
It sends no physical input, creates no normal app window or guard worker, and
performs no real shortcut registration or native cursor-constraint write.
Previous 1.2.2 own-window native diagnostics are historical and were not repeated.

## Remaining manual checks / 待人工检查

- Restart restoration and actual native language/appearance dropdown interaction.
- Running-program picker, Windows file dialog and real tray commands.
- Physical scrollbar, main/floating drag and focus behavior.
- Monitor removal and real per-monitor DPI transitions.
- Real hotkeys, game/fullscreen/anti-cheat behavior and Alt+Tab timing.
- Recovery after forced termination or operating-system hangs.

150 项自动检查与源码包独立重建通过，不等同于真实游戏或物理输入验证。
最初自然越界的根因尚未确认，未宣称反作弊官方认可。

Start paused, select a target without running it, save settings and enable
explicitly. Switch languages both directions and restart; confirm target names
remain unchanged. Check focus release, resize/stabilization, settings at all
edges, short-window scrolling, emergency release and ordinary exit separately.
