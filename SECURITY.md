# Security / 安全反馈

This utility can constrain a shared Windows cursor resource. Report failures involving foreground validation, coordinate conversion, stuck constraints, emergency controls or unsafe persistence. Describe the version, Windows display/DPI setup and a minimal reproduction. Redact usernames, personal paths, window contents and credentials.

涉及前台误判、坐标错误、无法释放、紧急快捷键失效或配置写入的问题，请提供版本、显示器/DPI设置及最小复现。不要附上未经脱敏的桌面截图、个人路径、窗口内容或凭证。

For an immediate problem, use emergency release or normal Alt+Tab away from the target, then quit normally. Do not disable anti-cheat, change system security settings or grant elevation to work around a failure.

Maintainer: RAINDAYS121. When a private vulnerability reporting channel is available on the repository, use it for sensitive details. Otherwise report only a redacted issue requesting a private contact method; do not post credentials, personal configurations or exploit details publicly. Private reporting setup and response times are not guaranteed.

Normal cleanup cannot guarantee recovery after forced termination or OS hangs. Ordinary release uses rectangle comparison because ClipCursor exposes no ownership identity; emergency release intentionally forces a clear on explicit user request.
