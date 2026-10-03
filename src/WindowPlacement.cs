// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Drawing;
using System.Windows.Forms;
namespace LoLMouseGuard
{
    static class SettingsPlacement
    {
        public static Rectangle Calculate(Rectangle main,Rectangle work,Size wanted,double scale)
        {
            if(work.Width<1 || work.Height<1) throw new ArgumentException("Empty work area");
            int width=Math.Min(wanted.Width,work.Width),height=Math.Min(wanted.Height,work.Height),gap=(int)Math.Round(8*scale);
            int x=Math.Max(work.Left,Math.Min(main.Left,work.Right-width));
            int down=main.Bottom+gap,up=main.Top-gap-height;
            int y=down+height<=work.Bottom && down>=work.Top?down:up>=work.Top && up+height<=work.Bottom?up:Math.Max(work.Top,Math.Min(down,work.Bottom-height));
            return new Rectangle(x,y,width,height);
        }
    }
}
