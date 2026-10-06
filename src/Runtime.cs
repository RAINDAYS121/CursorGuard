// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public string ActiveExecutable = "";
        public uint ActivePid;
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
        View view = new View { Status = "正在初始化，保持暂停", Detail = "正在读取已保存的保护开关，尚未施加约束。", Game = "暂停检测", Range = "尚未开始保护" };
        int disposed;
        public Worker(bool dpi) { dpiReady = dpi; thread = new Thread(Run); thread.Name = "LoL mouse guard and emergency hotkeys"; thread.IsBackground = false; thread.Start(); }
        public View ReadView() { lock (viewLock) return view; }
        public void Send(Command command) { requests.Enqueue(new Request { Command = command }); wake.Set(); }
        public void SaveSettings(Settings settings) { requests.Enqueue(new Request { Settings = settings.Copy() }); wake.Set(); }
        void Publish(GuardCore core, SettingsService settings, SettingsSession edit, bool stopped, string notice, Scene scene)
        {
            string game = core.Enabled ? "未识别到前台目标" : "暂停检测";
            string range = core.Enabled ? "等待有效程序区域" : "尚未开始保护";
            if (core.Enabled && scene != null && settings.Current.Matches(scene.Executable))
            {
                game = scene.Executable + "  ·  PID " + scene.Pid;
                Box target;
                if (scene.TryTarget(out target)) range = target.Width + " × " + target.Height + " px   ·   " + target;
            }
            string detail = (core.Detail ?? "").Replace("Ctrl+Alt+F9", settings.Current.Emergency.Display());
            View published = new View { Enabled = !stopped && edit.EnabledIntent, CanEnable = core.CanEnable && !edit.Editing, Status = core.Status, Detail = detail,
                Stopped = stopped, Editing = !stopped && edit.Editing, Settings = settings.Current.Copy(), Notice = notice, Game = game, Range = range };
            ViewTarget.Update(published,scene,core.Enabled && !edit.Editing && !stopped);
            lock (viewLock) view = published;
        }
        void Run()
        {
            WindowsBackend backend = new WindowsBackend(); GuardCore core = new GuardCore(backend, dpiReady);
            FileSettingsStore store = new FileSettingsStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LoLMouseGuard", "settings.json"));
            string notice; Settings initial = store.Load(out notice);
            backend.TargetExecutables = new List<string>(initial.TargetExecutables);
            SettingsService settings = new SettingsService(backend, store, new UserStartupStore(), Application.ExecutablePath, initial);
            bool quit = false;SettingsSession edit=new SettingsSession(core,settings);
            try
            {
                Native.Message message; Native.PeekMessage(out message, IntPtr.Zero, 0, 0, 0);
                string keyError; core.SetHotkeys(settings.RegisterCurrent(out keyError), keyError);
                string restoreNotice;edit.RestoreOnStartup(out restoreNotice);if(restoreNotice!=null) notice=restoreNotice;
                Stopwatch clock = Stopwatch.StartNew();
                while (!quit)
                {
                    for (int i = 0; i < 64 && Native.PeekMessage(out message, IntPtr.Zero, 0, 0, 1); i++)
                    {
                        if (message.Id == 0x0312)
                        {
                            ulong id = message.WParam.ToUInt64();
                            if (id == 2) edit.Pause(true,out notice);
                            else if (id == 3) {edit.Shutdown(out notice);quit = true;}
                            else if (id == 1 && !edit.Editing) { if (core.Enabled) edit.Pause(false,out notice); else edit.Enable(out notice); }
                        }
                        else if (message.Id == 0x0012) {edit.Shutdown(out notice);quit = true;}
                    }
                    if(quit) break;
                    Request request;
                    while (requests.TryDequeue(out request))
                    {
                        if (request.Settings != null)
                        {
                            edit.Save(request.Settings,out notice);backend.TargetExecutables=new List<string>(settings.Current.TargetExecutables);
                        }
                        else if (request.Command == Command.Quit) {edit.Shutdown(out notice);quit=true;break;}
                        else if (request.Command == Command.Emergency) edit.Pause(true,out notice);
                        else if (request.Command == Command.Pause) edit.Pause(false,out notice);
                        else if (request.Command == Command.BeginSettings)
                        {
                            edit.Begin(out notice);
                        }
                        else if (request.Command == Command.CancelSettings)
                        {
                            edit.Cancel(out notice);
                        }
                        else if (!edit.Editing && request.Command == Command.Enable) edit.Enable(out notice);
                        else if (request.Command == Command.Toggle) {if(edit.Editing || core.Enabled) edit.Pause(false,out notice);else edit.Enable(out notice);}
                    }
                    if (quit) break;
                    Scene scene = null;
                    if (core.Enabled || core.OwnsClip) { scene = backend.ReadScene(); core.Step(scene, clock.ElapsedMilliseconds); }
                    string faultNotice;edit.ObserveFault(out faultNotice);if(faultNotice!=null) notice=faultNotice;
                    Publish(core, settings, edit, false, notice, scene); wake.WaitOne(20);
                }
            }
            catch (Exception e) { string failure;edit.Abort(out failure); notice = "工具异常：" + e.GetType().Name + "。已停止，请正常 Alt+Tab 切出。" + (failure==null?"":" "+failure); }
            finally { try { core.Stop(); } finally { settings.Dispose(); } Publish(core, settings, edit, true, notice, null); }
        }
        public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) { Send(Command.Quit); thread.Join(2000); } }
    }
}

