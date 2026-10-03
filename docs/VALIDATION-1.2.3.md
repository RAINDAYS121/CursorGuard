# CursorGuard 1.2.3 validation

- Build: existing .NET Framework compiler, warning level 4, warnings as errors.
- 132 passing automatic regressions; 114 existing and 18 language cases.
- Actual offscreen UI renderer in zh-CN and en; light/dark and four scales.
- Final source ZIP independently extracted, rebuilt and tested with the same suite.
- Portable/source ZIP input files compared with the corresponding source.
- No normal app launch, physical input, native constraint writes or real hotkeys.
- No user preference/runtime cleanup or game-memory access.
- Source changes preserve canonical protection decisions and user program names.

These checks validate the implementation and package correspondence, not real
game/fullscreen, anti-cheat or native interaction. No deterministic binary
reproduction is asserted. SHA-256 checksums accompany the two release ZIPs.
