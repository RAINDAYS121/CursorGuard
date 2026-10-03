// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
// CursorGuard 1.2.2. Public Win32 APIs only; no hooks, injection or game memory.
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyVersion("1.2.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.2.2.0")]
[assembly: System.Reflection.AssemblyProduct("CursorGuard")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 RAINDAYS121")]

namespace LoLMouseGuard
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Box : IEquatable<Box>
    {
        public int Left, Top, Right, Bottom;
        public Box(int l, int t, int r, int b) { Left = l; Top = t; Right = r; Bottom = b; }
        public long Width { get { return (long)Right - Left; } }
        public long Height { get { return (long)Bottom - Top; } }
        public bool Valid { get { return Width > 0 && Height > 0; } }
        public bool Contains(Position p) { return p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom; }
        public bool Equals(Box b) { return Left == b.Left && Top == b.Top && Right == b.Right && Bottom == b.Bottom; }
        public override bool Equals(object o) { return o is Box && Equals((Box)o); }
        public override int GetHashCode() { return Left ^ Top ^ Right ^ Bottom; }
        public override string ToString() { return String.Format("[{0}, {1}, {2}, {3}]", Left, Top, Right, Bottom); }
        public static Box Intersect(Box a, Box b) { return new Box(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom)); }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Position { public int X, Y; public Position(int x, int y) { X = x; Y = y; } }

    public sealed class Scene
    {
        public IntPtr Window;
        public uint Pid;
        public string Executable;
        public string TargetExecutable = "League of Legends.exe";
        public bool Visible, Minimized, CoordinatesReady, CursorReady;
        public Box Client, Monitor, VirtualScreen;
        public Position Cursor;
        public bool TryTarget(out Box target)
        {
            target = new Box();
            if (Window == IntPtr.Zero || Pid == 0 || !Visible || Minimized || !CoordinatesReady || !CursorReady ||
                !String.Equals(Executable, TargetExecutable, StringComparison.OrdinalIgnoreCase)) return false;
            target = Box.Intersect(Box.Intersect(Client, Monitor), VirtualScreen);
            // Ignore 1x1 rectangles observed during full-screen restoration/exit.
            return Client.Width >= 64 && Client.Height >= 64 && target.Width >= 64 && target.Height >= 64;
        }
    }

    public enum ApplyResult { Applied, Skipped, Failed, AppliedReleaseFailed }
    public interface ICursorBackend
    {
        bool ReadClip(out Box clip);
        ApplyResult Apply(Box target, Scene expected, out int error);
        bool Free(out int error);
    }

    // All state changes and ClipCursor calls belong to the independent guard thread.
    public sealed class GuardCore
    {
        readonly ICursorBackend backend;
        readonly bool dpiReady;
        bool hotkeysReady, enabled, ownsClip;
        Box ownedBox, pendingBox;
        IntPtr pendingWindow;
        uint pendingPid;
        long stableSince;
        public bool Enabled { get { return enabled; } }
        public bool CanEnable { get { return hotkeysReady && dpiReady; } }
        public bool OwnsClip { get { return ownsClip; } }
        public string Status { get; private set; }
        public string Detail { get; private set; }
        public GuardCore(ICursorBackend b, bool dpi) { backend = b; dpiReady = dpi; Status = "已暂停"; Detail = "启动时不约束鼠标。"; }
        public void SetHotkeys(bool ready, string failure)
        {
            hotkeysReady = ready;
            if (!ready) { enabled = false; Release(false); Status = "快捷键不可用，禁止启用"; Detail = failure; }
            else if (!dpiReady) { enabled = false; Status = "DPI 坐标检查失败，禁止启用"; Detail = "需要 Windows 10/11 的每显示器 DPI 坐标支持。"; }
            else { Status = "已暂停"; Detail = "快捷键已就绪。请手动启用。"; }
        }
        public bool Enable()
        {
            if (!CanEnable) return false;
            enabled = true; ResetPending(); Status = "已启用，等待程序前台"; Detail = "只有目标程序在前台才会保护鼠标。"; return true;
        }
        public void Pause(bool emergency)
        {
            enabled = false; ResetPending();
            bool ok = Release(emergency);
            if (CanEnable) { Status = ok ? (emergency ? "紧急释放后已暂停" : "已暂停") : "释放失败，已暂停"; if (ok) Detail = "本工具不会再次加约束，直到手动启用。"; }
        }
        public void Stop() { enabled = false; ResetPending(); Release(false); Status = "已退出"; }
        void ResetPending() { pendingWindow = IntPtr.Zero; pendingPid = 0; stableSince = 0; }
        bool Release(bool force)
        {
            if (!force && !ownsClip) return true;
            Box current;
            // ClipCursor has no owner identity. Avoid clearing a different rectangle
            // subsequently installed by another application.
            if (!force && backend.ReadClip(out current) && !current.Equals(ownedBox)) { ownsClip = false; return true; }
            int error;
            bool ok = backend.Free(out error);
            if (ok) ownsClip = false;
            else { enabled = false; Status = "释放失败，已暂停"; Detail = "Win32 错误 " + error + "；请正常 Alt+Tab 切出。"; }
            return ok;
        }
        public void Step(Scene s, long now)
        {
            if (!enabled) { if (ownsClip) Release(false); return; }
            Box target;
            if (!CanEnable || !s.TryTarget(out target))
            {
                Release(false); ResetPending();
                if (enabled) { Status = "已启用，当前不约束"; Detail = "等待目标窗口在前台且客户区有效；普通切屏会释放。"; }
                return;
            }
            if (pendingWindow != s.Window || pendingPid != s.Pid || !pendingBox.Equals(target))
            {
                Release(false); pendingWindow = s.Window; pendingPid = s.Pid; pendingBox = target; stableSince = now;
            }
            if (!enabled) return;
            if (!target.Contains(s.Cursor))
            {
                Release(false); Status = "等待鼠标自行回到程序区域"; Detail = "不会主动把副屏上的鼠标拉回。"; return;
            }
            if (now - stableSince < 100) { Status = "等待程序窗口稳定"; Detail = "确认前台窗口及客户区稳定 100 毫秒。"; return; }
            Box existing;
            if (!backend.ReadClip(out existing)) { Pause(false); Status = "无法读取鼠标约束，已暂停"; Detail = "请检查当前输入桌面，恢复后重新启用。"; return; }
            if (existing.Equals(target)) { Status = "保护中"; Detail = "程序区域 " + target + "；Alt+Tab 可正常切出。"; return; }
            int error;
            ApplyResult result = backend.Apply(target, s, out error);
            if (result == ApplyResult.Applied) { ownsClip = true; ownedBox = target; Status = "保护中"; Detail = "程序区域 " + target + "；Alt+Tab 可正常切出。"; }
            else if (result == ApplyResult.AppliedReleaseFailed)
            {
                // A successful clip followed by a failed immediate cleanup must
                // retain ownership so the paused loop can retry release.
                ownsClip = true; ownedBox = target; Pause(false);
                Status = "前台切换时释放失败，已暂停"; Detail = "Win32 错误 " + error + "；将重试清理。可按 Ctrl+Alt+F9 或正常 Alt+Tab 切出。";
            }
            else if (result == ApplyResult.Failed) { Pause(false); Status = "约束接口失败，已暂停"; Detail = "Win32 错误 " + error + "。不会提权或修改安全配置。"; }
            else { Release(false); ResetPending(); Status = "前台状态变化，暂不约束"; Detail = "等待程序窗口稳定。"; }
        }
    }

    static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo { public int Size; public Box Monitor, Work; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] public struct Message { public IntPtr Window; public uint Id; public UIntPtr WParam; public IntPtr LParam; public uint Time; public Position Point; public uint Private; }
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool GetClientRect(IntPtr h, out Box b);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool ClientToScreen(IntPtr h, ref Position p);
        [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfo(IntPtr h, ref MonitorInfo m);
        [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool GetCursorPos(out Position p);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool GetClipCursor(out Box b);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "ClipCursor")] public static extern bool Confine(ref Box b);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "ClipCursor")] public static extern bool Unconfine(IntPtr nullRect);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool QueryFullProcessImageName(IntPtr h, uint flags, StringBuilder name, ref int length);
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern bool PeekMessage(out Message m, IntPtr window, uint min, uint max, uint remove);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] public static extern IntPtr GetThreadDpiAwarenessContext();
        [DllImport("user32.dll")] public static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);
        [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int awareness);
        [DllImport("shcore.dll")] public static extern int GetProcessDpiAwareness(IntPtr process, out int awareness);
        public static bool PrepareDpi()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); return GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()) == 2; }
            catch (EntryPointNotFoundException)
            {
                try { SetProcessDpiAwareness(2); int a; return GetProcessDpiAwareness(IntPtr.Zero, out a) == 0 && a == 2; }
                catch (DllNotFoundException) { return false; }
            }
        }
    }

    sealed class WindowsBackend : ICursorBackend, IHotkeyBackend
    {
        public string TargetExecutable = "League of Legends.exe";
        public bool ReadClip(out Box b) { return Native.GetClipCursor(out b); }
        public bool Free(out int error) { bool ok = Native.Unconfine(IntPtr.Zero); error = ok ? 0 : Marshal.GetLastWin32Error(); return ok; }
        public bool Register(int id, uint modifiers, uint key, out int error) { bool ok = Native.RegisterHotKey(IntPtr.Zero, id, 0x4000 | modifiers, key); error = ok ? 0 : Marshal.GetLastWin32Error(); return ok; }
        public void Unregister(int id) { Native.UnregisterHotKey(IntPtr.Zero, id); }
        static string Executable(uint pid)
        {
            IntPtr h = Native.OpenProcess(0x1000, false, pid); // Query-limited metadata, never PROCESS_VM_READ.
            if (h == IntPtr.Zero) return null;
            try { StringBuilder b = new StringBuilder(32768); int n = b.Capacity; return Native.QueryFullProcessImageName(h, 0, b, ref n) ? Path.GetFileName(b.ToString()) : null; }
            finally { Native.CloseHandle(h); }
        }
        public Scene ReadScene()
        {
            Scene s = new Scene(); s.TargetExecutable = TargetExecutable; s.Window = Native.GetForegroundWindow();
            if (s.Window == IntPtr.Zero) return s;
            Native.GetWindowThreadProcessId(s.Window, out s.Pid);
            s.Executable = Executable(s.Pid);
            if (!String.Equals(s.Executable, TargetExecutable, StringComparison.OrdinalIgnoreCase)) return s;
            s.Visible = Native.IsWindowVisible(s.Window); s.Minimized = Native.IsIconic(s.Window);
            if (!s.Visible || s.Minimized) return s;
            Box local;
            if (!Native.GetClientRect(s.Window, out local)) return s;
            Position tl = new Position(local.Left, local.Top), br = new Position(local.Right, local.Bottom);
            if (!Native.ClientToScreen(s.Window, ref tl) || !Native.ClientToScreen(s.Window, ref br)) return s;
            s.Client = new Box(tl.X, tl.Y, br.X, br.Y);
            Native.MonitorInfo mi = new Native.MonitorInfo(); mi.Size = Marshal.SizeOf(typeof(Native.MonitorInfo));
            if (!Native.GetMonitorInfo(Native.MonitorFromWindow(s.Window, 2), ref mi)) return s;
            s.Monitor = mi.Monitor;
            int vx = Native.GetSystemMetrics(76), vy = Native.GetSystemMetrics(77);
            long right = (long)vx + Native.GetSystemMetrics(78), bottom = (long)vy + Native.GetSystemMetrics(79);
            if (right > Int32.MaxValue || bottom > Int32.MaxValue) return s;
            s.VirtualScreen = new Box(vx, vy, (int)right, (int)bottom);
            s.CoordinatesReady = s.VirtualScreen.Valid;
            s.CursorReady = Native.GetCursorPos(out s.Cursor);
            // Read both sides of the scene collection; drop torn foreground samples.
            uint verifyPid;
            if (Native.GetForegroundWindow() != s.Window || Native.GetWindowThreadProcessId(s.Window, out verifyPid) == 0 || verifyPid != s.Pid) s.CoordinatesReady = false;
            return s;
        }
        public ApplyResult Apply(Box target, Scene expected, out int error)
        {
            error = 0;
            Scene latest = ReadScene(); Box currentTarget;
            if (latest.Window != expected.Window || latest.Pid != expected.Pid || !latest.TryTarget(out currentTarget) ||
                !currentTarget.Equals(target) || !target.Contains(latest.Cursor)) return ApplyResult.Skipped;
            if (!Native.Confine(ref target)) { error = Marshal.GetLastWin32Error(); return ApplyResult.Failed; }
            // A foreground change can still race any public API call. Release on
            // immediate recheck; otherwise the next 20 ms poll notices it.
            if (Native.GetForegroundWindow() != expected.Window || Native.IsIconic(expected.Window))
            {
                Box now;
                if (!ReadClip(out now) || now.Equals(target))
                {
                    int e;
                    if (!Free(out e)) { error = e; return ApplyResult.AppliedReleaseFailed; }
                }
                return ApplyResult.Skipped;
            }
            return ApplyResult.Applied;
        }
    }

    // These tests use fake backends exclusively. They never register a hotkey,
    // create a window, read keyboard state, or call native ClipCursor.
    public sealed class FakeBackend : ICursorBackend, IHotkeyBackend
    {
        public Box Clip = new Box(-2560, 0, 2560, 1440);
        public int Applies, Frees, RegisterCalls, FailRegistrationAt, ApplyError;
        public bool FailRead, FailFree, SkipApply, AppliedReleaseFailure;
        public List<int> Unregistered = new List<int>();
        public Dictionary<int, Shortcut> Registered = new Dictionary<int, Shortcut>();
        public bool FailAllRegistrations;
        public bool ReadClip(out Box b) { b = Clip; return !FailRead; }
        public ApplyResult Apply(Box b, Scene s, out int e) { e = ApplyError; if (ApplyError != 0) return ApplyResult.Failed; if (SkipApply) return ApplyResult.Skipped; Applies++; Clip = b; if (AppliedReleaseFailure) { e = 5; return ApplyResult.AppliedReleaseFailed; } return ApplyResult.Applied; }
        public bool Free(out int e) { e = FailFree ? 5 : 0; Frees++; if (FailFree) return false; Clip = new Box(-2560, 0, 2560, 1440); return true; }
        public bool Register(int id, uint modifiers, uint key, out int e) { RegisterCalls++; e = (FailAllRegistrations || RegisterCalls == FailRegistrationAt) ? 1409 : 0; if (e == 0) Registered[id] = new Shortcut(modifiers, key); return e == 0; }
        public void Unregister(int id) { Unregistered.Add(id); Registered.Remove(id); }
    }
    static class SelfTests
    {
        public sealed class Check { public string name; public bool passed; public string error; }
        static Scene Game()
        {
            return new Scene { Window = new IntPtr(42), Pid = 100, Executable = "League of Legends.exe", Visible = true, CoordinatesReady = true, CursorReady = true,
                Client = new Box(0, 0, 2560, 1440), Monitor = new Box(0, 0, 2560, 1440), VirtualScreen = new Box(-2560, 0, 2560, 1440), Cursor = new Position(100, 100) };
        }
        static GuardCore Ready(FakeBackend b) { GuardCore c = new GuardCore(b, true); c.SetHotkeys(true, null); c.Enable(); return c; }
        static void Active(GuardCore c, Scene s) { c.Step(s, 0); c.Step(s, 100); }
        static void Assert(bool yes) { if (!yes) throw new Exception("assertion failed"); }
        public static int Run(string output)
        {
            List<Check> checks = new List<Check>();
            Action<string, Action> test = delegate(string name, Action action) { Check c = new Check { name = name }; try { action(); c.passed = true; } catch (Exception e) { c.error = e.Message; } checks.Add(c); };
            test("startup_paused_no_clip_writes", delegate { FakeBackend b = new FakeBackend(); GuardCore c = new GuardCore(b, true); c.SetHotkeys(true, null); c.Step(Game(), 1000); c.Stop(); Assert(!c.Enabled && b.Applies == 0 && b.Frees == 0); });
            test("foreground_game_stability_and_apply", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); c.Step(Game(), 0); c.Step(Game(), 99); Assert(b.Applies == 0); c.Step(Game(), 100); Assert(b.Applies == 1 && c.OwnsClip); });
            test("existing_game_clip_not_rewritten", delegate { FakeBackend b = new FakeBackend(); b.Clip = Game().Client; GuardCore c = Ready(b); Active(c, Game()); c.Stop(); Assert(b.Applies == 0 && b.Frees == 0); });
            test("alt_tab_releases_on_next_step", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.Executable = "browser.exe"; c.Step(s, 101); Assert(!c.OwnsClip && b.Frees == 1 && c.Enabled); });
            test("return_to_game_auto_restores", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); c.Step(new Scene(), 101); c.Step(Game(), 102); Assert(b.Applies == 1); c.Step(Game(), 202); Assert(b.Applies == 2); });
            test("minimized_releases", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.Minimized = true; c.Step(s, 101); Assert(!c.OwnsClip && b.Frees == 1); });
            test("foreground_null_releases", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); c.Step(new Scene(), 101); Assert(b.Frees == 1); });
            test("game_process_exit_releases", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.Pid = 0; s.Executable = null; c.Step(s, 101); Assert(b.Frees == 1); });
            test("client_launcher_never_locked", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Executable = "LeagueClientUx.exe"; Active(c, s); Assert(b.Applies == 0); });
            test("executable_name_case_insensitive", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Executable = "LEAGUE OF LEGENDS.EXE"; Active(c, s); Assert(b.Applies == 1); });
            test("negative_monitor_coordinates", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Client = s.Monitor = new Box(-2560, -200, 0, 1240); s.VirtualScreen = new Box(-2560, -200, 2560, 1440); s.Cursor = new Position(-100, 500); Active(c, s); Assert(b.Clip.Equals(s.Client)); });
            test("client_coordinates_clipped_to_game_monitor", delegate { Scene s = Game(); s.Client = new Box(-100, -20, 3000, 1500); Box t; Assert(s.TryTarget(out t) && t.Equals(s.Monitor)); });
            test("windowed_client_without_title_bar", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Client = new Box(200, 150, 1400, 1000); s.Cursor = new Position(300, 300); Active(c, s); Assert(b.Clip.Equals(s.Client)); });
            test("one_pixel_exit_rectangle_releases", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.Client = new Box(0, 0, 1, 1); c.Step(s, 101); Assert(b.Frees == 1 && b.Applies == 1); });
            test("empty_client_never_locked", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Client = new Box(0, 0, 0, 0); Active(c, s); Assert(b.Applies == 0); });
            test("dpi_failure_blocks_enable", delegate { FakeBackend b = new FakeBackend(); GuardCore c = new GuardCore(b, false); c.SetHotkeys(true, null); Assert(!c.Enable()); c.Step(Game(), 1000); Assert(b.Applies == 0); });
            test("mixed_dpi_physical_client_preserved", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Client = new Box(300, 225, 2100, 1275); s.Cursor = new Position(400, 300); Active(c, s); Assert(b.Clip.Equals(s.Client)); });
            test("outside_cursor_not_pulled_back", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Scene s = Game(); s.Cursor = new Position(-1, 500); Active(c, s); Assert(b.Applies == 0 && s.Cursor.X == -1); s.Cursor = new Position(1, 500); c.Step(s, 120); Assert(b.Applies == 1); });
            test("right_bottom_edges_exclusive", delegate { Box b = new Box(0, 0, 2560, 1440); Assert(b.Contains(new Position(2559, 1439)) && !b.Contains(new Position(2560, 1439)) && !b.Contains(new Position(2559, 1440))); });
            test("constraint_loss_repaired", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); b.Clip = new Box(-2560, 0, 2560, 1440); c.Step(Game(), 120); Assert(b.Applies == 2); });
            test("pause_prevents_reacquisition", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); c.Pause(false); c.Step(Game(), 1000); Assert(b.Frees == 1 && b.Applies == 1 && !c.Enabled); });
            test("emergency_release_even_without_own_clip", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); c.Pause(true); c.Step(Game(), 1000); Assert(b.Frees == 1 && b.Applies == 0 && !c.Enabled); });
            test("normal_exit_cleans_owned_clip", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); c.Stop(); c.Stop(); Assert(b.Frees == 1 && !c.OwnsClip && !c.Enabled); });
            test("other_app_new_rectangle_preserved", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Box other = new Box(-2400, 100, -400, 1200); b.Clip = other; c.Step(new Scene(), 101); Assert(b.Frees == 0 && b.Clip.Equals(other) && !c.OwnsClip); });
            test("hotkey_failure_rolls_back_and_blocks_enable", delegate { FakeBackend b = new FakeBackend(); b.FailRegistrationAt = 2; HotkeySet h = new HotkeySet(b); bool ready = h.RegisterAll(); GuardCore c = new GuardCore(b, true); c.SetHotkeys(ready, h.Failure); Assert(!ready && !c.Enable() && b.Unregistered.Count == 1 && b.Unregistered[0] == 1 && h.Failure.Contains("F9")); });
            test("all_hotkeys_unregister_on_exit", delegate { FakeBackend b = new FakeBackend(); HotkeySet h = new HotkeySet(b); Assert(h.RegisterAll()); h.Dispose(); h.Dispose(); Assert(b.Unregistered.Count == 3); });
            test("clip_read_failure_pauses", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); b.FailRead = true; Active(c, Game()); Assert(!c.Enabled && b.Applies == 0); });
            test("clip_write_failure_pauses", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); b.ApplyError = 5; Active(c, Game()); Assert(!c.Enabled && b.Applies == 0 && c.Status.Contains("失败")); });
            test("foreground_change_before_apply_skips", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); b.SkipApply = true; Active(c, Game()); Assert(b.Applies == 0 && !c.OwnsClip); });
            test("release_failure_disables_and_retries", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); b.FailFree = true; c.Step(new Scene(), 101); Assert(!c.Enabled && c.OwnsClip); b.FailFree = false; c.Step(new Scene(), 120); Assert(!c.OwnsClip); });
            test("monitor_change_releases_then_stabilizes", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.Client = s.Monitor = new Box(-2560, 0, 0, 1440); s.Cursor = new Position(-200, 500); c.Step(s, 101); Assert(b.Frees == 1 && b.Applies == 1); c.Step(s, 201); Assert(b.Applies == 2 && b.Clip.Left == -2560); });
            test("unverified_coordinates_fail_open", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.CoordinatesReady = false; c.Step(s, 101); Assert(b.Frees == 1 && b.Applies == 1); });
            test("cursor_read_failure_fail_open", delegate { FakeBackend b = new FakeBackend(); GuardCore c = Ready(b); Active(c, Game()); Scene s = Game(); s.CursorReady = false; c.Step(s, 101); Assert(b.Frees == 1 && b.Applies == 1); });
            test("apply_focus_race_cleanup_failure_retains_ownership", delegate { FakeBackend b = new FakeBackend(); b.AppliedReleaseFailure = true; b.FailFree = true; GuardCore c = Ready(b); Active(c, Game()); Assert(!c.Enabled && c.OwnsClip && b.Applies == 1); b.FailFree = false; c.Step(new Scene(), 120); Assert(!c.OwnsClip && b.Frees == 2); });
            int failed = checks.FindAll(delegate(Check c) { return !c.passed; }).Count;
            object report = new { version = "1.2.2", utc = DateTime.UtcNow.ToString("o"), mode = "simulation_only_no_native_input_or_clip_calls", passed = checks.Count - failed, failed = failed, tests = checks,
                unverified = new[] { "Actual Windows hotkey availability/delivery", "Actual game/full-screen/DPI behavior", "Measured Alt+Tab release latency", "Forced termination/OS hangs", "Anti-cheat compatibility" } };
            File.WriteAllText(output, new JavaScriptSerializer().Serialize(report), new UTF8Encoding(false));
            return failed == 0 ? 0 : 1;
        }
    }

    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--self-test") { string output = Path.GetFullPath(args[1]); int core = SelfTests.Run(output); int settings = SettingsTests.Append(output); int floating = FloatingTests.Append(output); int ui = Math.Max(Math.Max(UiRegressionTests.Append(output), ProgramIconTests.Append(output)),SettingsScrollTests.Append(output)); return Math.Max(ui, Math.Max(core, Math.Max(settings, floating))); }
            if (args.Length == 2 && args[0] == "--render-preview") return Previews.Render(Path.GetFullPath(args[1]));
            if (args.Length == 2 && args[0] == "--native-render-probe") return NativeRenderProbe.Run(Path.GetFullPath(args[1]));
            if (args.Length == 3 && args[0] == "--render-review") return ReviewBoards.Render(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]));
            bool startInTray = args.Length == 1 && args[0] == "--tray";
            if (args.Length != 0 && !startInTray) return 2;
            bool created;
            using (Mutex singleton = new Mutex(true, "Local\\LoLMouseGuard.v1", out created))
            {
                if (!created) { if (!startInTray) MessageBox.Show("工具已在此 Windows 会话中运行。请查看托盘图标。", "LoL 鼠标守护", MessageBoxButtons.OK, MessageBoxIcon.Information); return 0; }
                bool dpi = Native.PrepareDpi();
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Worker worker = new Worker(dpi);
                Application.ThreadException += delegate { worker.Send(Command.Emergency); Application.Exit(); };
                AppDomain.CurrentDomain.ProcessExit += delegate { worker.Dispose(); };
                try { Application.Run(new ControlPanel(worker, false, startInTray)); }
                finally { worker.Dispose(); singleton.ReleaseMutex(); }
            }
            return 0;
        }
    }
}

