// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace LoLMouseGuard
{
    static class MultipleTargetTests
    {
        static readonly string[] Names = {"GameA.exe", "GameB.exe"};
        static void Check(bool condition, string detail) {if(!condition) throw new Exception(detail);}
        static void Equal<T>(T actual,T expected,string detail)
        {if(!Object.Equals(actual,expected)) throw new Exception(detail+"; actual="+actual+", expected="+expected);}
        static Settings Targets(params string[] names) {Settings s=Settings.Defaults();s.SetTargets(names);return s;}
        static Scene SceneFor(string name,uint pid,int window,Box box)
        {return new Scene {Window=new IntPtr(window),Pid=pid,Executable=name,TargetExecutables=new List<string>(Names),Visible=true,CoordinatesReady=true,CursorReady=true,Client=box,Monitor=box,VirtualScreen=new Box(-2560,0,2560,1440),Cursor=new Position(box.Left+100,box.Top+100)};}
        static Scene A() {return SceneFor(Names[0],101,11,new Box(0,0,1920,1080));}
        static Scene B() {return SceneFor(Names[1],202,22,new Box(-1920,0,0,1080));}
        static GuardCore Ready(FakeBackend backend) {GuardCore core=new GuardCore(backend,true);core.SetHotkeys(true,null);core.Enable();return core;}
        static void Active(GuardCore core,Scene scene,long time) {core.Step(scene,time);core.Step(scene,time+100);}
        static void InFile(string output,Action<string> action)
        {
            string dir=Path.Combine(Path.GetDirectoryName(output),"targets-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            string file=Path.Combine(dir,"settings.json");
            try {action(file);}finally {if(File.Exists(file)) File.Delete(file);if(Directory.GetFileSystemEntries(dir).Length==0) Directory.Delete(dir);}
        }
        static string Legacy(string target)
        {
            Settings old=Settings.Defaults();old.Toggle=new Shortcut(6,0x41);old.StartWithWindows=true;old.TargetDisplay="name";
            JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> fields=json.Deserialize<Dictionary<string,object>>(json.Serialize(old));
            fields["Version"]=1;fields.Remove("TargetExecutables");if(target!=null) fields["TargetExecutable"]=target;
            return json.Serialize(fields);
        }
        sealed class ActiveIcons : IProgramIconSource,IActiveProgramIconSource
        {
            public int LegacyCalls;public uint LastPid;
            public IconDescriptor Resolve(string executable) {LegacyCalls++;return null;}
            public IconDescriptor Resolve(string executable,uint pid) {LastPid=pid;return new IconDescriptor {Path="C:\\synthetic\\"+pid+".exe",Stamp=pid};}
            public Bitmap Extract(string path,int pixels) {Bitmap b=new Bitmap(pixels,pixels);using(Graphics g=Graphics.FromImage(b)) g.Clear(LastPid==101?Color.Red:Color.Blue);return b;}
        }
        public static int Run(string output)
        {
            File.WriteAllText(output,new JavaScriptSerializer().Serialize(new {version="1.3.0",passed=0,failed=0,mode="focused_fake_targets_and_hidden_ui"}),new UTF8Encoding(false));
            return Append(output);
        }
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}finally {UiText.SetLanguage("zh-CN");Theme.Apply(false,false);}checks.Add(c);};
            test("targets_switch_releases_old_bounds_before_fresh_stability",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);core.Step(B(),101);
                Equal(backend.Frees,1,"Old target release");Equal(backend.Applies,1,"New target must wait");core.Step(B(),200);Equal(backend.Applies,1,"99 ms must wait");core.Step(B(),201);
                Equal(backend.Applies,2,"New target apply");Equal(backend.Clip,B().Client,"New client bounds");
            });
            test("targets_identity_change_with_same_window_pid_and_bounds_resets_stability",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Scene first=A();Active(core,first,0);Scene second=A();second.Executable=Names[1];core.Step(second,101);
                Equal(backend.Frees,1,"Changed executable releases");Equal(backend.Applies,1,"Same geometry cannot reuse old stability");core.Step(second,201);Equal(backend.Applies,2,"Fresh identity can protect");
            });
            test("targets_rapid_switching_requires_continuous_stability",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);core.Step(A(),0);core.Step(B(),60);core.Step(A(),120);core.Step(A(),219);Equal(backend.Applies,0,"Unstable focus");core.Step(A(),220);Equal(backend.Applies,1,"Continuous 100 ms");
            });
            test("targets_unlisted_foreground_releases_without_applying",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene other=A();other.Executable="Other.exe";core.Step(other,101);core.Step(other,1000);
                Equal(backend.Frees,1,"Unlisted focus release");Equal(backend.Applies,1,"Unlisted target apply count");Check(core.Enabled&&!core.OwnsClip,"Intention must survive focus loss");
            });
            test("targets_empty_list_never_confines_and_releases_owned_constraint",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene empty=A();empty.TargetExecutables=new string[0];core.Step(empty,101);core.Step(empty,201);
                Equal(backend.Frees,1,"Empty list releases");Equal(backend.Applies,1,"Empty list does not apply");Check(!core.OwnsClip&&core.Enabled,"Empty list waiting state");
            });
            test("targets_removed_active_program_is_no_longer_eligible",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene removed=A();removed.TargetExecutables=new[] {Names[1]};core.Step(removed,101);Equal(backend.Frees,1,"Removed active target releases");Equal(backend.Applies,1,"Removed target cannot apply");Active(core,B(),102);Equal(backend.Applies,2,"Remaining target still works");
            });
            test("targets_duplicate_processes_protect_only_current_window_with_new_stability",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene another=SceneFor(Names[0],303,33,new Box(-1600,0,0,900));core.Step(another,101);Equal(backend.Applies,1,"Duplicate process waits");Equal(backend.Frees,1,"Prior instance releases");core.Step(another,201);Equal(backend.Clip,another.Client,"Current instance coordinates");
            });
            test("targets_client_resize_requires_new_region_and_stability",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene resized=A();resized.Client=new Box(20,20,1200,800);resized.Cursor=new Position(200,200);core.Step(resized,101);Equal(backend.Frees,1,"Resize releases");core.Step(resized,201);Equal(backend.Clip,resized.Client,"Resized target region");
            });
            test("targets_outside_cursor_is_not_pulled_into_new_target",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene outside=B();outside.Cursor=new Position(100,100);core.Step(outside,101);core.Step(outside,201);Equal(backend.Applies,1,"Outside cursor must stay free");Check(!core.OwnsClip,"New target cannot claim outside cursor");
            });
            test("targets_exit_or_invalid_foreground_fails_open",delegate {
                foreach(int mode in new[] {0,1,2,3}) {FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Scene exited=A();if(mode==0) exited.Pid=0;else if(mode==1) exited.Visible=false;else if(mode==2) exited.Minimized=true;else exited.CoordinatesReady=false;core.Step(exited,101);Equal(backend.Frees,1,"Invalid foreground mode="+mode);Check(!core.OwnsClip,"Invalid foreground ownership");}
            });
            test("targets_failed_release_blocks_new_target_and_retries_cleanup",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);backend.FailFree=true;core.Step(B(),101);Check(!core.Enabled&&core.OwnsClip,"Release failure must pause");Equal(backend.Applies,1,"Cannot apply new target after failed release");backend.FailFree=false;core.Step(B(),201);Check(!core.OwnsClip&&!core.Enabled,"Cleanup should remain paused");
            });
            test("targets_preserve_later_external_constraint_on_switch",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);Box external=new Box(50,50,500,500);backend.Clip=external;core.Step(B(),101);Equal(backend.Clip,external,"External clip must remain during wait");Equal(backend.Frees,0,"Do not clear someone else's changed rectangle");
            });
            test("targets_foreground_race_before_apply_skips_without_claiming",delegate {
                FakeBackend backend=new FakeBackend {SkipApply=true};GuardCore core=Ready(backend);Active(core,B(),0);Equal(backend.Applies,0,"Race-skipped apply");Check(!core.OwnsClip,"Skipped ownership");
            });
            test("targets_case_insensitive_deduplication_keeps_first_real_names",delegate {
                Settings settings=Targets("Real Game.exe","REAL GAME.EXE","真实名称.exe","真实名称.EXE");Equal(settings.TargetExecutables.Count,2,"Unique executable count");Equal(settings.TargetExecutables[0],"Real Game.exe","Preserved name");Check(settings.Matches("real game.EXE")&&!settings.Matches("Other.exe"),"Case insensitive membership");
            });
            test("targets_copy_isolation_between_drafts_worker_and_view",delegate {
                Settings original=Targets(Names);Settings draft=original.Copy();draft.TargetExecutables.RemoveAt(0);Equal(original.TargetExecutables.Count,2,"Draft cannot mutate worker configuration");View view=new View {Settings=original.Copy()};view.Settings.TargetExecutables.Clear();Equal(original.TargetExecutables.Count,2,"View cannot mutate worker configuration");
            });
            test("targets_single_program_config_migrates_readonly_and_preserves_other_settings",delegate {
                InFile(output,delegate(string file) {string old=Legacy("真实 Game.exe");File.WriteAllText(file,old,Encoding.UTF8);FileSettingsStore store=new FileSettingsStore(file);string warning;Settings restored=store.Load(out warning);Equal(restored.Version,2,"Migrated version");Equal(restored.TargetExecutables.Count,1,"Migrated target count");Equal(restored.TargetExecutable,"真实 Game.exe","Migrated executable");Check(warning==null&&restored.Toggle.Modifiers==6&&restored.Toggle.Key==0x41&&restored.StartWithWindows&&restored.TargetDisplay=="name","Legacy preference preservation");Equal(File.ReadAllText(file,Encoding.UTF8),old,"Load must not rewrite original config");store.Save(restored);Dictionary<string,object> fields=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(file,Encoding.UTF8));Check(fields.ContainsKey("TargetExecutables")&&!fields.ContainsKey("TargetExecutable"),"Persist canonical list only");});
            });
            test("targets_older_config_without_target_keeps_default",delegate {
                InFile(output,delegate(string file) {File.WriteAllText(file,Legacy(null),Encoding.UTF8);string warning;Settings loaded=new FileSettingsStore(file).Load(out warning);Check(warning==null&&loaded.TargetExecutables.Count==1&&loaded.Matches("League of Legends.exe"),"Missing old field default");});
            });
            test("targets_empty_list_roundtrip_stays_empty_and_restart_stays_paused",delegate {
                InFile(output,delegate(string file) {FileSettingsStore store=new FileSettingsStore(file);store.Save(Targets());string warning;Settings loaded=store.Load(out warning);Equal(loaded.TargetExecutables.Count,0,"Empty list must not add League automatically");Check(warning==null,"Empty list valid");FakeBackend backend=new FakeBackend();using(SettingsService service=new SettingsService(backend,store,new MemoryStartupStore(),@"C:\Synthetic\Guard.exe",loaded)) {string error;GuardCore core=new GuardCore(backend,true);core.SetHotkeys(service.RegisterCurrent(out error),error);Scene candidate=A();candidate.TargetExecutables=loaded.TargetExecutables;core.Step(candidate,1000);Check(!core.Enabled&&backend.Applies==0,"Restart remains paused");core.Enable();Active(core,candidate,1001);Equal(backend.Applies,0,"Enabled empty list remains free");}});
            });
            test("targets_multi_list_persistence_deduplication_and_atomic_replacement",delegate {
                InFile(output,delegate(string file) {FileSettingsStore store=new FileSettingsStore(file);Settings value=Targets("GameA.exe","GAMEA.EXE","GameB.exe");store.Save(value);string warning;Settings loaded=store.Load(out warning);Equal(loaded.TargetExecutables.Count,2,"Saved list deduplication");store.Save(Targets("GameB.exe"));loaded=store.Load(out warning);Equal(loaded.TargetExecutables.Count,1,"Replacement count");Check(loaded.Matches("GameB.exe")&&!loaded.Matches("GameA.exe")&&Directory.GetFiles(Path.GetDirectoryName(file)).Length==1,"Atomic replacement cleanup");});
            });
            test("targets_corrupt_lists_fallback_without_modifying_original_file",delegate {
                foreach(string bad in new[] {"null","\"GameA.exe\"","[17]","[null]","[\"C:\\\\Bad.exe\"]","[\".exe\"]","[\"Game*.exe\"]"}) InFile(output,delegate(string file) {string data=new JavaScriptSerializer().Serialize(Targets(Names));int start=data.IndexOf("\"TargetExecutables\":")+"\"TargetExecutables\":".Length;int end=data.IndexOf(']',start)+1;data=data.Substring(0,start)+bad+data.Substring(end);File.WriteAllText(file,data,Encoding.UTF8);string warning;Settings loaded=new FileSettingsStore(file).Load(out warning);Check(warning!=null&&loaded.Validate()==null&&loaded.Matches("League of Legends.exe"),"Corrupt list fallback: "+bad);Equal(File.ReadAllText(file,Encoding.UTF8),data,"Corrupt config preserved");});
            });
            test("targets_new_config_missing_list_or_unknown_version_is_rejected",delegate {
                foreach(bool missing in new[] {false,true}) InFile(output,delegate(string file) {JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> fields=json.Deserialize<Dictionary<string,object>>(json.Serialize(Targets(Names)));if(missing) fields.Remove("TargetExecutables");else fields["Version"]=99;File.WriteAllText(file,json.Serialize(fields),Encoding.UTF8);string warning;new FileSettingsStore(file).Load(out warning);Check(warning!=null,"New malformed schema accepted");});
            });
            test("targets_failed_save_keeps_previous_list_keys_and_pauses_intention",delegate {
                FakeBackend backend=new FakeBackend();MemorySettingsStore store=new MemorySettingsStore {Value=Targets(Names),Fail=true};using(SettingsService service=new SettingsService(backend,store,new MemoryStartupStore(),@"C:\Synthetic\Guard.exe",store.Value)) {string notice;GuardCore core=new GuardCore(backend,true);core.SetHotkeys(service.RegisterCurrent(out notice),notice);core.Enable();SettingsSession edit=new SettingsSession(core,service);Check(edit.Begin(out notice),"Begin edit");Check(!edit.Save(Targets("New.exe"),out notice),"Failed store must fail save");Check(service.Current.Matches(Names[0])&&!service.Current.Matches("New.exe")&&backend.Registered.ContainsKey(2)&&!core.Enabled&&!edit.EnabledIntent,"Save error changed list or resumed protection");}
            });
            test("targets_successful_removal_of_active_target_restores_only_intention",delegate {
                FakeBackend backend=new FakeBackend();MemorySettingsStore store=new MemorySettingsStore {Value=Targets(Names)};using(SettingsService service=new SettingsService(backend,store,new MemoryStartupStore(),@"C:\Synthetic\Guard.exe",store.Value)) {string notice;GuardCore core=Ready(backend);service.RegisterCurrent(out notice);SettingsSession edit=new SettingsSession(core,service);Active(core,A(),0);edit.Begin(out notice);Check(!core.OwnsClip,"Edit must release active target");Check(edit.Save(Targets(Names[1]),out notice)&&core.Enabled,"Save preserves enabled intent");Scene removed=A();removed.TargetExecutables=service.Current.TargetExecutables;core.Step(removed,101);core.Step(removed,201);Equal(backend.Applies,1,"Removed target must not reapply");Scene remaining=B();remaining.TargetExecutables=service.Current.TargetExecutables;Active(core,remaining,202);Equal(backend.Applies,2,"Remaining target uses new validation");}
            });
            test("targets_quit_and_restart_release_then_require_manual_enable",delegate {
                FakeBackend backend=new FakeBackend();GuardCore core=Ready(backend);Active(core,A(),0);core.Stop();Check(!core.Enabled&&!core.OwnsClip&&backend.Frees==1,"Quit cleanup");GuardCore restarted=new GuardCore(backend,true);restarted.SetHotkeys(true,null);restarted.Step(B(),5000);Equal(backend.Applies,1,"Restart does not apply automatically");restarted.Enable();Active(restarted,B(),5001);Equal(backend.Applies,2,"Manual enable can protect new foreground target");
            });
            test("targets_active_view_follows_actual_foreground_and_clears_on_pause_or_miss",delegate {
                View view=new View {Enabled=true,CanEnable=true,Status="保护中",Settings=Targets(Names)};ViewTarget.Update(view,A(),true);Equal(view.ActiveExecutable,Names[0],"Active A name");Equal(view.ActivePid,(uint)101,"Active A PID");ViewTarget.Update(view,B(),true);Equal(view.ActiveExecutable,Names[1],"Active B name");Equal(StatusPresentation.Name(view),"GameB","Real active caption");Scene other=A();other.Executable="Other.exe";ViewTarget.Update(view,other,true);Equal(view.ActiveExecutable,"","Unlisted foreground clears old name");Equal(StatusPresentation.Name(view),"自动识别","Generic waiting caption");ViewTarget.Update(view,A(),false);Equal(view.ActiveExecutable,"","Paused detection clears active target");
            });
            test("targets_active_view_does_not_show_invalid_coordinates_or_removed_program",delegate {
                View view=new View {Settings=Targets(Names)};Scene invalid=A();invalid.CoordinatesReady=false;ViewTarget.Update(view,invalid,true);Equal(view.ActiveExecutable,"","Invalid scene caption");view.Settings=Targets(Names[1]);ViewTarget.Update(view,A(),true);Equal(view.ActiveExecutable,"","Removed foreground caption");
            });
            test("targets_icons_use_actual_active_pid_and_clear_without_target",delegate {
                ActiveIcons provider=new ActiveIcons();using(ProgramIconCache cache=new ProgramIconCache(provider)) {using(Bitmap a=cache.Read("Shared.exe",20,101)) Equal(a.GetPixel(0,0).ToArgb(),Color.Red.ToArgb(),"First PID icon");using(Bitmap b=cache.Read("Shared.exe",20,202)) Equal(b.GetPixel(0,0).ToArgb(),Color.Blue.ToArgb(),"Second PID icon");Equal(provider.LastPid,(uint)202,"Actual PID lookup");Equal(provider.LegacyCalls,0,"Do not discover another instance");Check(cache.Read("",20,0)==null,"No active target clears icon");}
            });
            test("targets_hidden_settings_add_remove_dedup_and_save_share_real_handlers",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) {UiText.SetLanguage(locale);View view=new View {Settings=Targets(Names),CanEnable=true,Enabled=true,Status="保护中"};using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {form.PreviewOpenSettings();Check(form.PreviewAddTarget("真实 New.exe"),"Add new target");Check(!form.PreviewAddTarget("真实 NEW.EXE"),"Deduplicate picker/manual entries");Equal(form.PreviewTargetList.Items.Count,3,"UI list item count");form.PreviewRemoveTarget(0);Equal(form.PreviewTargetList.Items.Count,2,"UI removal");form.PreviewSaveSettings();Check(!view.Settings.Matches(Names[0])&&view.Settings.Matches("真实 New.exe")&&view.Enabled&&!form.IsHandleCreated,"Saved targets or intention mismatch");}}
            });
            test("targets_hidden_settings_can_remove_every_program_and_save_empty_list",delegate {
                View view=new View {Settings=Targets(Names),CanEnable=true};using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {form.PreviewOpenSettings();form.PreviewRemoveTarget(0);form.PreviewRemoveTarget(0);form.PreviewSaveSettings();Equal(view.Settings.TargetExecutables.Count,0,"Empty UI list saved");Check(!form.IsHandleCreated,"Hidden settings must not create HWND");}
            });
            test("targets_hidden_settings_accepts_typed_filename_without_replacing_list",delegate {
                View view=new View {Settings=Targets(Names),CanEnable=true};using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {form.PreviewOpenSettings();form.PreviewTargetList.Text="Typed.exe";form.PreviewSaveSettings();Equal(view.Settings.TargetExecutables.Count,3,"Typed addition keeps prior targets");Check(view.Settings.Matches("Typed.exe"),"Typed addition persisted");}
            });
            test("targets_main_and_floating_show_active_name_in_both_languages_modes_and_scales",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) foreach(string mode in new[] {"icon","name"}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {UiText.SetLanguage(locale);View view=new View {Settings=Targets(Names),Enabled=true,CanEnable=true,Status="保护中"};view.Settings.TargetDisplay=mode;ViewTarget.Update(view,B(),true);using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) using(Bitmap main=form.RenderPreview(scale)) {Check(form.PreviewProgramToolTip.Contains(Names[1])&&!form.PreviewProgramToolTip.Contains(Names[0]),"Main tooltip must identify current target");Check(main.Width<=Math.Ceiling(320*scale)&&!form.IsHandleCreated,"Main compact bounds or HWND");}using(FloatingBar bar=new FloatingBar(true)) {bar.Present(view);using(Bitmap image=bar.PreviewBitmap(scale)) Check(image.Width<=Math.Ceiling(210*scale)&&!bar.IsHandleCreated,"Floating compact bounds or HWND");}ViewTarget.Update(view,null,false);Equal(StatusPresentation.Name(view),locale=="en"?"Auto detect":"自动识别","No stale name");Check(!StatusPresentation.Short(view).Contains("League"),"Waiting text must be generic");}
            });
            test("targets_settings_add_remove_controls_fit_at_200_percent_without_new_window",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) {UiText.SetLanguage(locale);View view=Previews.State("settings");view.Settings=Targets(Names);using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {form.PreviewSettings();using(Bitmap image=form.RenderPreview(2)) {Check(form.ClientRectangle.Contains(form.PreviewRemoveTargetBounds)&&!form.IsHandleCreated,"Remove control bounds or HWND");Equal(image.Height,804,"Preserved 200% settings geometry");}}}
            });
            test("targets_exe_picker_normalizes_to_filename_without_launching",delegate {
                Equal(ProgramSelection.FromFile(@"C:\Synthetic\GameB.exe"),Names[1],"EXE file basename");bool rejected=false;try {ProgramSelection.FromFile(@"C:\Synthetic\bad.txt");}catch(ArgumentException) {rejected=true;}Check(rejected,"Non EXE must be rejected");
            });
            test("targets_actual_file_save_failure_preserves_original_and_cleans_temporary_files",delegate {
                InFile(output,delegate(string file) {FileSettingsStore store=new FileSettingsStore(file);store.Save(Targets(Names));string original=File.ReadAllText(file,Encoding.UTF8);FileAttributes attributes=File.GetAttributes(file);File.SetAttributes(file,attributes|FileAttributes.ReadOnly);bool rejected=false;
                    try {store.Save(Targets("New.exe"));}catch(IOException) {rejected=true;}catch(UnauthorizedAccessException) {rejected=true;}finally {File.SetAttributes(file,attributes);}
                    Check(rejected,"Read-only destination must reject save");Equal(File.ReadAllText(file,Encoding.UTF8),original,"Original file preserved after actual filesystem failure");Equal(Directory.GetFiles(Path.GetDirectoryName(file)).Length,1,"No abandoned transaction files");});
            });
            test("targets_invalid_candidate_is_rejected_without_mutating_store_or_hotkeys",delegate {
                FakeBackend backend=new FakeBackend();MemorySettingsStore store=new MemorySettingsStore {Value=Targets(Names)};using(SettingsService service=new SettingsService(backend,store,new MemoryStartupStore(),@"C:\Synthetic\Guard.exe",store.Value)) {string notice;service.RegisterCurrent(out notice);Settings invalid=Targets("Bad*.exe");Check(!service.Save(invalid,out notice)&&store.Writes==0&&service.Ready&&backend.Registered.ContainsKey(2)&&service.Current.Matches(Names[0]),"Invalid list changed active settings");invalid=Targets(Names);invalid.Toggle=null;Check(!service.Save(invalid,out notice)&&store.Writes==0&&service.Ready,"Invalid shortcut must remain a validation error");}
            });
            test("targets_emergency_during_multi_program_edit_cancels_restore",delegate {
                FakeBackend backend=new FakeBackend();MemorySettingsStore store=new MemorySettingsStore {Value=Targets(Names)};using(SettingsService service=new SettingsService(backend,store,new MemoryStartupStore(),@"C:\Synthetic\Guard.exe",store.Value)) {string notice;GuardCore core=new GuardCore(backend,true);core.SetHotkeys(service.RegisterCurrent(out notice),notice);core.Enable();SettingsSession edit=new SettingsSession(core,service);edit.Begin(out notice);edit.Pause(true);Check(edit.Save(Targets(Names[1]),out notice)&&!core.Enabled&&!edit.EnabledIntent,"Emergency must override resume intention");Scene selected=B();selected.TargetExecutables=service.Current.TargetExecutables;Active(core,selected,1000);Equal(backend.Applies,0,"Emergency keeps every target paused");}
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c) {return !c.passed;}).Count;
            JavaScriptSerializer reportJson=new JavaScriptSerializer();Dictionary<string,object> report=reportJson.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));report["multiple_target_tests"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;
            report["multiple_target_scope"]="fake cursor/hotkey/startup/icon backends, isolated configuration files and real hidden UI handlers; no game, normal app launch, physical input, real hotkeys or native clipping";
            File.WriteAllText(output,reportJson.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
