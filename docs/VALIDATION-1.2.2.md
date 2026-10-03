# Version 1.2.2 validation / 本版验证边界

114 checks pass using fake backends, hidden controls, pure geometry, bitmap
paint, managed hover/Enter events and synthetic wheel/drag/key movement models.
The source ZIP is independently extracted, rebuilt and verified. Source
completeness is checked; deterministic binary reproduction is not claimed.

Validated changes: per-pixel alpha capsule edges, measured status spacing and
continuous gear; working-area settings placement/fixed header/scroll content;
committed appearance selection with no open-popup reconciliation; hide-to-tray
state independence and owned-release failure preserving emergency registration;
actual local EXE resource icons with current-target memory-only lifetime and
no application icon-file writer; stale extraction cancellation and neutral fallback.

The custom scrollbar uses a 5 logical px rounded thumb, 14 logical px hit strip,
no arrow buttons/independent track, fixed content width, bounded range/wheel/
drag/key behavior and child Enter reveal. Tests cover range min/max/shrink,
four DPI scales, high-resolution wheel accumulation, content changes and repeated
range toggling. Windows Shell caching is outside the application guarantee and
is not modified or cleaned.

Native rendering was examined with own nonactivating windows outside the desktop,
synthetic state and read-only own executable resources. No raw host handles,
coordinates, process identities, personal configurations or diagnostic logs
are included in public artifacts.

尚未实测：真实下拉/程序选择、滚轮与拖动手感、键盘实际投递、托盘恢复、
显示器/DPI 切换、真实快捷键、游戏全屏/反作弊兼容性，以及系统挂起恢复。
托管事件及程序化移动是逻辑验证，不是物理输入测试。
