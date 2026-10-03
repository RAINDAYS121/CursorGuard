// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LoLMouseGuard
{
    static class AlphaWindow
    {
        [StructLayout(LayoutKind.Sequential)] struct Point {public int X,Y;public Point(int x,int y){X=x;Y=y;}}
        [StructLayout(LayoutKind.Sequential)] struct Size {public int Width,Height;public Size(int w,int h){Width=w;Height=h;}}
        [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {public byte Operation,Flags,Alpha,Format;}
        [StructLayout(LayoutKind.Sequential)] struct Header {public uint Size;public int Width,Height;public ushort Planes,BitCount;public uint Compression,ImageSize;public int X,Y;public uint Used,Important;}
        [StructLayout(LayoutKind.Sequential)] struct Info {public Header Header;public uint Color;}
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr item);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr item);
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref Info info,uint usage,out IntPtr pixels,IntPtr section,uint offset);
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr h,IntPtr destination,ref Point position,ref Size size,IntPtr source,ref Point origin,uint key,ref Blend blend,uint flags);
        public static void Apply(IntPtr handle,Bitmap bitmap,PointF location)
        {
            IntPtr dc=CreateCompatibleDC(IntPtr.Zero),dib=IntPtr.Zero,old=IntPtr.Zero;
            if(dc==IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            try
            {
                Info info=new Info {Header=new Header {Size=40,Width=bitmap.Width,Height=-bitmap.Height,Planes=1,BitCount=32}};
                IntPtr pixels;dib=CreateDIBSection(dc,ref info,0,out pixels,IntPtr.Zero,0);
                if(dib==IntPtr.Zero || pixels==IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
                old=SelectObject(dc,dib);
                BitmapData data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
                try {byte[] row=new byte[bitmap.Width*4];for(int y=0;y<bitmap.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);Marshal.Copy(row,0,IntPtr.Add(pixels,y*row.Length),row.Length);}}
                finally {bitmap.UnlockBits(data);}
                Point position=new Point((int)location.X,(int)location.Y),origin=new Point(0,0);Size size=new Size(bitmap.Width,bitmap.Height);
                Blend blend=new Blend {Alpha=255,Format=1};
                if(!UpdateLayeredWindow(handle,IntPtr.Zero,ref position,ref size,dc,ref origin,0,ref blend,2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally {if(old!=IntPtr.Zero) SelectObject(dc,old);if(dib!=IntPtr.Zero) DeleteObject(dib);DeleteDC(dc);}
        }
    }
    // A click-through alpha surface behind the native control window. Its
    // outer edge has coverage alpha; the owner's hard region is inset into
    // the fully opaque interior, where identical fills hide the inner cut.
    sealed class RoundedFrame : Form
    {
        public const int Styles=0x00080000|0x08000000|0x00000080|0x00000020;
        [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
        string signature;
        public int Updates;
        public RoundedFrame() {FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get {CreateParams p=base.CreateParams;p.ExStyle|=Styles;return p;}}
        protected override void WndProc(ref Message message) {if(message.Msg==0x21){message.Result=new IntPtr(3);return;}base.WndProc(ref message);}
        public static Bitmap Surface(Size size,float radius)
        {
            Bitmap bitmap=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(bitmap)) {g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;Theme.Surface(g,new RectangleF(.5f,.5f,size.Width-1,size.Height-1),radius);}
            return bitmap;
        }
        public void Sync(Form owner,float radius)
        {
            if(owner.IsDisposed || !owner.Visible || owner.WindowState==FormWindowState.Minimized){Hide();return;}
            Rectangle bounds=owner.Bounds;
            string key=bounds.Size.ToString()+radius+Theme.SurfaceTop.ToArgb()+Theme.SurfaceBottom.ToArgb()+Theme.EdgeTop.ToArgb()+Theme.HighContrast;
            if(key!=signature) {using(Bitmap bitmap=Surface(bounds.Size,radius)) SetImage(bitmap,bounds.Location);signature=key;}
            if(!Visible) Show();
            if(!SetWindowPos(Handle,owner.Handle,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0010|0x0200)) throw new System.ComponentModel.Win32Exception();
        }
        public void SetImage(Bitmap bitmap,Point location)
        {Bounds=new Rectangle(location,bitmap.Size);AlphaWindow.Apply(Handle,bitmap,location);Updates++;}
    }
}
