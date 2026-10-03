// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    sealed class IconDescriptor {public string Path;public long Stamp;}
    interface IProgramIconSource {IconDescriptor Resolve(string executable);Bitmap Extract(string path,int pixels);}
    sealed class NativeProgramIconSource : IProgramIconSource
    {
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SHDefExtractIconW(string file,int index,uint flags,out IntPtr large,out IntPtr small,uint sizes);
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
        static string PathFor(uint pid,string executable)
        {
            IntPtr process=Native.OpenProcess(0x1000,false,pid);if(process==IntPtr.Zero) return null;
            try {StringBuilder path=new StringBuilder(32768);int length=path.Capacity;if(Native.QueryFullProcessImageName(process,0,path,ref length) && String.Equals(System.IO.Path.GetFileName(path.ToString()),executable,StringComparison.OrdinalIgnoreCase)) return path.ToString();}
            finally {Native.CloseHandle(process);}return null;
        }
        public IconDescriptor Resolve(string executable)
        {
            if(String.IsNullOrEmpty(executable)) return null;
            uint foregroundPid;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out foregroundPid);
            string path=PathFor(foregroundPid,executable);
            if(path==null)
            {
                Process[] processes=Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(executable));
                try {foreach(Process process in processes) {path=PathFor((uint)process.Id,executable);if(path!=null) break;}}
                finally {foreach(Process process in processes) process.Dispose();}
            }
            if(path==null || path.StartsWith(@"\\",StringComparison.Ordinal) || !File.Exists(path)) return null;
            return new IconDescriptor {Path=path,Stamp=File.GetLastWriteTimeUtc(path).Ticks};
        }
        public Bitmap Extract(string path,int pixels)
        {
            if(String.IsNullOrEmpty(path) || !System.IO.Path.IsPathRooted(path) || path.StartsWith(@"\\",StringComparison.Ordinal) || !File.Exists(path) || !String.Equals(System.IO.Path.GetExtension(path),".exe",StringComparison.OrdinalIgnoreCase)) return null;
            IntPtr large=IntPtr.Zero,small=IntPtr.Zero;
            try
            {
                int result=SHDefExtractIconW(path,0,0,out large,out small,(uint)(pixels | (pixels<<16)));
                if(result!=0 || large==IntPtr.Zero) return null;
                using(Icon borrowed=Icon.FromHandle(large)) return borrowed.ToBitmap();
            }
            catch(ArgumentException) {return null;}catch(System.ComponentModel.Win32Exception) {return null;}
            finally {if(large!=IntPtr.Zero) DestroyIcon(large);if(small!=IntPtr.Zero && small!=large) DestroyIcon(small);}
        }
    }
    sealed class ProgramIconCache : IDisposable
    {
        readonly IProgramIconSource source;readonly object gate=new object();
        string currentKey;Bitmap currentImage;long generation;bool disposed;
        public ProgramIconCache(IProgramIconSource provider) {source=provider;}
        void ClearImage() {if(currentImage!=null) currentImage.Dispose();currentImage=null;currentKey=null;}
        public void Reset() {lock(gate) {generation++;ClearImage();}}
        public Bitmap Read(string executable,int pixels)
        {
            long requested;lock(gate) {if(disposed) return null;requested=generation;}
            Bitmap extracted=null;
            try
            {
                IconDescriptor icon=source.Resolve(executable);
                string key=icon==null || String.IsNullOrEmpty(icon.Path)?null:icon.Path+"|"+icon.Stamp+"|"+pixels;
                lock(gate)
                {
                    if(disposed || requested!=generation) return null;
                    if(key==null) {ClearImage();return null;}
                    if(String.Equals(key,currentKey,StringComparison.OrdinalIgnoreCase)) return currentImage==null?null:(Bitmap)currentImage.Clone();
                    ClearImage();
                }
                // Resource I/O stays outside the state lock: changing a target
                // can discard its old image immediately without waiting for I/O.
                extracted=source.Extract(icon.Path,pixels);
                lock(gate)
                {
                    if(disposed || requested!=generation) return null;
                    ClearImage();currentKey=key;currentImage=extracted;extracted=null;
                    return currentImage==null?null:(Bitmap)currentImage.Clone();
                }
            }
            catch(Exception) {lock(gate) {if(requested==generation) ClearImage();}return null;}
            finally {if(extracted!=null) extracted.Dispose();}
        }
        public void Dispose() {lock(gate) {if(disposed) return;disposed=true;generation++;ClearImage();}}
    }
    static class ProgramIconDrawing
    {
        public static void Paint(Graphics g,Bitmap image,RectangleF bounds,bool enabled)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            float size=Math.Min(bounds.Width,bounds.Height),scale=size/20f,x=bounds.X+(bounds.Width-size)/2,y=bounds.Y+(bounds.Height-size)/2;
            if(image!=null) {g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,new RectangleF(x,y,size,size));return;}
            using(Pen pen=new Pen(enabled?Theme.Text:Theme.Muted,1.35f*scale))
            using(GraphicsPath shape=Theme.Rounded(new RectangleF(x+1.5f*scale,y+3*scale,size-3*scale,size-6*scale),2.4f*scale))
            {pen.LineJoin=LineJoin.Round;g.DrawPath(pen,shape);g.DrawLine(pen,x+2*scale,y+7.5f*scale,x+18*scale,y+7.5f*scale);}
            using(Brush dot=new SolidBrush(Theme.Muted)) {g.FillEllipse(dot,x+4*scale,y+4.4f*scale,1.2f*scale,1.2f*scale);g.FillEllipse(dot,x+6.5f*scale,y+4.4f*scale,1.2f*scale,1.2f*scale);}
        }
    }
    sealed class ProgramIconButton : PlainButton
    {
        Bitmap image;
        public bool NameOnly;
        public void SetImage(Bitmap value) {Bitmap old=image;image=value;if(old!=null) old.Dispose();Invalidate();}
        public bool HasProgramImage {get{return image!=null;}}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);if(NameOnly) return;
            float size=20*Height/30f;
            ProgramIconDrawing.Paint(e.Graphics,image,new RectangleF((Width-size)/2,(Height-size)/2,size,size),Enabled);
        }
        protected override void Dispose(bool disposing) {if(disposing && image!=null) {image.Dispose();image=null;}base.Dispose(disposing);}
    }
}
