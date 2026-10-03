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
    static class LocalizationTests
    {
        static void Check(bool value,string detail) {if(!value) throw new Exception(detail);}
        static void EnglishControls(Control parent,string rawTarget)
        {
            if(!(parent is TextBox) && !(parent is ProgramIconButton) && !(parent is ComboBox && parent.Text=="中文")) Check(!UiText.HasChinese(parent.Text),"Chinese owned caption: "+parent.Text);
            if(parent is TextBox) Check(parent.Text==rawTarget,"Target changed");
            if(!(parent is ProgramIconButton)) Check(!UiText.HasChinese(parent.AccessibleName),"Chinese accessible label");
            foreach(Control child in parent.Controls) EnglishControls(child,rawTarget);
        }
        static void EnglishMenu(ToolStrip menu)
        {foreach(ToolStripItem item in menu.Items) {Check(!UiText.HasChinese(item.Text+item.ToolTipText),"Chinese menu caption");}}
        public static int Append(string output)
        {
            string previous=UiText.Language;UiText.SetLanguage("zh-CN");
            List<SelfTests.Check> checks=new List<SelfTests.Check>();
            Action<string,Action> test=delegate(string name,Action action) {SelfTests.Check c=new SelfTests.Check {name=name};try {action();c.passed=true;}catch(Exception e) {c.error=e.ToString();}finally {UiText.SetLanguage("zh-CN");Theme.Apply(false,false);}checks.Add(c);};
            test("language_file_roundtrip_restores_english_and_existing_preferences",delegate {
                string directory=Path.Combine(Path.GetDirectoryName(output),"language-test-"+Guid.NewGuid().ToString("N")),file=Path.Combine(directory,"floating.json");
                try {FloatingFileStore store=new FloatingFileStore(file);FloatingPreferences p=new FloatingPreferences {Language="en",Enabled=true,Appearance="dark",HasPosition=true,Monitor="synthetic",X=-24,Y=52};store.Save(p);string warning;FloatingPreferences restored=store.Load(out warning);Check(warning==null && restored.Language=="en" && restored.Enabled && restored.Appearance=="dark" && restored.X==-24 && restored.Y==52 && restored.Monitor=="synthetic","Roundtrip lost preferences");UiText.SetLanguage(restored.Language);using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("paused")),true,false)) Check(form.PreviewLanguageChoice.SelectedIndex==1 && form.Text=="CursorGuard" && !form.IsHandleCreated,"English startup did not render");restored.Language="zh-CN";store.Save(restored);Check(store.Load(out warning).Language=="zh-CN","Chinese not restored");}
                finally {if(File.Exists(file)) File.Delete(file);if(Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length==0) Directory.Delete(directory);}
            });
            test("language_legacy_and_unknown_values_preserve_position_and_appearance",delegate {
                string directory=Path.Combine(Path.GetDirectoryName(output),"language-legacy-"+Guid.NewGuid().ToString("N")),file=Path.Combine(directory,"floating.json");
                try {Directory.CreateDirectory(directory);foreach(string value in new[] {"",",\"Language\":\"fr\"",",\"Language\":null"}) {File.WriteAllText(file,"{\"Version\":1,\"Enabled\":true,\"HasPosition\":true,\"Monitor\":\"synthetic\",\"X\":42,\"Y\":18,\"Appearance\":\"dark\""+value+"}",Encoding.UTF8);string warning;FloatingPreferences p=new FloatingFileStore(file).Load(out warning);Check(warning==null && p.Language=="zh-CN" && p.Enabled && p.HasPosition && p.X==42 && p.Y==18 && p.Appearance=="dark","Legacy migration discarded valid settings");}}
                finally {if(File.Exists(file)) File.Delete(file);if(Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length==0) Directory.Delete(directory);}
            });
            test("language_switch_both_directions_preserves_custom_name_and_protection",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore();View view=Previews.State("protected");view.Settings.TargetExecutable="设置我的游戏.exe";string keys=view.Settings.Toggle.Display();
                using(FloatingController saved=new FloatingController(store,delegate {throw new Exception("Window must not be created");}))
                using(ControlPanel form=new ControlPanel(new PreviewSession(view),true,false)) {form.PreviewSettings();for(int i=0;i<6;i++) {int index=i%2==0?1:0;form.PreviewCommitLanguage(saved,index);Check(UiText.Language==(index==1?"en":"zh-CN") && saved.Preferences.Language==UiText.Language && form.PreviewLanguageChoice.SelectedIndex==index,"Language not committed");Check(view.Enabled && view.Status=="保护中" && view.Settings.TargetExecutable=="设置我的游戏.exe" && view.Settings.Toggle.Display()==keys,"Language changed protection or target");if(index==1) {EnglishControls(form,view.Settings.TargetExecutable);EnglishMenu(form.PreviewPanelMenu);}Check(!form.IsHandleCreated,"Language switch created native window");}}
            });
            test("language_failed_save_rolls_back_dropdown_captions_and_preference",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore {Fail=true};using(FloatingController saved=new FloatingController(store,delegate {return new FakeFloatingWindow();})) using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("paused")),true,false)) {form.PreviewSettings();form.PreviewCommitLanguage(saved,1);Check(UiText.Language=="zh-CN" && saved.Preferences.Language=="zh-CN" && form.PreviewLanguageChoice.SelectedIndex==0 && form.PreviewPanelMenu.Items[0].Text=="设置","Failed save changed language");Check(saved.Warning=="语言设置保存失败，保持原语言。","Missing save failure");}
            });
            test("language_failed_save_from_english_retains_english",delegate {
                UiText.SetLanguage("en");MemoryFloatingStore store=new MemoryFloatingStore {Fail=true,Value=new FloatingPreferences {Language="en"}};using(FloatingController saved=new FloatingController(store,delegate {return new FakeFloatingWindow();})) using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("paused")),true,false)) {form.PreviewSettings();form.PreviewCommitLanguage(saved,0);Check(UiText.Language=="en" && form.PreviewLanguageChoice.SelectedIndex==1 && !UiText.HasChinese(UiText.Display(saved.Warning)),"English rollback failed");EnglishMenu(form.PreviewPanelMenu);}
            });
            test("language_uncommitted_selection_does_not_save_or_change_global_language",delegate {
                using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.PreviewSettings();form.PreviewLanguageChoice.SelectedIndex=1;Check(UiText.Language=="zh-CN","Navigation committed a language");form.PreviewRefresh();Check(form.PreviewLanguageChoice.SelectedIndex==0 && UiText.Language=="zh-CN" && !form.IsHandleCreated,"Stored choice reconciliation failed");}
            });
            test("language_invalid_choice_cannot_replace_saved_preference",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore {Value=new FloatingPreferences {Language="en"}};using(FloatingController saved=new FloatingController(store,delegate {return new FakeFloatingWindow();})) {Check(!saved.SetLanguage("fr") && saved.Preferences.Language=="en" && store.Value.Language=="en","Unsupported language saved");}
            });
            test("language_other_preference_actions_keep_saved_language",delegate {
                MemoryFloatingStore store=new MemoryFloatingStore {Value=new FloatingPreferences {Language="en",HasPosition=true}};using(FloatingController saved=new FloatingController(store,delegate {return new FakeFloatingWindow();})) {saved.SetAppearance("dark");saved.SetEnabled(true);saved.ResetPosition();saved.Moved(new Rectangle(10,10,200,30),new[] {new MonitorArea {Name="synthetic",Work=new Rectangle(0,0,1000,700)}});Check(saved.Preferences.Language=="en" && store.Value.Language=="en" && saved.Preferences.Appearance=="dark","Other preference action lost language");}
            });
            test("language_main_and_floating_status_classification_stays_canonical",delegate {
                UiText.SetLanguage("en");foreach(string state in new[] {"paused","waiting","protected","error"}) {View view=Previews.State(state);Check(StatusPresentation.Kind(view)==state && !UiText.HasChinese(StatusPresentation.Short(view)+StatusPresentation.Heading(view)),"Status translation changed classification");using(FloatingBar bar=new FloatingBar(true)) {bar.Present(view);Check(bar.Text=="Cursor protection status" && !bar.IsHandleCreated,"Floating caption not localized");}}
                View waiting=Previews.State("waiting");waiting.Status="等待鼠标自行回到程序区域";Check(StatusPresentation.Short(waiting)=="Wait: mouse","Mouse wait category lost");waiting.Status="等待程序窗口稳定";Check(StatusPresentation.Short(waiting)=="Stabilizing","Stable wait category lost");
            });
            test("language_repeated_refresh_keeps_shortcuts_and_menu_captions",delegate {
                UiText.SetLanguage("en");using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.PreviewSettings();for(int i=0;i<10;i++) form.PreviewRefresh();EnglishControls(form,"League of Legends.exe");EnglishMenu(form.PreviewPanelMenu);Check(!form.IsHandleCreated,"Refresh created native window");}
            });
            test("language_bilingual_layout_at_four_scales_and_two_themes",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) foreach(bool dark in new[] {false,true}) foreach(float scale in new[] {1f,1.25f,1.5f,2f}) {UiText.SetLanguage(locale);Theme.Apply(dark,false);using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {form.PreviewSettings();using(Bitmap bitmap=form.RenderPreview(scale)) {Check(Math.Abs(bitmap.Width-380*scale)<=1 && Math.Abs(bitmap.Height-402*scale)<=1,"Settings size mismatch: locale="+locale+" dark="+dark+" scale="+scale+" actual="+bitmap.Size+" expected="+new Size((int)(380*scale),(int)(402*scale))+" form="+form.Bounds+" maxTrack="+SystemInformation.MaxWindowTrackSize+" maximum="+form.MaximumSize);ComboBox choice=form.PreviewLanguageChoice;Check(choice.Parent.ClientRectangle.Contains(choice.Bounds) && choice.Width>=(int)(220*scale),"Language choice clipped");Check(!form.IsHandleCreated && !choice.IsHandleCreated,"Layout created native window");}}
                using(FloatingBar bar=new FloatingBar(true)) {bar.Present(Previews.State("protected"));using(Bitmap bitmap=bar.PreviewBitmap(scale)) Check(bitmap.Width<=(int)(210*scale) && !bar.IsHandleCreated,"Floating layout overflow");}}
            });
            test("language_small_work_area_can_reveal_last_row_without_focus",delegate {
                UiText.SetLanguage("en");foreach(float scale in new[] {1f,1.25f,1.5f,2f}) using(ControlPanel form=new ControlPanel(new PreviewSession(Previews.State("settings")),true,false)) {using(Bitmap ignored=form.RenderPreview(scale)) {}form.PreviewOpenSettingsAt(new Rectangle(-500,100,200,44),new Rectangle(-800,0,800,(int)(180*scale)),scale);form.PreviewScrollToLanguage();Check(form.PreviewLanguageChoice.Parent.ClientRectangle.Contains(form.PreviewLanguageChoice.Bounds) && !form.IsHandleCreated,"Language unreachable on small screen");}
            });
            test("language_picker_preserves_real_window_title_and_executable",delegate {
                UiText.SetLanguage("en");ProgramCandidate candidate=new ProgramCandidate {Title="设置 中文真实窗口",Executable="游戏.exe",Pid=100};string original=candidate.ToString();using(ProgramPicker picker=new ProgramPicker(true,new[] {candidate})) using(Bitmap image=picker.PreviewBitmap()) {Check(candidate.Title=="设置 中文真实窗口" && candidate.Executable=="游戏.exe" && candidate.ToString()==original,"Translated user window content");Check(!UiText.HasChinese(picker.Text) && !picker.IsHandleCreated,"Picker title not translated");}
            });
            test("language_hotkey_conflict_keeps_numeric_error_and_shortcut",delegate {
                UiText.SetLanguage("en");string conflict=UiText.Display("Ctrl + Alt + F8 注册失败（Win32 1409）。快捷键可能已被其他程序占用。");Check(conflict.Contains("1409") && conflict.Contains("Ctrl + Alt + F8") && conflict.Contains("Another program") && !UiText.HasChinese(conflict),"Conflict detail lost");
            });
            test("language_validation_errors_are_complete_english_messages",delegate {
                UiText.SetLanguage("en");Settings settings=Settings.Defaults();settings.TargetExecutable="C:\\Example.exe";Check(UiText.Display(settings.Validate()).Contains("filename"),"Target validation missing");settings=Settings.Defaults();settings.Emergency=settings.Toggle.Copy();Check(UiText.Display(settings.Validate()).Contains("different combinations"),"Duplicate validation missing");settings=Settings.Defaults();settings.Toggle=new Shortcut(0,65);Check(UiText.Display(settings.Validate()).Contains("Ctrl or Alt"),"Modifier validation missing");
            });
            test("language_unknown_os_error_shows_type_and_hresult_in_english",delegate {
                UiText.SetLanguage("en");string text=UiText.ExceptionMessage(new IOException("系统拒绝访问"));Check(text.Contains("IOException") && text.Contains("0x") && !UiText.HasChinese(text),"OS error fallback lost code");
            });
            test("language_owned_dialog_button_and_title_follow_language",delegate {
                foreach(string locale in new[] {"zh-CN","en"}) {UiText.SetLanguage(locale);using(Form dialog=UiDialogs.Create(UiText.Display("三个快捷键不能使用相同组合。"),UiText.T("快捷键未保存"))) {Check(dialog.Controls[1].Text==(locale=="en"?"OK":"确定") && !dialog.IsHandleCreated,"Dialog button language mismatch");if(locale=="en") Check(!UiText.HasChinese(dialog.Text+dialog.Controls[0].Text),"Dialog contains Chinese");}}
            });
            test("language_catalog_has_no_missing_english_translations",delegate {
                UiText.SetLanguage("en");foreach(KeyValuePair<string,string> item in UiText.Catalog) Check(!UiText.HasChinese(item.Value) && UiText.T(item.Key)==item.Value && item.Value!="Application message","Missing English catalog translation");
            });
            int failed=checks.FindAll(delegate(SelfTests.Check c) {return !c.passed;}).Count;JavaScriptSerializer json=new JavaScriptSerializer();Dictionary<string,object> report=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(output,Encoding.UTF8));report["localization_tests"]=checks;report["passed"]=Convert.ToInt32(report["passed"])+checks.Count-failed;report["failed"]=Convert.ToInt32(report["failed"])+failed;report["language_test_scope"]="zh-CN/en preference migration and roundtrip, committed selection and failed-save rollback, real offscreen UI at four scales; user titles preserved; no native handles or normal app launch";File.WriteAllText(output,json.Serialize(report),new UTF8Encoding(false));UiText.SetLanguage(previous);return failed==0?0:1;
        }
    }
}
