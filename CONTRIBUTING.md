# Contributing / 参与贡献

Read the README, build locally, and run `verify.ps1`. Keep the foreground identity checks, pause-by-default behavior, independent emergency path and failure cleanup. Changes to target selection, ownership heuristics, hotkey transactions, coordinates or lifecycle need meaningful regression coverage. Pure appearance edits need visual inspection rather than tests that merely duplicate drawing constants.

请先阅读说明并完成构建和验证。保护核心的前台校验、启动暂停、紧急释放和失败清理必须保留。提交应说明具体问题、最终行为、验证方法和未覆盖的真实场景。

Do not add injection, game-memory access, input hooks, anti-cheat bypasses, security-setting changes, unsolicited networking or telemetry. Keep window titles transient in the explicitly opened picker; never include user paths, configurations, captures or credentials in a contribution.

Use focused pull requests. Explain why a change is needed, include reproduction steps and test results, and state when live-game behavior was not tested. Code and original assets use GPL-3.0-only; contributions must be compatible. Do not copy third-party artwork or code without recording its source and license.

The project is maintained under RAINDAYS121/CursorGuard. Use a focused pull request after the repository is available. Copyright (c) 2026 RAINDAYS121; GPL-3.0-only.
