// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.IO;

namespace LoLMouseGuard
{
    public static class TargetPrograms
    {
        public const string InvalidNameMessage = "目标程序请填写完整可执行文件名，例如 League of Legends.exe；不要填写路径或通配符。";
        public static bool ValidName(string name)
        {
            return !String.IsNullOrWhiteSpace(name) && name.Length <= 128 && name.Length > 4 &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name == name.Trim() &&
                name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                name.IndexOfAny(new[] {'/', '\\', '\r', '\n', '\0'}) < 0;
        }
        public static bool Contains(IEnumerable<string> names, string executable)
        {
            if (names == null || !ValidName(executable)) return false;
            foreach (string name in names)
                if (String.Equals(name, executable, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        public static List<string> Normalize(IEnumerable<string> names)
        {
            if (names == null) return null;
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names) if (seen.Add(name)) result.Add(name);
            return result;
        }
    }
    static class ViewTarget
    {
        public static void Update(View view, Scene scene, bool detecting)
        {
            view.ActiveExecutable = ""; view.ActivePid = 0;
            Box target;
            if (detecting && scene != null && view.Settings.Matches(scene.Executable) && scene.TryTarget(out target))
            { view.ActiveExecutable = scene.Executable; view.ActivePid = scene.Pid; }
        }
    }
}
