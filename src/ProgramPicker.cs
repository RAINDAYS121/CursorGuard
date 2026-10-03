// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LoLMouseGuard
{
    public sealed class ProgramCandidate
    {
        public string Title,Executable;
        public uint Pid;
        public override string ToString() {return (Title??Executable)+" · "+Executable;}
    }
    static class ProgramDiscovery
    {
        delegate bool EnumWindow(IntPtr window,IntPtr parameter);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback,IntPtr parameter);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
        public static List<ProgramCandidate> Read()
        {
            // Window captions are shown only in this explicitly opened picker.
            // They are never saved, included in logs, or queried by the guard loop.
            List<ProgramCandidate> result=new List<ProgramCandidate>();int own=Process.GetCurrentProcess().Id;
            EnumWindows(delegate(IntPtr window,IntPtr parameter)
            {
                if(!Native.IsWindowVisible(window)) return true;uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==0 || pid==own) return true;
                StringBuilder title=new StringBuilder(257);GetWindowText(window,title,title.Capacity);if(title.Length==0) return true;
                IntPtr process=Native.OpenProcess(0x1000,false,pid);if(process==IntPtr.Zero) return true;
                try
                {
                    StringBuilder path=new StringBuilder(32768);int count=path.Capacity;
                    if(Native.QueryFullProcessImageName(process,0,path,ref count)) result.Add(new ProgramCandidate {Title=title.ToString(),Executable=Path.GetFileName(path.ToString()),Pid=pid});
                }
                finally {Native.CloseHandle(process);}return result.Count<200;
            },IntPtr.Zero);
            result.Sort(delegate(ProgramCandidate a,ProgramCandidate b) {return StringComparer.CurrentCultureIgnoreCase.Compare(a.Title,b.Title);});return result;
        }
    }
    public static class ProgramSelection
    {
        public static string FromFile(string path)
        {string name=Path.GetFileName(path);Settings s=Settings.Defaults();s.TargetExecutable=name;if(s.Validate()!=null) throw new ArgumentException(UiText.T("请选择 .exe 程序文件。"));return name;}
    }
    sealed class ProgramPicker : Form
    {
        readonly ListBox list;
        readonly PlainButton refresh,accept,cancel;
        readonly bool preview;
        string notice=UiText.T("仅选择目标，不启动程序");
        public string SelectedExecutable {get;private set;}
        public ProgramPicker(bool offscreen,IList<ProgramCandidate> candidates)
        {
            preview=offscreen;Text=UiText.T("选择程序 · CursorGuard");ClientSize=new Size(380,310);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;BackColor=Theme.Background;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;
            using(GraphicsPath path=Theme.Rounded(new RectangleF(0,0,380,310),18)) Region=new Region(path);
            list=new ListBox {Location=new Point(16,82),Size=new Size(348,154),BorderStyle=BorderStyle.None,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=48,BackColor=Theme.Background,ForeColor=Theme.Text,AccessibleName=UiText.T("有可见窗口的程序")};
            list.DrawItem+=delegate(object sender,DrawItemEventArgs e) {if(e.Index>=0) PaintItem(e.Graphics,e.Bounds,(ProgramCandidate)list.Items[e.Index],(e.State&DrawItemState.Selected)!=0);};
            list.DoubleClick+=delegate {Choose();};Controls.Add(list);
            refresh=Add(UiText.T("刷新"),292,42,72,30,delegate {LoadCandidates(ProgramDiscovery.Read());});
            accept=Add(UiText.T("选择"),296,262,68,30,delegate {Choose();});cancel=Add(UiText.T("取消"),218,262,68,30,delegate {DialogResult=DialogResult.Cancel;Close();});
            CancelButton=cancel;AcceptButton=accept;LoadCandidates(candidates);
        }
        PlainButton Add(string text,int x,int y,int w,int h,EventHandler action) {PlainButton b=new PlainButton {Text=text,Location=new Point(x,y),Size=new Size(w,h)};b.Click+=action;Controls.Add(b);return b;}
        void LoadCandidates(IList<ProgramCandidate> candidates)
        {list.Items.Clear();foreach(ProgramCandidate c in candidates) list.Items.Add(c);if(list.Items.Count>0) list.SelectedIndex=0;accept.Enabled=list.Items.Count>0;notice=list.Items.Count==0?UiText.T("没有可选窗口，可从设置选择 .exe 文件"):UiText.T("仅选择目标，不启动程序");Invalidate();}
        void Choose() {ProgramCandidate c=list.SelectedItem as ProgramCandidate;if(c==null) return;SelectedExecutable=c.Executable;DialogResult=DialogResult.OK;Close();}
        static void PaintItem(Graphics g,Rectangle rect,ProgramCandidate c,bool selected)
        {
            using(Brush b=new SolidBrush(selected?Theme.Cap:Theme.Background)) g.FillRectangle(b,rect);
            Theme.Write(g,c.Title,rect.Left+8,rect.Top+3,rect.Width-16,22,12,Theme.Text,false,StringAlignment.Near);
            Theme.Write(g,c.Executable+" · PID "+c.Pid,rect.Left+8,rect.Top+25,rect.Width-16,19,10,Theme.Muted,false,StringAlignment.Near);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            Theme.Surface(g,new RectangleF(.5f,.5f,379,309),18);
            Theme.Write(g,UiText.T("选择程序"),16,10,348,27,14,Theme.Text,false,StringAlignment.Near);Theme.Write(g,UiText.T("当前有可见窗口的程序"),16,42,270,30,12,Theme.Muted,false,StringAlignment.Near);
            Theme.Write(g,notice,16,237,348,21,10,Theme.Muted,false,StringAlignment.Near);
        }
        public Bitmap PreviewBitmap()
        {
            if(!preview) throw new InvalidOperationException();Bitmap b=new Bitmap(380,310);using(Graphics g=Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);OnPaint(new PaintEventArgs(g,ClientRectangle));
                for(int i=0;i<Math.Min(3,list.Items.Count);i++) PaintItem(g,new Rectangle(list.Left,list.Top+i*48,list.Width,48),(ProgramCandidate)list.Items[i],i==0);
                foreach(PlainButton button in new[] {refresh,accept,cancel}) using(Bitmap image=new Bitmap(button.Width,button.Height)) {using(Graphics cg=Graphics.FromImage(image)) button.PaintPreview(cg);g.DrawImageUnscaled(image,button.Location);}
            }return b;
        }
    }
}

