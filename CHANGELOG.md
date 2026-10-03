# Changelog

## 1.2.3 — 2026-10-03

- Add persistent Simplified Chinese / English selection for owned main/floating UI, menus, picker, help, errors and shortcut conflicts. Preserve user filenames and real window titles.
- Save language in existing interface preferences; retain valid legacy appearance/position/visibility fields. Failed saves restore the previous dropdown and language.
- Organize production code, tests and PowerShell into src/, tests/ and scripts/; update manifest, build/package and CI paths. Portable executable remains at the root.
- Replace mixed-language English README with English captions and actual English renderer previews. Generate both languages with the app renderer and simulated data.
- 132 regressions pass, including 18 language cases, both themes and four offscreen scales. Independently rebuild and verify the final source ZIP. Physical interaction, game and anti-cheat remain unverified.
- Ordinary CI remains read-only. Existing 1.2.2 runtime folders and user configuration are preserved.

新增中英文选项并保存；源码分目录，英文说明与实际界面预览同步更新。保护逻辑不变。

## 1.2.2 — 2026-10-03

- Main program name replaced by its real read-only EXE icon, neutral fallback, full-name tooltip and clickable selection entry. Floating bar retains names; generic waiting text says 等待程序.
- Existing icon caching was memory-only. Retention is now limited to the current target, with reset/disposal and stale-extraction cancellation; no icon files or persistent cache directory are written. Windows Shell caching is outside this guarantee. Lifecycle and resource-fixture no-new-file checks pass.
- 图标此前已无磁盘缓存；本版进一步限制为当前目标的内存位图，切换目标/DPI 及退出立即释放，不保存图标字节，不清理 Windows 自身缓存。
- Per-pixel alpha edges for the main and floating capsules, while retaining native interactive controls.
- Measured status spacing and a continuous vector gear.
- Settings placement uses the current working area; fixed header and scrollable content; restore the main position on return.
- Replace the native arrow/track scrollbar with a theme-aware 5-logical-pixel rounded thumb, 14-pixel hit area and fixed content width. Wheel, thumb drag, keyboard paging and focus reveal use bounded scrolling; no system scrollbar settings or added dependency.
- 设置页滚动条改为细圆角滑块，无上下箭头和独立浅灰槽；命中区域保留宽度，显隐不挤动按钮，支持滚轮、拖动、键盘与焦点滚入可见。
- Suppress selection reconciliation and palette restyling while the native dropdown is open; save appearance only on committed user selection.
- Direct main-menu hide-to-tray entry, with independent floating visibility.
- Refuse settings editing if an owned constraint cannot be released, preserving emergency shortcuts; hiding remains unavailable during editing or a pending save.
- 114 regressions cover managed hover feedback without layout/worker changes, four settings edges at four scales, repeated/cancelled appearance commits, movement traces, cache/fallback, rollback and existing core behavior.
- Dedicated own-window native probe checks alpha frames, programmatic move/hide/show synchronization and synthetic work-area placement outside the virtual desktop. No physical input or real tray interaction is used.
- Portable/source ZIPs and SHA-256 checksums prepared locally; source ZIP extracted, rebuilt and checked independently. This does not establish deterministic binary reproduction.
- Copyright attribution confirmed as RAINDAYS121. Publish artifacts exclude raw host-specific native diagnostics; source includes a minimal read-only build workflow.
- Actual dropdown hover/keyboard selection, program picker, tray recovery, physical dragging/DPI transitions, game and anticheat behavior remain unverified. Publication status is tracked by GitHub Releases.

已修复主界面/悬浮条圆角合成、状态间距、连续齿轮图形、设置页工作区定位与滚动、外观下拉反复刷新干扰，以及主界面隐藏入口。主界面改用所选程序的实际 EXE 图标，保留完整名称提示和选择入口；缺失时统一回退，悬浮条保留名称，等待文案使用“等待程序”。114 项回归及源码包独立重建通过；托管悬停和程序化移动不等同于真实鼠标操作。下拉悬停、程序选择、托盘恢复、物理拖动/DPI 切换、游戏和反作弊仍待实测。

## 1.2.1 — 2026-10-03

- Approved compact layout: main height 44, maximum width 320; short target names reduce the window width while click targets retain their size.
- Show the selected process basename, omitting `.exe`, without friendly aliases, paths or unrelated window captions. Name and status stay close; long names retain full-filename tooltips.
- Floating bar adapts to name width, with a 210 × 30 maximum. Target changes resize and clamp its bounds; status changes do not shift the layout.
- Installed Microsoft YaHei UI for Chinese and Segoe UI for Latin/digits/shortcut keys; regular weights, single-line ellipsis and consistent spacing. No font distribution or downloads.
- Preserve the approved capsule material, guard behavior, configuration compatibility and nonactivation policies.
- 80 checks pass, including adaptive-width invariants, click bounds at 125%/150%/200%, target-change lifecycle and negative-monitor/DPI geometry. Source ZIP rebuilt independently.
- Include BUILDING.md in both package allowlists.

已确认紧凑外观：使用所选进程本名，短名称收紧，长名称省略并保留完整提示。保护核心及原有设置保持兼容。80 项检查与源码包独立构建通过；真实游戏、焦点、显示器 DPI 切换及反作弊兼容性仍未确认。

## 1.2.0

- Rename the product to **CursorGuard · 鼠标守卫** while retaining legacy configuration, startup value and single-instance identity.
- Select a visible running program, choose an `.exe` without launching it, or enter an executable filename. Default: League of Legends. Only the selected filename's actual foreground valid window is eligible.
- Capsule main window and optional floating bar show target program and actual paused/waiting/protected/error state.
- Refined neutral surfaces, subtle vertical gradients, edge highlights and lower-contrast dividers; a native drop-shadow request leaves system preferences unchanged.
- Persistent system/light/dark appearance; independent taskbar icon adaptation and high-contrast fallback.
- Floating visibility/position persistence with negative-coordinate, monitor removal and DPI recovery. Nonactivation window policy and long-name tooltips.
- Original transparent mouse/lock tray icons, four state glyphs, 16/20/24/32 px ICO frames and editable SVGs.
- Preserve the independent protection thread, 100 ms stabilization, conditional reapplication, ordinary cleanup and emergency release.
- Keep old startup paths unchanged when saving unrelated settings; change them only after an explicit startup preference change.
- Complete source, bilingual usage docs, GPLv3 text, build/test/package scripts and validation limits.

这一版不包含游戏注入、内存读取、反作弊修改、模糊特效或自动更新。行为测试为模拟验证；实战及真实焦点/DPI表现尚待人工确认。

## Earlier local versions

1.0.0 introduced the protection core and emergency shortcuts. 1.1.0 added editable shortcuts and optional per-user startup. 1.1.1 changed the interface. These were local iterations; this changelog does not assert that a public release was published.
