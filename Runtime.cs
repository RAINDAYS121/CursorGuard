// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace LoLMouseGuard
{
    public enum Command { Enable, Pause, Emergency, Toggle, Quit, BeginSettings, CancelSettings }
    public sealed class View
    {
        public bool Enabled, CanEnable, Stopped, Editing;
        public string Status, Detail, Game, Range, Notice;
        public Settings Settings = Settings.Defaults();
    }
    public interface IUiSession : IDisposable { View ReadView(); void Send(Command command); void SaveSettings(Settings settings); }
    sealed class Request { public Command Command; public Settings Settings; }
    static class SettingsEntry
    {
        // Do not unregister the emergency shortcut while our owned clip remains.
        public static bool TryBegin(GuardCore core,SettingsService settings,ref bool editing)
        {
            core.Pause(false);if(core.OwnsClip) return false;
            if(!editing) settings.BeginEdit();editing=true;return true;
        }
    }
    sealed class Worker : IUiSession
    {
        readonly ConcurrentQueue<Request> requests = new ConcurrentQueue<Request>();
        readonly AutoResetEvent wake = new AutoResetEvent(false);
        readonly object viewLock = new object();
        readonly Thread thread;
        readonly bool dpiReady;
        View view = new View { Status = "正在初始化，保持暂停", Detail = "不会自动启用鼠标约束。", Game = "暂停检测", Range = "尚未开始保护" };
        int disposed;
        public Worker(bool dpi) { dpiReady = dpi; thread = new Thread(Run); thread.Name = "LoL mouse guard and emergency hotkeys"; thread.IsBackground = false; thread.Start(); }
        public View ReadView() { lock (viewLock) return view; }
        public void Send(Command command) { requests.Enqueue(new Request { Command = command }); wake.Set(); }
        public void SaveSettings(Settings settings) { requests.Enqueue(new Request { Settings = settings.Copy() }); wake.Set(); }
        void Publish(GuardCore core, SettingsService settings, bool stopped, bool editing, string notice, Scene scene)
        {
            string game = core.Enabled ? "未识别到前台目标" : "暂停检测";
            string range = core.Enabled ? "等待有效程序区域" : "尚未开始保护";
            if (core.Enabled && scene != null && String.Equals(scene.Executable, settings.Current.TargetExecutable, StringComparison.OrdinalIgnoreCase))
            {
                game = scene.Executable + "  ·  PID " + scene.Pid;
                Box target;
                if (scene.TryTarget(out target)) range = target.Width + " × " + target.Height + " px   ·   " + target;
            }
            string detail = (core.Detail ?? "").Replace("Ctrl+Alt+F9", settings.Current.Emergency.Display());
            lock (viewLock) view = new View { Enabled = core.Enabled, CanEnable = core.CanEnable && !editing, Status = core.Status, Detail = detail,
                Stopped = stopped, Editing = editing, Settings = settings.Current.Copy(), Notice = notice, Game = game, Range = range };
        }
        void Run()
        {
            WindowsBackend backend = new WindowsBackend(); GuardCore core = new GuardCore(backend, dpiReady);
            FileSettingsStore store = new FileSettingsStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoLMouseGuard", "settings.json"));
            string notice; Settings initial = store.Load(out notice);
            backend.TargetExecutable = initial.TargetExecutable;
            SettingsService settings = new SettingsService(backend, store, new UserStartupStore(), Application.ExecutablePath, initial);
            bool quit = false, editing = false;
            try
            {
                Native.Message message; Native.PeekMessage(out message, IntPtr.Zero, 0, 0, 0);
                string keyError; core.SetHotkeys(settings.RegisterCurrent(out keyError), keyError);
                Stopwatch clock = Stopwatch.StartNew();
                while (!quit)
                {
                    for (int i = 0; i < 64 && Native.PeekMessage(out message, IntPtr.Zero, 0, 0, 1); i++)
                    {
                        if (message.Id == 0x0312)
                        {
                            ulong id = message.WParam.ToUInt64();
                            if (id == 2) core.Pause(true);
                            else if (id == 3) quit = true;
                            else if (id == 1 && !editing) { if (core.Enabled) core.Pause(false); else core.Enable(); }
                        }
                        else if (message.Id == 0x0012) quit = true;
                    }
                    Request request;
                    while (requests.TryDequeue(out request))
                    {
                        if (request.Settings != null)
                        {
                            if (!editing) { notice = "请先打开设置，保护暂停后才能修改。"; continue; }
                            string problem = request.Settings.Validate();
                            if (problem != null) { notice = problem; continue; }
                            settings.Save(request.Settings, out notice); backend.TargetExecutable = settings.Current.TargetExecutable; editing = false;
                            core.SetHotkeys(settings.Ready, notice);
                        }
                        else if (request.Command == Command.Quit) quit = true;
                        else if (request.Command == Command.Emergency) core.Pause(true);
                        else if (request.Command == Command.Pause) core.Pause(false);
                        else if (request.Command == Command.BeginSettings)
                        {
                            if (!SettingsEntry.TryBegin(core, settings, ref editing)) { notice = "尚未确认释放本工具约束，不能编辑。请使用紧急释放或正常 Alt+Tab。"; continue; }
                            core.SetHotkeys(false, "编辑设置期间保持暂停。Alt+F4 可随时退出。"); notice = null;
                        }
                        else if (request.Command == Command.CancelSettings)
                        {
                            if (editing) { string restoredError; core.SetHotkeys(settings.RegisterCurrent(out restoredError), restoredError); }
                            editing = false; notice = null;
                        }
                        else if (!editing && request.Command == Command.Enable) core.Enable();
                        else if (!editing && request.Command == Command.Toggle) { if (core.Enabled) core.Pause(false); else core.Enable(); }
                    }
                    if (quit) break;
                    Scene scene = null;
                    if (core.Enabled || core.OwnsClip) { scene = backend.ReadScene(); core.Step(scene, clock.ElapsedMilliseconds); }
                    Publish(core, settings, false, editing, notice, scene); wake.WaitOne(20);
                }
            }
            catch (Exception e) { core.Pause(false); notice = "工具异常：" + e.GetType().Name + "。已停止，请正常 Alt+Tab 切出。"; }
            finally { try { core.Stop(); } finally { settings.Dispose(); } Publish(core, settings, true, false, notice, null); }
        }
        public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) { Send(Command.Quit); thread.Join(2000); } }
    }
}

