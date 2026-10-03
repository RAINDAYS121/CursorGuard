// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace LoLMouseGuard
{
    static class ReviewBoards
    {
        static void Label(Graphics g,string text,float x,float y,Color color,int size=13)
        {using(Font font=new Font(UiTypography.Chinese,size,FontStyle.Regular,GraphicsUnit.Pixel)) using(Brush brush=new SolidBrush(color)) g.DrawString(text,font,brush,x,y);}
        static void Put(Graphics g,string file,int x,int y) {using(Image image=Image.FromFile(file)) g.DrawImageUnscaled(image,x,y);}
        public static int Render(string root,string native)
        {
            string previews=Path.Combine(root,"previews"),review=Path.Combine(previews,"review");Directory.CreateDirectory(review);
            Dictionary<string,object> report=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"test-results.json")));
            int testCount=Convert.ToInt32(report["passed"]);
            foreach(bool dark in new[] {false,true})
            using(Bitmap board=new Bitmap(1000,1090)) using(Graphics g=Graphics.FromImage(board))
            {
                Color foreground=dark?Color.FromArgb(237,237,237):Color.FromArgb(36,38,42);g.Clear(dark?Color.FromArgb(32,32,32):Color.FromArgb(241,242,244));
                string palette=dark?"dark":"light",suffix=dark?"-dark":"";
                Label(g,"CursorGuard 1.2.4 · "+(dark?"深色":"浅色")+"图标主界面预览",28,20,foreground,23);
                Label(g,"独立构建 · "+testCount+" 项回归通过 · 当前运行的 1.2.1 保留",28,61,foreground);
                Label(g,"原生客户区截图 + 运行时 alpha 曲面合成（窗口在屏幕外）",28,96,foreground);
                for(int i=0;i<2;i++) {int x=i==0?28:520;g.FillRectangle(i==0?Brushes.Black:Brushes.White,x,126,450,78);Put(g,Path.Combine(native,"native-main-"+palette+"-dpi100.png"),x+18,143);}
                Label(g,"100% 尺寸，黑色 / 白色背景",28,212,foreground);
                Put(g,Path.Combine(native,"native-main-"+palette+"-dpi200.png"),28,241);
                Put(g,Path.Combine(native,"native-main-fileicon-"+palette+"-dpi200.png"),520,241);
                Label(g,"200% DPI：左为中性回退；右从 CursorGuard.exe 提取真实图标（只读示例）",28,336,foreground);
                Label(g,"不同状态（同一渲染器的无窗口预览）",28,374,foreground,16);
                string[] states={"paused","protected","waiting","error"};for(int i=0;i<4;i++) Put(g,Path.Combine(previews,"preview-"+states[i]+suffix+".png"),28+(i%2)*480,412+(i/2)*60);
                Label(g,"正常设置面板",28,545,foreground,16);Put(g,Path.Combine(previews,"preview-settings"+suffix+".png"),28,578);
                Label(g,"工作区仅高 180 px：顶部按钮固定，内容滚动",450,545,foreground,16);
                Put(g,Path.Combine(native,"native-settings-short-"+palette+".png"),450,578);Put(g,Path.Combine(native,"native-settings-scrolled-"+palette+".png"),450,784);
                Label(g,"打开位置：优先下方；空间不足则上方；超高内容滚动。",28,990,foreground);
                Label(g,"点击程序图标选择目标，悬停查看完整进程本名。齿轮菜单可隐藏主界面。",28,1019,foreground);
                Label(g,"实际鼠标悬停、真实拖动及显示器 DPI 切换仍需用户复测。",28,1048,foreground);
                board.Save(Path.Combine(review,"preview-paused"+suffix+".png"),ImageFormat.Png);
            }
            using(Bitmap board=new Bitmap(1000,560)) using(Graphics g=Graphics.FromImage(board))
            {
                Color foreground=Color.FromArgb(36,38,42);g.Clear(Color.FromArgb(239,239,241));
                Label(g,"CursorGuard 1.2.4 · 悬浮条修复预览",28,20,foreground,23);Label(g,"原生客户区截图 + 运行时 alpha 曲面合成；未开启程序保护",28,65,foreground);
                string[] palettes={"light","dark"};for(int i=0;i<2;i++) {int y=112+i*152;for(int j=0;j<2;j++) {int x=j==0?28:520;g.FillRectangle(j==0?Brushes.Black:Brushes.White,x,y,450,75);Put(g,Path.Combine(native,"native-floating-"+palettes[i]+"-dpi100.png"),x+18,y+22);}Put(g,Path.Combine(native,"native-floating-"+palettes[i]+"-dpi200.png"),28,y+86);}
                Label(g,"短名 / 长名：按实际名称和状态测量宽度，长名称保留完整文件名提示",28,436,foreground);
                string[] names={"short","league","long"};for(int i=0;i<3;i++) Put(g,Path.Combine(previews,"preview-floating-name-"+names[i]+".png"),28+i*315,475);
                Label(g,"悬浮条显隐独立于主界面；无输入模拟、配置写入或窗口激活。",28,523,foreground);
                board.Save(Path.Combine(review,"preview-floating.png"),ImageFormat.Png);
            }
            return 0;
        }
    }
}
