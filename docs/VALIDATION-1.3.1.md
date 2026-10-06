# CursorGuard 1.3.1 local validation

44 focused checks pass: 28 new protection-intent cases and 16 prior settings
regressions. Full validation and an independently extracted/rebuilt final source
ZIP pass 214 checks, retaining 36 multiple-target cases, all language/layout
checks and 200% host-limit assertions.

Coverage: new/missing-field legacy defaults, strict boolean loading, read-only
migration, true/false atomic roundtrip, enable/normal quit/restart, explicit pause,
temporary settings suspension, foreground loss/minimize, 100 ms stability,
invalid/outside targets, multiple-program switching, hotkey/DPI gates, emergency
before/after restore and against stale settings drafts, failed writes without
retry loops, recovered save after emergency, cursor API and release failures,
exception abort, preserved keys/targets/display/startup and bilingual hidden UI.

Tests use fake cursor/hotkey/startup backends, isolated temporary config files and
hidden managed controls only. No real startup writes, normal app launches or
user input. Real game/global shortcuts/physical focus/DPI/anti-cheat behavior is
unverified. No Windows reboot/logout and no remote publication occurred.
