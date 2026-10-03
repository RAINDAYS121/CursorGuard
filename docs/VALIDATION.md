# Validation / 验证说明

Version 1.2.2 passes 114 regressions and an independent extraction/build of
the corresponding source ZIP. Build uses the existing .NET Framework compiler,
warnings as errors. Validation is summarized in [VALIDATION-1.2.2.md](VALIDATION-1.2.2.md).

Tests cover fake cursor/hotkey/startup/store transactions, focus/stability and
release decisions, rollback, target matching, memory icon lifetime, managed
hover paint, settings geometry, scrollbar wheel/drag/key models and child Enter
reveal. No physical input, real global hotkey registration or cursor clip write
is performed by the verification scripts.

Prior native rendering checks used only own nonactivating HWNDs outside the
virtual desktop, public read-only metadata, PrintWindow clients and alpha
surfaces. Public artifacts include synthetic UI previews and a sanitized summary;
raw native HWND/coordinate records and personal state are not distributed.

## Not yet confirmed / 尚未实测

- Physical dropdown/picker/file-dialog/icon-selection interaction.
- Actual scrollbar wheel/drag feel, keyboard delivery and real tray recovery.
- Main/floating drag without focus changes, monitor removal or physical DPI transitions.
- Real hotkey availability, game/fullscreen/anti-cheat compatibility and Alt+Tab latency.
- Recovery after forced termination or OS hangs.

114 项自动检查与独立重建通过，不等同于真实游戏或物理输入验证。
尚未确认最初自然越界的根因，不宣称反作弊官方认可。

Manual checks: start paused; select a program without launching it; explicitly
enable only after settings; check focus loss/release, resize/stability, all four
settings edges, short-window scrolling, repeated appearance choices, hide/tray
recovery, emergency release and monitor/DPI changes. Keep sensitive data redacted.
