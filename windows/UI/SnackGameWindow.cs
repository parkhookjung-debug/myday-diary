using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;

namespace MyDay.Windows.UI
{
    public sealed class SnackGameWindow : Form
    {
        private readonly SnackGame game=new SnackGame(Environment.TickCount);
        private readonly SnackGameCanvas canvas;
        private readonly Timer timer=new Timer {Interval=16};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly Button start=Design.Button("게임 시작",true),pause=Design.Button("일시정지");
        private readonly Label hint=Design.Label("마우스 / ← → / A D 이동   ·   Space 일시정지   ·   R 다시 시작",9);
        private bool left,right,spaceDown,restartDown;
        private double last;
        internal SnackGame Game {get{return game;}}
        public SnackGameWindow()
        {
            Text="MyDay · 상몬의 간식 산책";BackColor=Design.Paper;Font=Design.Font(10);
            StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,740);MinimumSize=Design.Size(680,570);KeyPreview=true;
            var titleBar=new Panel {Dock=DockStyle.Top,Height=Design.P(78),BackColor=Design.Paper};
            var title=Design.Label("상몬의 간식 산책",20,true);title.Location=Design.Point(26,12);titleBar.Controls.Add(title);
            var subtitle=Design.Label("먹을거리는 냠냠, 나머지는 쏙쏙!  ·  "+SnackRules.RoundSeconds+"초의 작은 산책",9);subtitle.ForeColor=Design.Muted;subtitle.Location=Design.Point(28,48);titleBar.Controls.Add(subtitle);
            var footer=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=Design.P(62),Padding=Design.Pad(20,12,8,8),WrapContents=false,BackColor=Design.Paper};
            start.Size=Design.Size(124,36);pause.Size=Design.Size(108,36);hint.Margin=Design.Pad(12,9,0,0);footer.Controls.AddRange(new Control[]{start,pause,hint});
            canvas=new SnackGameCanvas(game){Dock=DockStyle.Fill};Controls.Add(canvas);Controls.Add(titleBar);Controls.Add(footer);
            start.Click+=delegate {StartRound();};pause.Click+=delegate {TogglePause();};
            canvas.MouseDown+=delegate {canvas.Focus();};
            KeyDown+=delegate(object sender,KeyEventArgs e){e.SuppressKeyPress=PressGameKey(e.KeyCode);};
            KeyUp+=delegate(object sender,KeyEventArgs e){ReleaseGameKey(e.KeyCode);};
            Deactivate+=delegate {spaceDown=restartDown=false;PauseRound();};
            timer.Tick+=delegate {double now=clock.Elapsed.TotalSeconds;if(game.State!=SnackRoundState.Playing){last=now;return;}game.Step(now-last,(right?1:0)-(left?1:0));last=now;UpdateControls();canvas.Invalidate();};
            Shown+=delegate {last=clock.Elapsed.TotalSeconds;timer.Start();canvas.Focus();};UpdateControls();
        }
        internal bool PressGameKey(Keys key)
        {
            if(key==Keys.Left||key==Keys.A)left=true;
            else if(key==Keys.Right||key==Keys.D)right=true;
            else if(key==Keys.Space){if(!spaceDown){spaceDown=true;if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished)StartRound();else TogglePause();}}
            else if(key==Keys.R){if(!restartDown){restartDown=true;StartRound();}}
            else if(key==Keys.Escape)PauseRound();
            else return false;
            return true;
        }
        internal void ReleaseGameKey(Keys key){if(key==Keys.Left||key==Keys.A)left=false;if(key==Keys.Right||key==Keys.D)right=false;if(key==Keys.Space)spaceDown=false;if(key==Keys.R)restartDown=false;}
        internal void StartRound(){left=right=false;game.Start();last=clock.Elapsed.TotalSeconds;UpdateControls();canvas.Focus();canvas.Invalidate();}
        internal void PauseRound(){left=right=false;game.Pause();UpdateControls();canvas.Invalidate();}
        internal void TogglePause(){left=right=false;if(game.State==SnackRoundState.Paused)game.Resume();else game.Pause();last=clock.Elapsed.TotalSeconds;UpdateControls();canvas.Focus();canvas.Invalidate();}
        private void UpdateControls(){start.Text=game.State==SnackRoundState.Ready?"게임 시작":game.State==SnackRoundState.Finished?"다시 도전":"처음부터";pause.Text=game.State==SnackRoundState.Paused?"계속하기":"일시정지";pause.Enabled=game.State==SnackRoundState.Playing||game.State==SnackRoundState.Paused;}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
        internal void SavePreview(string file){using(var image=new Bitmap(960,600))using(var g=Graphics.FromImage(image)){SnackGameCanvas.DrawScene(g,game);image.Save(file);}}
    }
    internal sealed class SnackGameCanvas : Control
    {
        private readonly SnackGame game;
        public SnackGameCanvas(SnackGame game){this.game=game;DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable|ControlStyles.ResizeRedraw,true);AccessibleName="상몬 간식 게임. 마우스와 좌우 방향키로 이동";MouseMove+=delegate(object sender,MouseEventArgs e){var box=SceneBox();if(box.Contains(e.Location))game.Aim((e.X-box.Left)*960/box.Width);};}
        protected override bool IsInputKey(Keys keyData){var k=keyData&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||base.IsInputKey(keyData);}
        private RectangleF SceneBox(){float scale=Math.Min(Width/960f,Height/600f);return new RectangleF((Width-960*scale)/2,(Height-600*scale)/2,960*scale,600*scale);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Design.Paper);var box=SceneBox();if(box.Width<=0)return;var state=e.Graphics.Save();e.Graphics.TranslateTransform(box.Left,box.Top);e.Graphics.ScaleTransform(box.Width/960,box.Height/600);DrawScene(e.Graphics,game);e.Graphics.Restore(state);base.OnPaint(e);}
        private static void Fill(Graphics g,Color color,RectangleF box,float radius){using(var p=Design.Rounded(box,radius))using(var b=new SolidBrush(color))g.FillPath(b,p);}
        private static void DrawLabel(Graphics g,string text,float size,Color color,float x,float y,bool bold=false){using(var font=new Font("맑은 고딕",size*1.333f,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(color))g.DrawString(text,font,b,x,y);}
        public static void DrawScene(Graphics g,SnackGame game)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(244,247,238));
            using(var sky=new LinearGradientBrush(new Rectangle(0,80,960,500),Color.FromArgb(244,247,238),Color.FromArgb(231,239,217),90))g.FillRectangle(sky,0,80,960,520);
            using(var grass=new SolidBrush(Color.FromArgb(220,231,204))){g.FillEllipse(grass,-120,498,630,220);g.FillEllipse(grass,350,520,850,220);}
            using(var b=new SolidBrush(Color.FromArgb(255,255,252))){g.FillEllipse(b,90,136,90,24);g.FillEllipse(b,128,125,66,38);g.FillEllipse(b,745,194,85,22);g.FillEllipse(b,774,184,58,33);}
            for(int i=0;i<16;i++){float x=28+i*61;using(var p=new Pen(Color.FromArgb(180,198,156),2)){g.DrawLine(p,x,579,x+3,568);g.DrawLine(p,x+3,575,x+9,571);}}
            Fill(g,Color.FromArgb(255,254,250),new RectangleF(20,15,176,60),17);DrawLabel(g,"SCORE",9,Design.Muted,36,24,true);DrawLabel(g,game.Score.ToString("D3"),22,Design.Ink,100,22,true);
            Fill(g,Color.FromArgb(255,254,250),new RectangleF(371,15,218,60),17);DrawLabel(g,"남은 시간",9,Design.Muted,388,26);DrawLabel(g,Math.Ceiling(game.SecondsLeft).ToString("00")+"초",22,Design.Olive,480,22,true);
            Fill(g,Color.FromArgb(255,254,250),new RectangleF(754,15,186,60),17);DrawLabel(g,"목숨",10,Design.Muted,770,34);
            for(int i=0;i<SnackRules.StartingLives;i++)Heart(g,824+i*32,43,i<game.Lives?Color.FromArgb(219,128,118):Color.FromArgb(225,225,216));
            foreach(var drop in game.Drops)DrawItem(g,drop.Kind,(float)drop.X,(float)drop.Y,1);
            bool visible=game.Invulnerable<=0||((int)(game.Invulnerable*12))%2==0;
            if(visible){var pose=MonsterPose.ForActivity(game.Elapsed,PetActivity.Rest,game.Elapsed,0,-.3f);pose.Bob=0;pose.Closed=game.Bite>0;pose.Happy=game.Bite>0;pose.WidthScale*=game.Hit>0?1.11f:game.Bite>0?1.06f:1;pose.BodyScale*=game.Hit>0?.88f:1;MonsterPainter.Draw(g,new Rectangle((int)game.PlayerX-100,400,200,170),pose,true);}
            if(game.Bite>0)DrawLabel(g,"냠! +"+SnackRules.FoodPoints,15,Design.Olive,(float)game.PlayerX-32,383-(float)(.45-game.Bite)*30,true);
            if(game.Hit>0)DrawLabel(g,"앗! 조심",13,Color.FromArgb(157,100,103),(float)game.PlayerX-35,370,true);
            DrawLabel(g,"사과 · 딸기 · 쿠키 +"+SnackRules.FoodPoints+"점",11,Design.Olive,24,92,true);DrawLabel(g,"돌 · 가시는 피해주세요",11,Design.Muted,722,92);
            if(game.State!=SnackRoundState.Playing){
                using(var veil=new SolidBrush(Color.FromArgb(125,248,247,240)))g.FillRectangle(veil,0,80,960,520);
                Fill(g,Color.FromArgb(225,231,215),new RectangleF(236,191,500,242),24);Fill(g,Color.FromArgb(255,254,249),new RectangleF(230,184,500,242),24);
                string title=game.State==SnackRoundState.Ready?"상몬과 간식 산책":game.State==SnackRoundState.Paused?"잠깐 쉬는 중":game.Lives>0?SnackRules.RoundSeconds+"초 산책 완료!":"앗, 잠깐 쉬어갈까?";
                DrawLabel(g,title,25,Design.Ink,270,209,true);
                if(game.State==SnackRoundState.Ready){DrawLabel(g,"맛있는 간식은 먹고, 돌과 가시는 피해요.",13,Design.Muted,270,265);DrawItem(g,SnackKind.Apple,295,323,1.15f);DrawItem(g,SnackKind.Strawberry,350,323,1.15f);DrawItem(g,SnackKind.Cookie,405,323,1.15f);DrawLabel(g,"+10",13,Design.Olive,436,309,true);DrawItem(g,SnackKind.Rock,566,323,1.15f);DrawItem(g,SnackKind.Thorn,621,323,1.15f);DrawLabel(g,"피하기",11,Design.Muted,653,313);DrawLabel(g,"아래 게임 시작 또는 Space를 눌러주세요.",12,Design.Muted,270,375);}
                else if(game.State==SnackRoundState.Paused){DrawLabel(g,"창을 벗어나면 자동으로 멈춰요.",13,Design.Muted,270,277);DrawLabel(g,"계속하기 또는 Space로 돌아와요.",13,Design.Olive,270,340,true);}
                else {DrawLabel(g,game.Score+"점  ·  간식 "+game.Eaten+"개",22,Design.Olive,270,278,true);DrawLabel(g,"다시 도전 또는 R로 한 번 더!",13,Design.Muted,270,352);}
            }
        }
        private static void Heart(Graphics g,float x,float y,Color color){using(var p=new GraphicsPath()){p.AddBezier(x,y+6,x-22,y-5,x-11,y-20,x,y-9);p.AddBezier(x,y-9,x+11,y-20,x+22,y-5,x,y+6);using(var b=new SolidBrush(color))g.FillPath(b,p);}}
        internal static void DrawItem(Graphics g,SnackKind kind,float x,float y,float size)
        {
            var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(size,size);
            using(var shadow=new SolidBrush(Color.FromArgb(24,83,100,68)))g.FillEllipse(shadow,-17,17,35,6);
            if(kind==SnackKind.Apple){using(var b=new SolidBrush(Color.FromArgb(227,126,111))){g.FillEllipse(b,-18,-13,23,32);g.FillEllipse(b,-4,-13,23,32);}using(var p=new Pen(Color.FromArgb(119,89,65),3))g.DrawLine(p,0,-10,2,-22);using(var b=new SolidBrush(Color.FromArgb(141,168,103)))g.FillEllipse(b,2,-23,16,8);using(var p=new Pen(Color.FromArgb(249,181,157),3))g.DrawArc(p,-13,-9,12,21,130,100);}
            else if(kind==SnackKind.Strawberry){using(var p=new GraphicsPath()){p.AddBezier(-18,-10,-28,2,-2,27,0,24);p.AddBezier(0,24,13,22,25,4,18,-10);p.AddBezier(18,-10,7,-22,-8,-20,-18,-10);using(var b=new SolidBrush(Color.FromArgb(220,128,137)))g.FillPath(b,p);}using(var b=new SolidBrush(Color.FromArgb(137,164,98))){g.FillPolygon(b,new[]{new Point(-18,-14),new Point(-3,-13),new Point(0,-24),new Point(5,-13),new Point(19,-15),new Point(8,-5),new Point(-5,-7)});}using(var b=new SolidBrush(Color.FromArgb(255,233,186)))foreach(var p in new[]{new Point(-10,-1),new Point(3,-2),new Point(-4,9),new Point(10,7),new Point(0,17)})g.FillEllipse(b,p.X,p.Y,2,4);}
            else if(kind==SnackKind.Cookie){using(var b=new SolidBrush(Color.FromArgb(220,181,126)))g.FillEllipse(b,-20,-20,40,40);using(var p=new Pen(Color.FromArgb(196,151,98),2))g.DrawEllipse(p,-18,-18,36,36);using(var b=new SolidBrush(Color.FromArgb(126,93,69)))foreach(var p in new[]{new Point(-9,-7),new Point(6,-11),new Point(1,4),new Point(-12,8),new Point(11,10)})g.FillEllipse(b,p.X-3,p.Y-3,6,6);}
            else if(kind==SnackKind.Rock){var points=new[]{new Point(-23,8),new Point(-17,-13),new Point(0,-22),new Point(19,-11),new Point(23,13),new Point(4,21),new Point(-15,18)};using(var b=new SolidBrush(Color.FromArgb(153,159,158)))g.FillPolygon(b,points);using(var p=new Pen(Color.FromArgb(117,125,124),2))g.DrawPolygon(p,points);using(var p=new Pen(Color.FromArgb(197,203,196),3))g.DrawLines(p,new[]{new Point(-12,-6),new Point(0,-14),new Point(10,-6)});}
            else {var pts=new PointF[20];for(int i=0;i<pts.Length;i++){double a=i*Math.PI/10;float radius=i%2==0?25:17;pts[i]=new PointF((float)Math.Cos(a)*radius,(float)Math.Sin(a)*radius);}using(var b=new SolidBrush(Color.FromArgb(154,137,164)))g.FillPolygon(b,pts);using(var b=new SolidBrush(Color.FromArgb(186,173,193)))g.FillEllipse(b,-15,-15,30,30);using(var p=new Pen(Color.FromArgb(116,100,127),3)){g.DrawLine(p,-5,-5,5,5);g.DrawLine(p,5,-5,-5,5);}}
            g.Restore(state);
        }
    }
}
