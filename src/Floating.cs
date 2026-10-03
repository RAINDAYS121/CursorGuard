// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LoLMouseGuard
{
    public sealed class FloatingPreferences
    {
        public int Version = 1;
        public bool Enabled, HasPosition;
        public string Monitor;
        public string Appearance = "system";
        public string Language = "zh-CN";
        public double X, Y;
        public FloatingPreferences Copy() { return new FloatingPreferences { Version = Version, Enabled = Enabled, HasPosition = HasPosition, Monitor = Monitor, X = X, Y = Y, Appearance = Appearance, Language = Language }; }
        public bool Valid { get { return Version == 1 && (Appearance == "system" || Appearance == "light" || Appearance == "dark") && !Double.IsNaN(X) && !Double.IsInfinity(X) && !Double.IsNaN(Y) && !Double.IsInfinity(Y) && Math.Abs(X) < 1000000 && Math.Abs(Y) < 1000000; } }
    }
    public interface IFloatingStore { FloatingPreferences Load(out string warning); void Save(FloatingPreferences preferences); }
    public sealed class FloatingFileStore : IFloatingStore
    {
        readonly string path;
        public FloatingFileStore(string p) { path = p; }
        public FloatingPreferences Load(out string warning)
        {
            warning = null; if (!File.Exists(path)) return new FloatingPreferences();
            try { FloatingPreferences p = new JavaScriptSerializer().Deserialize<FloatingPreferences>(File.ReadAllText(path, Encoding.UTF8)); if (p == null || !p.Valid) throw new FormatException(); p.Language=UiText.Normalize(p.Language); return p; }
            catch (Exception) { warning = "悬浮条配置无法读取，已恢复默认关闭。"; return new FloatingPreferences(); }
        }
        public void Save(FloatingPreferences p)
        {
            if (!p.Valid) throw new FormatException("悬浮条位置无效。");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp", backup = temp + ".bak";
            try { File.WriteAllText(temp, new JavaScriptSerializer().Serialize(p), new UTF8Encoding(false)); if (File.Exists(path)) File.Replace(temp, path, backup); else File.Move(temp, path); }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (Exception) { } try { if (File.Exists(backup)) File.Delete(backup); } catch (Exception) { } }
        }
    }
    public sealed class MonitorArea
    {
        public string Name;
        public Rectangle Work;
        public double Scale = 1;
    }
    public static class FloatingGeometry
    {
        public const int Width = 210, Height = 30;
        public static Rectangle Clamp(Rectangle r, Rectangle work)
        {
            int width = Math.Min(r.Width, Math.Max(1, work.Width)), height = Math.Min(r.Height, Math.Max(1, work.Height));
            return new Rectangle(Math.Max(work.Left, Math.Min(r.Left, work.Right - width)), Math.Max(work.Top, Math.Min(r.Top, work.Bottom - height)), width, height);
        }
        public static Rectangle Restore(FloatingPreferences p, IList<MonitorArea> screens)
        {return Restore(p,screens,Width);}
        public static Rectangle Restore(FloatingPreferences p, IList<MonitorArea> screens,int logicalWidth)
        {
            if(logicalWidth<1 || logicalWidth>CompactBarLayout.FloatingMaximum) throw new ArgumentOutOfRangeException("logicalWidth");
            if (screens.Count == 0) throw new InvalidOperationException("没有可用显示器。");
            MonitorArea screen = screens[0]; bool found = false;
            foreach (MonitorArea candidate in screens) if (p.HasPosition && candidate.Name == p.Monitor) { screen = candidate; found = true; break; }
            double scale = screen.Scale > 0 && screen.Scale < 8 ? screen.Scale : 1;
            int width = (int)Math.Round(logicalWidth * scale), height = (int)Math.Round(Height * scale);
            int x = found ? screen.Work.Left + (int)Math.Round(p.X * scale) : screen.Work.Right - width - (int)Math.Round(20 * scale);
            int y = found ? screen.Work.Top + (int)Math.Round(p.Y * scale) : screen.Work.Top + (int)Math.Round(20 * scale);
            return Clamp(new Rectangle(x, y, width, height), screen.Work);
        }
        public static FloatingPreferences Capture(FloatingPreferences old, Rectangle bounds, IList<MonitorArea> screens)
        {
            if (screens.Count == 0) return old.Copy();
            MonitorArea selected = screens[0]; long best = Int64.MaxValue;
            long centerX = (long)bounds.Left + bounds.Width / 2, centerY = (long)bounds.Top + bounds.Height / 2;
            foreach (MonitorArea m in screens)
            {
                long dx = centerX < m.Work.Left ? m.Work.Left - centerX : centerX > m.Work.Right ? centerX - m.Work.Right : 0;
                long dy = centerY < m.Work.Top ? m.Work.Top - centerY : centerY > m.Work.Bottom ? centerY - m.Work.Bottom : 0;
                long distance = dx * dx + dy * dy; if (distance < best) { best = distance; selected = m; }
            }
            Rectangle safe = Clamp(bounds, selected.Work); double scale = selected.Scale > 0 && selected.Scale < 8 ? selected.Scale : 1;
            FloatingPreferences p = old.Copy(); p.HasPosition = true; p.Monitor = selected.Name; p.X = (safe.Left - selected.Work.Left) / scale; p.Y = (safe.Top - selected.Work.Top) / scale; return p;
        }
        public static string Signature(IList<MonitorArea> screens)
        { StringBuilder s = new StringBuilder(); foreach (MonitorArea m in screens) s.Append(m.Name).Append(m.Work).Append(m.Scale.ToString(System.Globalization.CultureInfo.InvariantCulture)); return s.ToString(); }
    }
    public interface IFloatingWindow : IDisposable
    {
        bool Shown { get; }
        void Place(Rectangle bounds);
        void Present(View view);
        void ShowPassive();
        void HidePassive();
    }
    public sealed class FloatingController : IDisposable
    {
        readonly IFloatingStore store;
        readonly Func<IFloatingWindow> factory;
        IFloatingWindow window;
        string screenSignature;
        string targetName;
        int logicalWidth=FloatingGeometry.Width;
        bool disposed;
        public FloatingPreferences Preferences { get; private set; }
        public string Warning { get; private set; }
        public FloatingController(IFloatingStore s, Func<IFloatingWindow> f)
        { store = s; factory = f; string warning; Preferences = store.Load(out warning); Preferences.Language=UiText.Normalize(Preferences.Language); Warning = warning; }
        public bool SetEnabled(bool enabled)
        {
            if (disposed) return false; FloatingPreferences p = Preferences.Copy(); p.Enabled = enabled;
            try { store.Save(p); Preferences = p; Warning = null; if (!enabled && window != null) window.HidePassive(); return true; }
            catch (Exception) { Warning = "悬浮条设置保存失败，保留原设置。"; return false; }
        }
        public bool SetAppearance(string choice)
        {
            if (disposed) return false; FloatingPreferences p = Preferences.Copy(); p.Appearance = choice;
            try { if (!p.Valid) throw new FormatException(); store.Save(p); Preferences = p; Warning = null; return true; }
            catch (Exception) { Warning = "外观设置保存失败，保持原设置。"; return false; }
        }
        public bool SetLanguage(string choice)
        {
            if(disposed) return false; FloatingPreferences p=Preferences.Copy();p.Language=choice;
            try {if(!UiText.Supported(choice)) throw new FormatException();store.Save(p);Preferences=p;Warning=null;return true;}
            catch(Exception) {Warning="语言设置保存失败，保持原语言。";return false;}
        }
        public void Refresh(View view, IList<MonitorArea> screens)
        {
            if (disposed) return;
            if (view.Stopped) { Dispose(); return; }
            if (!Preferences.Enabled) { if (window != null && window.Shown) window.HidePassive(); return; }
            try
            {
                bool created = window == null; if (created) window = factory();
                string name=StatusPresentation.Name(view),status=StatusPresentation.Short(view),key=name+"\n"+status;int previousWidth=logicalWidth;
                if(targetName!=key) {targetName=key;logicalWidth=CompactBarLayout.Floating(name,status).Width;}
                string signature = FloatingGeometry.Signature(screens);
                if (created || signature != screenSignature || previousWidth!=logicalWidth) { window.Place(FloatingGeometry.Restore(Preferences, screens,logicalWidth)); screenSignature = signature; }
                window.Present(view); if (!window.Shown) window.ShowPassive();
            }
            catch (Exception) { if (window != null) { window.Dispose(); window = null; } Preferences.Enabled = false; Warning = "悬浮条显示失败，已关闭；鼠标保护状态未改变。"; }
        }
        public void Moved(Rectangle bounds, IList<MonitorArea> screens)
        {
            if (disposed) return; FloatingPreferences p = FloatingGeometry.Capture(Preferences, bounds, screens);
            try { store.Save(p); Preferences = p; if (window != null) window.Place(FloatingGeometry.Restore(p, screens,logicalWidth)); Warning = null; }
            catch (Exception) { Warning = "悬浮条位置未能保存；可从托盘重置位置。"; }
        }
        public void ResetPosition()
        {
            if (disposed) return; FloatingPreferences p = Preferences.Copy(); p.HasPosition = false;
            try { store.Save(p); Preferences = p; screenSignature = null; Warning = null; }
            catch (Exception) { Warning = "悬浮条位置重置失败。"; }
        }
        public void Dispose() { if (disposed) return; disposed = true; if (window != null) { window.HidePassive(); window.Dispose(); window = null; } }
    }
    public static class FloatingWindowPolicy
    {
        public const int ExtendedStyles = 0x08000000 | 0x00000080; // NOACTIVATE, TOOLWINDOW.
        public const int MouseActivateResult = 3; // MA_NOACTIVATE, still deliver the mouse message.
    }
    static class MonitorReader
    {
        [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(Position point, uint flags);
        [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr monitor, int kind, out uint x, out uint y);
        public static IList<MonitorArea> Read()
        {
            List<MonitorArea> result = new List<MonitorArea>();
            List<Screen> screens = new List<Screen>(Screen.AllScreens); screens.Sort(delegate(Screen a, Screen b) { return b.Primary.CompareTo(a.Primary); });
            foreach (Screen s in screens)
            {
                double scale = 1;
                try { uint x, y; if (GetDpiForMonitor(MonitorFromPoint(new Position(s.Bounds.Left + 1, s.Bounds.Top + 1), 2), 0, out x, out y) == 0 && x >= 96 && x <= 768) scale = x / 96.0; }
                catch (DllNotFoundException) { } catch (EntryPointNotFoundException) { }
                result.Add(new MonitorArea { Name = s.DeviceName, Work = s.WorkingArea, Scale = scale });
            }
            return result;
        }
    }
    sealed class FloatingBar : Form, IFloatingWindow
    {
        View view = new View { CanEnable = true, Status = "已暂停" };
        readonly bool preview;
        readonly bool nativePreview;
        readonly ToolTip tips;
        string tipText;
        string targetName;
        CompactBarLayout layout;
        RoundedFrame frame;
        bool frameFailed;
        public event Action<Rectangle> PositionCommitted;
        [DllImport("user32.dll", SetLastError = true)] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr handle,uint message,IntPtr wParam,IntPtr lParam);
        public FloatingBar(bool offscreen,bool nativeOnlyPreview=false)
        {
            preview = offscreen;nativePreview=nativeOnlyPreview;Text = UiText.T("鼠标锁定状态"); FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
            targetName=StatusPresentation.Name(view);layout=CompactBarLayout.Floating(targetName);
            ClientSize = new Size(layout.Width, FloatingGeometry.Height); BackColor = Theme.Background; AutoScaleMode = AutoScaleMode.None; DoubleBuffered = true;
            if(!preview) {tips=new ToolTip {InitialDelay=500,AutoPopDelay=8000,ReshowDelay=100};frame=new RoundedFrame();}
            else if(nativePreview) frame=new RoundedFrame();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { CreateParams p = base.CreateParams; p.ExStyle |= FloatingWindowPolicy.ExtendedStyles; return p; } }
        protected override void OnSizeChanged(EventArgs e)
        {base.OnSizeChanged(e);if(ClientSize.Width<2 || ClientSize.Height<2) return;float inset=preview && !nativePreview || frameFailed?0:2;using(System.Drawing.Drawing2D.GraphicsPath p=Theme.Rounded(new RectangleF(inset,inset,ClientSize.Width-2*inset,ClientSize.Height-2*inset),Math.Max(1,ClientSize.Height/2f-inset))) {Region old=Region;Region=new Region(p);if(old!=null) old.Dispose();}SyncFrame();}
        void SyncFrame()
        {if(frame==null || frameFailed || IsDisposed) return;try {frame.Sync(this,ClientSize.Height/2f-.5f);}catch(Exception) {frameFailed=true;frame.Dispose();frame=null;OnSizeChanged(EventArgs.Empty);}}
        protected override void OnLocationChanged(EventArgs e) {base.OnLocationChanged(e);SyncFrame();}
        protected override void OnVisibleChanged(EventArgs e) {base.OnVisibleChanged(e);SyncFrame();}
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0021) { m.Result = new IntPtr(FloatingWindowPolicy.MouseActivateResult); return; }
            base.WndProc(ref m);
            if ((m.Msg == 0x0232 || m.Msg == 0x02E0) && !preview && PositionCommitted != null) PositionCommitted(Bounds);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {base.OnMouseDown(e);if(!preview && e.Button==MouseButtons.Left) {ReleaseCapture();SendMessage(Handle,0x00A1,new IntPtr(2),IntPtr.Zero);} } // Native drag message to this window only.
        public new bool Shown { get { return Visible; } }
        public IntPtr PreviewFrameHandle {get{return frame==null?IntPtr.Zero:frame.Handle;}}
        public int PreviewFrameUpdates {get{return frame==null?0:frame.Updates;}}
        public void Place(Rectangle bounds)
        { if (preview) Bounds = bounds; else if (!SetWindowPos(Handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0010 | 0x0200)) throw new System.ComponentModel.Win32Exception(); }
        public void Present(View source) { Text=UiText.T("鼠标锁定状态");view = source;string name=StatusPresentation.Name(view),status=StatusPresentation.Short(view),key=name+"\n"+status;if(targetName!=key) {targetName=key;layout=CompactBarLayout.Floating(name,status);} BackColor = Theme.Background;string full=StatusPresentation.Full(view);if(frameFailed) full+=UiText.T(" · 圆角合成失败，已回退到普通窗口");if(tips!=null && full!=tipText) {tipText=full;tips.SetToolTip(this,full);}SyncFrame();Invalidate(); }
        public void ShowPassive() { if (!preview) Show(); }
        public void HidePassive() { Hide(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.ScaleTransform(ClientSize.Width / (float)layout.Width, ClientSize.Height / (float)FloatingGeometry.Height);
            Theme.Surface(g,new RectangleF(.5f,.5f,layout.Width-1,FloatingGeometry.Height-1),14.5f);
            using (Brush b = new SolidBrush(StatusPresentation.Signal(view))) g.FillEllipse(b, 10, 12, 6, 6);
            Theme.Write(g, StatusPresentation.Name(view), layout.NameX, 3, layout.NameWidth, 24, UiTypography.Caption, Theme.Text, false, StringAlignment.Near);
            Theme.Write(g, "·", layout.SeparatorX, 3, 8, 24, UiTypography.Caption, Theme.Muted, false, StringAlignment.Center);
            Theme.Write(g, StatusPresentation.Short(view), layout.StatusX, 3, layout.StatusWidth, 24, UiTypography.Caption, Theme.Text, false, StringAlignment.Near);
        }
        public Bitmap PreviewBitmap()
        { Bitmap b = new Bitmap(ClientSize.Width, ClientSize.Height); using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.Transparent); OnPaint(new PaintEventArgs(g, ClientRectangle)); } return b; }
        public Bitmap PreviewBitmap(float scale)
        {
            if(!preview || scale<1 || scale>3) throw new ArgumentOutOfRangeException("scale");
            ClientSize=new Size((int)Math.Round(layout.Width*scale),(int)Math.Round(FloatingGeometry.Height*scale));
            return PreviewBitmap();
        }
        protected override void Dispose(bool disposing) {if(disposing) {if(tips!=null) tips.Dispose();if(frame!=null) {frame.Dispose();frame=null;}}base.Dispose(disposing);}
    }
}

