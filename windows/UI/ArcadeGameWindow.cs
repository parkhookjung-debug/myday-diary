using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI
{
    public enum ArcadeKind { Slingshot,Fishing }
    public sealed class ArcadeGameWindow : Form
    {
        private readonly ArcadeKind kind;
        private readonly GameRecords records;
        private readonly SlingshotGame sling=new SlingshotGame();
        private readonly FishingGame fish=new FishingGame(Environment.TickCount);
        private readonly MiniSceneCanvas canvas=new MiniSceneCanvas();
        private readonly Timer timer=new Timer {Interval=16};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly Button start=Design.Button("게임 시작",true),pause=Design.Button("일시정지"),export=Design.Button("결과 카드 저장");
        private readonly Label status=Design.Label("",9);
        private bool spaceDown,restartDown,pauseDown;
        private double last;
        private MiniGameResult result;
        internal SlingshotGame Sling {get{return sling;}}
        internal FishingGame Fish {get{return fish;}}
        private SnackRoundState Phase {get{return kind==ArcadeKind.Slingshot?sling.State:fish.State;}}
        public ArcadeGameWindow(ArcadeKind kind,GameRecords records=null)
        {
            this.kind=kind;this.records=records??new GameRecords();Text="MyDay · "+(kind==ArcadeKind.Slingshot?"상몬 슬링샷":"느긋한 상몬 낚시");BackColor=Design.Paper;Font=Design.Font(10);KeyPreview=true;StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,740);MinimumSize=Design.Size(680,570);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(78)};var title=Design.Label(kind==ArcadeKind.Slingshot?"상몬 슬링샷":"느긋한 상몬 낚시",20,true);title.Location=Design.Point(26,12);header.Controls.Add(title);
            var subtitle=Design.Label(kind==ArcadeKind.Slingshot?"상몬을 뒤로 당겼다 놓아 고리를 통과해요. · 10번의 기회":"찌가 움직이면 한 번, 초록 게이지에서 한 번! · Space / 클릭",9);subtitle.ForeColor=Design.Muted;subtitle.Location=Design.Point(28,48);header.Controls.Add(subtitle);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(66),Padding=Design.Pad(20,12,8,8),WrapContents=false};start.Size=Design.Size(120,36);pause.Size=Design.Size(105,36);export.Size=Design.Size(132,36);status.Margin=Design.Pad(10,8,0,0);footer.Controls.AddRange(new Control[]{start,pause,export,status});footer.Resize+=delegate{status.Visible=footer.ClientSize.Width>=Design.P(850);};
            canvas.Dock=DockStyle.Fill;canvas.Render=DrawScene;canvas.AccessibleName=kind==ArcadeKind.Slingshot?"상몬을 당겨 놓는 고리 게임":"입질과 초록 게이지에서 누르는 낚시 게임";
            canvas.Press=BeginPointer;canvas.Drag=MovePointer;canvas.Release=EndPointer;canvas.Cancel=delegate{sling.CancelAim();};
            Controls.Add(canvas);Controls.Add(header);Controls.Add(footer);start.Click+=delegate{StartRound();};pause.Click+=delegate{TogglePause();};export.Click+=delegate{MiniGameArt.Export(this,result);};
            KeyDown+=delegate(object sender,KeyEventArgs e){e.Handled=PressKey(e.KeyCode);};KeyUp+=delegate(object sender,KeyEventArgs e){ReleaseKey(e.KeyCode);};KeyPress+=delegate(object sender,KeyPressEventArgs e){if(" pPrR".IndexOf(e.KeyChar)>=0||e.KeyChar==(char)27)e.Handled=true;};
            Deactivate+=delegate{spaceDown=restartDown=pauseDown=false;PauseRound();};timer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds;if(Phase==SnackRoundState.Playing){Advance(now-last);canvas.Invalidate();}last=now;};Shown+=delegate{last=clock.Elapsed.TotalSeconds;timer.Start();canvas.Focus();};UpdateUi();
        }
        internal bool BeginPointer(PointF point){if(kind==ArcadeKind.Fishing){Act();return false;}return sling.BeginAim(point.X,point.Y);}
        internal void MovePointer(PointF point){if(kind==ArcadeKind.Slingshot)sling.Aim(point.X,point.Y);}
        internal void EndPointer(PointF point){if(kind==ArcadeKind.Slingshot){sling.Aim(point.X,point.Y);sling.Launch();}}
        internal KeyEventArgs NativeKeyDownForTest(Keys key){var e=new KeyEventArgs(key);OnKeyDown(e);return e;}
        internal void NativeKeyUpForTest(Keys key){OnKeyUp(new KeyEventArgs(key));}
        internal void StartRound(){result=null;if(kind==ArcadeKind.Slingshot)sling.Start();else fish.Start();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        internal void Advance(double dt){if(kind==ArcadeKind.Slingshot)sling.Step(dt);else fish.Step(dt);UpdateUi();}
        internal void Act(){if(kind==ArcadeKind.Fishing){fish.Action();UpdateUi();canvas.Invalidate();}}
        internal void PauseRound(){sling.Pause();fish.Pause();canvas.Capture=false;UpdateUi();canvas.Invalidate();}
        internal void TogglePause(){if(Phase==SnackRoundState.Paused){sling.Resume();fish.Resume();}else PauseRound();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        internal bool PressKey(Keys key){if(key==Keys.Space){if(!spaceDown){spaceDown=true;if(Phase==SnackRoundState.Ready||Phase==SnackRoundState.Finished)StartRound();else if(kind==ArcadeKind.Fishing)Act();else TogglePause();}}else if(key==Keys.R){if(!restartDown){restartDown=true;StartRound();}}else if(key==Keys.P){if(!pauseDown){pauseDown=true;TogglePause();}}else if(key==Keys.Escape)PauseRound();else return false;return true;}
        internal void ReleaseKey(Keys key){if(key==Keys.Space)spaceDown=false;if(key==Keys.R)restartDown=false;if(key==Keys.P)pauseDown=false;}
        private void UpdateUi(){
            string id=kind==ArcadeKind.Slingshot?"sling":"fish";
            if(Phase==SnackRoundState.Finished&&result==null){result=new MiniGameResult{Name=kind==ArcadeKind.Slingshot?"상몬 슬링샷":"느긋한 상몬 낚시",Day=DateTime.Today,Score=kind==ArcadeKind.Slingshot?sling.Score:fish.Score,Details=kind==ArcadeKind.Slingshot?"고리 "+sling.Goals+" / 5 · 발사 "+sling.Shots+"회":"물고기 "+fish.Catches+"마리 · 완벽 "+fish.Perfects+"회"};records.Record(id,result.Score);}
            start.Text=Phase==SnackRoundState.Ready?"게임 시작":Phase==SnackRoundState.Finished?"다시 도전":"처음부터";pause.Enabled=Phase==SnackRoundState.Playing||Phase==SnackRoundState.Paused;pause.Text=Phase==SnackRoundState.Paused?"계속하기":"일시정지";export.Enabled=result!=null;status.Text=records.Problem??("최고 "+records.Best(id)+"점 · P 일시정지 / R 재시작");
        }
        internal void DrawScene(Graphics g){
            bool fishing=kind==ArcadeKind.Fishing;MiniGameArt.Background(g,fishing);MiniGameArt.Hud(g,fishing?fish.Score:sling.Score,fishing?fish.SecondsLeft:sling.SecondsLeft,fishing?"낚은 물고기 "+fish.Catches+"마리":"발사 "+sling.Shots+" / "+MiniGameRules.SlingShots);MiniGameArt.Text(g,"BEST "+records.Best(fishing?"fish":"sling"),10,Design.Muted,30,91,true);
            if(!fishing){
                using(var band=new Pen(Color.FromArgb(171,144,113),5)){g.DrawLine(band,210,448,206,535);g.DrawLine(band,240,448,245,535);if(sling.Aiming){g.DrawLine(band,210,448,(float)sling.X,(float)sling.Y);g.DrawLine(band,240,448,(float)sling.X,(float)sling.Y);}}
                if(sling.Aiming){using(var ink=new SolidBrush(Color.FromArgb(130,143,111)))for(int i=1;i<=18;i++){double t=i*.07;double x=sling.X-sling.PullX*MiniGameRules.SlingStrength*t,y=sling.Y-sling.PullY*MiniGameRules.SlingStrength*t+MiniGameRules.SlingGravity*t*t/2;if(x<SlingshotGame.MinX||x>SlingshotGame.MaxX||y<SlingshotGame.MinY||y>SlingshotGame.MaxY)break;g.FillEllipse(ink,(float)x-3,(float)y-3,6,6);}}
                if(sling.Goals<5){using(var outer=new Pen(Color.FromArgb(213,171,104),10))g.DrawEllipse(outer,(float)sling.TargetX-48,(float)sling.TargetY-48,96,96);using(var shine=new Pen(Color.FromArgb(255,240,181),3))g.DrawArc(shine,(float)sling.TargetX-46,(float)sling.TargetY-46,92,92,185,160);MiniGameArt.Text(g,"고리 "+(sling.Goals+1),11,Design.Olive,(float)sling.TargetX-22,(float)sling.TargetY+57,true);}
                float stretch=sling.Aiming?(float)(sling.Pull/MiniGameRules.SlingMaxPull*.65):sling.Impact>0?-.2f:0;MiniGameArt.Pet(g,(float)sling.X,(float)sling.Y,140,sling.Elapsed,stretch,sling.Flying?-12:0,sling.Success>0);
                if(sling.Success>0)MiniGameArt.Text(g,"쏙! 멋져요!",21,Design.Olive,390,152,true);
                MiniGameArt.Text(g,"상몬을 뒤로 당겼다가 놓기 · 점선으로 방향 확인",12,Design.Muted,270,555);
            }else {
                MiniGameArt.Pet(g,200,430,205,fish.Elapsed,fish.Phase==FishingPhase.Caught?.1f:0,0,fish.Phase==FishingPhase.Caught);
                using(var rod=new Pen(Color.FromArgb(146,116,88),5))g.DrawLine(rod,250,390,445,230);
                float bobY=370+(fish.Phase==FishingPhase.Bite?(float)Math.Sin(fish.Elapsed*30)*12:0);using(var line=new Pen(Color.FromArgb(139,159,145),1.5f))g.DrawLine(line,445,230,565,bobY);using(var b=new SolidBrush(Color.FromArgb(223,130,117)))g.FillEllipse(b,556,bobY-10,18,24);
                MiniGameArt.Text(g,fish.Message,17,Design.Olive,260,130,true);
                if(fish.Phase==FishingPhase.Bite)MiniGameArt.Text(g,"입질!",29,Color.FromArgb(194,111,88),610,280,true);
                if(fish.Phase==FishingPhase.Reeling){MiniGameArt.Card(g,new RectangleF(330,415,520,94),Color.FromArgb(255,254,248));using(var bar=new SolidBrush(Color.FromArgb(227,225,211)))g.FillRectangle(bar,350,450,480,22);using(var zone=new SolidBrush(Color.FromArgb(154,183,115)))g.FillRectangle(zone,(float)(350+(fish.Zone-MiniGameRules.FishZoneWidth/2)*480),443,(float)(MiniGameRules.FishZoneWidth*480),36);using(var pen=new Pen(Design.Accent,6))g.DrawLine(pen,(float)(350+fish.Indicator*480),436,(float)(350+fish.Indicator*480),487);}
                if(fish.Phase==FishingPhase.Caught){MiniGameArt.Fish(g,630,315,60);MiniGameArt.Text(g,fish.FishName,17,Design.Olive,565,250,true);}
                MiniGameArt.Text(g,"Space / 물가 클릭 : 던지기 → 입질 → 초록 칸에서 누르기",12,Design.Muted,235,555);
            }
            if(Phase==SnackRoundState.Ready)MiniGameArt.Overlay(g,fishing?"느긋한 상몬 낚시":"상몬 슬링샷",fishing?"찌가 움직일 때 누르고, 초록 칸에서 한 번 더!":"상몬을 잡고 뒤로 당겨 고리 안으로 날려보세요.","아래 게임 시작 또는 Space · 한 판 60초");
            else if(Phase==SnackRoundState.Paused)MiniGameArt.Overlay(g,"잠깐 쉬는 중","다른 창으로 이동하면 자동으로 멈춰요.","계속하기 또는 P로 돌아와요.");
            else if(Phase==SnackRoundState.Finished)MiniGameArt.Overlay(g,"오늘의 놀이 완료!",result==null?"":result.Score+"점 · "+result.Details,"결과 카드로 기록하기 · R로 다시 도전");
        }
        internal void SavePreview(string path){using(var bmp=new Bitmap(960,600))using(var g=Graphics.FromImage(bmp)){DrawScene(g);bmp.Save(path);}}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
