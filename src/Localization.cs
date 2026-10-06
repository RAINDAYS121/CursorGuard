// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    public static class UiText
    {
        static volatile bool english;
        public static string Language {get {return english?"en":"zh-CN";}}
        public static bool IsEnglish {get {return english;}}
        public static bool Supported(string choice) {return choice=="zh-CN" || choice=="en";}
        public static string Normalize(string choice) {return choice=="en"?"en":"zh-CN";}
        public static void SetLanguage(string choice) {if(!Supported(choice)) throw new ArgumentException("Unsupported language");english=choice=="en";}
        static readonly Dictionary<string,string> words=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {"自动识别", "Auto detect"},
            {" · 目标数量：", " · Targets: "},
            {"目标程序列表", "Target program list"},
            {"添加目标程序", "Add target program"},
            {"移除目标程序", "Remove target program"},
            {"添加输入的程序", "Add typed program"},
            {"目标程序列表无效。", "Invalid target program list."},
            {"已暂停", "Paused"},
            {"保护中", "Protected"},
            {"异常", "Error"},
            {"正在准备", "Starting"},
            {"等待程序", "Wait: program"},
            {"等待鼠标", "Wait: mouse"},
            {"等待稳定", "Stabilizing"},
            {"已启用 · ", "Enabled · "},
            {"已暂停 · 需要检查", "Paused · Check settings"},
            {"CursorGuard · 鼠标守卫", "CursorGuard"},
            {"鼠标锁定状态", "Cursor protection status"},
            {"启用或暂停鼠标保护", "Enable or pause cursor protection"},
            {"选择程序", "Choose program"},
            {"设置", "Settings"},
            {"‹ 返回", "‹ Back"},
            {"更多操作", "More actions"},
            {"完成", "Done"},
            {"编辑", "Edit"},
            {"启用或暂停快捷键", "Toggle shortcut"},
            {"紧急释放快捷键", "Emergency shortcut"},
            {"退出快捷键", "Quit shortcut"},
            {"登录 Windows 时启动", "Start with Windows"},
            {"显示实时状态悬浮条", "Show live status bar"},
            {"目标程序可执行文件名", "Target executable filename"},
            {"选择", "Choose"},
            {"外观：跟随系统、浅色、深色", "Appearance: system, light or dark"},
            {"跟随系统", "Follow system"},
            {"浅色", "Light"},
            {"深色", "Dark"},
            {"隐藏主界面到托盘", "Hide to tray"},
            {"退出", "Quit"},
            {"恢复默认快捷键", "Reset shortcuts"},
            {"重置悬浮条位置", "Reset status bar position"},
            {"完成设置或返回后可隐藏", "Finish settings or go back before hiding"},
            {"详情与帮助", "Details and help"},
            {"显示主界面", "Show main window"},
            {"启用 / 暂停", "Enable / Pause"},
            {"紧急释放并暂停", "Emergency release and pause"},
            {"显示状态悬浮条", "Show status bar"},
            {"CursorGuard · 已暂停", "CursorGuard · Paused"},
            {"圆角合成失败，已回退到普通窗口；鼠标保护状态未改变。", "Rounded rendering failed; using a regular window. Protection is unchanged."},
            {" · 圆角合成失败，已回退到普通窗口", " · Rounded rendering failed; using a regular window"},
            {"\n\n目标程序：", "\n\nTarget: "},
            {"\n紧急释放：", "\nEmergency release: "},
            {"\n退出：", "\nQuit: "},
            {"\n\n悬浮条可拖动；从托盘显示、隐藏或重置。独占全屏下可能不可见。", "\n\nDrag the status bar to move it. Show, hide or reset it from the tray. Exclusive fullscreen may hide it."},
            {"运行中的程序…", "Running programs…"},
            {"选择 .exe 文件…", "Choose an .exe file…"},
            {"程序 (*.exe)|*.exe", "Programs (*.exe)|*.exe"},
            {"选择目标程序（不会运行）", "Choose target program (will not run)"},
            {"请选择程序", "Choose a program"},
            {"手动输入", "Type a filename"},
            {"快捷键未保存", "Settings not saved"},
            {"设置结果", "Settings result"},
            {"\n点击选择程序", "\nClick to choose a program"},
            {"，选择程序", ", choose program"},
            {"开机自启", "Start with Windows"},
            {"状态悬浮条", "Status bar"},
            {"目标程序", "Target program"},
            {"外观", "Appearance"},
            {"紧急释放", "Emergency release"},
            {"按组合键 · Esc 取消", "Press shortcut · Esc cancels"},
            {"请加 Ctrl 或 Alt", "Include Ctrl or Alt"},
            {"英雄联盟 · 模拟窗口", "League of Legends · simulated window"},
            {"示例程序 · 模拟窗口", "Example program · simulated window"},
            {"状态悬浮条 · 四种状态", "Status bar · four states"},
            {"模拟状态预览 · 原尺寸", "Simulated state preview · actual size"},
            {"仅选择目标，不启动程序", "Select a target; the program will not run"},
            {"刷新", "Refresh"},
            {"取消", "Cancel"},
            {"当前有可见窗口的程序", "Programs with visible windows"},
            {"有可见窗口的程序", "Programs with visible windows"},
            {"没有可选窗口，可从设置选择 .exe 文件", "No visible programs. Choose an .exe file in settings."},
            {"请选择 .exe 程序文件。", "Choose an .exe program file."},
            {"选择程序 · CursorGuard", "Choose program · CursorGuard"},
            {"可用滚轮、方向键或 Page Up / Page Down 滚动", "Scroll with the wheel, arrow keys or Page Up / Page Down"},
            {"设置内容", "Settings content"},
            {"托盘图标 · 原尺寸与放大预览", "Tray icons · actual and enlarged sizes"},
            {"暂停", "Paused"},
            {"等待", "Waiting"},
            {"保护", "Protected"},
            {"悬浮条配置无法读取，已恢复默认关闭。", "Status bar preferences could not be read; defaults are restored and it is off."},
            {"悬浮条位置无效。", "Invalid status bar position."},
            {"没有可用显示器。", "No available monitors."},
            {"悬浮条设置保存失败，保留原设置。", "Could not save status bar preferences; previous settings are retained."},
            {"外观设置保存失败，保持原设置。", "Could not save appearance; previous settings are retained."},
            {"悬浮条显示失败，已关闭；鼠标保护状态未改变。", "The status bar could not be shown and is now off. Protection is unchanged."},
            {"悬浮条位置未能保存；可从托盘重置位置。", "Could not save the status bar position; reset it from the tray."},
            {"悬浮条位置重置失败。", "Could not reset the status bar position."},
            {"目标显示", "Target display"},
            {"目标程序显示方式", "Target program display"},
            {"程序图标", "Program icon"},
            {"程序名称", "Program name"},
            {"目标程序显示方式无效。", "Invalid target display mode."},
            {"设置已保存。启动时恢复上次主动选择的保护开关。", "Settings saved. Startup restores your last explicit protection choice."},
            {"正在读取已保存的保护开关，尚未施加约束。", "Reading your saved protection choice; no constraint applied yet."},
            {"保护开关未能保存，当前已暂停；重启后可能保留之前的开关，请检查配置文件写入权限。", "Could not save the protection switch. Currently paused; restarting may keep the previous choice. Check configuration write access."},
            {"语言", "Language"},
            {"界面语言", "Interface language"},
            {"英语", "English"},
            {"简体中文", "Simplified Chinese"},
            {"语言：简体中文或 English", "Language: Simplified Chinese or English"},
            {"语言设置保存失败，保持原语言。", "Could not save language; the previous language is retained."},
            {"不支持的语言。", "Unsupported language."},
            {"确定", "OK"},
            {"启动时不约束鼠标。", "Cursor protection starts paused."},
            {"只有目标程序在前台才会保护鼠标。", "Protection applies only while the target program is foreground."},
            {"快捷键不可用，禁止启用", "Shortcuts unavailable; enabling is blocked"},
            {"DPI 坐标检查失败，禁止启用", "DPI check failed; enabling is blocked"},
            {"需要 Windows 10/11 的每显示器 DPI 坐标支持。", "Windows 10/11 per-monitor DPI coordinates are required."},
            {"快捷键已就绪。请手动启用。", "Shortcuts are ready. Enable protection explicitly."},
            {"已启用，等待程序前台", "Enabled; waiting for foreground target"},
            {"紧急释放后已暂停", "Paused after emergency release"},
            {"释放失败，已暂停", "Release failed; paused"},
            {"本工具不会再次加约束，直到手动启用。", "No constraint will be applied until you enable protection again."},
            {"已退出", "Exited"},
            {"Win32 错误 ", "Win32 error "},
            {"；请正常 Alt+Tab 切出。", "; switch away normally with Alt+Tab."},
            {"已启用，当前不约束", "Enabled; currently unconstrained"},
            {"等待目标窗口在前台且客户区有效；普通切屏会释放。", "Waiting for a valid foreground target. Normal focus changes release protection."},
            {"等待鼠标自行回到程序区域", "Waiting for the cursor to return to the target"},
            {"不会主动把副屏上的鼠标拉回。", "The cursor will not be moved back from another monitor."},
            {"等待程序窗口稳定", "Waiting for the target window to stabilize"},
            {"等待程序窗口稳定。", "Waiting for the target window to stabilize."},
            {"确认前台窗口及客户区稳定 100 毫秒。", "Waiting for foreground identity and client bounds to remain stable for 100 ms."},
            {"无法读取鼠标约束，已暂停", "Cannot read cursor constraint; paused"},
            {"请检查当前输入桌面，恢复后重新启用。", "Check the input desktop and enable again after it recovers."},
            {"程序区域 ", "Target bounds "},
            {"；Alt+Tab 可正常切出。", "; normal Alt+Tab remains available."},
            {"前台切换时释放失败，已暂停", "Release failed during focus change; paused"},
            {"；将重试清理。可按 Ctrl+Alt+F9 或正常 Alt+Tab 切出。", "; cleanup will retry. Use Ctrl+Alt+F9 or switch away with Alt+Tab."},
            {"约束接口失败，已暂停", "Constraint API failed; paused"},
            {"。不会提权或修改安全配置。", ". No elevation or security changes will be attempted."},
            {"前台状态变化，暂不约束", "Foreground changed; temporarily unconstrained"},
            {"正在初始化，保持暂停", "Initializing; paused"},
            {"不会自动启用鼠标约束。", "Protection will not enable automatically."},
            {"暂停检测", "Detection paused"},
            {"尚未开始保护", "Protection has not started"},
            {"未识别到前台目标", "Foreground target not found"},
            {"等待有效程序区域", "Waiting for valid target bounds"},
            {"请先打开设置，保护暂停后才能修改。", "Open settings first; protection must pause before editing."},
            {"尚未确认释放本工具约束，不能编辑。请使用紧急释放或正常 Alt+Tab。", "Release is not confirmed, so editing is blocked. Use emergency release or normal Alt+Tab."},
            {"编辑设置期间保持暂停。Alt+F4 可随时退出。", "Paused while editing. Alt+F4 remains available to quit."},
            {"工具异常：", "Application error: "},
            {"。已停止，请正常 Alt+Tab 切出。", ". Stopped; switch away normally with Alt+Tab."},
            {"不支持的配置版本。", "Unsupported preferences version."},
            {"目标程序请填写完整可执行文件名，例如 League of Legends.exe；不要填写路径或通配符。", "Enter an executable filename, such as League of Legends.exe, without paths or wildcards."},
            {"快捷键至少包含 Ctrl 或 Alt，加一个普通按键。", "Use Ctrl or Alt with a regular key."},
            {"不能使用 Tab、F12、Delete、Windows 键或单独的修饰键；保留正常系统切换。", "Tab, F12, Delete, Windows keys and lone modifiers are reserved for normal system use."},
            {"三个快捷键不能使用相同组合。", "The three shortcuts must use different combinations."},
            {" 注册失败（Win32 ", " could not be registered (Win32 "},
            {"）。快捷键可能已被其他程序占用。", "). Another program may be using the shortcut."},
            {"配置损坏或无法读取，已回退默认快捷键；保护保持暂停。保存设置可替换此配置。", "Preferences are unreadable; default shortcuts are restored and protection remains paused. Saving settings can replace the file."},
            {"自启程序路径无效。请从解压后的固定位置运行工具。", "Invalid startup path. Run the extracted program from a stable folder."},
            {"设置已保存。保护保持暂停；自启登录也默认暂停。", "Settings saved. Protection remains paused, including at Windows startup."},
            {" 自启记录回滚失败，请检查设置中的自启开关。", " Startup rollback failed; check the startup switch."},
            {"设置保存失败：", "Could not save settings: "},
            {" 已恢复原快捷键，保护保持暂停。", " Previous shortcuts are restored; protection remains paused."},
            {" 原快捷键恢复失败，禁止启用：", " Previous shortcuts could not be restored; enabling is blocked: "},
            {"LoL 鼠标守护", "CursorGuard"},
            {"工具已在此 Windows 会话中运行。请查看托盘图标。", "CursorGuard is already running in this Windows session. Check the tray icon."},
        };
        public static bool HasChinese(string text) {return text!=null && Regex.IsMatch(text,@"[\u4e00-\u9fff]");}
        public static string T(string canonical)
        {if(!english || canonical==null) return canonical;string translated;if(words.TryGetValue(canonical,out translated)) return translated;return HasChinese(canonical)?"Application message":canonical;}
        public static string Display(string canonical)
        {
            if(!english || String.IsNullOrEmpty(canonical)) return canonical;
            string exact;if(words.TryGetValue(canonical,out exact)) return exact;
            List<string> keys=new List<string>(words.Keys);keys.Sort(delegate(string a,string b) {return b.Length.CompareTo(a.Length);});
            string result=canonical;foreach(string key in keys) result=result.Replace(key,words[key]);
            if(HasChinese(result)) {Match code=Regex.Match(result,@"(?:Win32|error)\s*([0-9]+)",RegexOptions.IgnoreCase);return "Application message unavailable in English. Check settings or retry."+(code.Success?" Win32 "+code.Groups[1].Value:" ");}
            return result;
        }
        public static string ExceptionMessage(Exception error)
        {if(!english) return error.Message;string result=Display(error.Message);return result.StartsWith("Application message unavailable",StringComparison.Ordinal)?error.GetType().Name+" (0x"+error.HResult.ToString("X8")+")":result;}
        static string Canonical(string value)
        {if(value==null) return null;if(words.ContainsKey(value)) return value;foreach(KeyValuePair<string,string> pair in words) if(pair.Value==value) return pair.Key;return null;}
        sealed class Binding {public string Text,Name,Description;}
        static readonly Font chineseFont=UiTypography.ControlFont(false),englishFont=UiTypography.ControlFont(true);
        public static Font ControlFont {get {return english?englishFont:chineseFont;}}
        public static void Apply(Control control)
        {
            Binding binding=control.Tag as Binding;
            if(binding==null) {binding=new Binding {Text=control is TextBox || control is ListBox || control is ComboBox?null:Canonical(control.Text),Name=Canonical(control.AccessibleName),Description=Canonical(control.AccessibleDescription)};control.Tag=binding;}
            if(binding.Text!=null) control.Text=T(binding.Text);if(binding.Name!=null) control.AccessibleName=T(binding.Name);if(binding.Description!=null) control.AccessibleDescription=T(binding.Description);
            foreach(Control child in control.Controls) Apply(child);
        }
        public static void ApplyMenu(ToolStrip strip)
        {
            if(strip==null) return;strip.Font=ControlFont;
            foreach(ToolStripItem item in strip.Items) {Binding binding=item.Tag as Binding;if(binding==null){binding=new Binding {Text=Canonical(item.Text),Description=Canonical(item.ToolTipText)};item.Tag=binding;}if(binding.Text!=null)item.Text=T(binding.Text);if(binding.Description!=null)item.ToolTipText=T(binding.Description);ToolStripDropDownItem menu=item as ToolStripDropDownItem;if(menu!=null && menu.HasDropDownItems) ApplyMenu(menu.DropDown);}
        }
        public static IEnumerable<KeyValuePair<string,string>> Catalog {get {return words;}}
    }
    static class LanguageSelection
    {
        public static int Index(string choice) {return choice=="en"?1:0;}
        public static void Commit(ComboBox combo,Func<string,bool> save,Action refresh)
        {if(combo.SelectedIndex<0) return;string choice=combo.SelectedIndex==1?"en":"zh-CN";if(save(choice)) UiText.SetLanguage(choice);refresh();}
    }
    static class UiDialogs
    {
        public static Form Create(string text,string title)
        {
            Form dialog=new Form {Text=title,ClientSize=new Size(460,270),FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,ShowInTaskbar=false,StartPosition=FormStartPosition.CenterParent,Font=UiText.ControlFont};
            TextBox body=new TextBox {Text=text,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,ScrollBars=ScrollBars.Vertical,Bounds=new Rectangle(16,16,428,202),Font=UiText.ControlFont};
            Button ok=new Button {Text=UiText.T("确定"),DialogResult=DialogResult.OK,Bounds=new Rectangle(354,230,90,28)};dialog.Controls.Add(body);dialog.Controls.Add(ok);dialog.AcceptButton=ok;dialog.CancelButton=ok;return dialog;
        }
        public static DialogResult Show(IWin32Window owner,string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon)
        {using(Form dialog=Create(text,title)) return dialog.ShowDialog(owner);}
        public static DialogResult Show(string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon)
        {return Show(null,text,title,buttons,icon);}
    }
}
