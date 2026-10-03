// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace LoLMouseGuard
{
    static class TrayIcons
    {
        public static readonly int[] Sizes = { 16, 20, 24, 32 };
        public static Color Badge(string kind) { return kind == "protected" ? Color.FromArgb(77, 132, 103) : kind == "error" ? Color.FromArgb(165, 87, 81) : kind == "waiting" ? Color.FromArgb(78, 112, 149) : Color.FromArgb(104, 111, 121); }
        public static Bitmap Draw(string kind, int size)
        { return Draw(kind,size,false,false); }
        public static Bitmap Draw(string kind, int size,bool darkTaskbar,bool highContrast)
        {
            Bitmap b = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias; g.ScaleTransform(size / 16f, size / 16f);
                Color outline=highContrast?SystemColors.WindowText:darkTaskbar?Color.FromArgb(197,204,214):Color.FromArgb(60,69,82),mouse=highContrast?SystemColors.Window:Color.FromArgb(249,250,252),badge=highContrast?SystemColors.Highlight:Badge(kind),mark=highContrast?SystemColors.HighlightText:Color.White;
                using (GraphicsPath p = Theme.Rounded(new RectangleF(2, 1.5f, 7.5f, 12), 3.7f))
                { using (Brush fill = new SolidBrush(mouse)) g.FillPath(fill, p); using (Pen line = new Pen(outline, 1)) g.DrawPath(line, p); }
                using (Pen p = new Pen(darkTaskbar&&!highContrast?Color.FromArgb(60,69,82):outline, 1) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawLine(p, 5.75f, 3, 5.75f, 5.5f);
                using (Pen white = new Pen(Color.White, 2.8f)) g.DrawArc(white, 10, 5.4f, 4, 5, 180, 180);
                using (Pen p = new Pen(badge, 1.2f)) g.DrawArc(p, 10, 5.4f, 4, 5, 180, 180);
                using (GraphicsPath lockBody = Theme.Rounded(new RectangleF(8.5f, 8, 7, 6.5f), 1.4f))
                { using (Brush fill = new SolidBrush(badge)) g.FillPath(fill, lockBody); using (Pen edge = new Pen(Color.White, .8f)) g.DrawPath(edge, lockBody); }
                using (Pen glyph = new Pen(mark, 1) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    if (kind == "protected") g.DrawLines(glyph, new[] { new PointF(10, 11.2f), new PointF(11.3f, 12.4f), new PointF(13.7f, 10) });
                    else if (kind == "error") { g.DrawLine(glyph, 12, 9.6f, 12, 11.3f); g.DrawLine(glyph, 12, 12.6f, 12, 12.7f); }
                    else if (kind == "waiting") { g.DrawEllipse(glyph, 10, 9.5f, 4, 4); g.DrawLine(glyph, 12, 10.2f, 12, 11.5f); g.DrawLine(glyph, 12, 11.5f, 13, 11.5f); }
                    else { g.DrawLine(glyph, 10.7f, 10, 10.7f, 12.7f); g.DrawLine(glyph, 13.2f, 10, 13.2f, 12.7f); }
                }
            }
            return b;
        }
        public static byte[] Ico(string kind)
        { return Ico(kind,false,false); }
        public static byte[] Ico(string kind,bool darkTaskbar,bool highContrast)
        {
            List<byte[]> frames = new List<byte[]>(); foreach (int size in Sizes) using (Bitmap b = Draw(kind, size,darkTaskbar,highContrast)) using (MemoryStream png = new MemoryStream()) { b.Save(png, ImageFormat.Png); frames.Add(png.ToArray()); }
            using (MemoryStream bytes = new MemoryStream()) using (BinaryWriter w = new BinaryWriter(bytes))
            {
                w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)frames.Count); int offset = 6 + 16 * frames.Count;
                for (int i = 0; i < frames.Count; i++) { w.Write((byte)Sizes[i]); w.Write((byte)Sizes[i]); w.Write((byte)0); w.Write((byte)0); w.Write((ushort)1); w.Write((ushort)32); w.Write(frames[i].Length); w.Write(offset); offset += frames[i].Length; }
                foreach (byte[] frame in frames) w.Write(frame); w.Flush(); return bytes.ToArray();
            }
        }
        public static Icon Create(string kind,bool darkTaskbar,bool highContrast) { using (MemoryStream stream = new MemoryStream(Ico(kind,darkTaskbar,highContrast))) using (Icon original = new Icon(stream, new Size(16, 16))) return (Icon)original.Clone(); }
        static string Svg(string kind)
        {
            Color badge = Badge(kind); string color = String.Format("#{0:X2}{1:X2}{2:X2}", badge.R, badge.G, badge.B);
            string mark = kind == "protected" ? "M10 11.2 11.3 12.4 13.7 10" : kind == "error" ? "M12 9.6v1.7M12 12.6v.1" : kind == "waiting" ? "M12 10.2v1.3h1" : "M10.7 10v2.7M13.2 10v2.7";
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\"><rect x=\"2\" y=\"1.5\" width=\"7.5\" height=\"12\" rx=\"3.7\" fill=\"#F9FAFC\" stroke=\"#3C4552\"/><path d=\"M5.75 3v2.5\" stroke=\"#3C4552\" stroke-linecap=\"round\"/><path d=\"M10 8V7.9a2 2 0 0 1 4 0V8\" fill=\"none\" stroke=\"white\" stroke-width=\"2.8\"/><path d=\"M10 8V7.9a2 2 0 0 1 4 0V8\" fill=\"none\" stroke=\"" + color + "\" stroke-width=\"1.2\"/><rect x=\"8.5\" y=\"8\" width=\"7\" height=\"6.5\" rx=\"1.4\" fill=\"" + color + "\" stroke=\"white\" stroke-width=\".8\"/>" + (kind == "waiting" ? "<circle cx=\"12\" cy=\"11.5\" r=\"2\" fill=\"none\" stroke=\"white\"/>" : "") + "<path d=\"" + mark + "\" fill=\"none\" stroke=\"white\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";
        }
        public static void Export(string directory)
        {
            Directory.CreateDirectory(directory); string[] kinds = { "paused", "waiting", "protected", "error" };
            foreach (string kind in kinds) { File.WriteAllBytes(Path.Combine(directory, "tray-" + kind + ".ico"), Ico(kind)); File.WriteAllBytes(Path.Combine(directory, "tray-" + kind + "-dark.ico"), Ico(kind,true,false)); File.WriteAllText(Path.Combine(directory, "tray-" + kind + ".svg"), Svg(kind), new UTF8Encoding(false)); File.WriteAllText(Path.Combine(directory,"tray-"+kind+"-dark.svg"),Svg(kind).Replace("stroke=\"#3C4552\"", "stroke=\"#C5CCD6\""),new UTF8Encoding(false)); }
            File.WriteAllBytes(Path.Combine(directory, "CursorGuard.ico"), Ico("paused"));
            using (Bitmap sheet = new Bitmap(580, 286)) using (Graphics g = Graphics.FromImage(sheet))
            {
                g.Clear(Color.FromArgb(242, 243, 245)); Theme.Write(g, UiText.T("托盘图标 · 原尺寸与放大预览"), 20, 7, 540, 30, 16, Theme.Text, false, StringAlignment.Near);
                string[] labels = { UiText.T("暂停"), UiText.T("等待"), UiText.T("保护"), UiText.T("异常") };
                for (int row = 0; row < 2; row++)
                {
                    Rectangle band = new Rectangle(20, 47 + row * 115, 540, 105); Color bg = row == 0 ? Color.White : Color.FromArgb(31, 34, 39);
                    using (Brush b = new SolidBrush(bg)) g.FillRectangle(b, band);
                    for (int i = 0; i < kinds.Length; i++)
                    {
                        int x = 36 + i * 133; Theme.Write(g, labels[i], x, band.Top + 4, 119, 22, 11, row == 0 ? Theme.Muted : Color.FromArgb(204, 209, 218), false, StringAlignment.Near);
                        using (Bitmap icon = Draw(kinds[i], 16,row==1,false))
                        { g.DrawImageUnscaled(icon, x, band.Top + 42); System.Drawing.Drawing2D.InterpolationMode old = g.InterpolationMode; g.InterpolationMode = InterpolationMode.NearestNeighbor; g.DrawImage(icon, x + 44, band.Top + 28, 64, 64); g.InterpolationMode = old; }
                    }
                }
            sheet.Save(Path.Combine(directory, "preview-tray-icons.png"), ImageFormat.Png);
            }
        }
    }
}

