// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace LoLMouseGuard
{
    static class ProtectionIntentTests
    {
        static void Check(bool value,string why) {if(!value) throw new Exception(why);}
        static Settings Configuration(bool wanted)
        {
            Settings s=Settings.Defaults();s.ProtectionEnabled=wanted;s.StartWithWindows=true;
            s.SetTargets(new[] {"GameA.exe","GameB.exe"});s.TargetDisplay="name";
            s.Toggle=new Shortcut(6,0x41);s.Emergency=new Shortcut(3,0x42);s.Exit=new Shortcut(6,0x58);return s;
        }
        static Scene Target(string name="GameA.exe",int window=42)
        {return new Scene {Window=new IntPtr(window),Pid=(uint)window,Executable=name,TargetExecutables=new List<string> {"GameA.exe","GameB.exe"},Visible=true,CoordinatesReady=true,CursorReady=true,Client=new Box(0,0,1920,1080),Monitor=new Box(0,0,1920,1080),VirtualScreen=new Box(-1920,0,1920,1080),Cursor=new Position(100,100)};}
        sealed class UnusedStartup : IStartupStore
        {
            public int Reads,Writes;
            public string Read() {Reads++;throw new Exception("Startup must not be read for a protection toggle");}
            public void Write(string value) {Writes++;throw new Exception("Startup must not be written for a protection toggle");}
        }
        sealed class Harness : IUiSession
        {
            public readonly FakeBackend Backend=new FakeBackend();
            public readonly MemorySettingsStore Store;
            public readonly UnusedStartup Startup=new UnusedStartup();
            public readonly GuardCore Core;
            public readonly SettingsService Service;
            public readonly SettingsSession Edit;
            public string Notice;public bool Stopped;Scene scene;
            public Harness(bool wanted,bool dpi=true,bool keys=true,MemorySettingsStore shared=null)
            {
                Store=shared??new MemorySettingsStore {Value=Configuration(wanted)};
                Backend.FailAllRegistrations=!keys;Core=new GuardCore(Backend,dpi);
                Service=new SettingsService(Backend,Store,Startup,@"C:\Synthetic\Guard.exe",Store.Value);
                Core.SetHotkeys(Service.RegisterCurrent(out Notice),Notice);Edit=new SettingsSession(Core,Service);
            }
            public void Restore() {Edit.RestoreOnStartup(out Notice);}
            public void Step(Scene current,long time) {scene=current;Core.Step(current,time);string failure;Edit.ObserveFault(out failure);if(failure!=null) Notice=failure;}
            public View ReadView()
            {
                View v=new View {Enabled=!Stopped&&Edit.EnabledIntent,CanEnable=Core.CanEnable&&!Edit.Editing,Editing=!Stopped&&Edit.Editing,Stopped=Stopped,Settings=Service.Current.Copy(),Status=Core.Status,Detail=Core.Detail,Notice=Notice};
                ViewTarget.Update(v,scene,Core.Enabled&&!Edit.Editing&&!Stopped);return v;
            }
            public void Send(Command command)
            {
                if(command==Command.BeginSettings) Edit.Begin(out Notice);
                else if(command==Command.CancelSettings) Edit.Cancel(out Notice);
                else if(command==Command.Quit) {Edit.Shutdown();Stopped=true;}
                else if(command==Command.Emergency || command==Command.Pause) Edit.Pause(command==Command.Emergency,out Notice);
                else if(command==Command.Enable) Edit.Enable(out Notice);
                else if(command==Command.Toggle) {if(Edit.Editing || Core.Enabled) Edit.Pause(false,out Notice);else Edit.Enable(out Notice);}
            }
            public void SaveSettings(Settings settings) {Edit.Save(settings,out Notice);}
            public void Dispose() {Edit.Shutdown();Core.Stop();Service.Dispose();Check(Startup.Reads==0&&Startup.Writes==0,"Startup touched");}
        }
        static void InFile(string output,Action<string> action)
        {
            string directory=Path.Combine(Path.GetDirectoryName(output),"intent-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string file=Path.Combine(directory,"settings.json");
            try {action(file);}finally {if(File.Exists(file)) File.Delete(file);if(Directory.GetFileSystemEntries(directory).Length==0) Directory.Delete(directory);}
        }
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}finally {UiText.SetLanguage("zh-CN");Theme.Apply(false,false);}checks.Add(c);};
            test("intent_new_install_defaults_paused_without_writing",delegate {
                InFile(output,delegate(string file) {string warning;Settings value=new FileSettingsStore(file).Load(out warning);Check(!value.ProtectionEnabled&&warning==null&&!File.Exists(file),"New install inferred enabled or wrote config");});
            });
            test("intent_v1_and_v2_legacy_missing_field_default_paused_preserve_settings",delegate {
                InFile(output,delegate(string file) {foreach(int version in new[] {1,2}) {JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> fields=json.Deserialize<Dictionary<string,object>>(json.Serialize(Configuration(true)));fields.Remove("ProtectionEnabled");fields["Version"]=version;if(version==1) {fields.Remove("TargetExecutables");fields["TargetExecutable"]="GameB.exe";}string raw=json.Serialize(fields);File.WriteAllText(file,raw,new UTF8Encoding(false));string warning;Settings loaded=new FileSettingsStore(file).Load(out warning);Check(warning==null&&!loaded.ProtectionEnabled&&loaded.StartWithWindows&&loaded.Toggle.Key==0x41&&loaded.TargetDisplay=="name"&&loaded.Matches("GameB.exe")&&File.ReadAllText(file)==raw,"Legacy intent inferred or preferences changed");}});
            });
            test("intent_file_roundtrip_true_false_and_copy_preserves_all_preferences",delegate {
                InFile(output,delegate(string file) {FileSettingsStore store=new FileSettingsStore(file);foreach(bool wanted in new[] {true,false,true}) {store.Save(Configuration(wanted));string warning;Settings loaded=store.Load(out warning);Check(warning==null&&loaded.ProtectionEnabled==wanted&&loaded.Copy().ProtectionEnabled==wanted&&loaded.TargetExecutables.Count==2&&loaded.Toggle.Modifiers==6&&loaded.Emergency.Key==0x42&&loaded.Exit.Key==0x58&&loaded.StartWithWindows&&loaded.TargetDisplay=="name"&&Directory.GetFiles(Path.GetDirectoryName(file)).Length==1,"Roundtrip or atomic replacement lost preferences");}});
            });
            test("intent_invalid_field_type_falls_back_paused_without_rewrite",delegate {
                InFile(output,delegate(string file) {foreach(object bad in new object[] {"true",1,null,new object[0]}) {JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> fields=json.Deserialize<Dictionary<string,object>>(json.Serialize(Configuration(true)));fields["ProtectionEnabled"]=bad;string raw=json.Serialize(fields);File.WriteAllText(file,raw,new UTF8Encoding(false));string warning;Settings loaded=new FileSettingsStore(file).Load(out warning);Check(warning!=null&&!loaded.ProtectionEnabled&&File.ReadAllText(file)==raw,"Malformed intent enabled or config rewritten");}});
            });
            test("intent_saved_enabled_startup_arms_waiting_without_cursor_apply",delegate {
                using(Harness h=new Harness(true)) {h.Restore();Check(h.Core.Enabled&&h.Edit.EnabledIntent&&h.Backend.Applies==0&&!h.Core.OwnsClip&&h.Store.Writes==0&&StatusPresentation.Short(h.ReadView())=="等待程序","Startup applied clip, wrote preferences or reported protected");}
            });
            test("intent_saved_paused_startup_remains_paused_with_valid_target",delegate {
                using(Harness h=new Harness(false)) {h.Restore();h.Step(Target(),0);h.Step(Target(),1000);Check(!h.Core.Enabled&&h.Backend.Applies==0&&h.Store.Writes==0,"Paused intent enabled");}
            });
            test("intent_enable_normal_quit_then_restart_restores_choice",delegate {
                MemorySettingsStore store=new MemorySettingsStore {Value=Configuration(false)};
                using(Harness h=new Harness(false, true,true,store)) {h.Restore();h.Send(Command.Enable);h.Step(Target(),0);h.Step(Target(),100);h.Send(Command.Quit);Check(store.Value.ProtectionEnabled&&!h.Core.Enabled&&!h.Core.OwnsClip&&h.Backend.Frees==1,"Normal exit changed intent or failed release");}
                using(Harness h=new Harness(true,true,true,store)) {h.Restore();Check(h.Core.Enabled&&h.Backend.Applies==0,"Restart lost saved enable");}
            });
            test("intent_explicit_toggle_pause_then_restart_stays_paused",delegate {
                MemorySettingsStore store=new MemorySettingsStore {Value=Configuration(true)};
                using(Harness h=new Harness(true,true,true,store)) {h.Restore();h.Send(Command.Toggle);Check(!store.Value.ProtectionEnabled&&!h.Core.Enabled,"Toggle pause not persisted");}
                using(Harness h=new Harness(false,true,true,store)) {h.Restore();Check(!h.Core.Enabled&&h.Backend.Applies==0,"Restart ignored pause");}
            });
            test("intent_settings_temporary_pause_cancel_and_save_do_not_persist_off",delegate {
                foreach(bool save in new[] {false,true}) using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);h.Send(Command.BeginSettings);Check(!h.Core.Enabled&&h.Edit.EnabledIntent&&h.Store.Value.ProtectionEnabled&&h.Store.Writes==0,"Temporary suspension changed intent");if(save) h.SaveSettings(h.Service.Current.Copy());else h.Send(Command.CancelSettings);Check(h.Core.Enabled&&h.Store.Value.ProtectionEnabled&&h.Backend.Applies==0,"Settings exit lost intent or applied clip");}
            });
            test("intent_focus_loss_and_minimize_release_preserve_enabled_choice",delegate {
                foreach(bool minimize in new[] {false,true}) using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);Scene other=Target();if(minimize) other.Minimized=true;else other.Executable="Other.exe";h.Step(other,101);Check(h.Core.Enabled&&!h.Core.OwnsClip&&h.Store.Value.ProtectionEnabled&&h.Store.Writes==0&&h.Backend.Frees==1&&StatusPresentation.Short(h.ReadView())!="保护中","Focus loss overwrote intent or reported protected");}
            });
            test("intent_restore_requires_100ms_stable_foreground_before_protection",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),1000);h.Step(Target(),1099);Check(h.Backend.Applies==0&&StatusPresentation.Short(h.ReadView())=="等待稳定","Stability skipped");h.Step(Target(),1100);Check(h.Backend.Applies==1&&StatusPresentation.Short(h.ReadView())=="保护中","Stable target not protected");}
            });
            test("intent_restore_rejects_unlisted_minimized_invalid_and_outside_cursor",delegate {
                foreach(int kind in new[] {0,1,2,3,4}) using(Harness h=new Harness(true)) {h.Restore();Scene s=Target();if(kind==0) s.Executable="Other.exe";else if(kind==1) s.Minimized=true;else if(kind==2) s.Window=IntPtr.Zero;else if(kind==3) s.CoordinatesReady=false;else s.Cursor=new Position(-500,100);h.Step(s,0);h.Step(s,1000);Check(h.Core.Enabled&&h.Backend.Applies==0&&h.Store.Value.ProtectionEnabled&&StatusPresentation.Short(h.ReadView())!="保护中","Unsafe restored target clipped or misreported");}
            });
            test("intent_restore_multi_program_switch_releases_and_rechecks_identity",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);h.Step(Target("GameB.exe",55),101);Check(h.Backend.Frees==1&&h.Backend.Applies==1,"Old target not released");h.Step(Target("GameB.exe",55),200);Check(h.Backend.Applies==1,"New target stabilized too soon");h.Step(Target("GameB.exe",55),201);Check(h.Backend.Applies==2&&h.Store.Value.ProtectionEnabled&&h.Store.Writes==0,"Multi target switch lost intention");}
            });
            test("intent_hotkey_and_dpi_failures_block_restore_and_disarm",delegate {
                foreach(bool dpiFailure in new[] {false,true}) using(Harness h=new Harness(true,!dpiFailure,dpiFailure)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);Check(!h.Core.Enabled&&!h.Core.CanEnable&&!h.Store.Value.ProtectionEnabled&&h.Backend.Applies==0,"Readiness failure restored protection");}
            });
            test("intent_emergency_before_startup_restore_has_priority",delegate {
                using(Harness h=new Harness(true)) {h.Send(Command.Emergency);h.Restore();Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled&&h.Backend.Applies==0&&h.Backend.Frees==1,"Startup override emergency");}
            });
            test("intent_emergency_after_restore_and_repeat_restore_stays_paused",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);h.Send(Command.Emergency);h.Restore();h.Step(Target(),200);Check(!h.Core.Enabled&&!h.Core.OwnsClip&&!h.Store.Value.ProtectionEnabled&&h.Backend.Applies==1,"Repeated restore overrode emergency");}
            });
            test("intent_emergency_while_editing_beats_stale_saved_draft",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);Settings stale=h.Service.Current.Copy();h.Send(Command.Emergency);h.SaveSettings(stale);Check(!h.Core.Enabled&&!h.Edit.EnabledIntent&&!h.Store.Value.ProtectionEnabled&&stale.ProtectionEnabled&&h.Backend.Applies==0,"Old settings draft undid emergency or was mutated");}
            });
            test("intent_explicit_pause_before_restore_cannot_be_overridden",delegate {
                using(Harness h=new Harness(true)) {h.Send(Command.Pause);h.Restore();Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled&&h.Store.Writes==1,"Startup overrode manual pause");}
            });
            test("intent_enable_save_failure_keeps_runtime_paused_and_previous_preferences",delegate {
                using(Harness h=new Harness(false)) {h.Restore();h.Store.Fail=true;h.Send(Command.Enable);Check(!h.Core.Enabled&&!h.Edit.EnabledIntent&&!h.Store.Value.ProtectionEnabled&&h.Notice!=null&&h.Backend.Applies==0&&h.Service.Current.TargetExecutables.Count==2,"Failed save enabled or lost settings");}
            });
            test("intent_emergency_save_failure_releases_immediately_warns_and_does_not_retry_loop",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);h.Store.Fail=true;h.Send(Command.Emergency);int writes=h.Store.Writes;for(int i=0;i<50;i++) h.Step(Target(),200+i);Check(!h.Core.Enabled&&!h.Core.OwnsClip&&h.Notice!=null&&h.Store.Value.ProtectionEnabled&&h.Store.Writes==writes,"Persistence failure prevented emergency release or caused repeated writes");h.Restore();Check(!h.Core.Enabled,"Startup retry overrode failed emergency");}
            });
            test("intent_failed_emergency_save_then_successful_settings_save_keeps_pause",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);Settings draft=h.Service.Current.Copy();h.Store.Fail=true;h.Send(Command.Emergency);h.Store.Fail=false;h.SaveSettings(draft);Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled&&h.Notice.StartsWith("设置已保存",StringComparison.Ordinal),"Recovered save resurrected emergency intent");}
            });
            test("intent_cursor_api_fault_disarms_saved_restore_without_automatic_retry",delegate {
                foreach(bool read in new[] {false,true}) using(Harness h=new Harness(true)) {h.Restore();if(read) h.Backend.FailRead=true;else h.Backend.ApplyError=5;h.Step(Target(),0);h.Step(Target(),100);Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled&&h.Core.Status.Contains("已暂停"),"API fault kept saved enabled");h.Backend.FailRead=false;h.Backend.ApplyError=0;h.Step(Target(),500);h.Restore();Check(!h.Core.Enabled,"Fault automatically resumed");}
            });
            test("intent_release_failure_retains_cleanup_ownership_but_disarms_restart",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);h.Backend.FailFree=true;h.Step(Target("GameB.exe",55),101);Check(!h.Core.Enabled&&h.Core.OwnsClip&&!h.Store.Value.ProtectionEnabled&&h.Backend.Registered.ContainsKey(2),"Release fault lost ownership or emergency key");h.Send(Command.Enable);Check(!h.Core.Enabled,"Enabled over owned failed clip");h.Backend.FailFree=false;h.Step(Target(),200);Check(!h.Core.OwnsClip&&!h.Core.Enabled,"Cleanup resumed protection");}
            });
            test("intent_exception_abort_cancels_settings_resume_and_saved_restore",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);h.Edit.Abort(out h.Notice);h.Send(Command.CancelSettings);h.Restore();Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled&&!h.Edit.EnabledIntent,"Fault abort overridden by settings or startup");}
            });
            test("intent_settings_failure_disarms_but_normal_exit_during_edit_keeps_choice",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);Settings bad=h.Service.Current.Copy();bad.TargetDisplay="bad";h.SaveSettings(bad);Check(!h.Core.Enabled&&!h.Store.Value.ProtectionEnabled,"Failed settings resumed on next launch");}
                using(Harness h=new Harness(true)) {h.Restore();h.Send(Command.BeginSettings);h.Send(Command.Quit);Check(!h.Core.Enabled&&h.Store.Value.ProtectionEnabled,"Normal quit during settings overwrote intention");}
            });
            test("intent_failed_normal_exit_cleanup_disarms_restart_and_retains_release_ownership",delegate {
                using(Harness h=new Harness(true)) {h.Restore();h.Step(Target(),0);h.Step(Target(),100);h.Backend.FailFree=true;h.Edit.Shutdown(out h.Notice);Check(!h.Core.Enabled&&h.Core.OwnsClip&&!h.Store.Value.ProtectionEnabled,"Exit cleanup fault retained restart intent or lost ownership");h.Backend.FailFree=false;h.Step(Target(),200);Check(!h.Core.OwnsClip&&!h.Core.Enabled,"Exit cleanup resumed protection");}
            });
            test("intent_toggle_preserves_custom_keys_targets_display_and_never_touches_startup",delegate {
                using(Harness h=new Harness(false)) {h.Restore();h.Send(Command.Toggle);h.Send(Command.Toggle);h.Send(Command.Enable);Settings saved=h.Store.Value;Check(saved.ProtectionEnabled&&saved.StartWithWindows&&saved.TargetExecutables.Count==2&&saved.TargetDisplay=="name"&&saved.Toggle.Key==0x41&&saved.Emergency.Key==0x42&&saved.Exit.Key==0x58&&h.Backend.RegisterCalls==3&&h.Startup.Reads==0&&h.Startup.Writes==0,"Switch disturbed other settings or startup");}
            });
            test("intent_hidden_bilingual_ui_restored_waiting_stays_distinct_from_protected",delegate {
                foreach(string language in new[] {"zh-CN","en"}) using(Harness h=new Harness(true)) {UiText.SetLanguage(language);h.Restore();using(ControlPanel form=new ControlPanel(h,true,false)) {form.PreviewRefresh();Check(h.ReadView().Enabled&&StatusPresentation.Short(h.ReadView())==(language=="en"?"Wait: program":"等待程序")&&!form.IsHandleCreated,"Restored switch misreported protection");form.PreviewOpenSettings();Check(h.ReadView().Enabled&&h.ReadView().Editing&&h.Store.Value.ProtectionEnabled,"UI settings overwrote switch");form.PreviewCancelSettings();Check(h.Core.Enabled&&!form.IsHandleCreated,"UI cancel lost restored state");}}
            });
            JavaScriptSerializer serializer=new JavaScriptSerializer();Dictionary<string,object> report=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));
            int failed=checks.FindAll(delegate(SelfTests.Check c) {return !c.passed;}).Count;
            report["protection_intent_tests"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;
            report["protection_intent_scope"]="fake cursor/hotkey/startup backends, isolated configuration files, real hidden UI handlers; no normal app, game, real keys, startup registry or native clipping";
            File.WriteAllText(output,serializer.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
