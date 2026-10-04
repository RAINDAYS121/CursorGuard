// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    static class SettingsSessionTests
    {
        static void Check(bool value,string message) {if(!value) throw new Exception(message);}
        static Scene Target() {return new Scene {Window=new IntPtr(42),Pid=100,Executable="League of Legends.exe",Visible=true,CoordinatesReady=true,CursorReady=true,Client=new Box(0,0,1920,1080),Monitor=new Box(0,0,1920,1080),VirtualScreen=new Box(-1920,0,1920,1080),Cursor=new Position(100,100)};}
        sealed class Harness : IUiSession
        {
            public readonly FakeBackend Backend=new FakeBackend();
            public readonly MemorySettingsStore Store=new MemorySettingsStore();
            public readonly GuardCore Core;
            public readonly SettingsService Service;
            public readonly SettingsSession Edit;
            public bool Stopped;
            public Harness(bool enabled,bool dpi=true) {Core=new GuardCore(Backend,dpi);Service=new SettingsService(Backend,Store,new MemoryStartupStore(),@"C:\Example\Guard.exe",Store.Value);string notice;Core.SetHotkeys(Service.RegisterCurrent(out notice),notice);Edit=new SettingsSession(Core,Service);if(enabled) Core.Enable();}
            public View ReadView() {return new View {Enabled=!Stopped&&Edit.EnabledIntent,CanEnable=Core.CanEnable&&!Edit.Editing,Editing=!Stopped&&Edit.Editing,Stopped=Stopped,Settings=Service.Current.Copy(),Status=Core.Status};}
            public void Send(Command command) {string notice;if(command==Command.BeginSettings) Edit.Begin(out notice);else if(command==Command.CancelSettings) Edit.Cancel(out notice);else if(command==Command.Emergency || command==Command.Pause) Edit.Pause(command==Command.Emergency);else if(command==Command.Quit) {Edit.Abort();Stopped=true;}}
            public void SaveSettings(Settings settings) {string notice;Edit.Save(settings,out notice);}
            public void Dispose() {Edit.Abort();Stopped=true;Core.Stop();Service.Dispose();}
        }
        sealed class ThrowingStore : ISettingsStore
        {
            public Settings Load(out string warning) {warning=null;return Settings.Defaults();}
            public void Save(Settings settings) {throw new IOException("simulated persistence error");}
        }
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}finally {UiText.SetLanguage("zh-CN");Theme.Apply(false,false);}checks.Add(c);};
            test("settings_intent_repeated_begin_cancel_and_done_restore_only_enabled_intent",delegate {
                foreach(bool enabled in new[] {false,true}) using(Harness h=new Harness(enabled)) for(int i=0;i<8;i++) {string notice;Check(h.Edit.Begin(out notice)&&h.Edit.Begin(out notice),"Repeated begin failed");Check(h.Edit.EnabledIntent==enabled&&!h.Core.Enabled&&!h.Core.OwnsClip&&!h.Service.Ready,"Editing changed intention or clipped");if(i%2==0) Check(h.Edit.Cancel(out notice),"Cancel failed");else Check(h.Edit.Save(h.Service.Current.Copy(),out notice),"Save failed");Check(h.Core.Enabled==enabled&&h.Service.Ready&&!h.Edit.Editing&&h.Backend.Applies==0,"Completion lost intention or applied stale clip");}
            });
            test("settings_resume_requires_fresh_foreground_cursor_and_stable_identity",delegate {
                using(Harness h=new Harness(true)) {Scene target=Target();h.Core.Step(target,0);h.Core.Step(target,100);Check(h.Core.OwnsClip,"Initial clip missing");string notice;h.Edit.Begin(out notice);Check(!h.Core.OwnsClip&&h.Backend.Frees==1,"Editing did not release");h.Edit.Cancel(out notice);Check(h.Core.Enabled&&!h.Core.OwnsClip&&h.Backend.Applies==1,"Resume immediately clipped");Scene other=Target();other.Executable="Browser.exe";h.Core.Step(other,200);Check(!h.Core.OwnsClip,"Wrong foreground clipped");target.Cursor=new Position(-50,100);h.Core.Step(target,300);Check(!h.Core.OwnsClip,"Outside cursor clipped");target.Cursor=new Position(100,100);target.Window=new IntPtr(43);h.Core.Step(target,400);h.Core.Step(target,499);Check(!h.Core.OwnsClip,"Stability bypassed");h.Core.Step(target,500);Check(h.Core.OwnsClip&&h.Backend.Applies==2,"Fresh target did not resume");h.Core.Step(other,501);Check(!h.Core.OwnsClip&&h.Core.Enabled,"Normal focus loss stopped releasing");}
            });
            test("settings_explicit_pause_emergency_and_abort_override_resume_on_all_exits",delegate {
                foreach(int action in new[] {0,1,2}) foreach(bool save in new[] {false,true}) using(Harness h=new Harness(true)) {string notice;h.Edit.Begin(out notice);if(action==2) h.Edit.Abort();else h.Edit.Pause(action==1);if(save) h.Edit.Save(h.Service.Current.Copy(),out notice);else h.Edit.Cancel(out notice);Check(!h.Core.Enabled&&!h.Edit.EnabledIntent&&h.Backend.Applies==0,"Explicit stop was overridden");}
            });
            test("settings_ui_back_done_and_repeated_open_keep_switch_and_draft",delegate {
                foreach(bool save in new[] {false,true}) using(Harness h=new Harness(true)) using(ControlPanel form=new ControlPanel(h,true,false)) {form.PreviewOpenSettings();form.PreviewTargetDisplayChoice.SelectedIndex=1;form.PreviewOpenSettings();Check(form.PreviewTargetDisplayChoice.SelectedIndex==1&&h.ReadView().Enabled&&h.Edit.Editing&&!h.Core.Enabled,"Repeated open reset draft or switch");if(save) {form.PreviewSaveSettings();form.PreviewRefresh();}else form.PreviewCancelSettings();Check(h.Core.Enabled&&!h.Edit.Editing&&h.Service.Current.TargetDisplay==(save?"name":"icon")&&!form.IsHandleCreated,"UI finish did not restore or persist");}
            });
            test("settings_ui_window_close_quits_without_resuming",delegate {
                using(Harness h=new Harness(true)) using(ControlPanel form=new ControlPanel(h,true,false)) {form.PreviewOpenSettings();form.PreviewCloseSettingsWindow();string notice;h.Edit.Cancel(out notice);Check(h.Stopped&&!h.Core.Enabled&&!h.Edit.EnabledIntent&&h.Backend.Applies==0&&!form.IsHandleCreated,"Window close resumed protection");}
            });
            test("settings_shortcut_edit_success_restores_intent_after_registration",delegate {
                using(Harness h=new Harness(true)) {string notice;h.Edit.Begin(out notice);Settings candidate=h.Service.Current.Copy();candidate.Toggle=new Shortcut(6,0x41);Check(h.Edit.Save(candidate,out notice)&&h.Core.Enabled&&h.Backend.Registered[1].Key==0x41&&h.Backend.Applies==0,"Shortcut commit did not safely resume");}
            });
            test("settings_shortcut_capture_cancel_keeps_existing_value_and_intent",delegate {
                using(Harness h=new Harness(true)) using(ControlPanel form=new ControlPanel(h,true,false)) {form.PreviewOpenSettings();foreach(Control child in form.PreviewTargetDisplayChoice.Parent.Controls) {ShortcutButton key=child as ShortcutButton;if(key!=null) {key.Capturing=true;key.SetValue(key.Value);Check(!key.Capturing,"Shortcut capture cancel failed");}}form.PreviewCancelSettings();Check(h.Core.Enabled&&h.Service.Current.Toggle.Key==0x77&&!form.IsHandleCreated,"Capture cancel lost original intent");}
            });
            test("settings_store_exception_keeps_paused_and_restores_hotkeys",delegate {
                FakeBackend backend=new FakeBackend();using(SettingsService service=new SettingsService(backend,new ThrowingStore(),new MemoryStartupStore(),@"C:\Example\Guard.exe",Settings.Defaults())) {GuardCore core=new GuardCore(backend,true);string notice;core.SetHotkeys(service.RegisterCurrent(out notice),notice);core.Enable();SettingsSession edit=new SettingsSession(core,service);edit.Begin(out notice);Check(!edit.Save(service.Current.Copy(),out notice)&&!core.Enabled&&!edit.EnabledIntent&&service.Ready&&backend.Registered.Count==3&&backend.Applies==0,"Save exception resumed or lost shortcuts");}
            });
            test("settings_validation_and_hotkey_registration_errors_never_resume",delegate {
                foreach(bool conflict in new[] {false,true}) using(Harness h=new Harness(true)) {string notice;h.Edit.Begin(out notice);Settings candidate=h.Service.Current.Copy();if(conflict) h.Backend.FailAllRegistrations=true;else candidate.TargetDisplay="invalid";Check(!h.Edit.Save(candidate,out notice)&&!h.Core.Enabled&&!h.Edit.EnabledIntent&&h.Backend.Applies==0,"Error resumed intention");Check(conflict?!h.Core.CanEnable:h.Service.Ready,"Incorrect registration error state");}
            });
            test("settings_cancel_registration_failure_and_dpi_failure_stay_paused",delegate {
                using(Harness h=new Harness(true)) {string notice;h.Edit.Begin(out notice);h.Backend.FailAllRegistrations=true;Check(!h.Edit.Cancel(out notice)&&!h.Core.Enabled&&!h.Core.CanEnable,"Cancel conflict resumed");}
                using(Harness h=new Harness(true,false)) {string notice;h.Edit.Begin(out notice);h.Edit.Cancel(out notice);Check(!h.Core.Enabled&&!h.Core.CanEnable,"DPI failure enabled protection");}
            });
            test("settings_release_failure_preserves_emergency_registration_and_discards_resume",delegate {
                using(Harness h=new Harness(true)) {h.Core.Step(Target(),0);h.Core.Step(Target(),100);h.Backend.FailFree=true;string notice;Check(!h.Edit.Begin(out notice)&&!h.Edit.Editing&&!h.Edit.EnabledIntent&&h.Core.OwnsClip&&h.Backend.Registered.ContainsKey(2),"Release failure lost emergency registration");h.Backend.FailFree=false;h.Edit.Pause(true);h.Edit.Begin(out notice);h.Edit.Cancel(out notice);Check(!h.Core.Enabled&&!h.Core.OwnsClip,"Failed begin retained stale resume");}
            });
            test("target_display_missing_field_defaults_icon_and_preserves_other_settings",delegate {
                JavaScriptSerializer json=new JavaScriptSerializer();Settings old=Settings.Defaults();old.Toggle=new Shortcut(6,0x41);old.TargetExecutable="My Real Game.exe";old.StartWithWindows=true;Dictionary<string,object> values=json.Deserialize<Dictionary<string,object>>(json.Serialize(old));values.Remove("TargetDisplay");Settings loaded=json.Deserialize<Settings>(json.Serialize(values));Check(loaded.TargetDisplay=="icon"&&loaded.TargetExecutable==old.TargetExecutable&&loaded.StartWithWindows&&loaded.Toggle.Key==0x41&&loaded.Validate()==null,"Legacy field migration lost settings");
            });
            test("target_display_file_roundtrip_preserves_name_and_icon_modes",delegate {
                string dir=Path.Combine(Path.GetDirectoryName(output),"display-test-"+Guid.NewGuid().ToString("N")),file=Path.Combine(dir,"settings.json");try {FileSettingsStore store=new FileSettingsStore(file);foreach(string mode in new[] {"name","icon"}) {Settings value=Settings.Defaults();value.TargetDisplay=mode;value.TargetExecutable="真实程序.exe";store.Save(value);string notice;Settings loaded=store.Load(out notice);Check(loaded.TargetDisplay==mode&&loaded.TargetExecutable==value.TargetExecutable&&loaded.Copy().TargetDisplay==mode&&notice==null,"Display mode did not persist");}}finally {if(File.Exists(file)) File.Delete(file);if(Directory.Exists(dir)&&Directory.GetFileSystemEntries(dir).Length==0) Directory.Delete(dir);}
            });
            test("language_choices_have_native_names_in_both_languages",delegate {
                foreach(string language in new[] {"zh-CN","en"}) {UiText.SetLanguage(language);using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.PreviewSettings();Check((string)form.PreviewLanguageChoice.Items[0]=="中文"&&(string)form.PreviewLanguageChoice.Items[1]=="English","Language native names changed");Check((string)form.PreviewTargetDisplayChoice.Items[0]==(language=="en"?"Program icon":"程序图标")&&(string)form.PreviewTargetDisplayChoice.Items[1]==(language=="en"?"Program name":"程序名称"),"Display options untranslated");}}
            });
            test("target_display_both_modes_preserve_status_and_real_name_across_palettes_and_scales",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) foreach(string mode in new[] {"icon","name"}) foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {UiText.SetLanguage(locale);Theme.Apply(dark,false);View view=Previews.State("protected");view.Settings.TargetDisplay=mode;view.Settings.TargetExecutable="真实名称 very long program name 123456789.exe";view.ActiveExecutable=view.Settings.TargetExecutable;string status=StatusPresentation.Short(view);using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) using(Bitmap main=form.RenderPreview(scale)) {Check(main.Width<=Math.Ceiling(320*scale)&&form.PreviewTargetCaption==(mode=="name"?StatusPresentation.Name(view):"")&&!form.IsHandleCreated,"Main display overflow or translated name");}using(FloatingBar bar=new FloatingBar(true)) {bar.Present(view);using(Bitmap image=bar.PreviewBitmap(scale)) Check(image.Width<=Math.Ceiling(210*scale)&&!bar.IsHandleCreated,"Floating overflow");}Check(view.Enabled&&view.Status=="保护中"&&StatusPresentation.Short(view)==status,"Mode changed protection state");}
            });
            test("target_display_switch_repositions_floating_with_same_saved_location",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore {Value=new FloatingPreferences {Enabled=true,HasPosition=true,Monitor="synthetic",X=900,Y=550}};FakeFloatingWindow window=new FakeFloatingWindow();View view=Previews.State("protected");List<MonitorArea> screens=new List<MonitorArea> {new MonitorArea {Name="synthetic",Work=new Rectangle(0,0,1000,600),Scale=1.5}};using(FloatingController controller=new FloatingController(store,delegate {return window;})) {controller.Refresh(view,screens);int iconWidth=window.Bounds.Width;view.Settings.TargetDisplay="name";controller.Refresh(view,screens);Check(window.Bounds.Width>iconWidth&&screens[0].Work.Contains(window.Bounds)&&store.Value.X==900&&store.Value.Y==550&&window.Shows==1&&view.Enabled,"Switch lost position or recreated window");view.Settings.TargetDisplay="icon";controller.Refresh(view,screens);Check(window.Bounds.Width==iconWidth&&screens[0].Work.Contains(window.Bounds),"Icon restore failed");}
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c) {return !c.passed;}).Count;JavaScriptSerializer serializer=new JavaScriptSerializer();Dictionary<string,object> report=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));report["settings_124_regressions"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;report["settings_124_scope"]="fake core/backends/stores, real hidden UI handlers, persistence in isolated temporary files, offscreen layouts; no live game, real shortcuts, focus or clipping";File.WriteAllText(output,serializer.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
