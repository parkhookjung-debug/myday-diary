using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.Character
{
    internal sealed class DesktopBallWindow : Form
    {
        private readonly FetchGame game;
        private readonly Stopwatch clock=Stopwatch.StartNew();
        public bool MenuOpen {get{return ContextMenuStrip.Visible;}}
        public DesktopBallWindow(FetchGame game,Action stop){this.game=game;Text="MyDay 상몬 공놀이";Size=new Size(64,64);AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;AccessibleName="상몬 공놀이. 공을 드래그해서 던지세요. 우클릭으로 끝내기.";ContextMenuStrip=new ContextMenuStrip();ContextMenuStrip.Items.Add("공놀이 끝내기",null,delegate{stop();});Shown+=delegate{RefreshFrame();};FormClosed+=delegate{if(game.Active)stop();};}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80|0x8000000;return p;}}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){BeginPointer(Cursor.Position,clock.Elapsed.TotalSeconds);Capture=true;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)MovePointer(Cursor.Position,clock.Elapsed.TotalSeconds);}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left&&game.Ball.Held){EndPointer(Cursor.Position,clock.Elapsed.TotalSeconds);Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&game.Ball.Held)game.CancelDrag();}
        internal void BeginPointer(PointF p,double time){game.BeginDrag(p,time);}
        internal void MovePointer(PointF p,double time){game.Drag(p,time);}
        internal void EndPointer(PointF p,double time){game.Release(p,time);}
        internal Bitmap MakeFrame(){var image=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;float radius=(float)MiniGameRules.FetchRadius;var box=new RectangleF(Width/2f-radius,Height/2f-radius,radius*2,radius*2);using(var shadow=new SolidBrush(Color.FromArgb(30,71,78,57)))g.FillEllipse(shadow,box.X-2,box.Bottom-4,box.Width+4,8);using(var b=new LinearGradientBrush(box,Color.FromArgb(255,219,153),Color.FromArgb(226,151,101),65))g.FillEllipse(b,box);var state=g.Save();g.TranslateTransform(Width/2f,Height/2f);g.RotateTransform((game.Ball.Position.X+game.Ball.Position.Y)*.5f);using(var p=new Pen(Color.FromArgb(255,246,217),3)){g.DrawArc(p,-radius*.8f,-radius,radius*1.2f,radius*2,-70,140);g.DrawArc(p,-radius*.8f,-radius,radius*1.2f,radius*2,110,140);}g.Restore(state);using(var highlight=new SolidBrush(Color.FromArgb(170,255,255,241)))g.FillEllipse(highlight,box.X+5,box.Y+4,8,5);}return image;}
        internal void RefreshFrame(){if(IsDisposed)return;Location=new Point((int)Math.Round(game.Ball.Position.X-Width/2f),(int)Math.Round(game.Ball.Position.Y-Height/2f));if(IsHandleCreated&&Visible)using(var frame=MakeFrame())LayeredWindow.Update(Handle,frame,Location);}
        protected override void Dispose(bool disposing){if(disposing&&ContextMenuStrip!=null)ContextMenuStrip.Dispose();base.Dispose(disposing);}
    }
}
