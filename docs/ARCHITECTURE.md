# Architecture / 程序原理

Production code is in `src/`, regression code in `tests/`, and build commands in
`scripts/`. Portable executable/configuration paths remain at the project root.

`UiText` translates owned UI from canonical domain messages. `GuardCore` and
worker snapshots retain their original canonical statuses for classification.
User executable basenames, manual input and actual window titles bypass
translation. `FloatingPreferences.Language` is saved in the existing version-1
interface preferences; absent/unknown language values normalize to zh-CN without
discarding valid appearance/position/visibility data. Candidate language is saved
before the global UI changes; failure retains the previous language. Native
file-dialog controls remain owned and localized by Windows.

`Worker` owns hotkey registration, state transitions and the guard loop on an independent thread. It publishes immutable-by-convention `View` snapshots to the UI. The panel, tray and floating bar consume the same snapshot; they never infer protected state from the switch alone.

`WindowsBackend` reads the actual foreground HWND and PID, then query-limited process-image metadata. It obtains client coordinates only when the actual filename belongs to the configured target list, converts them to screen coordinates, obtains the monitor/virtual desktop, and rereads foreground identity to reject torn samples. It does not request PROCESS_VM_READ.

`GuardCore` waits for a stable valid rectangle and an inside cursor. Stability identity includes HWND, PID, executable filename and client bounds; changing any component releases the old owned rectangle before a fresh 100 ms interval. It compares GetClipCursor with the target and reapplies only when needed. `WindowsBackend.Apply` rereads identity/geometry before calling ClipCursor and checks focus/minimization immediately afterward. No sequence of separate public API calls can make focus and ClipCursor fully atomic; later changes are detected by the next poll.

The target is the intersection of physical client area, its monitor rectangle and virtual desktop. This deliberately keeps confinement on one monitor. No coordinates are entered manually. Client areas below 64 × 64 are rejected to avoid invalid restoration/exit samples.

Normal cleanup tracks the last rectangle applied by this utility and avoids clearing a different newer rectangle. Windows supplies no ClipCursor owner token, so equal rectangles remain ambiguous. The user's emergency command explicitly calls release even without recorded ownership. Release failures pause and retain known ownership for retry while the worker remains alive.

`SettingsService` registers candidate hotkeys and persists configuration transactionally, restoring previous hotkeys/startup value on failure. Unchanged startup preference leaves its existing path alone. SettingsSession releases first while retaining the prior enable intention. Successful save or Back restores that intention with fresh validation; explicit pause, emergency, quit or errors cancel restoration.

`FloatingController` owns optional window lifecycle and separate interface preferences. It creates no floating window while disabled, saves monitor-relative logical offsets, restores physical bounds for current DPI, and clamps to a work area. Native drag messages are addressed only to the floating/main window after a user click. The floating window uses WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, ShowWithoutActivation, MA_NOACTIVATE and SetWindowPos with SWP_NOACTIVATE; no input is sent to the target game.

`StatusPresentation` derives the displayed name only from the eligible active foreground snapshot basename, omitting the `.exe` suffix. `CompactBarLayout` measures installed regular system fonts and caps the name width, reserving the longest status so status changes cannot resize the window. The main capsule caps at 320 × 44 logical pixels and the floating capsule at 210 × 30. Target-name changes update floating width and restore/clamp its position using the current monitor DPI. Tooltip content contains the complete basename and state, never a path or transient window caption.

Theme preference uses read-only per-user Windows app/taskbar settings with a one-second poll. High contrast uses system colors. DWM calls modify only this application's own window appearance, not Windows theme settings.

The program chooser enumerates visible windows only after the user explicitly opens it. Captions/PIDs are transient UI content; only basenames are saved in a case-insensitive unique list. Version-1 single-target settings migrate in memory, preserving other preferences; version-2 lists are atomically saved only after an explicit successful save. Empty lists remain empty. Active icon lookups use the snapshot PID and stay memory-only; idle snapshots clear identity and use a neutral icon. File selection extracts the basename without executing it.

API references: [ClipCursor](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-clipcursor), [extended window styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles), [GetClientRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclientrect), [ClientToScreen](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-clienttoscreen).
