using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI
{
    public sealed class JumpGameWindow : Form
    {
        private readonly JumpGame game=new JumpGame(Environment.TickCount);
        private readonly GameRecords records;
        private readonly MiniSceneCanvas canvas=new MiniSceneCanvas();
        private readonly Timer timer=new Timer{Interval=16};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly HashSet<Keys> held=new HashSet<Keys>();
        private readonly Button start=Design.Button("게임 시작",true),pause=Design.Button("일시정지"),export=Design.Button("결과 카드 저장");
        private readonly Label status=Design.Label("",9);
        private double last;
        private MiniGameResult result;
        internal JumpGame Game {get{return game;}}
        public JumpGameWindow(GameRecords records=null)
        {
            this.records=records??new GameRecords();Text="MyDay · 상몬 통통 점프";BackColor=Design.Paper;Font=Design.Font(10);KeyPreview=true;StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,740);MinimumSize=Design.Size(680,570);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(78)};var title=Design.Label("상몬 통통 점프",20,true);title.Location=Design.Point(26,12);header.Controls.Add(title);var note=Design.Label("Space / ↑ / 클릭으로 점프 · 장애물 +20점, 간식 +10점",9);note.ForeColor=Design.Muted;note.Location=Design.Point(28,48);header.Controls.Add(note);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(66),Padding=Design.Pad(20,12,8,8),WrapContents=false};start.Size=Design.Size(120,36);pause.Size=Design.Size(105,36);export.Size=Design.Size(132,36);status.Margin=Design.Pad(10,8,0,0);footer.Controls.AddRange(new Control[]{start,pause,export,status});footer.Resize+=delegate{status.Visible=footer.ClientSize.Width>=Design.P(850);};
            canvas.Dock=DockStyle.Fill;canvas.AccessibleName="Space 또는 위쪽 방향키, 클릭으로 뛰어넘는 상몬 점프 게임";canvas.Render=DrawScene;canvas.Press=p=>{ActionJump();return false;};Controls.Add(canvas);Controls.Add(header);Controls.Add(footer);
            start.Click+=delegate{StartRound();};pause.Click+=delegate{TogglePause();};export.Click+=delegate{MiniGameArt.Export(this,result);};
            KeyDown+=delegate(object sender,KeyEventArgs e){e.Handled=PressKey(e.KeyCode);};KeyUp+=delegate(object sender,KeyEventArgs e){held.Remove(e.KeyCode);};KeyPress+=delegate(object sender,KeyPressEventArgs e){if(" wWpPrR".IndexOf(e.KeyChar)>=0||e.KeyChar==(char)27)e.Handled=true;};
            Deactivate+=delegate{held.Clear();PauseRound();};timer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds;if(game.State==SnackRoundState.Playing){Advance(now-last);canvas.Invalidate();}last=now;};Shown+=delegate{last=clock.Elapsed.TotalSeconds;timer.Start();canvas.Focus();};UpdateUi();
        }
        internal void StartRound(){result=null;game.Start();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        internal void Advance(double seconds){game.Step(seconds);UpdateUi();}
        internal void ActionJump(){if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished)StartRound();else game.Jump();canvas.Invalidate();}
        internal void PauseRound(){game.Pause();UpdateUi();canvas.Invalidate();}
        internal void TogglePause(){if(game.State==SnackRoundState.Paused)game.Resume();else game.Pause();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        internal bool PressKey(Keys key){if(key!=Keys.Space&&key!=Keys.Up&&key!=Keys.W&&key!=Keys.P&&key!=Keys.R&&key!=Keys.Escape)return false;if(!held.Add(key))return true;if(key==Keys.Space||key==Keys.Up||key==Keys.W)ActionJump();else if(key==Keys.P)TogglePause();else if(key==Keys.R)StartRound();else PauseRound();return true;}
        internal KeyEventArgs NativeKeyDownForTest(Keys key){var e=new KeyEventArgs(key);OnKeyDown(e);return e;}
        internal void NativeKeyUpForTest(Keys key){OnKeyUp(new KeyEventArgs(key));}
        internal void PointerForTest(){canvas.Press(PointF.Empty);}
        private void UpdateUi(){if(game.State==SnackRoundState.Finished&&result==null){result=new MiniGameResult{Name="상몬 통통 점프",Day=DateTime.Today,Score=game.Score,Details="장애물 "+game.Cleared+"개 · 간식 "+game.Eaten+"개"};records.Record("jump",game.Score);}start.Text=game.State==SnackRoundState.Ready?"게임 시작":game.State==SnackRoundState.Finished?"다시 도전":"처음부터";pause.Text=game.State==SnackRoundState.Paused?"계속하기":"일시정지";pause.Enabled=game.State==SnackRoundState.Playing||game.State==SnackRoundState.Paused;export.Enabled=result!=null;status.Text=records.Problem??("최고 "+records.Best("jump")+"점 · P 정지 / R 재시작");}
        internal void DrawScene(Graphics g){
            MiniGameArt.Background(g,false);MiniGameArt.Hud(g,game.Score,game.SecondsLeft,"목숨 "+game.Lives+" / "+MiniGameRules.JumpLives);MiniGameArt.Text(g,"BEST "+records.Best("jump"),10,Design.Muted,30,91,true);MiniGameArt.Text(g,"넘은 장애물 "+game.Cleared+" · 먹은 간식 "+game.Eaten,12,Design.Olive,365,104,true);
            using(var line=new Pen(Color.FromArgb(180,199,151),3))g.DrawLine(line,20,(float)JumpGame.Ground,940,(float)JumpGame.Ground);
            foreach(var item in game.Items){if(item.Food){if(!item.Resolved)SnackGameCanvas.DrawItem(g,SnackKind.Strawberry,(float)item.X,(float)(JumpGame.Ground-item.Height),1);}else{float x=(float)item.X,y=(float)(JumpGame.Ground-item.Height),w=(float)item.Width,h=(float)item.Height;using(var rock=new SolidBrush(item.Resolved?Color.FromArgb(186,193,177):Color.FromArgb(155,171,148)))g.FillPolygon(rock,new[]{new PointF(x,y+h),new PointF(x+3,y+h*.3f),new PointF(x+w*.35f,y),new PointF(x+w*.8f,y+5),new PointF(x+w,y+h)});using(var shine=new Pen(Color.FromArgb(219,228,205),3))g.DrawLine(shine,x+8,y+17,x+w*.4f,y+9);}}
            using(var shadow=new SolidBrush(Color.FromArgb(24,86,95,70)))g.FillEllipse(shadow,135,522,95,12);
            if(game.Invulnerable<=0||((int)(game.Invulnerable*12))%2==0){float stretch=game.Grounded?(game.Landing>0?.2f:0):-.1f;float vertical=game.Grounded?(game.Landing>0?.85f:1):1.1f;MiniGameArt.Pet(g,(float)JumpGame.PlayerX,(float)(JumpGame.Ground-42-game.Height),158,game.Elapsed,stretch,game.Grounded?0:-4,game.Bite>0,vertical);}
            if(game.Bite>0)MiniGameArt.Text(g,"냠! +10",15,Design.Olive,145,(float)(JumpGame.Ground-165-game.Height),true);MiniGameArt.Text(g,"Space / ↑ / 클릭 점프 · P / Esc 일시정지 · R 다시 시작",12,Design.Muted,250,558);
            if(game.State==SnackRoundState.Ready)MiniGameArt.Overlay(g,"통통, 뛰어볼까요?","장애물은 뛰어넘고, 공중의 간식은 냠냠!","게임 시작 또는 Space · 한 판 60초, 목숨 3개");else if(game.State==SnackRoundState.Paused)MiniGameArt.Overlay(g,"잠깐 쉬는 중","시간과 장애물도 함께 멈췄어요.","계속하기 또는 P로 돌아와요.");else if(game.State==SnackRoundState.Finished)MiniGameArt.Overlay(g,"오늘의 점프 완료!",result==null?"":result.Score+"점 · "+result.Details,"결과 카드로 남기기 · R로 다시 도전");
        }
        internal void SavePreview(string path){using(var image=new Bitmap(960,600))using(var g=Graphics.FromImage(image)){DrawScene(g);image.Save(path);}}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
