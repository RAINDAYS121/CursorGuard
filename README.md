# CursorGuard · 鼠标守卫

[English](README.en.md) · [详细说明](docs/README.zh-CN.md)

Windows 10/11 鼠标限制工具：在指定游戏或应用处于前台时，将鼠标限制在窗口内，帮助避免双屏使用时误移到副屏。

![深色主界面](previews/zh-CN/preview-paused-dark.png)
![设置界面](previews/zh-CN/preview-settings.png)

## 下载与快速开始

[下载 v1.2.4 程序包](https://github.com/RAINDAYS121/CursorGuard/releases/download/v1.2.4/CursorGuard-1.2.4.zip) · [全部版本、源码与校验文件](https://github.com/RAINDAYS121/CursorGuard/releases)

1. 完整解压程序包，运行 `CursorGuard.exe`。无需安装或管理员权限，启动时默认暂停。
2. 打开“设置”，选择目标程序，点击“完成”保存。默认目标为英雄联盟。
3. 打开启用开关，或按 `Ctrl + Alt + F8`；切回目标程序，并将鼠标移入窗口。
4. 确认状态显示“保护中”。切出目标程序或暂停时会释放鼠标。

| 默认快捷键 | 操作 |
| --- | --- |
| `Ctrl + Alt + F8` | 启用 / 暂停 |
| `Ctrl + Alt + F9` | 紧急释放并暂停 |
| `Ctrl + Alt + F10` | 退出 |

## 主要功能

- 选择游戏或应用作为目标，副屏可保持正常显示。
- 显示实际保护状态；可选悬浮条，支持程序图标或名称显示。
- 支持中文 / English、浅色 / 深色外观、自定义快捷键和可选开机自启。
- 编辑设置时释放鼠标，完成或返回后恢复原来的启用意图。

## 使用提示

- 需要立即停止时，按紧急释放快捷键或使用托盘菜单；编辑设置时可用 `Alt + F4` 退出。
- 更新时先正常退出旧版，再解压到新目录；不要覆盖正在运行的程序。
- 游戏实战及反作弊兼容性尚未确认，建议先在非对局环境试用。悬浮条可能被独占全屏遮住。

## 详细文档与许可

[完整使用说明与故障排查](docs/README.zh-CN.md) · [验证范围](docs/VALIDATION.md) · [构建与测试](BUILDING.md) · [更新记录](CHANGELOG.md)

采用 [GPLv3（仅第 3 版）](LICENSE) 许可。版权所有 © 2026 RAINDAYS121。
