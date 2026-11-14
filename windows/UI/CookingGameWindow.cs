using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;
namespace MyDay.Windows.UI
{
    public sealed class CookingGameWindow : Form
    {
        private readonly CookingGame game=new CookingGame();
        private readonly GameRecords records;
        private readonly MiniSceneCanvas canvas=new MiniSceneCanvas();
        private readonly Timer timer=new Timer{Interval=16};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly HashSet<Keys> held=new HashSet<Keys>();
        private readonly Button start=Design.Button("요리 시작",true),pause=Design.Button("일시정지"),export=Design.Button("결과 카드 저장"),fire=Design.Button("꾹 눌러 불 뿜기",true);
        private readonly Label status=Design.Label("",9);
        private int source;
        private bool closing;
        private double last;
        private MiniGameResult result;
        internal CookingGame Game {get{return game;}}
        public CookingGameWindow(GameRecords records=null)
        {
            this.records=records??new GameRecords();Text="MyDay · 상몬 불꽃 요리";BackColor=Design.Paper;Font=Design.Font(10);KeyPreview=true;StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,740);MinimumSize=Design.Size(680,570);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(78)};var title=Design.Label("상몬 불꽃 요리",20,true);title.Location=Design.Point(26,12);header.Controls.Add(title);var note=Design.Label("Space / 마우스 꾹 누르기 → 황금색 구간에서 놓기 · 60초 안에 간식 8개",9);note.ForeColor=Design.Muted;note.Location=Design.Point(28,48);header.Controls.Add(note);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(66),Padding=Design.Pad(20,12,8,8),WrapContents=false};start.Size=Design.Size(110,36);pause.Size=Design.Size(100,36);export.Size=Design.Size(132,36);fire.Size=Design.Size(154,36);status.Margin=Design.Pad(10,8,0,0);footer.Controls.AddRange(new Control[]{start,pause,fire,export,status});footer.Resize+=delegate{status.Visible=footer.ClientSize.Width>=Design.P(1100);};
            canvas.Dock=DockStyle.Fill;canvas.AccessibleName="누르고 있다 황금색 구간에서 놓아 간식을 굽는 상몬 불꽃 요리";canvas.Render=DrawScene;canvas.Press=p=>BeginPointer();canvas.Release=p=>EndHeat(2);canvas.Cancel=delegate{if(source==2)CancelInput();};Controls.Add(canvas);Controls.Add(header);Controls.Add(footer);
            start.Click+=delegate{StartRound();};pause.Click+=delegate{TogglePause();};export.Click+=delegate{MiniGameArt.Export(this,result);};
            fire.MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)BeginHeat(2);};fire.MouseUp+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)EndHeat(2);};fire.MouseCaptureChanged+=delegate{if(!fire.Capture&&source==2)CancelInput();};
            KeyDown+=delegate(object s,KeyEventArgs e){e.Handled=PressKey(e.KeyCode);};KeyUp+=delegate(object s,KeyEventArgs e){held.Remove(e.KeyCode);if(e.KeyCode==Keys.Space)EndHeat(1);};KeyPress+=delegate(object s,KeyPressEventArgs e){if(" pPrR".IndexOf(e.KeyChar)>=0||e.KeyChar==(char)27)e.Handled=true;};
            Deactivate+=delegate{held.Clear();PauseRound();};timer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds;if(game.State==SnackRoundState.Playing){Advance(now-last);canvas.Invalidate();}last=now;};Shown+=delegate{last=clock.Elapsed.TotalSeconds;timer.Start();canvas.Focus();};UpdateUi();
        }
        internal void StartRound(){CancelInput();result=null;game.Start();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        private void ClearCapture(){source=0;canvas.Capture=false;fire.Capture=false;}
        internal void CancelInput(){if(closing||source==0)return;ClearCapture();game.CancelHeat();canvas.Invalidate();}
        private bool BeginHeat(int input){if(source!=0||!game.BeginHeat())return false;source=input;canvas.Invalidate();return true;}
        private void EndHeat(int input){if(source!=input)return;ClearCapture();game.ReleaseHeat();UpdateUi();canvas.Invalidate();}
        internal bool BeginPointer(){if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished){StartRound();return false;}return BeginHeat(2);}
        internal void EndPointer(){EndHeat(2);}
        internal void LoseFocusForTest(){OnDeactivate(EventArgs.Empty);}
        internal void CaptureLossForTest(){canvas.Capture=true;canvas.Capture=false;}
        internal void ButtonCaptureLossForTest(){fire.Capture=true;fire.Capture=false;}
        internal void VerifyFooterForTest(){foreach(var button in new[]{start,pause,fire,export})if(!button.Parent.ClientRectangle.Contains(button.Bounds))throw new InvalidOperationException("Cooking footer button clipped");}
        internal void Advance(double seconds){game.Step(seconds);if(!game.Breathing)ClearCapture();UpdateUi();}
        internal void PauseRound(){CancelInput();game.Pause();UpdateUi();canvas.Invalidate();}
        internal void TogglePause(){if(game.State==SnackRoundState.Paused)game.Resume();else PauseRound();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        private bool PressKey(Keys key){if(key!=Keys.Space&&key!=Keys.P&&key!=Keys.R&&key!=Keys.Escape)return false;if(!held.Add(key))return true;if(key==Keys.Space){if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished)StartRound();else BeginHeat(1);}else if(key==Keys.P)TogglePause();else if(key==Keys.R)StartRound();else PauseRound();return true;}
        internal KeyEventArgs NativeKeyDownForTest(Keys key){var e=new KeyEventArgs(key);OnKeyDown(e);return e;}
        internal void NativeKeyUpForTest(Keys key){OnKeyUp(new KeyEventArgs(key));}
        private void UpdateUi(){if(game.State==SnackRoundState.Finished&&result==null){result=new MiniGameResult{Name="상몬 불꽃 요리",Day=DateTime.Today,Score=game.Score,Details="완성 "+game.Cooked+"개 · 완벽 "+game.Perfects+"개 · 주문 "+game.Completed+"개"};records.Record("bake",game.Score);}start.Text=game.State==SnackRoundState.Ready?"요리 시작":game.State==SnackRoundState.Finished?"다시 굽기":"처음부터";pause.Text=game.State==SnackRoundState.Paused?"계속하기":"일시정지";pause.Enabled=game.State==SnackRoundState.Playing||game.State==SnackRoundState.Paused;fire.Enabled=game.State==SnackRoundState.Playing&&game.Phase!=CookingPhase.Feedback;export.Enabled=result!=null;status.Text=records.Problem??("최고 "+records.Best("bake")+"점");}
        internal void DrawScene(Graphics g)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(248,242,229));
            using(var grout=new Pen(Color.FromArgb(235,226,209),1)){for(int x=0;x<960;x+=80)g.DrawLine(grout,x,130,x,390);for(int y=150;y<390;y+=60)g.DrawLine(grout,0,y,960,y);}
            MiniGameArt.Hud(g,game.Score,game.SecondsLeft,"주문 "+Math.Min(MiniGameRules.BakeOrders,game.OrderIndex+1)+" / "+MiniGameRules.BakeOrders);
            MiniGameArt.Text(g,"BEST "+records.Best("bake"),10,Design.Muted,30,94,true);MiniGameArt.Text(g,game.Recipe+(game.Phase==CookingPhase.Feedback?" · 결과":" 굽는 중"),20,Design.Ink,564,121,true);
            using(var table=new SolidBrush(Color.FromArgb(223,202,171)))g.FillRectangle(table,0,381,960,14);
            MiniGameArt.Card(g,new RectangleF(493,213,390,190),Color.FromArgb(239,225,203));
            using(var plate=new SolidBrush(Color.FromArgb(255,254,249)))g.FillEllipse(plate,548,336,278,45);
            CookingArt.Food(g,game.RecipeIndex,game.Heat,683,294,1.15f);
            if(game.Breathing){float wave=(float)Math.Sin(game.Elapsed*24)*9;using(var flame=new GraphicsPath()){flame.AddBezier(310,307,407,270+wave,510,345,577,306);flame.AddBezier(577,306,535,380,407,365-wave,310,330);flame.CloseFigure();using(var brush=new SolidBrush(Color.FromArgb(230,246,173,88)))g.FillPath(brush,flame);}using(var core=new SolidBrush(Color.FromArgb(255,235,160)))g.FillEllipse(core,357,310,155,25);}
            var pose=MonsterPose.ForActivity(game.Elapsed,PetActivity.Rest,game.Elapsed,0,0);pose.Happy=game.Outcome==BakeOutcome.Good||game.Outcome==BakeOutcome.Perfect;pose.Closed=pose.Happy;pose.Curious=game.Outcome==BakeOutcome.Under||game.Outcome==BakeOutcome.Burnt;pose.EyeScale=game.Outcome==BakeOutcome.Burnt?1.2f:1;pose.FireStrength=game.Breathing?.7f:0;pose.Tilt=game.Breathing?-4:pose.Happy?4:0;MonsterPainter.Draw(g,new Rectangle(36,185,380,285),pose,false);
            if(game.Outcome==BakeOutcome.Burnt){using(var smoke=new Pen(Color.FromArgb(150,134,124),5)){g.DrawArc(smoke,638,195,24,55,170,170);g.DrawArc(smoke,682,177,22,55,170,170);}MiniGameArt.Text(g,"앗, 뜨거워!",14,Design.Muted,650,190,true);}
            MiniGameArt.Text(g,"굽기",12,Design.Muted,180,442,true);MiniGameArt.Card(g,new RectangleF(265,440,600,30),Color.FromArgb(225,216,197));float zone=(float)game.Target;MiniGameArt.Card(g,new RectangleF(265+(zone-(float)MiniGameRules.BakeTargetWidth/2)*6,440,(float)MiniGameRules.BakeTargetWidth*6,30),Color.FromArgb(235,192,101));MiniGameArt.Card(g,new RectangleF(265+(zone-(float)MiniGameRules.BakePerfectWidth/2)*6,440,(float)MiniGameRules.BakePerfectWidth*6,30),Color.FromArgb(157,181,125));
            if(game.Phase!=CookingPhase.ReadyItem){float x=265+(float)game.Heat*6;using(var marker=new Pen(Design.Ink,4))g.DrawLine(marker,x,434,x,477);}
            MiniGameArt.Text(g,"덜 익음",10,Design.Muted,265,482);MiniGameArt.Text(g,"황금색에서 놓기 · 초록색은 완벽!",10,Design.Olive,471,482,true);MiniGameArt.Text(g,"타요!",10,Design.Muted,825,482);
            MiniGameArt.Text(g,game.Message,14,Design.Ink,180,531,true);MiniGameArt.Text(g,"Space / 마우스 꾹 → 놓기 · P / Esc 정지 · R 다시 시작",10,Design.Muted,245,571);
            if(game.State==SnackRoundState.Ready)MiniGameArt.Overlay(g,"상몬의 작은 간식 가게","불을 꾹 뿜어 쿠키, 식빵, 마시멜로를 구워요.","시작 후 꾹 누르기 → 황금색에서 놓기 · 완벽 +50점");else if(game.State==SnackRoundState.Paused)MiniGameArt.Overlay(g,"불을 끄고 쉬는 중","굽던 간식은 다시 구울 수 있어요. 주문은 줄지 않아요.","계속하기 또는 P · 다시 누르면 불이 나와요.");else if(game.State==SnackRoundState.Finished)MiniGameArt.Overlay(g,"오늘의 간식 가게 마감!",result==null?"":result.Score+"점 · "+result.Details,"결과 카드로 남기기 · R로 다시 굽기");
        }
        internal void SavePreview(string path){using(var image=new Bitmap(960,600))using(var g=Graphics.FromImage(image)){DrawScene(g);image.Save(path);}}
        protected override void Dispose(bool disposing){if(disposing){closing=true;source=0;game.CancelHeat();timer.Dispose();}base.Dispose(disposing);}
    }
    internal static class CookingArt
    {
        public static void Food(Graphics g,int kind,double heat,float x,float y,float scale)
        {
            var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);float t=(float)Math.Max(0,Math.Min(1,(heat-25)/65));Color color=Color.FromArgb((int)(252-65*t),(int)(231-95*t),(int)(187-108*t));if(heat>90)color=Color.FromArgb(99,80,67);
            using(var b=new SolidBrush(color))using(var p=new Pen(Color.FromArgb(158,120,83),3)){
                if(kind==0){g.FillEllipse(b,-62,-60,124,120);g.DrawEllipse(p,-62,-60,124,120);using(var chip=new SolidBrush(Color.FromArgb(114,83,65))){float[] xs={-28,8,34,-11,30,-34};float[] ys={-25,-35,0,10,33,30};for(int i=0;i<xs.Length;i++)g.FillEllipse(chip,xs[i],ys[i],10,8);}}
                else if(kind==1){using(var bread=new GraphicsPath()){bread.AddArc(-65,-65,130,85,180,180);bread.AddLine(65,-22,55,57);bread.AddLine(55,57,-55,57);bread.CloseFigure();g.FillPath(b,bread);g.DrawPath(p,bread);}using(var crumb=new SolidBrush(Color.FromArgb(80,255,249,215)))g.FillEllipse(crumb,-38,-30,76,62);}
                else{using(var stick=new Pen(Color.FromArgb(162,124,80),5))g.DrawLine(stick,-95,15,100,15);for(int i=-1;i<=1;i++){var box=new RectangleF(i*44-20,-35,40,74);using(var shape=Design.Rounded(box,12)){g.FillPath(b,shape);g.DrawPath(p,shape);}}}
            }g.Restore(state);
        }
    }
}
