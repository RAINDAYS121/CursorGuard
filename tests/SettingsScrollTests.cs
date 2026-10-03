// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    static class SettingsScrollTests
    {
        static void Check(bool value) {if(!value) throw new Exception("assertion failed");}
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}checks.Add(c);};
            test("scroll_min_max_large_range_and_content_shrink_clamp",delegate {SettingsScrollModel m=new SettingsScrollModel {Content=Int32.MaxValue,Viewport=100};m.Move(Int64.MaxValue);Check(m.Offset==m.Maximum);m.Move(Int64.MinValue);Check(m.Offset==0);m.Move(200);m.Content=80;m.Move(m.Offset);Check(m.Maximum==0 && m.Offset==0);});
            test("scroll_thumb_dpi_is_thin_with_wide_hit_area_and_no_arrows",delegate {
                foreach(double scale in new[] {1.0,1.25,1.5,2.0}) {SettingsScrollModel m=new SettingsScrollModel {Content=(int)(1000*scale),Viewport=(int)(180*scale)};Rectangle hit=new Rectangle(0,6,(int)(14*scale),(int)(168*scale));Rectangle top=m.Thumb(hit,scale);Check(hit.Contains(top) && top.Width<=(int)Math.Ceiling(5*scale) && hit.Width>=top.Width*2 && top.Top==hit.Top);m.Move(m.Maximum);Check(m.Thumb(hit,scale).Bottom==hit.Bottom);m.Content=m.Viewport;Check(m.Thumb(hit,scale).IsEmpty);}
            });
            test("scroll_wheel_accumulates_high_resolution_and_preserves_limits",delegate {SettingsScrollModel m=new SettingsScrollModel {Content=500,Viewport=100};for(int i=0;i<3;i++) m.Wheel(-30,3,20);Check(m.Offset==0);m.Wheel(-30,3,20);Check(m.Offset==60);m.Wheel(120,3,20);Check(m.Offset==0);m.Wheel(-120,-1,20);Check(m.Offset==100);for(int i=0;i<20;i++) m.Wheel(-120,3,20);Check(m.Offset==400);});
            test("scroll_drag_model_traverses_full_range_at_all_scales",delegate {
                foreach(double scale in new[] {1.0,1.25,1.5,2.0}) {SettingsScrollModel m=new SettingsScrollModel {Content=(int)(286*scale),Viewport=(int)(140*scale)};Rectangle hit=new Rectangle(0,6,(int)(14*scale),(int)(128*scale));int travel=hit.Height-m.Thumb(hit,scale).Height;m.Drag(0,travel,hit,scale);Check(m.Offset==m.Maximum);m.Drag(m.Maximum,-travel,hit,scale);Check(m.Offset==0);m.Drag(0,travel/2,hit,scale);Check(m.Offset>0 && m.Offset<m.Maximum);}
            });
            test("scroll_keyboard_and_child_reveal_without_physical_focus",delegate {
                using(SettingsBody body=new SettingsBody {Size=new Size(380,140),AutoScrollMinSize=new Size(360,286)}) {using(Button lower=new Button {Location=new Point(300,240),Size=new Size(60,30)}) {body.Controls.Add(lower);body.ApplyKey(Keys.End);Check(body.ScrollOffset==body.ScrollMaximum);body.ApplyKey(Keys.Home);Check(body.ScrollOffset==0);typeof(Control).GetMethod("OnEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lower,new object[] {EventArgs.Empty});Check(body.ClientRectangle.Contains(lower.Bounds));int revealed=body.ScrollOffset;body.ApplyKey(Keys.PageUp);Check(body.ScrollOffset<revealed);body.ApplyKey(Keys.Home);Check(body.ScrollOffset==0);body.ApplyKey(Keys.PageDown);Check(body.ScrollOffset>0);Check(!body.IsHandleCreated && !lower.IsHandleCreated);}}
            });
            test("scroll_dynamic_content_resize_and_repeated_hide_keep_width",delegate {
                using(SettingsBody body=new SettingsBody {Size=new Size(380,286),AutoScrollMinSize=new Size(360,286)}) using(Button lower=new Button {Location=new Point(300,500),Size=new Size(60,30)}) {
                    body.Controls.Add(lower);Check(body.ScrollMaximum>0);body.SetOffset(Int64.MaxValue);body.Controls.Remove(lower);Check(body.ScrollMaximum==0 && body.ScrollOffset==0);int width=body.ClientSize.Width;
                    for(int cycle=0;cycle<12;cycle++) {body.AutoScrollMinSize=new Size(360,700);body.SetOffset(9999);Check(body.ScrollMaximum>0 && body.ClientSize.Width==width);body.AutoScrollMinSize=new Size(360,286);Check(body.ScrollOffset==0 && body.ScrollThumbBounds.IsEmpty && body.ClientSize.Width==width);}
                    CreateParams p=(CreateParams)typeof(SettingsBody).GetProperty("CreateParams",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(body,null);Check((p.Style&(0x00200000|0x00100000))==0 && !body.AutoScroll && !body.IsHandleCreated);
                }
            });
            test("scroll_theme_render_and_settings_focus_reveal_preserve_header",delegate {
                foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {Theme.Apply(dark,false);View view=Previews.State("settings");using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {using(Bitmap initial=form.RenderPreview(scale)) {}Rectangle anchor=new Rectangle(-800,200,form.Width,form.Height);form.PreviewOpenSettingsAt(anchor,new Rectangle(-1200,0,1200,(int)(180*scale)),scale);SettingsBody body=null;foreach(Control child in form.Controls) if(child is SettingsBody) body=(SettingsBody)child;Check(body!=null && body.ScrollMaximum>0 && body.ScrollHitBounds.Width>=(int)(14*scale));form.PreviewScrollToLanguage();foreach(Control child in body.Controls) if(child is ComboBox) Check(body.ClientRectangle.Contains(child.Bounds));using(Bitmap image=new Bitmap(body.Width,body.Height)) using(Graphics g=Graphics.FromImage(image)) body.PaintPreview(g);Check(!body.IsHandleCreated && !form.IsHandleCreated);form.PreviewReturn();}}
                Theme.Apply(false,false);
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c) {return !c.passed;}).Count;JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> report=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));report["settings_scroll_tests"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;report["scroll_test_scope"]="pure range/thumb/drag/wheel/key models and hidden controls with explicit child reveal; no physical scroll/drag/keyboard/focus interaction";File.WriteAllText(output,json.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
