// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LoLMouseGuard
{
    public sealed class Shortcut
    {
        public uint Modifiers;
        public uint Key;
        public Shortcut() { }
        public Shortcut(uint modifiers, uint key) { Modifiers = modifiers; Key = key; }
        public Shortcut Copy() { return new Shortcut(Modifiers, Key); }
        public string Display()
        {
            string result = "";
            if ((Modifiers & 2) != 0) result += "Ctrl + ";
            if ((Modifiers & 1) != 0) result += "Alt + ";
            if ((Modifiers & 4) != 0) result += "Shift + ";
            string key = ((Keys)Key).ToString();
            if (Key >= 0x30 && Key <= 0x39) key = ((char)Key).ToString();
            return result + key;
        }
    }
    public sealed class Settings
    {
        public int Version = 1;
        public Shortcut Toggle = new Shortcut(3, 0x77);
        public Shortcut Emergency = new Shortcut(3, 0x78);
        public Shortcut Exit = new Shortcut(3, 0x79);
        public bool StartWithWindows;
        public string TargetExecutable = "League of Legends.exe";
        public static Settings Defaults() { return new Settings(); }
        public Settings Copy() { return new Settings { Version = Version, Toggle = Toggle.Copy(), Emergency = Emergency.Copy(), Exit = Exit.Copy(), StartWithWindows = StartWithWindows, TargetExecutable = TargetExecutable }; }
        [ScriptIgnore]
        public Shortcut[] Keys { get { return new[] { Toggle, Emergency, Exit }; } }
        public string Validate()
        {
            if (Version != 1) return "不支持的配置版本。";
            if (String.IsNullOrWhiteSpace(TargetExecutable) || TargetExecutable.Length > 128 || !TargetExecutable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || TargetExecutable != TargetExecutable.Trim() || TargetExecutable.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || TargetExecutable.IndexOfAny(new[] {'/','\\','\r','\n','\0'}) >= 0)
                return "目标程序请填写完整可执行文件名，例如 League of Legends.exe；不要填写路径或通配符。";
            HashSet<string> combinations = new HashSet<string>();
            foreach (Shortcut key in Keys)
            {
                if (key == null || key.Modifiers > 7 || (key.Modifiers & 3) == 0 || key.Key < 0x08 || key.Key > 0xFE)
                    return "快捷键至少包含 Ctrl 或 Alt，加一个普通按键。";
                uint k = key.Key;
                if (k == 9 || k == 0x7B || k == 0x10 || k == 0x11 || k == 0x12 || (k >= 0xA0 && k <= 0xA5) || k == 0x5B || k == 0x5C || k == 0x2E)
                    return "不能使用 Tab、F12、Delete、Windows 键或单独的修饰键；保留正常系统切换。";
                if (!combinations.Add(key.Modifiers + ":" + key.Key)) return "三个快捷键不能使用相同组合。";
            }
            return null;
        }
    }
    public interface IHotkeyBackend { bool Register(int id, uint modifiers, uint key, out int error); void Unregister(int id); }
    public sealed class HotkeySet : IDisposable
    {
        readonly IHotkeyBackend backend;
        readonly Shortcut[] keys;
        readonly List<int> registered = new List<int>();
        public string Failure { get; private set; }
        public HotkeySet(IHotkeyBackend b) : this(b, Settings.Defaults().Keys) { }
        public HotkeySet(IHotkeyBackend b, Shortcut[] shortcuts) { backend = b; keys = shortcuts; }
        public bool RegisterAll()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                int error;
                if (!backend.Register(i + 1, keys[i].Modifiers, keys[i].Key, out error))
                {
                    Failure = keys[i].Display() + " 注册失败（Win32 " + error + "）。快捷键可能已被其他程序占用。";
                    Dispose(); return false;
                }
                registered.Add(i + 1);
            }
            return true;
        }
        public void Dispose() { foreach (int id in registered) backend.Unregister(id); registered.Clear(); }
    }
    public interface ISettingsStore { Settings Load(out string warning); void Save(Settings settings); }
    public interface IStartupStore { string Read(); void Write(string command); }
    public sealed class FileSettingsStore : ISettingsStore
    {
        readonly string path;
        public FileSettingsStore(string p) { path = p; }
        public Settings Load(out string warning)
        {
            warning = null;
            if (!File.Exists(path)) return Settings.Defaults();
            try
            {
                string data = File.ReadAllText(path, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var fields = serializer.Deserialize<Dictionary<string, object>>(data);
                if (fields == null || !fields.ContainsKey("Toggle") || !fields.ContainsKey("Emergency") || !fields.ContainsKey("Exit") || !fields.ContainsKey("StartWithWindows")) throw new FormatException();
                Settings result = serializer.Deserialize<Settings>(data);
                if (result == null || result.Validate() != null) throw new FormatException();
                return result;
            }
            catch (Exception)
            {
                warning = "配置损坏或无法读取，已回退默认快捷键；保护保持暂停。保存设置可替换此配置。";
                return Settings.Defaults();
            }
        }
        public void Save(Settings settings)
        {
            string problem = settings.Validate(); if (problem != null) throw new FormatException(problem);
            string directory = Path.GetDirectoryName(path); Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, "settings-" + Guid.NewGuid().ToString("N") + ".tmp");
            string backup = Path.Combine(directory, "settings-" + Guid.NewGuid().ToString("N") + ".bak");
            try
            {
                File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(settings), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, backup); else File.Move(temporary, path);
            }
            finally
            {
                // These exact unique files belong to this save; cleanup is best-effort.
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception) { }
                try { if (File.Exists(backup)) File.Delete(backup); } catch (Exception) { }
            }
        }
    }
    public sealed class UserStartupStore : IStartupStore
    {
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "LoLMouseGuard";
        public string Read() { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, false)) return key == null ? null : key.GetValue(ValueName) as string; }
        public void Write(string command)
        {
            if (command == null)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, true)) if (key != null) key.DeleteValue(ValueName, false);
            }
            else using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath)) key.SetValue(ValueName, command, RegistryValueKind.String);
        }
        public static string CommandFor(string executable)
        {
            if (String.IsNullOrWhiteSpace(executable) || !Path.IsPathRooted(executable) || executable.IndexOfAny(new[] { '"', '\r', '\n', '\0' }) >= 0)
                throw new ArgumentException("自启程序路径无效。请从解压后的固定位置运行工具。");
            return "\"" + executable + "\" --tray";
        }
    }
    public sealed class SettingsService : IDisposable
    {
        readonly IHotkeyBackend backend;
        readonly ISettingsStore store;
        readonly IStartupStore startup;
        readonly string executable;
        HotkeySet active;
        public Settings Current { get; private set; }
        public bool Ready { get; private set; }
        public SettingsService(IHotkeyBackend b, ISettingsStore s, IStartupStore a, string exe, Settings initial)
        { backend = b; store = s; startup = a; executable = exe; Current = initial.Copy(); }
        public bool RegisterCurrent(out string message)
        {
            if (active != null) active.Dispose();
            active = new HotkeySet(backend, Current.Keys); Ready = active.RegisterAll(); message = active.Failure; return Ready;
        }
        public void BeginEdit() { if (active != null) active.Dispose(); Ready = false; }
        public bool Save(Settings candidate, out string message)
        {
            string problem = candidate.Validate();
            if (problem != null) { message = problem; return false; }
            HotkeySet next = new HotkeySet(backend, candidate.Keys);
            string oldStartup = null; bool startupRead = false, startupTouched = false;
            try
            {
                if (!next.RegisterAll()) throw new InvalidOperationException(next.Failure);
                if (candidate.StartWithWindows != Current.StartWithWindows)
                {
                    oldStartup = startup.Read(); startupRead = true;
                    startupTouched = true;
                    startup.Write(candidate.StartWithWindows ? UserStartupStore.CommandFor(executable) : null);
                }
                store.Save(candidate);
                if (active != null) active.Dispose();
                active = next; Current = candidate.Copy(); Ready = true;
                message = "设置已保存。保护保持暂停；自启登录也默认暂停。"; return true;
            }
            catch (Exception e)
            {
                next.Dispose();
                string rollback = "";
                if (startupRead && startupTouched)
                {
                    try { startup.Write(oldStartup); } catch (Exception) { rollback = " 自启记录回滚失败，请检查设置中的自启开关。"; }
                }
                string oldKeyError; bool restored = RegisterCurrent(out oldKeyError);
                message = "设置保存失败：" + UiText.ExceptionMessage(e) + (restored ? " 已恢复原快捷键，保护保持暂停。" : " 原快捷键恢复失败，禁止启用：" + oldKeyError) + rollback;
                return false;
            }
        }
        public void Dispose() { if (active != null) active.Dispose(); Ready = false; }
    }
}

