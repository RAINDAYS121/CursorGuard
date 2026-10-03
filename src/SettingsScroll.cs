// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    sealed class SettingsScrollModel
    {
        public int Offset {get;private set;}
        public int Viewport,Content;
        int wheel;
        public int Maximum {get{return Math.Max(0,Content-Math.Max(0,Viewport));}}
        public void Move(long position) {Offset=(int)Math.Max(0,Math.Min(Maximum,position));}
        public void Wheel(int delta,int lines,int linePixels)
        {wheel+=delta;int steps=wheel/120;wheel%=120;Move((long)Offset-(long)steps*(lines<0?Viewport:lines*linePixels));}
        public bool Key(Keys key,int line)
        {long next=Offset;switch(key) {case Keys.Up:next-=line;break;case Keys.Down:next+=line;break;case Keys.PageUp:next-=Math.Max(line,Viewport-line);break;case Keys.PageDown:next+=Math.Max(line,Viewport-line);break;case Keys.Home:next=0;break;case Keys.End:next=Maximum;break;default:return false;}Move(next);return true;}
        public void Reveal(Rectangle bounds,int margin)
        {if(bounds.Height>Viewport-2*margin || bounds.Top<Offset+margin) Move(bounds.Top-margin);else if(bounds.Bottom>Offset+Viewport-margin) Move(bounds.Bottom-Viewport+margin);}
        public Rectangle Thumb(Rectangle hit,double scale)
        {if(Maximum==0 || hit.Height<1) return Rectangle.Empty;int width=Math.Max(3,(int)Math.Round(5*scale)),height=Math.Min(hit.Height,Math.Max((int)Math.Round(24*scale),(int)Math.Round(hit.Height*Math.Min(1.0,Viewport/(double)Math.Max(1,Content)))));int y=hit.Top+(int)Math.Round((hit.Height-height)*Offset/(double)Maximum);return new Rectangle(hit.Left+(hit.Width-width)/2,y,width,height);}
        public void Drag(int startOffset,int pixelDelta,Rectangle hit,double scale)
        {int travel=hit.Height-Thumb(hit,scale).Height;if(travel>0) Move((long)startOffset+(long)Math.Round((double)pixelDelta*Maximum/travel));}
    }
    sealed class SettingsBody : ScrollableControl
    {
        public Action<Graphics> PaintContent;
        readonly Dictionary<Control,Point> positions=new Dictionary<Control,Point>();
        readonly SettingsScrollModel scroll=new SettingsScrollModel();
        Size content;double scale=1;bool arranging,hover,dragging;int dragY,dragOffset;
        public SettingsBody() {AutoScroll=false;DoubleBuffered=true;TabStop=true;AccessibleName=UiText.T("设置内容");AccessibleDescription=UiText.T("可用滚轮、方向键或 Page Up / Page Down 滚动");SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        public new Size AutoScrollMinSize
        {get{return content;}set {double next=value.Width>0?value.Width/360.0:1;int old=scroll.Offset;double ratio=next/scale;content=value;scale=next;UpdateRange();SetOffset((long)Math.Round(old*ratio));}}
        public new Point AutoScrollPosition {get{return new Point(0,-scroll.Offset);}set {SetOffset(value.Y);}}
        public int ScrollOffset {get{return scroll.Offset;}}
        public int ScrollMaximum {get{return scroll.Maximum;}}
        public Rectangle ScrollHitBounds {get {int margin=Math.Max(1,(int)Math.Round(6*scale)),width=Math.Max(14,(int)Math.Round(14*scale));return new Rectangle(Math.Max(0,Width-width-(int)Math.Round(3*scale)),margin,Math.Min(width,Width),Math.Max(0,Height-2*margin));}}
        public Rectangle ScrollThumbBounds {get{return scroll.Thumb(ScrollHitBounds,scale);}}
        protected override CreateParams CreateParams {get {CreateParams p=base.CreateParams;p.Style&=~(0x00200000|0x00100000);return p;}}
        protected override void OnControlAdded(ControlEventArgs e)
        {base.OnControlAdded(e);Control child=e.Control;positions[child]=new Point(child.Left,child.Top+scroll.Offset);child.LocationChanged+=ChildGeometry;child.SizeChanged+=ChildGeometry;child.Enter+=ChildEntered;child.MouseWheel+=ChildWheel;UpdateRange();}
        protected override void OnControlRemoved(ControlEventArgs e)
        {Control child=e.Control;child.LocationChanged-=ChildGeometry;child.SizeChanged-=ChildGeometry;child.Enter-=ChildEntered;child.MouseWheel-=ChildWheel;positions.Remove(child);base.OnControlRemoved(e);UpdateRange();}
        void ChildGeometry(object sender,EventArgs args)
        {if(arranging) return;Control child=(Control)sender;positions[child]=new Point(child.Left,child.Top+scroll.Offset);UpdateRange();}
        void ChildEntered(object sender,EventArgs args) {ScrollControlIntoView((Control)sender);}
        void ChildWheel(object sender,MouseEventArgs args)
        {ComboBox combo=sender as ComboBox;if(combo!=null && combo.DroppedDown) return;ApplyWheel(args.Delta);HandledMouseEventArgs handled=args as HandledMouseEventArgs;if(handled!=null) handled.Handled=true;}
        void UpdateRange()
        {if(positions==null || scroll==null || arranging) return;int extent=content.Height;foreach(KeyValuePair<Control,Point> child in positions) extent=Math.Max(extent,child.Value.Y+child.Key.Height+(int)Math.Round(8*scale));scroll.Content=extent;scroll.Viewport=ClientSize.Height;SetOffset(scroll.Offset);}
        public void SetOffset(long offset)
        {if(scroll==null || positions==null) return;scroll.Move(offset);arranging=true;try {foreach(KeyValuePair<Control,Point> child in positions) child.Key.Location=new Point(child.Value.X,child.Value.Y-scroll.Offset);}finally {arranging=false;}Invalidate();}
        public void ApplyWheel(int delta) {scroll.Wheel(delta,SystemInformation.MouseWheelScrollLines,Math.Max(1,(int)Math.Round(22*scale)));SetOffset(scroll.Offset);}
        public bool ApplyKey(Keys key) {bool handled=scroll.Key(key,Math.Max(1,(int)Math.Round(22*scale)));if(handled) SetOffset(scroll.Offset);return handled;}
        public new void ScrollControlIntoView(Control child)
        {while(child!=null && child.Parent!=this) child=child.Parent;Point original;if(child!=null && positions.TryGetValue(child,out original)) {scroll.Reveal(new Rectangle(original,child.Size),Math.Max(1,(int)Math.Round(6*scale)));SetOffset(scroll.Offset);}}
        protected override void OnSizeChanged(EventArgs e) {base.OnSizeChanged(e);UpdateRange();}
        protected override void ScaleControl(SizeF factor,BoundsSpecified specified) {SetOffset(0);base.ScaleControl(factor,specified);}
        protected override void OnMouseWheel(MouseEventArgs e) {ApplyWheel(e.Delta);HandledMouseEventArgs handled=e as HandledMouseEventArgs;if(handled!=null) handled.Handled=true;base.OnMouseWheel(e);}
        protected override bool ProcessCmdKey(ref Message message,Keys key)
        {Control focused=null;foreach(Control child in Controls) if(child.ContainsFocus) {focused=child;break;}ShortcutButton shortcut=focused as ShortcutButton;if(!(focused is TextBoxBase) && !(focused is ComboBox) && (shortcut==null || !shortcut.Capturing) && ApplyKey(key)) return true;return base.ProcessCmdKey(ref message,key);}
        protected override void OnMouseDown(MouseEventArgs e)
        {base.OnMouseDown(e);if(e.Button!=MouseButtons.Left || scroll.Maximum==0 || !ScrollHitBounds.Contains(e.Location)) return;Focus();Rectangle thumb=ScrollThumbBounds;if(e.Y>=thumb.Top && e.Y<thumb.Bottom) {dragging=true;dragY=e.Y;dragOffset=scroll.Offset;Capture=true;}else SetOffset((long)scroll.Offset+(e.Y<thumb.Top?-1:1)*Math.Max(1,ClientSize.Height-(int)(22*scale)));Invalidate();}
        protected override void OnMouseMove(MouseEventArgs e)
        {base.OnMouseMove(e);if(dragging) {scroll.Drag(dragOffset,e.Y-dragY,ScrollHitBounds,scale);SetOffset(scroll.Offset);}bool next=ScrollHitBounds.Contains(e.Location);if(next!=hover) {hover=next;Invalidate(ScrollHitBounds);}}
        protected override void OnMouseUp(MouseEventArgs e) {base.OnMouseUp(e);if(dragging) {dragging=false;Capture=false;Invalidate();}}
        protected override void OnMouseCaptureChanged(EventArgs e) {base.OnMouseCaptureChanged(e);if(!Capture) {dragging=false;Invalidate();}}
        protected override void OnMouseLeave(EventArgs e) {base.OnMouseLeave(e);hover=false;Invalidate(ScrollHitBounds);}
        protected override void OnPaintBackground(PaintEventArgs e) {Theme.ControlBackdrop(e.Graphics,this);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);GraphicsState saved=e.Graphics.Save();try {e.Graphics.TranslateTransform(0,-scroll.Offset);if(PaintContent!=null) PaintContent(e.Graphics);}finally {e.Graphics.Restore(saved);}
            Rectangle thumb=ScrollThumbBounds;if(!thumb.IsEmpty) {e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath shape=Theme.Rounded(thumb,thumb.Width/2f)) using(Brush brush=new SolidBrush(hover || dragging?Theme.Text:Theme.Muted)) e.Graphics.FillPath(brush,shape);}
        }
        public void PaintPreview(Graphics graphics) {OnPaintBackground(new PaintEventArgs(graphics,ClientRectangle));OnPaint(new PaintEventArgs(graphics,ClientRectangle));}
    }
}
