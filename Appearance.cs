// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LoLMouseGuard
{
    // Installed system fonts only; no font downloads or redistribution.
    static class UiTypography
    {
        public const float Body=13f, Caption=12.5f, Key=11.5f;
        public static readonly string Chinese=Resolve("Microsoft YaHei UI"),Latin=Resolve("Segoe UI");
        static string Resolve(string requested)
        {try {using(FontFamily family=new FontFamily(requested)) return family.Name;} catch(ArgumentException) {return SystemFonts.MessageBoxFont.FontFamily.Name;}}
        public static string Family(string text)
        {if(text!=null) foreach(char c in text) if(c>0x024f) return Chinese;return Latin;}
        public static Font PixelFont(string text,float size,bool bold)
        {return new Font(Family(text),size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
        public static Font ControlFont(bool latin)
        {return new Font(latin?Latin:Chinese,9.75f,FontStyle.Regular,GraphicsUnit.Point);}
    }
    // Text widths include GDI+ overhang; only small explicit spacing is added.
    sealed class CompactBarLayout
    {
        public const int MainMaximum=320,FloatingMaximum=210;
        public int Width,NameX,NameWidth,SeparatorX,StatusX,StatusWidth;
        public int FirstDividerX,ToggleX,SecondDividerX,SettingsX;
        public int IconX,IconWidth;
        static int TextWidth(string text,float size)
        {
            using(Bitmap bitmap=new Bitmap(1,1)) using(Graphics g=Graphics.FromImage(bitmap))
            using(Font font=UiTypography.PixelFont(text,size,false)) using(StringFormat format=new StringFormat {FormatFlags=StringFormatFlags.NoWrap})
                return (int)Math.Ceiling(g.MeasureString(text??"",font,Int32.MaxValue,format).Width);
        }
        public static CompactBarLayout ForMain(string name,string status="已暂停")
        {
            CompactBarLayout r=new CompactBarLayout {NameX=14,StatusWidth=TextWidth(status,UiTypography.Caption)};
            r.NameWidth=Math.Max(12,Math.Min(Math.Min(131,MainMaximum-127-r.StatusWidth),TextWidth(name,UiTypography.Body)));
            r.SeparatorX=r.NameX+r.NameWidth+4;r.StatusX=r.SeparatorX+13;
            r.FirstDividerX=r.StatusX+r.StatusWidth+6;r.ToggleX=r.FirstDividerX+8;
            r.SecondDividerX=r.ToggleX+46;r.SettingsX=r.SecondDividerX+6;r.Width=r.SettingsX+30;
            return r;
        }
        public static CompactBarLayout Floating(string name,string status="已暂停")
        {
            CompactBarLayout r=new CompactBarLayout {NameX=22,StatusWidth=TextWidth(status,UiTypography.Caption)};
            r.NameWidth=Math.Max(12,Math.Min(Math.Min(114,FloatingMaximum-41-r.StatusWidth),TextWidth(name,UiTypography.Caption)));
            r.SeparatorX=r.NameX+r.NameWidth+2;r.StatusX=r.SeparatorX+10;
            r.Width=r.StatusX+r.StatusWidth+7;return r;
        }
        public static CompactBarLayout ForMainIcon(string status)
        {
            CompactBarLayout r=new CompactBarLayout {IconX=10,IconWidth=30,StatusX=44,StatusWidth=TextWidth(status,UiTypography.Caption)};
            r.FirstDividerX=r.StatusX+r.StatusWidth+6;r.ToggleX=r.FirstDividerX+8;
            r.SecondDividerX=r.ToggleX+46;r.SettingsX=r.SecondDividerX+6;r.Width=r.SettingsX+30;return r;
        }
    }
    public sealed class SystemTheme
    {public bool AppsLight=true,TaskbarLight=true,HighContrast;}
    public static class ThemeChoice
    {public static bool IsDark(string choice,bool appsLight) {return choice=="dark" || (choice=="system" && !appsLight);}}
    static class AppearanceSelection
    {
        public static int Index(string choice) {return choice=="dark"?2:choice=="light"?1:0;}
        public static bool ShouldReconcile(bool droppedDown,int current,int saved) {return !droppedDown && current!=saved;}
        public static void Reconcile(ComboBox control,string choice)
        {int saved=Index(choice);if(ShouldReconcile(control.DroppedDown,control.SelectedIndex,saved)) control.SelectedIndex=saved;}
        public static void Commit(ComboBox control,Func<string,bool> save,Action refresh)
        {if(control.SelectedIndex>=0) {save(new[] {"system","light","dark"}[control.SelectedIndex]);refresh();}}
    }
    static class SystemThemeReader
    {
        public static SystemTheme Read()
        {
            SystemTheme t=new SystemTheme {HighContrast=SystemInformation.HighContrast};
            try
            {
                using(RegistryKey key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",false))
                {if(key!=null) {t.AppsLight=ReadLight(key.GetValue("AppsUseLightTheme"));t.TaskbarLight=ReadLight(key.GetValue("SystemUsesLightTheme"));}}
            }
            catch(Exception) { } // Read-only fallback: a readable light palette.
            return t;
        }
        static bool ReadLight(object value) {return !(value is int) || (int)value!=0;}
    }
    static class WindowTheme
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int bytes);
        public static void Apply(IntPtr handle,bool dark,Color caption)
        {
            try {int useDark=dark?1:0,color=ColorTranslator.ToWin32(caption);DwmSetWindowAttribute(handle,20,ref useDark,4);DwmSetWindowAttribute(handle,35,ref color,4);}
            catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { }
        }
    }
}

