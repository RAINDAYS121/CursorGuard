// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LoLMouseGuard
{
    static class Theme
    {
        public static Color Background = Color.FromArgb(250, 250, 250), Text = Color.FromArgb(38, 40, 45),
            Muted = Color.FromArgb(115, 122, 133), Line = Color.FromArgb(225, 228, 232),
            Blue = Color.FromArgb(87, 125, 161), Green = Color.FromArgb(77, 132, 103), Red = Color.FromArgb(165, 87, 81);
        public static Color Cap=Color.FromArgb(240,241,243),Hover=Color.FromArgb(239,241,243),SwitchOff=Color.FromArgb(217,220,225);
        public static Color SurfaceTop=Color.FromArgb(251,251,251),SurfaceBottom=Color.FromArgb(246,246,246),EdgeTop=Color.FromArgb(221,223,226),EdgeBottom=Color.FromArgb(210,213,217);
        public static bool HighContrast;
        public static bool Dark;
        public static void Apply(bool dark,bool highContrast)
        {
            Dark=dark;HighContrast=highContrast;
            if(highContrast) {Background=SurfaceTop=SurfaceBottom=SystemColors.Window;Text=SystemColors.WindowText;Muted=SystemColors.GrayText;Line=EdgeTop=EdgeBottom=SystemColors.ControlDark;Blue=Green=SystemColors.Highlight;Red=SystemColors.WindowText;Cap=Hover=SystemColors.Control;SwitchOff=SystemColors.ControlDark;return;}
            Background=dark?Color.FromArgb(37,37,37):Color.FromArgb(249,249,249);Text=dark?Color.FromArgb(226,226,227):Color.FromArgb(38,40,45);
            SurfaceTop=dark?Color.FromArgb(43,43,43):Color.FromArgb(251,251,251);SurfaceBottom=dark?Color.FromArgb(33,33,33):Color.FromArgb(246,246,246);
            EdgeTop=dark?Color.FromArgb(77,77,77):Color.FromArgb(221,223,226);EdgeBottom=dark?Color.FromArgb(49,49,49):Color.FromArgb(210,213,217);
            Muted=dark?Color.FromArgb(163,163,166):Color.FromArgb(115,122,133);Line=dark?Color.FromArgb(57,57,57):Color.FromArgb(231,233,235);
            Blue=dark?Color.FromArgb(117,158,196):Color.FromArgb(87,125,161);Green=dark?Color.FromArgb(118,177,147):Color.FromArgb(77,132,103);Red=dark?Color.FromArgb(219,139,129):Color.FromArgb(165,87,81);
            Cap=dark?Color.FromArgb(43,43,45):Color.FromArgb(240,241,243);Hover=dark?Color.FromArgb(49,49,49):Color.FromArgb(239,241,243);SwitchOff=dark?Color.FromArgb(67,67,69):Color.FromArgb(217,220,225);
        }
        public static void Surface(Graphics g,RectangleF r,float radius)
        {
            using(GraphicsPath p=Rounded(r,radius))
            {
                using(LinearGradientBrush fill=new LinearGradientBrush(r,SurfaceTop,SurfaceBottom,LinearGradientMode.Vertical)) g.FillPath(fill,p);
                using(LinearGradientBrush edge=new LinearGradientBrush(r,EdgeTop,EdgeBottom,LinearGradientMode.Vertical)) using(Pen pen=new Pen(edge,1)) g.DrawPath(pen,p);
            }
            if(HighContrast) return;
            RectangleF inner=new RectangleF(r.X+1,r.Y+1,r.Width-2,r.Height-2);
            using(GraphicsPath p=Rounded(inner,Math.Max(1,radius-1)))
            using(LinearGradientBrush sheen=new LinearGradientBrush(inner,Color.FromArgb(Dark?15:150,255,255,255),Color.FromArgb(Dark?30:5,0,0,0),LinearGradientMode.Vertical))
            using(Pen pen=new Pen(sheen,.7f)) g.DrawPath(pen,p);
        }
        public static void ControlBackdrop(Graphics g,Control control)
        {
            Control parent=control;int y=0;
            while(parent.Parent!=null) {y+=parent.Top;parent=parent.Parent;}
            if(parent==control) {g.Clear(Background);return;}
            RectangleF sample=new RectangleF(0,.5f-y,Math.Max(1,parent.Width-1),Math.Max(1,parent.Height-1));
            using(LinearGradientBrush fill=new LinearGradientBrush(sample,SurfaceTop,SurfaceBottom,LinearGradientMode.Vertical)) g.FillRectangle(fill,control.ClientRectangle);
        }
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath(); float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        public static void Write(Graphics g, string text, float x, float y, float w, float h, float size, Color color, bool bold, StringAlignment align)
        {
            using (Font f = UiTypography.PixelFont(text,size,bold))
            using (Brush b = new SolidBrush(color))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment=align;sf.LineAlignment=StringAlignment.Center;
                sf.Trimming=StringTrimming.EllipsisCharacter;sf.FormatFlags=StringFormatFlags.NoWrap;
                g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
                g.DrawString(text ?? "", f, b, new RectangleF(x,y,w,h), sf);
            }
        }
    }
    public static class StatusPresentation
    {
        public static string FileName(View v) {return Path.GetFileName(v.Settings.TargetExecutable??"");}
        public static string Name(View v)
        {string file=FileName(v);return file.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?file.Substring(0,file.Length-4):file;}
        public static string Full(View v) {return FileName(v)+" · "+Heading(v);}
        public static string Kind(View v)
        {
            if (v.Editing) return "paused";
            string s = v.Status ?? "";
            if (s.Contains("初始化")) return "loading";
            if (!v.CanEnable || s.Contains("失败") || s.Contains("无法") || s.Contains("异常")) return "error";
            if (!v.Enabled) return "paused";
            return s == "保护中" ? "protected" : "waiting";
        }
        public static string Short(View v)
        {
            string kind = Kind(v);
            if (kind == "protected") return UiText.T("保护中");
            if (kind == "error") return UiText.T("异常");
            if (kind == "loading") return UiText.T("正在准备");
            if (kind == "waiting") return (v.Status ?? "").Contains("鼠标") ? UiText.T("等待鼠标") : (v.Status ?? "").Contains("稳定") ? UiText.T("等待稳定") : UiText.T("等待程序");
            return UiText.T("已暂停");
        }
        public static string Heading(View v)
        { return Kind(v) == "waiting" ? UiText.T("已启用 · ") + Short(v) : Kind(v) == "error" ? UiText.T("已暂停 · 需要检查") : Short(v); }
        public static Color Signal(View v)
        { string kind = Kind(v); return kind == "protected" ? Theme.Green : kind == "error" ? Theme.Red : kind == "waiting" || kind == "loading" ? Theme.Blue : Theme.Muted; }
    }
    class PlainButton : Button
    {
        public string Glyph;
        bool hover;
        public PlainButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; BackColor = Theme.Background; Cursor = Cursors.Hand;
            MouseEnter += delegate { hover = true; Invalidate(); }; MouseLeave += delegate { hover = false; Invalidate(); };
        }
        public void PaintPreview(Graphics g) { OnPaint(new PaintEventArgs(g, ClientRectangle)); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Theme.ControlBackdrop(g,this);
            if (hover && Enabled) using (GraphicsPath p = Theme.Rounded(new RectangleF(0,0,Width-1,Height-1),4)) using (Brush b = new SolidBrush(Theme.Hover)) g.FillPath(b,p);
            Color color = Enabled ? Theme.Text : Theme.Muted;
            if (Glyph == "gear")
            {
                float scale = Height/30f, cx=Width/2f, cy=Height/2f;
                using (Pen pen = new Pen(color, 1.3f*scale))
                using(GraphicsPath gear=new GraphicsPath())
                {
                    PointF[] points=new PointF[36];
                    for(int i=0;i<points.Length;i++) {double a=(i-1)*Math.PI/18;float r=(i%6==1 || i%6==2 || i%6==3)?7:5.2f;points[i]=new PointF(cx+(float)Math.Cos(a)*r*scale,cy+(float)Math.Sin(a)*r*scale);}
                    pen.LineJoin=LineJoin.Round;gear.AddPolygon(points);g.DrawPath(pen,gear);
                    g.DrawEllipse(pen,cx-2.2f*scale,cy-2.2f*scale,4.4f*scale,4.4f*scale);
                }
            }
            else Theme.Write(g,Text,0,0,Width,Height,13*Height/30f,color,false,StringAlignment.Center);
            if (Focused && ShowFocusCues) using (Pen pen = new Pen(Theme.Blue)) g.DrawRectangle(pen,1,1,Width-3,Height-3);
        }
    }
    sealed class SwitchControl : CheckBox
    {
        public SwitchControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer,true);
            BackColor=Theme.Background; Text=""; Cursor=Cursors.Hand; CheckedChanged+=delegate { Invalidate(); };
        }
        public void PaintPreview(Graphics g) { OnPaint(new PaintEventArgs(g,ClientRectangle)); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics; Theme.ControlBackdrop(g,this); g.SmoothingMode=SmoothingMode.AntiAlias;
            float h=Height-2, knob=h-4, width=Width-2;
            Color fill=Checked && Enabled ? Theme.Blue : Theme.SwitchOff;
            using (GraphicsPath p=Theme.Rounded(new RectangleF(1,1,width,h),h/2)) using (Brush b=new SolidBrush(fill)) g.FillPath(b,p);
            float knobX=Checked ? Width-knob-3 : 3;
            if(Theme.HighContrast) {using(Brush b=new SolidBrush(SystemColors.HighlightText)) g.FillEllipse(b,knobX,3,knob,knob);}
            else using(LinearGradientBrush b=new LinearGradientBrush(new RectangleF(knobX,3,knob,knob),Color.FromArgb(248,248,248),Color.FromArgb(220,220,222),LinearGradientMode.Vertical)) g.FillEllipse(b,knobX,3,knob,knob);
            if (Focused && ShowFocusCues) using (Pen p=new Pen(Theme.Blue)) g.DrawRectangle(p,0,0,Width-1,Height-1);
        }
    }
    sealed class ShortcutButton : PlainButton
    {
        public Shortcut Value;
        public bool Capturing;
        public string Problem;
        public event EventHandler Changed;
        public ShortcutButton() { Click+=delegate { Capturing=true; Problem=null; Focus(); Invalidate(); }; }
        public void SetValue(Shortcut value) { Value=value.Copy(); Capturing=false; Problem=null; Invalidate(); }
        protected override bool IsInputKey(Keys data) { return Capturing || base.IsInputKey(data); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Capturing) { base.OnKeyDown(e); return; }
            if (e.Alt && e.KeyCode==Keys.F4) { FindForm().Close(); return; }
            e.Handled=true; e.SuppressKeyPress=true;
            if (e.KeyCode==Keys.Escape && !e.Control && !e.Alt && !e.Shift) { SetValue(Value); return; }
            if (e.KeyCode==Keys.ControlKey || e.KeyCode==Keys.ShiftKey || e.KeyCode==Keys.Menu) return;
            Shortcut next=new Shortcut((uint)((e.Control?2:0)|(e.Alt?1:0)|(e.Shift?4:0)),(uint)e.KeyCode);
            Settings check=Settings.Defaults(); check.Toggle=next; check.Emergency=new Shortcut(7,0x70); check.Exit=new Shortcut(7,0x71);
            string error=check.Validate();
            if (error!=null && !error.Contains("相同")) Problem=error; else SetValue(next);
            Invalidate(); if (Changed!=null) Changed(this,EventArgs.Empty);
        }
        protected override void OnLostFocus(EventArgs e) { if (Capturing) SetValue(Value); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics; Theme.ControlBackdrop(g,this); g.SmoothingMode=SmoothingMode.AntiAlias; float scale=Height/30f;
            if (Capturing) { Theme.Write(g,Problem==null ? UiText.T("按组合键 · Esc 取消") : UiText.T("请加 Ctrl 或 Alt"),0,0,Width,Height,11*scale,Problem==null?Theme.Blue:Theme.Red,false,StringAlignment.Far); return; }
            string[] caps=(Value==null ? "" : Value.Display()).Split(new[] { " + " },StringSplitOptions.RemoveEmptyEntries);
            float right=Width-4*scale;
            for (int i=caps.Length-1;i>=0;i--)
            {
                float w;
                using(Font keyFont=UiTypography.PixelFont(caps[i],UiTypography.Key*scale,false))
                    w=(float)Math.Ceiling(g.MeasureString(caps[i],keyFont,PointF.Empty,StringFormat.GenericTypographic).Width)+14*scale;
                RectangleF r=new RectangleF(right-w,5*scale,w,20*scale);
                using (GraphicsPath p=Theme.Rounded(r,4*scale)) { using (Brush b=new SolidBrush(Theme.Cap)) g.FillPath(b,p); using (Pen pen=new Pen(Theme.Line)) g.DrawPath(pen,p); }
                Theme.Write(g,caps[i],r.X,r.Y,r.Width,r.Height,UiTypography.Key*scale,Enabled?Theme.Text:Theme.Muted,false,StringAlignment.Center); right-=w+4*scale;
            }
            if (Focused && ShowFocusCues) using (Pen p=new Pen(Theme.Blue)) g.DrawRectangle(p,1,1,Width-3,Height-3);
        }
    }
    sealed class ControlPanel : Form
    {
        readonly IUiSession session;
        readonly bool preview;
        readonly bool nativePreview;
        readonly SwitchControl toggle,startup,floatingToggle;
        readonly PlainButton settingsButton,back,done,more,chooseProgram;
        readonly ProgramIconButton programIcon;
        readonly ProgramIconCache programIcons;
        bool iconBusy,pendingProgramChoice;
        long iconGeneration;
        string iconTarget;
        int iconPixels;
        DateTime iconRead=DateTime.MinValue;
        readonly ShortcutButton[] shortcuts=new ShortcutButton[3];
        readonly TextBox target;
        readonly ComboBox appearance,language,targetDisplay;
        string languageSignature;
        readonly SettingsBody settingsBody;
        RoundedFrame frame;
        bool frameFailed;
        string frameWarning;
        Rectangle? mainAnchor;
        string placementSignature;
        double settingsScale=1;
        readonly ToolTip tips;
        readonly NotifyIcon tray;
        readonly ContextMenuStrip trayMenu,moreMenu,panelMenu;
        ContextMenuStrip programMenu;
        readonly ToolStripMenuItem trayToggle,trayFloating;
        readonly ToolStripItem moreHide;
        readonly Dictionary<string,Icon> icons=new Dictionary<string,Icon>();
        readonly System.Windows.Forms.Timer timer;
        readonly FloatingController floating;
        IList<MonitorArea> monitors=new List<MonitorArea>();
        DateTime monitorRead=DateTime.MinValue;
        bool closing,settingsPage,startupHidden,pendingSave;
        int pageWidth=320,pageHeight=44;
        CompactBarLayout mainLayout;
        string layoutName;
        string trayKind;
        string themeSignature;
        bool updatingAppearance;
        DateTime themeRead=DateTime.MinValue;
        SystemTheme systemTheme;
        View view;
        public ControlPanel(IUiSession source,bool offscreen,bool startInTray,bool nativeOnlyPreview=false)
        {
            session=source; preview=offscreen;nativePreview=nativeOnlyPreview;startupHidden=startInTray;view=source.ReadView();
            Text=UiText.T("CursorGuard · 鼠标守卫"); ShowIcon=false; ClientSize=new Size(320,44); BackColor=Theme.Background;
            Font=UiText.ControlFont; AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=preview?AutoScaleMode.None:AutoScaleMode.Dpi;
            StartPosition=FormStartPosition.CenterScreen; FormBorderStyle=FormBorderStyle.None; MaximizeBox=false; DoubleBuffered=true;
            toggle=new SwitchControl { Location=new Point(238,10),Size=new Size(40,24),AutoCheck=false,AccessibleName=UiText.T("启用或暂停鼠标保护") };
            toggle.Click+=delegate { session.Send(Command.Toggle); }; Controls.Add(toggle);
            programIcon=new ProgramIconButton {Location=new Point(10,7),Size=new Size(30,30),AccessibleName=UiText.T("选择程序")};
            programIcon.Click+=delegate {BeginProgramChoice();};Controls.Add(programIcon);
            settingsButton=ButtonAt("",290,7,26,30,delegate {if(panelMenu!=null) panelMenu.Show(settingsButton,new Point(0,settingsButton.Height));else OpenSettings();}); settingsButton.Glyph="gear"; settingsButton.AccessibleName=UiText.T("设置");
            back=ButtonAt(UiText.T("‹ 返回"),8,5,74,30,delegate { CancelSettings(); });
            more=ButtonAt("···",283,5,28,30,delegate { if (moreMenu!=null) moreMenu.Show(more,new Point(0,more.Height)); }); more.AccessibleName=UiText.T("更多操作");
            done=ButtonAt(UiText.T("完成"),320,5,50,30,delegate { if (view.Editing) SaveSettings(); else session.Send(Command.BeginSettings); });
            string[] labels={UiText.T("启用或暂停快捷键"),UiText.T("紧急释放快捷键"),UiText.T("退出快捷键")};
            for(int i=0;i<3;i++) { shortcuts[i]=new ShortcutButton { Location=new Point(166,42+i*37),Size=new Size(194,30),AccessibleName=labels[i] }; Controls.Add(shortcuts[i]); }
            startup=new SwitchControl { Location=new Point(318,159),Size=new Size(42,24),AutoCheck=true,AccessibleName=UiText.T("登录 Windows 时启动") }; Controls.Add(startup);
            floatingToggle=new SwitchControl { Location=new Point(318,201),Size=new Size(42,24),AutoCheck=true,AccessibleName=UiText.T("显示实时状态悬浮条") }; Controls.Add(floatingToggle);
            target=new TextBox { Location=new Point(134,242),Size=new Size(166,25),BorderStyle=BorderStyle.FixedSingle,BackColor=Theme.Background,ForeColor=Theme.Text,Font=UiTypography.ControlFont(true),AccessibleName=UiText.T("目标程序可执行文件名") }; Controls.Add(target);
            chooseProgram=ButtonAt(UiText.T("选择"),304,240,56,30,delegate {ChooseProgram();});
            appearance=new ComboBox {Location=new Point(134,280),Size=new Size(226,25),DropDownStyle=ComboBoxStyle.DropDownList,Font=UiText.ControlFont,AccessibleName=UiText.T("外观：跟随系统、浅色、深色")};appearance.Items.AddRange(new object[] {UiText.T("跟随系统"),UiText.T("浅色"),UiText.T("深色")});appearance.SelectedIndex=0;Controls.Add(appearance);
            language=new ComboBox {Location=new Point(134,318),Size=new Size(226,25),DropDownStyle=ComboBoxStyle.DropDownList,Font=UiText.ControlFont,AccessibleName=UiText.T("界面语言")};Controls.Add(language);
            targetDisplay=new ComboBox {Location=new Point(134,356),Size=new Size(226,25),DropDownStyle=ComboBoxStyle.DropDownList,Font=UiText.ControlFont,AccessibleName=UiText.T("目标程序显示方式")};targetDisplay.Items.AddRange(new object[] {UiText.T("程序图标"),UiText.T("程序名称")});Controls.Add(targetDisplay);
            settingsBody=new SettingsBody {Location=new Point(0,40),Size=new Size(380,362),AutoScrollMinSize=new Size(360,362)};
            settingsBody.PaintContent=PaintSettingsContent;Controls.Add(settingsBody);
            foreach(Control child in new Control[] {shortcuts[0],shortcuts[1],shortcuts[2],startup,floatingToggle,target,chooseProgram,appearance,language,targetDisplay})
            {Point location=child.Location;settingsBody.Controls.Add(child);child.Location=new Point(location.X,location.Y-40);}
            SetDraft(view.Settings);
            panelMenu=new ContextMenuStrip();panelMenu.Items.Add(UiText.T("设置"),null,delegate {OpenSettings();});
            panelMenu.Items.Add(UiText.T("隐藏主界面到托盘"),null,delegate {HidePanel();});panelMenu.Items.Add(UiText.T("退出"),null,delegate {Close();});
            ContextMenuStrip=panelMenu;settingsButton.ContextMenuStrip=panelMenu;
            if(!preview)
            {
                tips=new ToolTip {InitialDelay=500,AutoPopDelay=8000,ReshowDelay=100};
                programIcons=new ProgramIconCache(new NativeProgramIconSource());
                floating=new FloatingController(new FloatingFileStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LoLMouseGuard","floating.json")),delegate
                { FloatingBar bar=new FloatingBar(false); bar.PositionCommitted+=delegate(Rectangle r) { floating.Moved(r,ReadMonitors()); }; return bar; });
                floatingToggle.Click+=delegate { floating.SetEnabled(floatingToggle.Checked); floatingToggle.Checked=floating.Preferences.Enabled; RefreshView(); };
                appearance.SelectionChangeCommitted+=delegate {if(!updatingAppearance) AppearanceSelection.Commit(appearance,floating.SetAppearance,delegate {themeSignature=null;RefreshView();});};
                appearance.DropDownClosed+=delegate {if(!IsDisposed) BeginInvoke((MethodInvoker)delegate {if(!IsDisposed) RefreshView();});};
                language.SelectionChangeCommitted+=delegate {LanguageSelection.Commit(language,floating.SetLanguage,RefreshView);};
                language.DropDownClosed+=delegate {if(!IsDisposed) BeginInvoke((MethodInvoker)delegate {if(!IsDisposed) RefreshView();});};
                moreMenu=new ContextMenuStrip();
                moreMenu.Items.Add(UiText.T("恢复默认快捷键"),null,delegate { if(view.Editing) SetDraft(Settings.Defaults()); });
                moreMenu.Items.Add(UiText.T("重置悬浮条位置"),null,delegate { floating.ResetPosition(); RefreshView(); });
                moreHide=moreMenu.Items.Add(UiText.T("隐藏主界面到托盘"),null,delegate { HidePanel(); });moreHide.ToolTipText=UiText.T("完成设置或返回后可隐藏");
                moreMenu.Items.Add(UiText.T("退出"),null,delegate { Close(); });
                moreMenu.Items.Add(UiText.T("详情与帮助"),null,delegate { ShowDetails(); });
                trayMenu=new ContextMenuStrip();
                trayMenu.Items.Add(UiText.T("显示主界面"),null,delegate { OpenPanel(); });
                trayToggle=new ToolStripMenuItem(UiText.T("启用 / 暂停"),null,delegate { session.Send(Command.Toggle); }); trayMenu.Items.Add(trayToggle);
                trayMenu.Items.Add(UiText.T("紧急释放并暂停"),null,delegate { session.Send(Command.Emergency); });
                trayMenu.Items.Add(UiText.T("设置"),null,delegate { OpenPanel(); OpenSettings(); });
                trayFloating=new ToolStripMenuItem(UiText.T("显示状态悬浮条"),null,delegate { floating.SetEnabled(!floating.Preferences.Enabled); RefreshView(); }); trayMenu.Items.Add(trayFloating);
                trayMenu.Items.Add(UiText.T("重置悬浮条位置"),null,delegate { floating.ResetPosition(); RefreshView(); });
                trayMenu.Items.Add(new ToolStripSeparator()); trayMenu.Items.Add(UiText.T("退出"),null,delegate { Close(); });
                foreach(string kind in new[] { "paused","waiting","protected","error" }) icons[kind]=TrayIcons.Create(kind,false,false);
                tray=new NotifyIcon { Icon=icons["paused"],Text=UiText.T("CursorGuard · 已暂停"),ContextMenuStrip=trayMenu,Visible=true }; tray.DoubleClick+=delegate { OpenPanel(); };
                timer=new System.Windows.Forms.Timer { Interval=200 }; timer.Tick+=delegate { RefreshView(); }; timer.Start();
                Resize+=delegate { if(WindowState==FormWindowState.Minimized) HidePanel(); };
                frame=new RoundedFrame();
            }
            if(nativePreview) {ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;frame=new RoundedFrame();}
            RefreshView();
        }
        IList<MonitorArea> ReadMonitors()
        { if(DateTime.UtcNow-monitorRead>TimeSpan.FromSeconds(1) || monitors.Count==0) { monitors=MonitorReader.Read(); monitorRead=DateTime.UtcNow; } return monitors; }
        protected override void SetVisibleCore(bool value)
        { if(startupHidden && value) { if(!IsHandleCreated) CreateHandle(); base.SetVisibleCore(false); return; } base.SetVisibleCore(value); }
        void OpenPanel() { startupHidden=false; WindowState=FormWindowState.Normal; Show(); Activate(); }
        void HidePanel() {Hide();} // Visibility only; the worker and floating bar remain independent.
        protected override bool ShowWithoutActivation {get{return nativePreview;}}
        protected override CreateParams CreateParams {get {CreateParams p=base.CreateParams;if(nativePreview) p.ExStyle|=FloatingWindowPolicy.ExtendedStyles|0x20;return p;}}
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr handle,uint message,IntPtr wParam,IntPtr lParam);
        protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(!preview && e.Button==MouseButtons.Left) {ReleaseCapture();SendMessage(Handle,0x00A1,new IntPtr(2),IntPtr.Zero);} }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);if(ClientSize.Width<2 || ClientSize.Height<2) return;
            float inset=preview && !nativePreview || frameFailed?0:2;
            using(GraphicsPath p=Theme.Rounded(new RectangleF(inset,inset,ClientSize.Width-2*inset,ClientSize.Height-2*inset),Math.Max(1,(settingsPage?18*(float)settingsScale:ClientSize.Height/2f)-inset)))
            {Region old=Region;Region=new Region(p);if(old!=null) old.Dispose();}
            SyncFrame();
        }
        void SyncFrame()
        {
            if(frame==null || frameFailed || IsDisposed) return;
            try {frame.Sync(this,settingsPage?18*(float)settingsScale-.5f:ClientSize.Height/2f-.5f);}
            catch(Exception) {frameFailed=true;frameWarning="圆角合成失败，已回退到普通窗口；鼠标保护状态未改变。";frame.Dispose();frame=null;OnSizeChanged(EventArgs.Empty);}
        }
        protected override void OnLocationChanged(EventArgs e) {base.OnLocationChanged(e);SyncFrame();}
        protected override void OnVisibleChanged(EventArgs e) {base.OnVisibleChanged(e);SyncFrame();}
        protected override void OnActivated(EventArgs e) {base.OnActivated(e);SyncFrame();}
        void ShowDetails()
        {
            UiDialogs.Show(this,"CursorGuard 1.2.4\n\n"+UiText.Display(view.Status)+"\n"+UiText.Display(view.Detail)+"\n"+UiText.Display(view.Notice)+"\n"+UiText.Display(floating.Warning)+"\n"+UiText.Display(frameWarning)+UiText.T("\n\n目标程序：")+view.Settings.TargetExecutable+UiText.T("\n紧急释放：")+view.Settings.Emergency.Display()+UiText.T("\n退出：")+view.Settings.Exit.Display()+UiText.T("\n\n悬浮条可拖动；从托盘显示、隐藏或重置。独占全屏下可能不可见。"),UiText.T("详情与帮助"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        void OpenSettings()
        {
            if(settingsPage) {RefreshView();return;}
            if(!settingsPage) {mainAnchor=Bounds;settingsScale=ClientSize.Width/(double)pageWidth;placementSignature=null;}
            SetDraft(view.Settings);settingsPage=true;session.Send(Command.BeginSettings);RefreshView();
        }
        void CancelSettings() {session.Send(Command.CancelSettings);LeaveSettings();RefreshView();}
        void BeginProgramChoice() {pendingProgramChoice=true;OpenSettings();}
        void LeaveSettings()
        {
            pendingProgramChoice=false;settingsPage=false;UpdatePage();
            if(mainAnchor.HasValue) {Rectangle original=mainAnchor.Value;Location=original.Location;mainAnchor=null;placementSignature=null;}
        }
        void ChooseProgram()
        {
            if(preview || !view.Editing) return;
            if(programMenu!=null) programMenu.Dispose();
            programMenu=new ContextMenuStrip {BackColor=Theme.Background,ForeColor=Theme.Text};ContextMenuStrip choices=programMenu;
                choices.Items.Add(UiText.T("运行中的程序…"),null,delegate {using(ProgramPicker picker=new ProgramPicker(false,ProgramDiscovery.Read())) if(picker.ShowDialog(this)==DialogResult.OK) target.Text=picker.SelectedExecutable;});
                choices.Items.Add(UiText.T("选择 .exe 文件…"),null,delegate {using(OpenFileDialog files=new OpenFileDialog {Filter=UiText.T("程序 (*.exe)|*.exe"),CheckFileExists=true,Multiselect=false,Title=UiText.T("选择目标程序（不会运行）")}) if(files.ShowDialog(this)==DialogResult.OK) {try {target.Text=ProgramSelection.FromFile(files.FileName);}catch(ArgumentException error) {UiDialogs.Show(this,UiText.ExceptionMessage(error),UiText.T("请选择程序"),MessageBoxButtons.OK,MessageBoxIcon.Information);}}});
                choices.Items.Add(UiText.T("手动输入"),null,delegate {target.Focus();target.SelectAll();});choices.Show(chooseProgram,new Point(0,chooseProgram.Height));
        }
        void SetDraft(Settings settings) { for(int i=0;i<3;i++) shortcuts[i].SetValue(settings.Keys[i]); startup.Checked=settings.StartWithWindows; target.Text=settings.TargetExecutable;targetDisplay.SelectedIndex=settings.TargetDisplay=="name"?1:0; }
        void SaveSettings()
        {
            foreach(ShortcutButton key in shortcuts) if(key.Problem!=null) { UiDialogs.Show(this,UiText.Display(key.Problem),UiText.T("快捷键未保存"),MessageBoxButtons.OK,MessageBoxIcon.Information); return; }
            Settings candidate=new Settings { Toggle=shortcuts[0].Value.Copy(),Emergency=shortcuts[1].Value.Copy(),Exit=shortcuts[2].Value.Copy(),StartWithWindows=startup.Checked,TargetExecutable=target.Text.Trim(),TargetDisplay=targetDisplay.SelectedIndex==1?"name":"icon" };
            string problem=candidate.Validate(); if(problem!=null) { UiDialogs.Show(this,UiText.Display(problem),UiText.T("快捷键未保存"),MessageBoxButtons.OK,MessageBoxIcon.Information); return; }
            pendingSave=true; session.SaveSettings(candidate);
        }
        void RefreshView()
        {
            RefreshLanguage();RefreshTheme();
            view=session.ReadView(); toggle.Checked=view.Enabled; toggle.Enabled=view.CanEnable && !view.Stopped;
            if(tips!=null && tips.GetToolTip(this)!=StatusPresentation.Full(view)) tips.SetToolTip(this,StatusPresentation.Full(view));
            if(floating!=null) { floating.Refresh(view,floating.Preferences.Enabled ? ReadMonitors() : monitors); floatingToggle.Checked=floating.Preferences.Enabled; }
            if(tray!=null)
            {
                string kind=StatusPresentation.Kind(view); if(kind=="loading") kind="waiting";
                if(trayKind!=kind) { tray.Icon=icons[kind]; trayKind=kind; }
                tray.Text="CursorGuard · "+StatusPresentation.Heading(view); trayToggle.Enabled=view.CanEnable && !view.Stopped; trayFloating.Checked=floating.Preferences.Enabled;
            }
            if(pendingSave && !view.Editing)
            {
                pendingSave=false; SetDraft(view.Settings);
                if(!preview && !String.IsNullOrEmpty(view.Notice) && !view.Notice.StartsWith("设置已保存",StringComparison.Ordinal)) UiDialogs.Show(this,UiText.Display(view.Notice),UiText.T("设置结果"),MessageBoxButtons.OK,MessageBoxIcon.Information);
                else LeaveSettings();
            }
            UpdatePage();RefreshProgramIcon();SyncFrame();Invalidate();
            if(!preview && pendingProgramChoice && view.Editing && !pendingSave) {pendingProgramChoice=false;ChooseProgram();}
            if(view.Stopped && !closing) Close();
        }
        void RefreshProgramIcon()
        {
            string targetName=StatusPresentation.FileName(view),description=StatusPresentation.Full(view)+UiText.T("\n点击选择程序");
            programIcon.AccessibleName=targetName+UiText.T("，选择程序");programIcon.AccessibleDescription=description;
            programIcon.NameOnly=view.Settings.TargetDisplay=="name";programIcon.Text=programIcon.NameOnly?StatusPresentation.Name(view):"";
            if(tips!=null && tips.GetToolTip(programIcon)!=description) tips.SetToolTip(programIcon,description);
            int pixels=Math.Max(16,(int)Math.Round(20*programIcon.Height/30.0));
            if(iconTarget!=targetName || iconPixels!=pixels) {iconTarget=targetName;iconPixels=pixels;iconGeneration++;iconRead=DateTime.MinValue;programIcon.SetImage(null);if(programIcons!=null) programIcons.Reset();}
            if(programIcon.NameOnly || settingsPage || preview || !IsHandleCreated || !Visible || iconBusy || DateTime.UtcNow<iconRead) return;
            iconBusy=true;long generation=iconGeneration;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                Bitmap image=programIcons.Read(targetName,pixels);
                try
                {
                    if(IsDisposed || Disposing || !IsHandleCreated) {if(image!=null) image.Dispose();return;}
                    BeginInvoke((MethodInvoker)delegate
                    {
                        iconBusy=false;
                        if(IsDisposed || generation!=iconGeneration) {if(image!=null) image.Dispose();return;}
                        programIcon.SetImage(image);iconRead=DateTime.UtcNow.AddSeconds(5);
                    });
                }
                catch(InvalidOperationException) {if(image!=null) image.Dispose();}
            });
        }
        void RefreshLanguage()
        {
            if(appearance.DroppedDown || language.DroppedDown || targetDisplay.DroppedDown) return;
            string choice=floating==null?UiText.Language:floating.Preferences.Language;UiText.SetLanguage(choice);
            int saved=LanguageSelection.Index(choice);
            if(language.SelectedIndex!=saved) language.SelectedIndex=language.Items.Count>saved?saved:-1;
            if(languageSignature==choice) return;languageSignature=choice;
            UiText.Apply(this);UiText.ApplyMenu(panelMenu);UiText.ApplyMenu(moreMenu);UiText.ApplyMenu(trayMenu);UiText.ApplyMenu(programMenu);
            int selected=appearance.SelectedIndex;appearance.Items.Clear();appearance.Items.AddRange(new object[] {UiText.T("跟随系统"),UiText.T("浅色"),UiText.T("深色")});appearance.SelectedIndex=selected<0?0:selected;
            language.Items.Clear();language.Items.AddRange(new object[] {"中文","English"});language.SelectedIndex=saved;
            int displayIndex=targetDisplay.SelectedIndex;targetDisplay.Items.Clear();targetDisplay.Items.AddRange(new object[] {UiText.T("程序图标"),UiText.T("程序名称")});targetDisplay.SelectedIndex=displayIndex<0?0:displayIndex;
            Invalidate();settingsBody.Invalidate();
        }
        void RefreshTheme()
        {
            if(preview) return;
            if(systemTheme==null || DateTime.UtcNow-themeRead>TimeSpan.FromSeconds(1)) {systemTheme=SystemThemeReader.Read();themeRead=DateTime.UtcNow;}
            string choice=floating.Preferences.Appearance;bool dark=ThemeChoice.IsDark(choice,systemTheme.AppsLight);string signature=choice+dark+systemTheme.TaskbarLight+systemTheme.HighContrast;
            updatingAppearance=true;AppearanceSelection.Reconcile(appearance,choice);updatingAppearance=false;
            if(appearance.DroppedDown || language.DroppedDown || targetDisplay.DroppedDown) return; // Do not restyle/recreate a native popup while it tracks the mouse.
            if(signature==themeSignature) return;themeSignature=signature;Theme.Apply(dark,systemTheme.HighContrast);BackColor=Theme.Background;ForeColor=Theme.Text;
            ApplyControlTheme(this);
            if(trayMenu!=null) {foreach(ContextMenuStrip menu in new[] {trayMenu,moreMenu,panelMenu}) {menu.BackColor=Theme.Background;menu.ForeColor=Theme.Text;}}
            if(tray!=null)
            {
                Dictionary<string,Icon> previous=new Dictionary<string,Icon>(icons);
                foreach(string kind in new[] {"paused","waiting","protected","error"}) icons[kind]=TrayIcons.Create(kind,!systemTheme.TaskbarLight,systemTheme.HighContrast);
                tray.Icon=icons[trayKind??"paused"];foreach(Icon old in previous.Values) old.Dispose();
            }
            if(IsHandleCreated) WindowTheme.Apply(Handle,dark,Theme.Background);Invalidate();
        }
        protected override void OnHandleCreated(EventArgs e) {base.OnHandleCreated(e);if(!preview) WindowTheme.Apply(Handle,Theme.Dark,Theme.Background);}
        static void ApplyControlTheme(Control parent)
        {foreach(Control c in parent.Controls) {if(c.BackColor!=Theme.Background) c.BackColor=Theme.Background;if(c.ForeColor!=Theme.Text) c.ForeColor=Theme.Text;c.Invalidate();ApplyControlTheme(c);}}
        void UpdatePage()
        {
            string name=StatusPresentation.Name(view),status=StatusPresentation.Short(view),layoutKey=name+"\n"+status+"\n"+view.Settings.TargetDisplay;
            bool nameOnly=view.Settings.TargetDisplay=="name";
            if(mainLayout==null || layoutName!=layoutKey) {mainLayout=nameOnly?CompactBarLayout.ForMain(name,status):CompactBarLayout.ForMainIcon(status);layoutName=layoutKey;}
            int wantedWidth=settingsPage?380:mainLayout.Width,wantedHeight=settingsPage?402:44;
            if(pageWidth!=wantedWidth || pageHeight!=wantedHeight)
            {
                double scale=!preview && mainAnchor.HasValue?settingsScale:ClientSize.Width/(double)pageWidth;
                pageWidth=wantedWidth;pageHeight=wantedHeight;
                ClientSize=new Size((int)Math.Round(pageWidth*scale),(int)Math.Round(pageHeight*scale));
            }
            if(settingsPage && !preview && mainAnchor.HasValue)
            {
                Screen screen=Screen.FromRectangle(mainAnchor.Value);Rectangle work=screen.WorkingArea;
                foreach(MonitorArea area in ReadMonitors()) if(area.Name==screen.DeviceName) settingsScale=area.Scale;
                string signature=work.ToString()+settingsScale;
                if(placementSignature!=signature)
                {
                    placementSignature=signature;
                    Bounds=SettingsPlacement.Calculate(mainAnchor.Value,work,new Size((int)Math.Round(380*settingsScale),(int)Math.Round(402*settingsScale)),settingsScale);
                    OnSizeChanged(EventArgs.Empty);
                }
            }
            double uiScale=settingsPage && !preview?settingsScale:ClientSize.Width/(double)pageWidth;
            int header=(int)Math.Round(40*uiScale);
            settingsBody.Bounds=new Rectangle(0,header,ClientSize.Width,Math.Max(1,ClientSize.Height-header));
            settingsBody.AutoScrollMinSize=new Size((int)Math.Round(360*uiScale),(int)Math.Round(362*uiScale));
            more.Location=new Point(ClientSize.Width-(int)Math.Round(97*uiScale),(int)Math.Round(5*uiScale));
            done.Location=new Point(ClientSize.Width-(int)Math.Round(60*uiScale),(int)Math.Round(5*uiScale));
            if(!settingsPage)
            {
                double scale=ClientSize.Width/(double)pageWidth;
                toggle.Location=new Point((int)Math.Round(mainLayout.ToggleX*scale),(int)Math.Round(10*scale));
                settingsButton.Location=new Point((int)Math.Round(mainLayout.SettingsX*scale),(int)Math.Round(7*scale));
                programIcon.Bounds=new Rectangle((int)Math.Round((nameOnly?mainLayout.NameX:mainLayout.IconX)*scale),(int)Math.Round(7*scale),(int)Math.Round((nameOnly?mainLayout.NameWidth:mainLayout.IconWidth)*scale),(int)Math.Round(30*scale));
            }
            toggle.Visible=settingsButton.Visible=programIcon.Visible=!settingsPage;
            programIcon.Enabled=!view.Stopped && !pendingSave;
            settingsBody.Visible=settingsPage;
            back.Visible=done.Visible=more.Visible=startup.Visible=floatingToggle.Visible=target.Visible=chooseProgram.Visible=appearance.Visible=language.Visible=targetDisplay.Visible=settingsPage;
            foreach(ShortcutButton key in shortcuts) { key.Visible=settingsPage; key.Enabled=view.Editing && !pendingSave; }
            startup.Enabled=target.Enabled=chooseProgram.Enabled=targetDisplay.Enabled=view.Editing && !pendingSave; done.Text=view.Editing?UiText.T("完成"):UiText.T("编辑"); done.Enabled=!view.Stopped && !pendingSave;
            back.Enabled=!pendingSave;
            // Editing deliberately suspends global shortcuts. Keep the direct
            // hide action available after return/save, when emergency is restored.
            panelMenu.Items[1].Enabled=!view.Editing && !pendingSave;
            panelMenu.Items[1].ToolTipText=UiText.T("完成设置或返回后可隐藏");
            if(moreHide!=null) moreHide.Enabled=!view.Editing && !pendingSave;
        }
        PlainButton ButtonAt(string text,int x,int y,int w,int h,EventHandler action)
        { PlainButton b=new PlainButton { Text=text,Location=new Point(x,y),Size=new Size(w,h) }; b.Click+=action; Controls.Add(b); return b; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
            float scale=settingsPage && !preview?(float)settingsScale:ClientSize.Width/(float)pageWidth;g.ScaleTransform(scale,scale);
            Theme.Surface(g,new RectangleF(.5f,.5f,ClientSize.Width/scale-1,ClientSize.Height/scale-1),settingsPage?18:21.5f);
            if(!settingsPage)
            {
                Theme.Write(g,StatusPresentation.Short(view),mainLayout.StatusX,8,mainLayout.StatusWidth,28,UiTypography.Caption,Theme.Text,false,StringAlignment.Near);
                if(view.Settings.TargetDisplay=="name") Theme.Write(g,"·",mainLayout.SeparatorX,8,8,28,UiTypography.Caption,Theme.Muted,false,StringAlignment.Center);
                using(Pen p=new Pen(Theme.Line)) {g.DrawLine(p,mainLayout.FirstDividerX,12,mainLayout.FirstDividerX,32);g.DrawLine(p,mainLayout.SecondDividerX,12,mainLayout.SecondDividerX,32);}return;
            }
        }
        void PaintSettingsContent(Graphics g)
        {
            float scale=preview?ClientSize.Width/(float)pageWidth:(float)settingsScale;
            g.ScaleTransform(scale,scale);g.TranslateTransform(0,-40);
            string[] labels={UiText.T("启用 / 暂停"),UiText.T("紧急释放"),UiText.T("退出")};
            for(int i=0;i<3;i++) Theme.Write(g,labels[i],16,42+i*37,140,30,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,149,364,149);
            Theme.Write(g,UiText.T("开机自启"),16,155,250,32,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,191,364,191);
            Theme.Write(g,UiText.T("状态悬浮条"),16,197,250,32,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,233,364,233);
            Theme.Write(g,UiText.T("目标程序"),16,238,112,32,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,272,364,272);
            Theme.Write(g,UiText.T("外观"),16,277,112,32,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,310,364,310);
            Theme.Write(g,UiText.T("语言"),16,315,112,32,13,Theme.Text,false,StringAlignment.Near);
            using(Pen pen=new Pen(Theme.Line)) g.DrawLine(pen,16,348,364,348);
            Theme.Write(g,UiText.T("目标显示"),16,353,112,32,13,Theme.Text,false,StringAlignment.Near);
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            closing=true; if(timer!=null) { timer.Stop(); timer.Dispose(); }
            if(floating!=null) floating.Dispose(); if(tray!=null) { tray.Visible=false; tray.Dispose(); }
            if(tips!=null) tips.Dispose();
            if(trayMenu!=null) trayMenu.Dispose(); if(moreMenu!=null) moreMenu.Dispose(); foreach(Icon icon in icons.Values) icon.Dispose(); session.Dispose(); base.OnFormClosed(e);
            if(programMenu!=null) programMenu.Dispose();if(panelMenu!=null) panelMenu.Dispose();
            if(frame!=null) {frame.Dispose();frame=null;}
        }
        public void PreviewSettings() { settingsPage=true; SetDraft(view.Settings); UpdatePage(); }
        public void PreviewOpenSettingsAt(Rectangle main,Rectangle work,double scale)
        {
            if(!preview) throw new InvalidOperationException();mainAnchor=main;settingsScale=scale;settingsPage=true;
            UpdatePage();Bounds=SettingsPlacement.Calculate(main,work,new Size((int)Math.Round(380*scale),(int)Math.Round(402*scale)),scale);
            settingsBody.Bounds=new Rectangle(0,(int)Math.Round(40*scale),ClientSize.Width,Math.Max(1,ClientSize.Height-(int)Math.Round(40*scale)));
            settingsBody.AutoScrollMinSize=new Size((int)Math.Round(360*scale),(int)Math.Round(362*scale));
            OnSizeChanged(EventArgs.Empty);
        }
        public void PreviewReturn() {if(!preview) throw new InvalidOperationException();LeaveSettings();}
        public void PreviewRefresh() {if(!preview) throw new InvalidOperationException();RefreshView();}
        public string PreviewProgramToolTip {get{return programIcon.AccessibleDescription;}}
        public bool PreviewHasProgramIcon {get{return programIcon.HasProgramImage;}}
        public Rectangle PreviewProgramClickBounds {get{return programIcon.Bounds;}}
        public void PreviewClickProgram() {if(!preview) throw new InvalidOperationException();BeginProgramChoice();}
        public void PreviewUseIconFile(string path)
        {if(!preview) throw new InvalidOperationException();programIcon.SetImage(new NativeProgramIconSource().Extract(path,Math.Max(16,(int)Math.Round(programIcon.Height*20/30.0))));}
        public void PreviewHide() {if(!preview) throw new InvalidOperationException();HidePanel();}
        public void PreviewInvokeHideEntry() {if(!preview) throw new InvalidOperationException();panelMenu.Items[1].PerformClick();}
        public bool PreviewHideEntryEnabled {get{return panelMenu.Items[1].Enabled;}}
        public void PreviewScrollToAppearance() {if(!preview) throw new InvalidOperationException();settingsBody.ScrollControlIntoView(appearance);}
        public void PreviewScrollToLanguage() {if(!preview) throw new InvalidOperationException();settingsBody.ScrollControlIntoView(language);}
        public void PreviewScrollToTargetDisplay() {if(!preview) throw new InvalidOperationException();settingsBody.ScrollControlIntoView(targetDisplay);}
        public ComboBox PreviewLanguageChoice {get{return language;}}
        public ComboBox PreviewTargetDisplayChoice {get{return targetDisplay;}}
        public string PreviewTargetCaption {get{return programIcon.Text;}}
        public void PreviewOpenSettings() {if(!preview) throw new InvalidOperationException();OpenSettings();}
        public void PreviewCancelSettings() {if(!preview) throw new InvalidOperationException();CancelSettings();}
        public void PreviewSaveSettings() {if(!preview) throw new InvalidOperationException();SaveSettings();}
        public void PreviewCloseSettingsWindow() {if(!preview) throw new InvalidOperationException();OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));}
        public ContextMenuStrip PreviewPanelMenu {get{return panelMenu;}}
        public void PreviewCommitLanguage(FloatingController saved,int index)
        {if(!preview) throw new InvalidOperationException();language.SelectedIndex=index;LanguageSelection.Commit(language,saved.SetLanguage,delegate {UiText.SetLanguage(saved.Preferences.Language);RefreshView();});}
        public bool PreviewScrollRequired {get{return settingsBody.AutoScrollMinSize.Height>settingsBody.ClientSize.Height;}}
        public int PreviewFrameUpdates {get{return frame==null?0:frame.Updates;}}
        public IntPtr PreviewFrameHandle {get{return frame==null?IntPtr.Zero:frame.Handle;}}
        protected override void Dispose(bool disposing) {if(disposing) {if(frame!=null) {frame.Dispose();frame=null;}if(programIcons!=null) programIcons.Dispose();if(panelMenu!=null) panelMenu.Dispose();}base.Dispose(disposing);}
        public Bitmap RenderPreview(float scale)
        {
            if(!preview || scale<1 || scale>3) throw new ArgumentOutOfRangeException("scale");
            if(scale!=1) Scale(new SizeF(scale,scale));
            return RenderPreview();
        }
        public Bitmap RenderPreview()
        {
            // Actual client renderer with transparent rounded corners. No window
            // is shown, and no worker, native input or hotkey backend is created.
            Bitmap b=new Bitmap(ClientSize.Width,ClientSize.Height);
            using(Graphics g=Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);OnPaint(new PaintEventArgs(g,ClientRectangle));g.ResetTransform();
                float scale=ClientSize.Width/(float)pageWidth;
                using(GraphicsPath clip=Theme.Rounded(new RectangleF(0,0,b.Width,b.Height),settingsPage?18*scale:b.Height/2f)) g.SetClip(clip);
                if(settingsPage) {GraphicsState state=g.Save();g.SetClip(settingsBody.Bounds,CombineMode.Intersect);g.TranslateTransform(settingsBody.Left,settingsBody.Top);settingsBody.PaintPreview(g);g.Restore(state);}
                Control[] children=settingsPage ? new Control[] {back,more,done,shortcuts[0],shortcuts[1],shortcuts[2],startup,floatingToggle,target,chooseProgram,appearance,language,targetDisplay} : new Control[] {programIcon,toggle,settingsButton};
                foreach(Control child in children)
                {
                    using(Bitmap c=new Bitmap(child.Width,child.Height)) { using(Graphics cg=Graphics.FromImage(c)) { PlainButton button=child as PlainButton; SwitchControl sw=child as SwitchControl; if(button!=null) button.PaintPreview(cg); else if(sw!=null) sw.PaintPreview(cg); else { Theme.ControlBackdrop(cg,child); using(Pen p=new Pen(Theme.Line)) cg.DrawRectangle(p,0,0,c.Width-1,c.Height-1); Theme.Write(cg,child.Text,6*scale,0,c.Width-12*scale,c.Height,UiTypography.Body*scale,Theme.Text,false,StringAlignment.Near); if(child==appearance || child==language || child==targetDisplay) Theme.Write(cg,"⌄",c.Width-22*scale,0,18*scale,c.Height,13*scale,Theme.Muted,false,StringAlignment.Center); } } GraphicsState childClip=g.Save();if(child.Parent==settingsBody) g.SetClip(settingsBody.Bounds,CombineMode.Intersect);g.DrawImageUnscaled(c,child.Left+(child.Parent==settingsBody?settingsBody.Left:0),child.Top+(child.Parent==settingsBody?settingsBody.Top:0));g.Restore(childClip); }
                }
            }
            return b;
        }
    }
    sealed class PreviewSession : IUiSession
    {
        readonly View view;
        public PreviewSession(View v) { view=v; }
        public View ReadView() { return view; }
        public void Send(Command c) { if(c==Command.BeginSettings) view.Editing=true; }
        public void SaveSettings(Settings settings) { view.Settings=settings.Copy(); }
        public void Dispose() { }
    }
    static class Previews
    {
        public static View State(string kind)
        {
            View v=new View { CanEnable=true,Status="已暂停" };
            if(kind=="waiting") { v.Enabled=true; v.Status="已启用，当前不约束"; }
            if(kind=="protected") { v.Enabled=true; v.Status="保护中"; }
            if(kind=="error") { v.CanEnable=false; v.Status="快捷键不可用，禁止启用"; }
            if(kind=="settings") v.Editing=true; return v;
        }
        public static int Render(string directory)
        {
            Directory.CreateDirectory(directory); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            string[] names={"paused","waiting","protected","error","settings"};
            foreach(bool dark in new[] {false,true})
            {
                Theme.Apply(dark,false);
                foreach(string name in names) using(ControlPanel form=new ControlPanel(new PreviewSession(State(name)),true,false))
                { if(name=="settings") form.PreviewSettings(); using(Bitmap b=form.RenderPreview()) b.Save(Path.Combine(directory,"preview-"+name+(dark?"-dark":"")+".png"),ImageFormat.Png); }
                string[] targets={"League of Legends.exe","Paint.exe","AnExtremelyLongTargetProgramNameForLayoutVerification.exe"};
                string[] targetLabels={"league","short","long"};
                for(int i=0;i<targets.Length;i++)
                {
                    View sample=State("paused");sample.Settings.TargetExecutable=targets[i];sample.Settings.TargetDisplay="name";
                    using(ControlPanel form=new ControlPanel(new PreviewSession(sample),true,false)) using(Bitmap b=form.RenderPreview()) b.Save(Path.Combine(directory,"preview-name-"+targetLabels[i]+(dark?"-dark":"")+".png"),ImageFormat.Png);
                    using(FloatingBar bar=new FloatingBar(true)) {bar.Present(sample);using(Bitmap b=bar.PreviewBitmap(1)) b.Save(Path.Combine(directory,"preview-floating-name-"+targetLabels[i]+(dark?"-dark":"")+".png"),ImageFormat.Png);}
                }
                foreach(float scale in new[] {1.25f,1.5f,2f})
                {
                    int percent=(int)Math.Round(scale*100);
                    using(ControlPanel form=new ControlPanel(new PreviewSession(State("paused")),true,false))
                    using(Bitmap b=form.RenderPreview(scale)) b.Save(Path.Combine(directory,"preview-paused-dpi"+percent+(dark?"-dark":"")+".png"),ImageFormat.Png);
                    using(ControlPanel form=new ControlPanel(new PreviewSession(State("settings")),true,false))
                    {form.PreviewSettings();using(Bitmap b=form.RenderPreview(scale)) b.Save(Path.Combine(directory,"preview-settings-dpi"+percent+(dark?"-dark":"")+".png"),ImageFormat.Png);}
                    using(FloatingBar bar=new FloatingBar(true))
                    {bar.Present(State("protected"));using(Bitmap b=bar.PreviewBitmap(scale)) b.Save(Path.Combine(directory,"preview-floating-dpi"+percent+(dark?"-dark":"")+".png"),ImageFormat.Png);}
                    View longName=State("waiting");longName.Settings.TargetDisplay="name";longName.Settings.TargetExecutable="AnExtremelyLongTargetProgramNameForLayoutVerification.exe";
                    using(ControlPanel form=new ControlPanel(new PreviewSession(longName),true,false))
                    using(Bitmap b=form.RenderPreview(scale)) b.Save(Path.Combine(directory,"preview-long-name-dpi"+percent+(dark?"-dark":"")+".png"),ImageFormat.Png);
                }
                foreach(float scale in new[] {1f,1.25f,1.5f,2f})
                {
                    string iconFile=typeof(Program).Assembly.Location;View own=State("paused");own.Settings.TargetExecutable=Path.GetFileName(iconFile);
                    using(ControlPanel form=new ControlPanel(new PreviewSession(own),true,false))
                    {using(Bitmap ignored=form.RenderPreview(scale)) {}form.PreviewUseIconFile(iconFile);using(Bitmap bitmap=form.RenderPreview()) bitmap.Save(Path.Combine(directory,"preview-file-icon-dpi"+(int)(scale*100)+(dark?"-dark":"")+".png"),ImageFormat.Png);}
                }
                using(ProgramPicker picker=new ProgramPicker(true,new[] {new ProgramCandidate {Title=UiText.T("英雄联盟 · 模拟窗口"),Executable="League of Legends.exe",Pid=100},new ProgramCandidate {Title=UiText.T("示例程序 · 模拟窗口"),Executable="ExampleGame.exe",Pid=200}})) using(Bitmap b=picker.PreviewBitmap()) b.Save(Path.Combine(directory,"preview-program-picker"+(dark?"-dark":"")+".png"),ImageFormat.Png);
            }
            Theme.Apply(false,false);
            using(Bitmap board=new Bitmap(472,180)) using(Graphics g=Graphics.FromImage(board))
            {
                g.Clear(Color.FromArgb(240,242,245)); Theme.Write(g,UiText.T("状态悬浮条 · 四种状态"),16,5,348,26,13,Theme.Text,false,StringAlignment.Near);
                for(int i=0;i<4;i++) using(FloatingBar bar=new FloatingBar(true)) {Theme.Apply(i>=2,false);bar.Present(State(names[i])); using(Bitmap b=bar.PreviewBitmap()) g.DrawImageUnscaled(b,16+(i%2)*230,44+(i/2)*54); }
                Theme.Apply(false,false);
                Theme.Write(g,UiText.T("模拟状态预览 · 原尺寸"),16,151,348,20,10,Theme.Muted,false,StringAlignment.Near); board.Save(Path.Combine(directory,"preview-floating.png"),ImageFormat.Png);
            }
            TrayIcons.Export(Path.Combine(directory,"icons")); return 0;
        }
    }
}

