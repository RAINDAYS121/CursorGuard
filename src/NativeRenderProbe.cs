// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    static class NativeRenderProbe
    {
        [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool GetClipCursor(out Rect rect);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] static extern int GetWindowRgn(IntPtr window,IntPtr region);
        [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
        [DllImport("gdi32.dll")] static extern IntPtr CreateRectRgn(int x,int y,int right,int bottom);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr region);
        static object Metadata(IntPtr window)
        {
            IntPtr region=CreateRectRgn(0,0,0,0);int type;try {type=GetWindowRgn(window,region);}finally {DeleteObject(region);}
            long styles=GetWindowLongPtr(window,-20).ToInt64();return new {handle="0x"+window.ToInt64().ToString("x"),layered=(styles&0x80000)!=0,noactivate=(styles&0x8000000)!=0,transparent=(styles&0x20)!=0,region_type=type};
        }
        static Bitmap Capture(Form owner,float radius,out bool captured)
        {
            // Native non-layered HWND clients are opaque. GDI PrintWindow does
            // not maintain an alpha channel, so capture RGB before alpha-frame
            // composition instead of interpreting GDI bytes as premultiplied.
            Bitmap client=new Bitmap(owner.Width,owner.Height,PixelFormat.Format32bppRgb);
            using(Graphics g=Graphics.FromImage(client)) {IntPtr dc=g.GetHdc();try {captured=PrintWindow(owner.Handle,dc,0);}finally {g.ReleaseHdc(dc);}}
            Bitmap composite=RoundedFrame.Surface(owner.Size,radius);IntPtr nativeRegion=CreateRectRgn(0,0,0,0);
            try {GetWindowRgn(owner.Handle,nativeRegion);using(Region clip=Region.FromHrgn(nativeRegion)) using(Graphics g=Graphics.FromImage(composite)) {g.SetClip(clip,System.Drawing.Drawing2D.CombineMode.Replace);g.DrawImageUnscaled(client,0,0);}}
            finally {DeleteObject(nativeRegion);client.Dispose();}return composite;
        }
        static bool FrameMatches(Form owner,IntPtr alphaFrame)
        {
            Rect bounds;
            return alphaFrame!=IntPtr.Zero && GetWindowRect(alphaFrame,out bounds) &&
                new Rectangle(bounds.Left,bounds.Top,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top)==owner.Bounds &&
                IsWindowVisible(alphaFrame)==owner.Visible && GetForegroundWindow()!=owner.Handle && GetForegroundWindow()!=alphaFrame;
        }
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Native.PrepareDpi();
            Rectangle desktop=SystemInformation.VirtualScreen;Point hiddenLocation=new Point(desktop.Right+1200,desktop.Bottom+1200);
            IntPtr foreground=GetForegroundWindow();Rect clipBefore;GetClipCursor(out clipBefore);
            List<object> records=new List<object>();int capturedCount=0;bool valid=true;
            foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f})
            {
                Theme.Apply(dark,false);string suffix=(dark?"dark":"light")+"-dpi"+(int)(scale*100);
                View synthetic=Previews.State("paused");
                using(ControlPanel panel=new ControlPanel(new PreviewSession(synthetic),true,false,true))
                {
                    using(Bitmap ignored=panel.RenderPreview(scale)) {}panel.Location=hiddenLocation;panel.Show();Application.DoEvents();
                    valid &= panel.PreviewFrameUpdates>0 && !desktop.IntersectsWith(panel.Bounds);
                    bool captured;using(Bitmap native=Capture(panel,panel.Height/2f-.5f,out captured)) native.Save(Path.Combine(directory,"native-main-"+suffix+".png"),ImageFormat.Png);
                    if(captured) capturedCount++;
                    records.Add(new {kind="main",theme=dark?"dark":"light",scale=scale,owner=Metadata(panel.Handle),alpha_frame=Metadata(panel.PreviewFrameHandle),updates=panel.PreviewFrameUpdates,native_client_printed=captured,outside_desktop=!desktop.IntersectsWith(panel.Bounds)});
                    string resourceFile=typeof(Program).Assembly.Location;synthetic.Settings.TargetExecutable=Path.GetFileName(resourceFile);synthetic.ActiveExecutable=synthetic.Settings.TargetExecutable;panel.PreviewRefresh();panel.PreviewUseIconFile(resourceFile);Application.DoEvents();
                    using(Bitmap native=Capture(panel,panel.Height/2f-.5f,out captured)) native.Save(Path.Combine(directory,"native-main-fileicon-"+suffix+".png"),ImageFormat.Png);
                    valid &= captured && panel.PreviewHasProgramIcon && panel.PreviewProgramToolTip.Contains(Path.GetFileName(resourceFile));
                    records.Add(new {kind="main_resource_icon",theme=dark?"dark":"light",scale=scale,icon_loaded=panel.PreviewHasProgramIcon,native_client_printed=captured,outside_desktop=!desktop.IntersectsWith(panel.Bounds),source="read-only own executable resources; no program launch"});
                    synthetic.Settings.TargetExecutable="League of Legends.exe";synthetic.ActiveExecutable=synthetic.Settings.TargetExecutable;panel.PreviewRefresh();
                    Rectangle main=panel.Bounds;bool lifecycle=FrameMatches(panel,panel.PreviewFrameHandle);
                    panel.PreviewHide();Application.DoEvents();lifecycle &= !panel.Visible && !IsWindowVisible(panel.PreviewFrameHandle);
                    panel.Show();Application.DoEvents();lifecycle &= FrameMatches(panel,panel.PreviewFrameHandle);
                    foreach(Point offset in new[] {new Point(25,25),new Point(-50,200),new Point(500,-50),new Point(800,600)}) {
                        panel.Location=new Point(hiddenLocation.X+offset.X,hiddenLocation.Y+offset.Y);Application.DoEvents();lifecycle &= !desktop.IntersectsWith(panel.Bounds) && FrameMatches(panel,panel.PreviewFrameHandle);
                    }
                    valid &= lifecycle;records.Add(new {kind="main_managed_move_hide_show",theme=dark?"dark":"light",scale=scale,passed=lifecycle,movements=4,scope="Own off-desktop window placement and visibility; no drag input or real tray interaction"});
                    valid &= synthetic.Settings.TargetExecutable=="League of Legends.exe" && !synthetic.Enabled;
                    if(scale==1)
                    {
                        Rectangle work=new Rectangle(hiddenLocation.X-100,hiddenLocation.Y-100,900,180);main=new Rectangle(hiddenLocation.X,hiddenLocation.Y+20,panel.Width,44);
                        panel.PreviewOpenSettingsAt(main,work,1);Application.DoEvents();
                        records.Add(new {kind="short_settings",bounds=panel.Bounds.ToString(),work=work.ToString(),scroll_required=panel.PreviewScrollRequired,owner=Metadata(panel.Handle),alpha_frame=Metadata(panel.PreviewFrameHandle)});
                        SettingsBody body=null;foreach(Control child in panel.Controls) if(child is SettingsBody) body=(SettingsBody)child;
                        long bodyStyle=body==null?0:GetWindowLongPtr(body.Handle,-16).ToInt64();bool customScroll=body!=null && body.ScrollMaximum>0 && !body.ScrollThumbBounds.IsEmpty && (bodyStyle&(0x00200000|0x00100000))==0;
                        records.Add(new {kind="custom_settings_scroll",theme=dark?"dark":"light",passed=customScroll,no_native_scrollbar=(bodyStyle&(0x00200000|0x00100000))==0,thumb=body==null?"":body.ScrollThumbBounds.ToString(),hit=body==null?"":body.ScrollHitBounds.ToString()});
                        valid &= customScroll && work.Contains(panel.Bounds) && panel.PreviewScrollRequired;
                        using(Bitmap native=Capture(panel,18,out captured)) native.Save(Path.Combine(directory,"native-settings-short-"+(dark?"dark":"light")+".png"),ImageFormat.Png);
                        panel.PreviewScrollToAppearance();Application.DoEvents();
                        foreach(Control child in body.Controls) if(child is ComboBox) valid &= body.ClientRectangle.Contains(child.Bounds);
                        using(Bitmap native=Capture(panel,18,out captured)) native.Save(Path.Combine(directory,"native-settings-scrolled-"+(dark?"dark":"light")+".png"),ImageFormat.Png);
                        panel.PreviewReturn();valid &= panel.Location==main.Location;
                    }
                    if(scale==1 || scale==2) {
                        Rectangle work=new Rectangle(hiddenLocation.X-100,hiddenLocation.Y-100,1800,1100);
                        foreach(Point corner in new[] {work.Location,new Point(work.Right-panel.Width,work.Top),new Point(work.Left,work.Bottom-(int)(44*scale)),new Point(work.Right-panel.Width,work.Bottom-(int)(44*scale))}) {
                            Rectangle anchor=new Rectangle(corner,new Size(panel.Width,(int)(44*scale)));panel.PreviewOpenSettingsAt(anchor,work,scale);Application.DoEvents();
                            bool inside=work.Contains(panel.Bounds) && !desktop.IntersectsWith(panel.Bounds) && FrameMatches(panel,panel.PreviewFrameHandle);panel.PreviewReturn();Application.DoEvents();inside &= panel.Location==anchor.Location;valid &= inside;
                            records.Add(new {kind="settings_edge_placement",theme=dark?"dark":"light",scale=scale,anchor=anchor.ToString(),passed=inside,scope="Synthetic off-desktop work area; no physical monitor or DPI switch"});
                        }
                    }
                    panel.Hide();
                }
                using(FloatingBar bar=new FloatingBar(true,true))
                {
                    bar.Present(synthetic);using(Bitmap ignored=bar.PreviewBitmap(scale)) {}bar.Location=hiddenLocation;bar.Show();Application.DoEvents();
                    bool captured;using(Bitmap native=Capture(bar,bar.Height/2f-.5f,out captured)) native.Save(Path.Combine(directory,"native-floating-"+suffix+".png"),ImageFormat.Png);
                    if(captured) capturedCount++;records.Add(new {kind="floating",theme=dark?"dark":"light",scale=scale,owner=Metadata(bar.Handle),alpha_frame=Metadata(bar.PreviewFrameHandle),updates=bar.PreviewFrameUpdates,native_client_printed=captured,outside_desktop=!desktop.IntersectsWith(bar.Bounds)});
                    bool lifecycle=FrameMatches(bar,bar.PreviewFrameHandle);Size size=bar.Size;
                    foreach(Point offset in new[] {new Point(-50,200),new Point(500,-50),new Point(800,600)}) {bar.Place(new Rectangle(new Point(hiddenLocation.X+offset.X,hiddenLocation.Y+offset.Y),size));Application.DoEvents();lifecycle &= FrameMatches(bar,bar.PreviewFrameHandle) && !desktop.IntersectsWith(bar.Bounds);}
                    bar.HidePassive();Application.DoEvents();lifecycle &= !bar.Visible && !IsWindowVisible(bar.PreviewFrameHandle);bar.Show();Application.DoEvents();lifecycle &= FrameMatches(bar,bar.PreviewFrameHandle);
                    records.Add(new {kind="floating_managed_move_hide_show",theme=dark?"dark":"light",scale=scale,passed=lifecycle,movements=3,scope="Own off-desktop window placement and visibility; no physical drag"});
                    valid &= lifecycle && bar.PreviewFrameUpdates>0 && !desktop.IntersectsWith(bar.Bounds);bar.Hide();
                }
            }
            Rect clipAfter;GetClipCursor(out clipAfter);bool sameClip=clipBefore.Left==clipAfter.Left && clipBefore.Top==clipAfter.Top && clipBefore.Right==clipAfter.Right && clipBefore.Bottom==clipAfter.Bottom;
            var report=new {utc=DateTime.UtcNow.ToString("o"),passed=valid && capturedCount==16,records=records,native_captures=capturedCount,foreground_unchanged=foreground==GetForegroundWindow(),clip_region_unchanged=sameClip,scope="Own preview HWNDs outside the virtual desktop; actual UpdateLayeredWindow, region/styles, PrintWindow clients, managed placement and visibility. Composites reconstruct native clients plus the applied alpha bitmap. Movement is programmatic, not physical drag. No visible desktop capture, physical input, live dropdown traversal, real tray interaction, game interaction, worker, config, hotkeys or ClipCursor writes."};
            File.WriteAllText(Path.Combine(directory,"native-probe.json"),new JavaScriptSerializer().Serialize(report),new UTF8Encoding(false));Theme.Apply(false,false);return report.passed?0:1;
        }
    }
}
