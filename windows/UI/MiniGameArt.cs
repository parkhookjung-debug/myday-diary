using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;
namespace MyDay.Windows.UI
{
    internal static class MiniGameArt
    {
        public static void Text(Graphics g,string value,float size,Color color,float x,float y,bool bold=false){using(var font=new Font("맑은 고딕",size*1.333f,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var ink=new SolidBrush(color))g.DrawString(value,font,ink,x,y);}
        public static void Card(Graphics g,RectangleF box,Color color){using(var p=Design.Rounded(box,20))using(var b=new SolidBrush(color))g.FillPath(b,p);}
        public static void Background(Graphics g,bool pond){g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(245,247,237));using(var b=new SolidBrush(Color.FromArgb(222,233,203))){g.FillEllipse(b,-100,480,700,250);g.FillEllipse(b,450,490,700,260);}using(var b=new SolidBrush(Color.FromArgb(255,255,251))){g.FillEllipse(b,130,130,130,30);g.FillEllipse(b,173,117,80,45);g.FillEllipse(b,690,175,150,25);}if(pond){using(var b=new SolidBrush(Color.FromArgb(183,220,217)))g.FillEllipse(b,305,300,610,260);using(var p=new Pen(Color.FromArgb(216,239,230),3)){g.DrawArc(p,390,370,220,65,10,150);g.DrawArc(p,650,440,130,30,0,160);}}}
        public static void Hud(Graphics g,int score,double seconds,string third){Card(g,new RectangleF(20,15,245,64),Color.FromArgb(255,254,250));Text(g,"SCORE",9,Design.Muted,36,24,true);Text(g,score.ToString("D3"),23,Design.Ink,115,23,true);Card(g,new RectangleF(371,15,218,64),Color.FromArgb(255,254,250));Text(g,"남은 시간",9,Design.Muted,387,29);Text(g,Math.Ceiling(seconds).ToString("00")+"초",22,Design.Olive,480,23,true);Card(g,new RectangleF(705,15,235,64),Color.FromArgb(255,254,250));Text(g,third,14,Design.Ink,726,31,true);}
        public static void Pet(Graphics g,float x,float y,float width,double time,float stretch=0,float tilt=0,bool happy=false,float vertical=1){var pose=MonsterPose.ForActivity(time,PetActivity.Rest,time,0,0);pose.Bob=0;pose.WidthScale*=1+stretch;pose.BodyScale*=(1-stretch*.3f)*vertical;pose.Tilt+=tilt;pose.Happy=happy;pose.Closed=happy;MonsterPainter.Draw(g,new Rectangle((int)(x-width/2),(int)(y-width*.44f),(int)width,(int)(width*.85f)),pose,true);}
        public static void Overlay(Graphics g,string title,string body,string hint){using(var veil=new SolidBrush(Color.FromArgb(120,248,247,239)))g.FillRectangle(veil,0,90,960,510);Card(g,new RectangleF(195,185,570,230),Color.FromArgb(255,254,250));Text(g,title,25,Design.Ink,230,213,true);Text(g,body,13,Design.Muted,230,278);Text(g,hint,12,Design.Olive,230,355,true);}
        public static void Fish(Graphics g,float x,float y,float size){using(var b=new SolidBrush(Color.FromArgb(236,179,126))){g.FillEllipse(b,x-size,y-size*.45f,size*2,size);g.FillPolygon(b,new[]{new PointF(x+size*.8f,y),new PointF(x+size*1.7f,y-size*.65f),new PointF(x+size*1.7f,y+size*.65f)});}using(var b=new SolidBrush(Design.Ink))g.FillEllipse(b,x-size*.55f,y-size*.12f,size*.13f,size*.13f);}
        public static void WriteResult(string file,MiniGameResult result){if(result==null)throw new InvalidOperationException("No completed round");using(var image=new Bitmap(960,600))using(var g=Graphics.FromImage(image)){Background(g,false);Card(g,new RectangleF(35,35,890,530),Color.FromArgb(255,254,249));Text(g,"SANGMON PLAYROOM",12,Design.Muted,75,65,true);Text(g,result.Name,29,Design.Ink,75,112,true);Text(g,result.Day.ToString("yyyy.MM.dd"),12,Design.Muted,75,168);Text(g,result.Score+"점",67,Design.Olive,75,228,true);Text(g,result.Details,16,Design.Ink,80,360);Text(g,result.Challenge??"나의 작은 도전, 한 번 더!",13,Design.Muted,80,432);Text(g,"상몬과 함께 놀아요",12,Design.Muted,80,512);Pet(g,718,365,310,.5f,0,-5,true);image.Save(file,System.Drawing.Imaging.ImageFormat.Png);}}
        public static void Export(Form owner,MiniGameResult result){if(result==null)return;using(var dialog=new SaveFileDialog {Filter="PNG 결과 카드|*.png",DefaultExt="png",FileName="상몬-결과-"+result.Day.ToString("yyyyMMdd")+".png"})if(dialog.ShowDialog(owner)==DialogResult.OK){try{WriteResult(dialog.FileName,result);}catch(Exception ex){MessageBox.Show(owner,"결과 카드를 저장하지 못했어요.\n"+ex.Message,"상몬 게임");}}}
    }
    internal sealed class MiniSceneCanvas : Control
    {
        public Action<Graphics> Render;
        public Func<PointF,bool> Press;
        public Action<PointF> Drag,Release;
        public Action Cancel;
        public MiniSceneCanvas(){DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable|ControlStyles.ResizeRedraw,true);}
        protected override bool IsInputKey(Keys keyData){var k=keyData&Keys.KeyCode;return k==Keys.Up||k==Keys.Down||k==Keys.Left||k==Keys.Right||base.IsInputKey(keyData);}
        private RectangleF Box(){float s=Math.Min(Width/960f,Height/600f);return new RectangleF((Width-960*s)/2,(Height-600*s)/2,960*s,600*s);}
        private PointF World(Point p){var b=Box();return b.Width<=0?PointF.Empty:new PointF((p.X-b.X)*960/b.Width,(p.Y-b.Y)*600/b.Height);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Design.Paper);var b=Box();if(b.Width>0&&Render!=null){var state=e.Graphics.Save();e.Graphics.TranslateTransform(b.X,b.Y);e.Graphics.ScaleTransform(b.Width/960,b.Height/600);Render(e.Graphics);e.Graphics.Restore(state);}base.OnPaint(e);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();if(Press!=null&&Press(World(e.Location)))Capture=true;}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&Drag!=null)Drag(World(e.Location));}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left){if(Release!=null)Release(World(e.Location));Capture=false;}}
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&Cancel!=null)Cancel();}
    }
}
