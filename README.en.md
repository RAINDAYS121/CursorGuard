# CursorGuard

[中文](README.md) · [Detailed guide](docs/README.en.md)

A Windows 10/11 utility that confines the cursor to the foreground game or application from your target list, helping prevent accidental movement onto a second monitor.

![Dark main window](previews/en/preview-paused-dark.png)
![Settings](previews/en/preview-settings.png)

## Download and quick start

Download packages and check publication status on GitHub Releases.

[Download v1.3.0](https://github.com/RAINDAYS121/CursorGuard/releases/download/v1.3.0/CursorGuard-1.3.0.zip) · [All releases, source and checksums](https://github.com/RAINDAYS121/CursorGuard/releases)

1. Extract the complete package and run `CursorGuard.exe`. No installation or administrator access is required. From 1.3.1, startup restores your explicit protection choice. New installs and the first legacy-config upgrade start paused.
2. Open **Settings** and use **+** to add programs, or select a list entry and use **−** to remove it. Save with **Done**. League of Legends is included by default.
3. Turn on the enable switch or press `Ctrl + Alt + F8`, then focus any added program and move the cursor into its window.
4. Confirm that the state says **Protected**. Switching away or pausing releases the cursor.

| Default shortcut | Action |
| --- | --- |
| `Ctrl + Alt + F8` | Enable / pause |
| `Ctrl + Alt + F9` | Emergency release and pause |
| `Ctrl + Alt + F10` | Quit |

## Features

- Add multiple games or applications; automatically recognize the current foreground target while keeping the second monitor active.
- See the actual protection state and recognized program, with an optional floating bar and program icon or name display.
- Choose 中文 / English, light or dark appearance, custom shortcuts and optional Windows startup.
- The local 1.3.1 update remembers protection choice across normal exits, focus changes and settings editing. Explicit pause, emergency or protection faults cancel restoration.

## Usage notes

- To stop immediately, use emergency release or the tray menu. While editing settings, `Alt + F4` quits.
- Exit the old version normally before extracting an update into a new folder. Do not overwrite a running copy.
- Live gameplay and anti-cheat compatibility remain unconfirmed; try it outside a match first. Exclusive fullscreen may hide the floating bar.

## Documentation and license

[Full guide and troubleshooting](docs/README.en.md) · [Validation limits](docs/VALIDATION.md) · [Build and test](BUILDING.md) · [Changelog](CHANGELOG.md)

Licensed under [GPLv3 (GPL-3.0-only)](LICENSE). Copyright © 2026 RAINDAYS121.
