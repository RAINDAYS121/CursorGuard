# CursorGuard 1.2.4 validation

148 automatic regressions pass with warnings treated as errors. The 16 new
cases cover repeated settings entry, enabled/paused restoration, Back/Done and
window-close handlers, shortcut editing, persistence/validation/registration
errors, release failure, emergency/pause/quit precedence, fresh foreground and
stability, display migration and roundtrip, fixed native language names, both
display modes and palettes at 100/125/150/200%, and floating position clamping.
Existing memory-only icon-cache lifecycle and cancellation tests also pass.

All native operations in the suite use fake backends. Real managed UI handlers
and offscreen rendering run without creating visible windows, taking focus,
sending input, writing effective preferences or clipping the physical cursor.
Independent source-ZIP rebuild and the same suite are required before release.

未测试边界：真实游戏、实际全局快捷键、物理鼠标拖动/焦点切换、原生下拉菜单和托盘、
实际显示器 DPI 切换、独占全屏可见性及反作弊兼容性。当前游戏不被测试进程干扰。
