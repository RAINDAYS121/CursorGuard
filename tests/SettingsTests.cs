// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace LoLMouseGuard
{
    sealed class MemorySettingsStore : ISettingsStore
    {
        public Settings Value = Settings.Defaults(); public bool Fail; public int Writes;
        public Settings Load(out string warning) { warning = null; return Value.Copy(); }
        public void Save(Settings settings) { Writes++; if (Fail) throw new IOException("模拟保存失败"); Value = settings.Copy(); }
    }
    sealed class MemoryStartupStore : IStartupStore
    {
        public Dictionary<string, string> Values = new Dictionary<string, string>();
        public int Writes, FailAt;
        public string Read() { string value; return Values.TryGetValue("LoLMouseGuard", out value) ? value : null; }
        public void Write(string value)
        { Writes++; if (value == null) Values.Remove("LoLMouseGuard"); else Values["LoLMouseGuard"] = value; if (Writes == FailAt) throw new IOException("模拟自启写入失败"); }
    }
    static class SettingsTests
    {
        static void Check(bool condition) { if (!condition) throw new Exception("assertion failed"); }
        static Settings Alternative() { return new Settings { Toggle = new Shortcut(6, 0x41), Emergency = new Shortcut(3, 0x42), Exit = new Shortcut(6, 0x58), StartWithWindows = true }; }
        static SettingsService Service(FakeBackend backend, MemorySettingsStore store, MemoryStartupStore startup)
        { SettingsService service = new SettingsService(backend, store, startup, @"C:\Example User\LoLMouseGuard.exe", store.Value); string message; Check(service.RegisterCurrent(out message)); service.BeginEdit(); return service; }
        public static int Append(string output)
        {
            List<SelfTests.Check> checks = new List<SelfTests.Check>();
            Action<string, Action> test = delegate(string name, Action action) { SelfTests.Check result = new SelfTests.Check { name = name }; try { action(); result.passed = true; } catch (Exception e) { result.error = e.Message; } checks.Add(result); };
            test("settings_persistence_roundtrip_and_atomic_replace", delegate
            {
                string dir = Path.Combine(Path.GetDirectoryName(output), "config-test-" + Guid.NewGuid().ToString("N")); string file = Path.Combine(dir, "settings.json");
                try { FileSettingsStore store = new FileSettingsStore(file); store.Save(Alternative()); string warning; Settings loaded = store.Load(out warning); Check(warning == null && loaded.Toggle.Key == 0x41 && loaded.StartWithWindows); store.Save(Settings.Defaults()); loaded = store.Load(out warning); Check(loaded.Toggle.Key == 0x77 && !loaded.StartWithWindows && Directory.GetFiles(dir).Length == 1); }
                finally { if (File.Exists(file)) File.Delete(file); if (Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0) Directory.Delete(dir); }
            });
            test("corrupt_and_missing_config_safely_defaults_with_warning", delegate
            {
                string file = Path.Combine(Path.GetDirectoryName(output), "config-corrupt-" + Guid.NewGuid().ToString("N") + ".json");
                try { FileSettingsStore store = new FileSettingsStore(file); string warning; Settings s = store.Load(out warning); Check(warning == null && !s.StartWithWindows); File.WriteAllText(file, "{broken", Encoding.UTF8); s = store.Load(out warning); Check(warning != null && s.Emergency.Key == 0x78 && !s.StartWithWindows); File.WriteAllText(file, "{}", Encoding.UTF8); s = store.Load(out warning); Check(warning != null && s.Validate() == null); }
                finally { if (File.Exists(file)) File.Delete(file); }
            });
            test("duplicate_and_system_switch_shortcuts_rejected", delegate { Settings s = Settings.Defaults(); s.Exit = s.Emergency.Copy(); Check(s.Validate() != null); s = Settings.Defaults(); s.Toggle.Key = 9; Check(s.Validate() != null); s.Toggle.Key = 0x7B; Check(s.Validate() != null); s.Toggle = new Shortcut(0, 0x41); Check(s.Validate() != null); });
            test("custom_keys_saved_and_registered_with_modifiers", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore(); using (SettingsService service = Service(b, store, startup)) { string message; Check(service.Save(Alternative(), out message) && service.Ready); Check(b.Registered[2].Key == 0x42 && b.Registered[1].Modifiers == 6 && store.Value.Toggle.Key == 0x41); } Check(b.Registered.Count == 0); });
            test("new_key_conflict_restores_previous_emergency_and_exit", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore(); using (SettingsService service = Service(b, store, startup)) { b.FailRegistrationAt = b.RegisterCalls + 2; string message; Check(!service.Save(Alternative(), out message) && service.Ready); Check(b.Registered[2].Key == 0x78 && b.Registered[3].Key == 0x79 && service.Current.Toggle.Key == 0x77 && store.Writes == 0 && startup.Writes == 0); } });
            test("file_save_failure_rolls_back_keys_and_startup", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore { Fail = true }; MemoryStartupStore startup = new MemoryStartupStore(); startup.Values["LoLMouseGuard"] = "old-command"; using (SettingsService service = Service(b, store, startup)) { string message; Check(!service.Save(Alternative(), out message) && service.Ready); Check(startup.Read() == "old-command" && b.Registered[2].Key == 0x78 && !store.Value.StartWithWindows); } });
            test("partial_startup_write_failure_restores_previous_value", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore { FailAt = 1 }; startup.Values["LoLMouseGuard"] = "old-command"; using (SettingsService service = Service(b, store, startup)) { string message; Check(!service.Save(Alternative(), out message) && service.Ready); Check(startup.Read() == "old-command" && store.Writes == 0); } });
            test("key_rollback_failure_blocks_guard_enable", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore(); using (SettingsService service = Service(b, store, startup)) { b.FailAllRegistrations = true; string message; Check(!service.Save(Alternative(), out message) && !service.Ready); GuardCore guard = new GuardCore(b, true); guard.SetHotkeys(service.Ready, message); Check(!guard.Enable() && b.Registered.Count == 0); } });
            test("cancel_edit_restores_keys_without_persistence", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore(); using (SettingsService service = Service(b, store, startup)) { string message; Check(service.RegisterCurrent(out message) && b.Registered.Count == 3); Check(store.Writes == 0 && startup.Writes == 0); } });
            test("startup_path_is_quoted_and_quiet_argument_is_fixed", delegate { Check(UserStartupStore.CommandFor(@"C:\Example User\LoLMouseGuard.exe") == "\"C:\\Example User\\LoLMouseGuard.exe\" --tray"); bool rejected = false; try { UserStartupStore.CommandFor("C:\\x\" --evil.exe"); } catch (ArgumentException) { rejected = true; } Check(rejected); });
            test("disable_startup_removes_only_this_apps_value", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); MemoryStartupStore startup = new MemoryStartupStore(); store.Value.StartWithWindows = true; startup.Values["LoLMouseGuard"] = "old"; startup.Values["OtherApp"] = "keep"; using (SettingsService service = Service(b, store, startup)) { string message; Check(service.Save(Settings.Defaults(), out message)); Check(startup.Read() == null && startup.Values["OtherApp"] == "keep" && !store.Value.StartWithWindows); } });
            test("startup_setting_does_not_write_registry_or_enable_on_launch", delegate { FakeBackend b = new FakeBackend(); MemorySettingsStore store = new MemorySettingsStore(); store.Value.StartWithWindows = true; MemoryStartupStore startup = new MemoryStartupStore(); using (SettingsService service = new SettingsService(b, store, startup, @"C:\Guard.exe", store.Value)) { string error; GuardCore core = new GuardCore(b, true); core.SetHotkeys(service.RegisterCurrent(out error), error); Check(!core.Enabled && b.Applies == 0 && startup.Writes == 0); } });
            test("restore_default_keys_and_startup_off", delegate { Settings s = Settings.Defaults(); Check(s.Toggle.Display() == "Ctrl + Alt + F8" && s.Emergency.Display() == "Ctrl + Alt + F9" && s.Exit.Display() == "Ctrl + Alt + F10" && !s.StartWithWindows); });
            test("status_paused_waiting_protected_error_are_distinct", delegate { View v = new View { CanEnable = true, Status = "已暂停" }; Check(StatusPresentation.Kind(v) == "paused"); v.Enabled = true; v.Status = "已启用，当前不约束"; Check(StatusPresentation.Kind(v) == "waiting"); v.Status = "保护中"; Check(StatusPresentation.Kind(v) == "protected"); v.CanEnable = false; v.Enabled = false; v.Status = "快捷键不可用，禁止启用"; Check(StatusPresentation.Kind(v) == "error"); });
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var report = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(output, Encoding.UTF8));
            int failed = checks.FindAll(delegate(SelfTests.Check check) { return !check.passed; }).Count;
            report["settings_tests"] = checks; report["passed"] = Convert.ToInt32(report["passed"]) + checks.Count - failed; report["failed"] = Convert.ToInt32(report["failed"]) + failed;
            report["registry_tests"] = "fake backend only; no real HKCU Run writes"; report["ui_previews"] = "hidden/offscreen; no hotkey registrations or ClipCursor calls";
            File.WriteAllText(output, serializer.Serialize(report), new UTF8Encoding(false)); return failed == 0 ? 0 : 1;
        }
    }
}


