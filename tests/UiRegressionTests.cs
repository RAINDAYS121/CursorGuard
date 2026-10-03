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
    static class UiRegressionTests
    {
        static void Check(bool condition) {if(!condition) throw new Exception("assertion failed");}
        static void Check(bool condition,string message) {if(!condition) throw new Exception(message);}
        sealed class RecordingSession : IUiSession
        {
            public readonly View View;
            public readonly List<Command> Commands=new List<Command>();
            public RecordingSession(View view) {View=view;}
            public View ReadView() {return View;}
            public void Send(Command command) {Commands.Add(command);}
            public void SaveSettings(Settings settings) {throw new InvalidOperationException("Unexpected settings write");}
            public void Dispose() {}
        }
        static Bitmap ButtonPixels(PlainButton button)
        {Bitmap image=new Bitmap(button.Width,button.Height);using(Graphics g=Graphics.FromImage(image)) button.PaintPreview(g);return image;}
        static bool SamePixels(Bitmap a,Bitmap b)
        {if(a.Size!=b.Size) return false;for(int y=0;y<a.Height;y++) for(int x=0;x<a.Width;x++) if(a.GetPixel(x,y)!=b.GetPixel(x,y)) return false;return true;}
        static void ManagedHover(Control control,bool entered)
        {typeof(Control).GetMethod(entered?"OnMouseEnter":"OnMouseLeave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[] {EventArgs.Empty});}
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check check=new SelfTests.Check {name=name};try {action();check.passed=true;}catch(Exception e){check.error=e.ToString();}checks.Add(check);};
            test("settings_below_then_above_restores_main_anchor",delegate {
                Rectangle work=new Rectangle(0,0,1920,1040),top=new Rectangle(30,20,285,44),bottom=new Rectangle(1700,970,285,44);
                Rectangle down=SettingsPlacement.Calculate(top,work,new Size(380,364),1),up=SettingsPlacement.Calculate(bottom,work,new Size(380,364),1);
                Check(down.Top==top.Bottom+8 && up.Bottom==bottom.Top-8 && work.Contains(up));
                using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.Bounds=bottom;form.PreviewOpenSettingsAt(bottom,work,1);Check(form.Bounds==up);form.PreviewReturn();Check(form.Location==bottom.Location && form.Height==44 && !form.IsHandleCreated);}
            });
            test("settings_all_corners_taskbars_negative_monitors_and_dpi",delegate {
                foreach(Rectangle work in new[] {new Rectangle(0,40,1920,1000),new Rectangle(48,0,1872,1040),new Rectangle(-2560,-200,2560,1400),new Rectangle(-1280,40,1280,700)})
                foreach(double scale in new[] {1.0,1.25,1.5,2.0})
                foreach(Point corner in new[] {work.Location,new Point(work.Right-250,work.Top),new Point(work.Left,work.Bottom-44),new Point(work.Right-250,work.Bottom-44)})
                {Rectangle main=new Rectangle(corner,new Size(250,44)),placed=SettingsPlacement.Calculate(main,work,new Size((int)Math.Round(380*scale),(int)Math.Round(364*scale)),scale);Check(work.Contains(placed));}
            });
            test("settings_short_work_area_scrolls_and_header_remains_visible",delegate {
                Rectangle work=new Rectangle(-800,100,800,180),main=new Rectangle(-500,210,285,44);
                using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.PreviewOpenSettingsAt(main,work,1);Check(work.Contains(form.Bounds) && form.PreviewScrollRequired);foreach(Control child in form.Controls) if(child is PlainButton) Check(form.ClientRectangle.Contains(child.Bounds));form.PreviewReturn();Check(form.Location==main.Location);}
            });
            test("rounded_alpha_has_fractional_edge_on_both_palettes_and_dpi",delegate {
                foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {Theme.Apply(dark,false);using(Bitmap bitmap=RoundedFrame.Surface(new Size((int)(285*scale),(int)(44*scale)),21.5f*scale)) {int partial=0;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){int a=bitmap.GetPixel(x,y).A;if(a>0 && a<255) partial++;}Check(partial>40 && bitmap.GetPixel(0,0).A==0 && bitmap.GetPixel(bitmap.Width/2,bitmap.Height/2).A==255);}}
                Theme.Apply(false,false);
            });
            test("status_width_tracks_actual_text_without_blank_reserve",delegate {
                foreach(string status in new[] {"已暂停","保护中","异常","等待程序","等待鼠标","等待稳定","正在准备"}) foreach(string name in new[] {"Paint","League of Legends",new string('W',150)}) {
                    CompactBarLayout main=CompactBarLayout.ForMain(name,status),bar=CompactBarLayout.Floating(name,status);
                    Check(main.FirstDividerX-main.StatusX-main.StatusWidth==6 && main.Width<=320 && bar.Width<=210 && bar.StatusX+bar.StatusWidth+7==bar.Width);
                }
                Check(CompactBarLayout.ForMain("Paint","异常").Width<CompactBarLayout.ForMain("Paint","已暂停").Width);
            });
            test("appearance_hover_model_is_not_overwritten_by_periodic_refresh",delegate {
                // Old code assigned the persisted selection during dropdown tracking.
                for(int move=0;move<300;move++) {int tracked=move%3;Check(!AppearanceSelection.ShouldReconcile(true,tracked,0));}
                Check(AppearanceSelection.ShouldReconcile(false,2,0) && !AppearanceSelection.ShouldReconcile(false,0,0));
            });
            test("appearance_programmatic_sync_has_no_committed_selection_event",delegate {
                using(ComboBox combo=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList}) {combo.Items.AddRange(new object[] {"system","light","dark"});int committed=0,changed=0;combo.SelectionChangeCommitted+=delegate {committed++;};combo.SelectedIndexChanged+=delegate {changed++;};foreach(string choice in new[] {"system","light","dark","light","system","dark"}) {AppearanceSelection.Reconcile(combo,choice);Check(combo.SelectedIndex==AppearanceSelection.Index(choice));}int before=changed;AppearanceSelection.Reconcile(combo,"dark");Check(committed==0 && changed==before && !combo.IsHandleCreated);}
            });
            test("main_hide_refresh_and_settings_return_preserve_state",delegate {
                View view=Previews.State("protected");string target=view.Settings.TargetExecutable;using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {for(int i=0;i<5;i++){form.PreviewInvokeHideEntry();form.PreviewRefresh();Check(view.Enabled && !view.Editing && view.Settings.TargetExecutable==target && !form.IsHandleCreated);}Rectangle main=new Rectangle(120,950,285,44);form.PreviewOpenSettingsAt(main,new Rectangle(0,0,1920,1040),1);form.PreviewReturn();form.PreviewHide();Check(view.Enabled && view.Settings.TargetExecutable==target && form.Location==main.Location);}
            });
            test("appearance_commit_repeats_preserve_target_and_rollback_on_failure",delegate {
                using(ComboBox combo=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList}) {combo.Items.AddRange(new object[] {"system","light","dark"});MemoryFloatingStore store=new MemoryFloatingStore();store.Value=new FloatingPreferences {Enabled=true,HasPosition=true,X=11,Y=29};using(FloatingController floating=new FloatingController(store,delegate {return new FakeFloatingWindow();})) {int refreshed=0;for(int i=0;i<12;i++) {combo.SelectedIndex=i%3;AppearanceSelection.Commit(combo,floating.SetAppearance,delegate {refreshed++;});Check(floating.Preferences.Appearance==new[] {"system","light","dark"}[i%3] && floating.Preferences.Enabled && floating.Preferences.X==11);}store.Fail=true;combo.SelectedIndex=0;AppearanceSelection.Commit(combo,floating.SetAppearance,delegate {AppearanceSelection.Reconcile(combo,floating.Preferences.Appearance);});Check(floating.Preferences.Appearance=="dark" && combo.SelectedIndex==2 && refreshed==12);}}
            });
            test("hide_entry_waits_for_settings_return_to_preserve_emergency_registration",delegate {
                View view=Previews.State("protected");using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {Check(form.PreviewHideEntryEnabled);view.Editing=true;form.PreviewRefresh();Check(!form.PreviewHideEntryEnabled);view.Editing=false;form.PreviewRefresh();Check(form.PreviewHideEntryEnabled && view.Enabled && !form.IsHandleCreated);}
            });
            test("settings_release_failure_keeps_emergency_until_owned_clip_is_freed",delegate {
                FakeBackend backend=new FakeBackend();MemorySettingsStore store=new MemorySettingsStore();MemoryStartupStore startup=new MemoryStartupStore();using(SettingsService settings=new SettingsService(backend,store,startup,@"C:\Example\Guard.exe",store.Value)) {string message;Check(settings.RegisterCurrent(out message));GuardCore core=new GuardCore(backend,true);core.SetHotkeys(settings.Ready,null);Check(core.Enable());Scene scene=new Scene {Window=new IntPtr(42),Pid=100,Executable="League of Legends.exe",Visible=true,CoordinatesReady=true,CursorReady=true,Client=new Box(0,0,1920,1080),Monitor=new Box(0,0,1920,1080),VirtualScreen=new Box(-1920,0,1920,1080),Cursor=new Position(100,100)};core.Step(scene,0);core.Step(scene,100);Check(core.OwnsClip);backend.FailFree=true;bool editing=false;Check(!SettingsEntry.TryBegin(core,settings,ref editing) && !editing && !core.Enabled && core.OwnsClip && settings.Ready && backend.Registered.ContainsKey(2));backend.FailFree=false;core.Pause(true);Check(!core.OwnsClip && backend.Registered.ContainsKey(2));Check(SettingsEntry.TryBegin(core,settings,ref editing) && editing && !core.OwnsClip && !core.Enabled && backend.Registered.Count==0);Check(settings.RegisterCurrent(out message) && backend.Registered.ContainsKey(2) && !core.Enabled);}
            });
            test("main_managed_hover_feedback_preserves_layout_commands_and_dpi",delegate {
                foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {
                    Theme.Apply(dark,false);View view=Previews.State("protected");RecordingSession session=new RecordingSession(view);
                    using(ControlPanel form=new ControlPanel(session,true,false)) {
                        using(Bitmap initial=form.RenderPreview(scale)) {}
                        // Settle explicit preview scaling through the production
                        // layout path before comparing managed hover frames.
                        form.PreviewRefresh();
                        foreach(Control child in form.Controls) {PlainButton button=child as PlainButton;if(button==null || (button.Glyph!="gear" && !(button is ProgramIconButton))) continue;
                            Rectangle bounds=button.Bounds;Size size=form.ClientSize;using(Bitmap before=ButtonPixels(button)) {
                                for(int cycle=0;cycle<8;cycle++) {ManagedHover(button,true);form.PreviewRefresh();using(Bitmap hover=ButtonPixels(button)) Check(!SamePixels(before,hover),"Missing hover feedback: "+button.GetType().Name+" dark="+dark+" scale="+scale);Check(button.Bounds==bounds && form.ClientSize==size && form.ClientRectangle.Contains(bounds),"Hover/refresh geometry: "+button.GetType().Name+" dark="+dark+" scale="+scale+" before="+bounds+" after="+button.Bounds+" client="+form.ClientSize);ManagedHover(button,false);using(Bitmap after=ButtonPixels(button)) Check(SamePixels(before,after),"Hover leave does not restore pixels: "+button.GetType().Name+" scale="+scale);}
                            }
                        }
                        Check(session.Commands.Count==0 && view.Enabled && view.Settings.TargetExecutable=="League of Legends.exe" && !form.IsHandleCreated);
                    }
                }
                Theme.Apply(false,false);
            });
            test("settings_four_edges_keep_header_controls_and_restore_scaled_main",delegate {
                foreach(Rectangle work in new[] {new Rectangle(0,40,1920,1000),new Rectangle(-2000,-500,2000,800),new Rectangle(-1200,70,1200,180)}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {
                    View view=Previews.State("settings");RecordingSession session=new RecordingSession(view);
                    foreach(Point corner in new[] {work.Location,new Point(work.Right-400,work.Top),new Point(work.Left,work.Bottom-100),new Point(work.Right-400,work.Bottom-100)}) using(ControlPanel form=new ControlPanel(session,true,false)) {
                        using(Bitmap initial=form.RenderPreview(scale)) {}Size mainSize=form.ClientSize;Rectangle main=new Rectangle(corner,mainSize);
                        form.PreviewOpenSettingsAt(main,work,scale);form.PreviewRefresh();Check(work.Contains(form.Bounds));
                        foreach(Control child in form.Controls) if(child is PlainButton && (child.Text=="‹ 返回" || child.Text=="完成" || child.Text=="···")) {Check(form.ClientRectangle.Contains(child.Bounds));Check(child.Bottom<=(int)Math.Round(40*scale));}
                        Check(form.PreviewScrollRequired==(work.Height<(int)Math.Round(364*scale)));
                        form.PreviewReturn();Check(form.Location==main.Location && form.ClientSize==mainSize && session.Commands.Count==0 && !form.IsHandleCreated);
                    }
                }
            });
            test("appearance_cancel_and_repeated_same_commit_keep_other_preferences",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore();store.Value=new FloatingPreferences {Enabled=true,HasPosition=true,Monitor="secondary",X=120,Y=30,Appearance="dark"};
                using(FloatingController floating=new FloatingController(store,delegate {return new FakeFloatingWindow();})) using(ComboBox combo=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList}) {
                    combo.Items.AddRange(new object[] {"system","light","dark"});int saves=0,refreshes=0;Func<string,bool> save=delegate(string choice) {saves++;return floating.SetAppearance(choice);};Action refresh=delegate {refreshes++;AppearanceSelection.Reconcile(combo,floating.Preferences.Appearance);};
                    combo.SelectedIndex=0;AppearanceSelection.Reconcile(combo,floating.Preferences.Appearance);Check(combo.SelectedIndex==2 && saves==0);
                    combo.SelectedIndex=-1;AppearanceSelection.Commit(combo,save,refresh);Check(saves==0 && refreshes==0 && floating.Preferences.Appearance=="dark");
                    for(int i=0;i<40;i++) {combo.SelectedIndex=2;AppearanceSelection.Commit(combo,save,refresh);Check(floating.Preferences.Appearance=="dark" && floating.Preferences.Enabled && floating.Preferences.HasPosition && floating.Preferences.Monitor=="secondary" && floating.Preferences.X==120 && floating.Preferences.Y==30);}
                    Check(saves==40 && refreshes==40 && !combo.IsHandleCreated);
                }
            });
            test("main_managed_move_and_hide_menu_keep_worker_snapshot_independent",delegate {
                foreach(string state in new[] {"paused","waiting","protected","error"}) {
                    View view=Previews.State(state);RecordingSession session=new RecordingSession(view);bool enabled=view.Enabled;string status=view.Status,target=view.Settings.TargetExecutable;
                    using(ControlPanel form=new ControlPanel(session,true,false)) foreach(Point location in new[] {new Point(0,40),new Point(1700,900),new Point(-2500,-100),new Point(-100,1000)}) {
                        form.Location=location;form.PreviewInvokeHideEntry();form.PreviewRefresh();Check(form.Location==location && view.Enabled==enabled && view.Status==status && view.Settings.TargetExecutable==target && session.Commands.Count==0 && !form.IsHandleCreated);
                    }
                }
            });
            test("floating_managed_move_trace_clamps_edges_and_monitor_scales",delegate {
                List<MonitorArea> screens=new List<MonitorArea> {new MonitorArea {Name="primary",Work=new Rectangle(0,40,1920,1000)},new MonitorArea {Name="secondary",Work=new Rectangle(-2560,-200,2560,1440),Scale=1.5}};
                MemoryFloatingStore store=new MemoryFloatingStore();FakeFloatingWindow window=new FakeFloatingWindow();View view=Previews.State("protected");string target=view.Settings.TargetExecutable;
                using(FloatingController floating=new FloatingController(store,delegate {return window;})) {
                    floating.SetEnabled(true);floating.Refresh(view,screens);
                    foreach(Rectangle moved in new[] {new Rectangle(0,40,210,30),new Rectangle(1900,1000,210,30),new Rectangle(-2600,-250,315,45),new Rectangle(-120,1200,315,45),new Rectangle(700,400,210,30)}) {
                        floating.Moved(moved,screens);MonitorArea selected=screens.Find(delegate(MonitorArea monitor) {return monitor.Name==floating.Preferences.Monitor;});Check(selected!=null && selected.Work.Contains(window.Bounds) && floating.Preferences.Enabled && view.Enabled && view.Status=="保护中" && view.Settings.TargetExecutable==target && window.Shown);
                    }
                    floating.Moved(new Rectangle(-2400,0,315,45),screens);screens[1].Scale=2;floating.Refresh(view,screens);Check(window.Bounds.Width<=(int)(210*2) && screens[1].Work.Contains(window.Bounds) && window.Shows==1);floating.ResetPosition();floating.Refresh(view,screens);Check(screens[0].Work.Contains(window.Bounds) && view.Enabled && window.Disposes==0);
                }
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c){return !c.passed;}).Count;
            JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> report=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));
            report["ui_122_regressions"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;
            report["ui_122_test_scope"]="hidden controls, managed hover events, pure geometry, bitmap alpha, fake sessions and movement traces; no physical pointer/keys, native dropdown traversal, system theme changes, real tray, hotkeys or cursor clipping";
            File.WriteAllText(output,json.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
