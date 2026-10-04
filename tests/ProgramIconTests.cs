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
    sealed class FakeProgramIconSource : IProgramIconSource
    {
        public bool Missing,Fail,NoIcon;public long Stamp=1;public int Extractions;
        public readonly List<Bitmap> Images=new List<Bitmap>();public Action DuringExtraction;
        public IconDescriptor Resolve(string name) {if(Fail) throw new UnauthorizedAccessException();return Missing?null:new IconDescriptor {Path=@"C:\Fixture\"+name,Stamp=Stamp};}
        public Bitmap Extract(string path,int pixels) {Extractions++;if(NoIcon) return null;Bitmap image=new Bitmap(pixels,pixels);using(Graphics g=Graphics.FromImage(image)) g.Clear(path.EndsWith("First.exe",StringComparison.OrdinalIgnoreCase)?Color.Red:Color.Blue);Images.Add(image);if(DuringExtraction!=null) DuringExtraction();return image;}
    }
    static class ProgramIconTests
    {
        static void Check(bool condition) {if(!condition) throw new Exception("assertion failed");}
        static bool Disposed(Bitmap image) {try {int width=image.Width;return width==0;}catch(ArgumentException) {return true;}}
        public static int Append(string output)
        {
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}checks.Add(c);};
            test("icon_cache_target_change_stamp_and_dpi_refresh",delegate {
                FakeProgramIconSource source=new FakeProgramIconSource();using(ProgramIconCache cache=new ProgramIconCache(source)) {
                    using(Bitmap first=cache.Read("First.exe",20)) using(Bitmap again=cache.Read("First.exe",20)) {Check(first.GetPixel(0,0).ToArgb()==Color.Red.ToArgb() && source.Extractions==1);first.Dispose();Check(again.GetPixel(0,0).ToArgb()==Color.Red.ToArgb());}
                    using(Bitmap second=cache.Read("Second.exe",20)) Check(second.GetPixel(0,0).ToArgb()==Color.Blue.ToArgb() && source.Extractions==2);
                    using(Bitmap larger=cache.Read("Second.exe",40)) Check(larger.Width==40 && source.Extractions==3);
                    source.Stamp++;using(Bitmap updated=cache.Read("Second.exe",40)) Check(updated!=null && source.Extractions==4);
                }
            });
            test("icon_absent_unreadable_missing_resource_use_neutral_fallback",delegate {
                FakeProgramIconSource source=new FakeProgramIconSource {Missing=true};using(ProgramIconCache cache=new ProgramIconCache(source)) {Check(cache.Read("First.exe",20)==null && source.Extractions==0);source.Missing=false;source.Fail=true;Check(cache.Read("First.exe",20)==null);source.Fail=false;source.NoIcon=true;Check(cache.Read("First.exe",20)==null && cache.Read("First.exe",20)==null && source.Extractions==1);source.NoIcon=false;source.Stamp++;using(Bitmap found=cache.Read("First.exe",20)) Check(found!=null && source.Extractions==2);}
                using(ProgramIconButton button=new ProgramIconButton()) Check(!button.HasProgramImage && !button.IsHandleCreated);
            });
            test("main_icon_layout_has_same_width_for_short_and_long_target_names",delegate {
                int width=0;foreach(string target in new[] {"Paint.exe","League of Legends.exe",new string('A',110)+".exe"}) {
                    View view=Previews.State("waiting");view.Settings.TargetExecutable=target;view.ActiveExecutable=target;using(ControlPanel panel=new ControlPanel(new PreviewSession(view),true,false)) {if(width==0) width=panel.Width;Check(panel.Width==width && panel.PreviewProgramToolTip.Contains(target) && panel.PreviewProgramToolTip.Contains("点击选择程序") && !panel.IsHandleCreated);}
                }
            });
            test("main_icon_click_opens_selection_edit_path_without_native_input",delegate {
                View view=Previews.State("paused");using(ControlPanel panel=new ControlPanel(new PreviewSession(view),true,false)) {panel.PreviewClickProgram();Check(view.Editing && panel.Height==402 && !panel.IsHandleCreated);panel.PreviewReturn();Check(panel.Height==44 && !panel.IsHandleCreated);}
            });
            test("program_icon_click_target_and_tooltip_at_125_150_200_dpi",delegate {
                foreach(float scale in new[] {1.25f,1.5f,2f}) using(ControlPanel panel=new ControlPanel(new PreviewSession(Previews.State("protected")),true,false)) using(Bitmap image=panel.RenderPreview(scale)) {Check(panel.ClientRectangle.Contains(panel.PreviewProgramClickBounds) && panel.PreviewProgramClickBounds.Width>=(int)(30*scale) && panel.PreviewProgramClickBounds.Height>=(int)(30*scale) && panel.PreviewProgramToolTip.Contains("League of Legends.exe") && !panel.IsHandleCreated);}
            });
            test("real_own_executable_icon_resource_extracted_at_requested_dpi_without_launch",delegate {
                string path=typeof(Program).Assembly.Location;NativeProgramIconSource source=new NativeProgramIconSource();foreach(int size in new[] {20,25,30,40}) using(Bitmap image=source.Extract(path,size)) Check(image!=null && image.Width==size && image.Height==size);
                Check(source.Extract(Path.Combine(Path.GetDirectoryName(path),"missing-icon-target.exe"),20)==null);Check(source.Extract(@"\\unreadable-host\app.exe",20)==null);
            });
            test("target_refresh_drops_previous_icon_and_updates_long_name_tooltip",delegate {
                View view=Previews.State("paused");view.Settings.TargetExecutable=Path.GetFileName(typeof(Program).Assembly.Location);view.ActiveExecutable=view.Settings.TargetExecutable;using(ControlPanel panel=new ControlPanel(new PreviewSession(view),true,false)) {panel.PreviewUseIconFile(typeof(Program).Assembly.Location);Check(panel.PreviewHasProgramIcon);view.Settings.TargetExecutable=new string('A',110)+".exe";view.ActiveExecutable=view.Settings.TargetExecutable;panel.PreviewRefresh();Check(!panel.PreviewHasProgramIcon && panel.PreviewProgramToolTip.Contains(view.Settings.TargetExecutable) && !panel.IsHandleCreated);}
            });
            test("waiting_program_text_applies_to_main_and_floating_status",delegate {
                View view=Previews.State("waiting");view.Settings.TargetExecutable="Editor.exe";Check(StatusPresentation.Short(view)=="等待程序" && !StatusPresentation.Full(view).Contains("游戏"));
                FakeBackend backend=new FakeBackend();GuardCore core=new GuardCore(backend,true);core.SetHotkeys(true,null);core.Enable();Check(core.Status.Contains("程序") && !core.Status.Contains("游戏"));
            });
            test("icon_current_only_lifecycle_reset_and_button_release_old_bitmaps",delegate {
                FakeProgramIconSource source=new FakeProgramIconSource();ProgramIconCache cache=new ProgramIconCache(source);
                using(Bitmap first=cache.Read("First.exe",20)) {using(Bitmap second=cache.Read("Second.exe",20)) Check(Disposed(source.Images[0]) && !Disposed(source.Images[1]) && first.GetPixel(0,0).ToArgb()==Color.Red.ToArgb());}
                using(Bitmap returning=cache.Read("First.exe",20)) Check(source.Extractions==3 && Disposed(source.Images[1]));
                cache.Reset();Check(Disposed(source.Images[2]));using(Bitmap refreshed=cache.Read("First.exe",20)) Check(source.Extractions==4);cache.Dispose();Check(Disposed(source.Images[3]) && cache.Read("First.exe",20)==null);
                Bitmap a=new Bitmap(20,20),b=new Bitmap(20,20);using(ProgramIconButton button=new ProgramIconButton()) {button.SetImage(a);button.SetImage(b);Check(Disposed(a) && !Disposed(b));button.SetImage(null);Check(Disposed(b));Bitmap final=new Bitmap(20,20);button.SetImage(final);button.Dispose();Check(Disposed(final));}
            });
            test("icon_reset_during_resource_extraction_discards_stale_result",delegate {
                FakeProgramIconSource source=new FakeProgramIconSource();using(ProgramIconCache cache=new ProgramIconCache(source)) {
                    source.DuringExtraction=delegate {cache.Reset();};Check(cache.Read("First.exe",20)==null && Disposed(source.Images[0]));source.DuringExtraction=null;using(Bitmap current=cache.Read("Second.exe",20)) Check(current!=null && source.Extractions==2);
                    source.Missing=true;Check(cache.Read("Second.exe",20)==null && Disposed(source.Images[1]));
                }
            });
            test("icon_resource_read_creates_no_application_icon_files",delegate {
                string directory=Path.Combine(Path.GetDirectoryName(output),"icon-resource-test-"+Guid.NewGuid().ToString("N")),file=Path.Combine(directory,"resource.exe");
                try {Directory.CreateDirectory(directory);File.Copy(typeof(Program).Assembly.Location,file);NativeProgramIconSource source=new NativeProgramIconSource();foreach(int size in new[] {20,25,30,40}) using(Bitmap image=source.Extract(file,size)) Check(image!=null && image.Width==size);Check(Directory.GetFiles(directory,"*",SearchOption.AllDirectories).Length==1 && Directory.GetDirectories(directory,"*",SearchOption.AllDirectories).Length==0);}
                finally {if(File.Exists(file)) File.Delete(file);if(Directory.Exists(directory) && Directory.GetFiles(directory).Length==0) Directory.Delete(directory);}
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c){return !c.passed;}).Count;
            JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> report=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));report["program_icon_tests"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;
            report["program_icon_test_scope"]="single-current in-memory retention/disposal and stale-result cancellation, hidden UI, own EXE resource reads plus no new files/subdirectories in the resource fixture; source has no application icon-file writer. Windows Shell cache is outside this guarantee. No target launch, actual game process reads, input or global hotkeys";
            File.WriteAllText(output,json.Serialize(report),new UTF8Encoding(false));return failed==0?0:1;
        }
    }
}
