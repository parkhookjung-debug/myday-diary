using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;
namespace MyDay.Windows.UI
{
    public sealed class BlockGameWindow : Form
    {
        private readonly BlockGame game=new BlockGame(Environment.TickCount);
        private readonly GameRecords records;
        private readonly BlockArt art=new BlockArt();
        private readonly MiniSceneCanvas canvas=new MiniSceneCanvas();
        private readonly Timer timer=new Timer{Interval=16};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly HashSet<Keys> held=new HashSet<Keys>();
        private readonly Button start=Design.Button("게임 시작",true),pause=Design.Button("일시정지"),drop=Design.Button("바로 내리기"),export=Design.Button("결과 카드 저장");
        private readonly Label status=Design.Label("",9);
        private double last,inputRemainder,sideRepeat,downRepeat;
        private int direction;
        private bool softHeld;
        private MiniGameResult result;
        internal BlockGame Game {get{return game;}}
        public BlockGameWindow(GameRecords records=null)
        {
            this.records=records??new GameRecords();Text="MyDay · 상몬 블록 쌓기";BackColor=Design.Paper;Font=Design.Font(10);KeyPreview=true;StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,780);MinimumSize=Design.Size(680,600);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(78)};var title=Design.Label("상몬 블록 쌓기",20,true);title.Location=Design.Point(26,12);header.Controls.Add(title);var note=Design.Label("← → 이동 · ↑ / X 회전 · Z 반대로 회전 · ↓ 빠르게 · Space 바로 내리기",9);note.ForeColor=Design.Muted;note.Location=Design.Point(28,48);header.Controls.Add(note);
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(66),Padding=Design.Pad(20,12,8,8),WrapContents=false};start.Size=Design.Size(110,36);pause.Size=Design.Size(100,36);drop.Size=Design.Size(120,36);export.Size=Design.Size(132,36);status.Margin=Design.Pad(10,8,0,0);footer.Controls.AddRange(new Control[]{start,pause,drop,export,status});footer.Resize+=delegate{status.Visible=footer.ClientSize.Width>=Design.P(1000);};
            canvas.Dock=DockStyle.Fill;canvas.AccessibleName="상몬 블록 쌓기. 방향키로 이동과 회전, Space로 바로 내리기. P 정지.";canvas.Render=DrawScene;canvas.Press=p=>{Pointer(p);return false;};Controls.Add(canvas);Controls.Add(header);Controls.Add(footer);
            start.Click+=delegate{StartRound();};pause.Click+=delegate{TogglePause();};drop.Click+=delegate{if(game.State==SnackRoundState.Playing){game.HardDrop();UpdateUi();canvas.Focus();canvas.Invalidate();}};export.Click+=delegate{MiniGameArt.Export(this,result);};
            KeyDown+=delegate(object s,KeyEventArgs e){e.Handled=PressKey(e.KeyCode);};KeyUp+=delegate(object s,KeyEventArgs e){ReleaseKey(e.KeyCode);};KeyPress+=delegate(object s,KeyPressEventArgs e){if(" xXzZpPrR".IndexOf(e.KeyChar)>=0||e.KeyChar==(char)13||e.KeyChar==(char)27)e.Handled=true;};Deactivate+=delegate{held.Clear();PauseRound();};timer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds;if(game.State==SnackRoundState.Playing){Advance(now-last);canvas.Invalidate();}last=now;};Shown+=delegate{last=clock.Elapsed.TotalSeconds;timer.Start();canvas.Focus();};UpdateUi();
        }
        private void ClearMotion(){direction=0;softHeld=false;inputRemainder=sideRepeat=downRepeat=0;}
        internal void StartRound(){ClearMotion();result=null;game.Start();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        internal void Advance(double seconds){if(game.State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;inputRemainder+=Math.Min(.25,seconds);const double h=1.0/120;while(inputRemainder+1e-10>=h&&game.State==SnackRoundState.Playing){inputRemainder-=h;if(direction!=0){sideRepeat-=h;if(sideRepeat<=0){game.Move(direction);sideRepeat+=.05;}}if(softHeld){downRepeat-=h;if(downRepeat<=0){game.SoftDrop();downRepeat+=.035;}}game.Step(h);}UpdateUi();}
        internal void PauseRound(){ClearMotion();game.Pause();UpdateUi();canvas.Invalidate();}
        internal void TogglePause(){if(game.State==SnackRoundState.Paused)game.Resume();else PauseRound();last=clock.Elapsed.TotalSeconds;UpdateUi();canvas.Focus();canvas.Invalidate();}
        private bool PressKey(Keys key){if(key!=Keys.Left&&key!=Keys.Right&&key!=Keys.Up&&key!=Keys.Down&&key!=Keys.X&&key!=Keys.Z&&key!=Keys.Space&&key!=Keys.Enter&&key!=Keys.P&&key!=Keys.R&&key!=Keys.Escape)return false;if(!held.Add(key))return true;if(key==Keys.R)StartRound();else if(key==Keys.P)TogglePause();else if(key==Keys.Escape)PauseRound();else if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished){if(key==Keys.Space||key==Keys.Enter)StartRound();}else if(game.State==SnackRoundState.Playing){if(key==Keys.Left||key==Keys.Right){direction=key==Keys.Left?-1:1;sideRepeat=.16;game.Move(direction);}else if(key==Keys.Down){softHeld=true;downRepeat=.035;game.SoftDrop();}else if(key==Keys.Up||key==Keys.X)game.Rotate(1);else if(key==Keys.Z)game.Rotate(-1);else if(key==Keys.Space)game.HardDrop();}UpdateUi();canvas.Invalidate();return true;}
        private void ReleaseKey(Keys key){held.Remove(key);if(key==Keys.Down)softHeld=false;if((key==Keys.Left&&direction==-1)||(key==Keys.Right&&direction==1)){direction=held.Contains(Keys.Left)?-1:held.Contains(Keys.Right)?1:0;sideRepeat=.16;}}
        internal void Pointer(PointF p){if(game.State==SnackRoundState.Ready||game.State==SnackRoundState.Finished){StartRound();return;}if(game.State!=SnackRoundState.Playing)return;if(new RectangleF(40,460,52,40).Contains(p))game.Move(-1);else if(new RectangleF(102,460,52,40).Contains(p))game.Move(1);else if(new RectangleF(164,460,104,40).Contains(p))game.Rotate(1);else if(new RectangleF(40,510,228,40).Contains(p))game.HardDrop();UpdateUi();canvas.Invalidate();}
        internal KeyEventArgs NativeKeyDownForTest(Keys key){var e=new KeyEventArgs(key);OnKeyDown(e);return e;}
        internal void NativeKeyUpForTest(Keys key){OnKeyUp(new KeyEventArgs(key));}
        internal void LoseFocusForTest(){OnDeactivate(EventArgs.Empty);}
        internal void VerifyFooterForTest(){foreach(var b in new[]{start,pause,drop,export})if(!b.Parent.ClientRectangle.Contains(b.Bounds))throw new InvalidOperationException("Block game footer clipped");}
        private void UpdateUi(){if(game.State==SnackRoundState.Finished&&result==null){result=new MiniGameResult{Name="상몬 블록 쌓기",Day=DateTime.Today,Score=game.Score,Details=game.Lines+"줄 · 속도 Lv."+game.Level+" · "+game.Pieces+"개 블록"};records.Record("blocks",game.Score);}start.Text=game.State==SnackRoundState.Ready?"게임 시작":game.State==SnackRoundState.Finished?"다시 쌓기":"처음부터";pause.Text=game.State==SnackRoundState.Paused?"계속하기":"일시정지";pause.Enabled=game.State==SnackRoundState.Playing||game.State==SnackRoundState.Paused;drop.Enabled=game.State==SnackRoundState.Playing;export.Enabled=result!=null;status.Text=records.Problem??("최고 "+records.Best("blocks")+"점");}
        internal void DrawScene(Graphics g)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.FromArgb(246,245,235));MiniGameArt.Card(g,new RectangleF(20,15,245,64),Color.FromArgb(255,254,250));MiniGameArt.Text(g,"SCORE",9,Design.Muted,36,24,true);MiniGameArt.Text(g,game.Score.ToString("D3"),21,Design.Ink,105,24,true);MiniGameArt.Card(g,new RectangleF(371,15,218,64),Color.FromArgb(255,254,250));MiniGameArt.Text(g,"속도",10,Design.Muted,391,31);MiniGameArt.Text(g,"Lv. "+game.Level,21,Design.Olive,470,25,true);MiniGameArt.Card(g,new RectangleF(705,15,235,64),Color.FromArgb(255,254,250));MiniGameArt.Text(g,"완성한 줄  "+game.Lines,15,Design.Ink,729,29,true);
            MiniGameArt.Card(g,new RectangleF(20,100,278,470),Color.FromArgb(255,254,250));MiniGameArt.Text(g,"차곡차곡, 상몬!",19,Design.Ink,42,117,true);MiniGameArt.Text(g,"상몬 네 마리가 한 묶음이에요.",10,Design.Muted,42,158);
            foreach(var c in BlockGame.CellsFor(BlockKind.T,0))art.Tile(g,75+c.X*46,201+c.Y*46,46,(int)BlockKind.T,false);MiniGameArt.Pet(g,154,360,185,game.Elapsed,0,game.Flash>0?-4:0,game.Flash>0);MiniGameArt.Text(g,"한 줄을 가득 채워요.",11,Design.Olive,54,414,true);
            DrawControl(g,new RectangleF(40,460,52,40),"←");DrawControl(g,new RectangleF(102,460,52,40),"→");DrawControl(g,new RectangleF(164,460,104,40),"회전 ↑");DrawControl(g,new RectangleF(40,510,228,40),"바로 내리기 · Space");
            const int cell=24,left=350,top=95;MiniGameArt.Card(g,new RectangleF(left-8,top-8,MiniGameRules.BlocksColumns*cell+16,MiniGameRules.BlocksRows*cell+16),Color.FromArgb(224,220,202));using(var fill=new SolidBrush(Color.FromArgb(239,237,224)))g.FillRectangle(fill,left,top,MiniGameRules.BlocksColumns*cell,MiniGameRules.BlocksRows*cell);using(var line=new Pen(Color.FromArgb(223,220,205),1)){for(int x=0;x<=MiniGameRules.BlocksColumns;x++)g.DrawLine(line,left+x*cell,top,left+x*cell,top+MiniGameRules.BlocksRows*cell);for(int y=0;y<=MiniGameRules.BlocksRows;y++)g.DrawLine(line,left,top+y*cell,left+MiniGameRules.BlocksColumns*cell,top+y*cell);}
            var saved=g.Save();g.SetClip(new Rectangle(left,top,MiniGameRules.BlocksColumns*cell,MiniGameRules.BlocksRows*cell));for(int y=0;y<MiniGameRules.BlocksRows;y++)for(int x=0;x<MiniGameRules.BlocksColumns;x++)if(game.CellAt(x,y)>0)art.Tile(g,left+x*cell,top+y*cell,cell,game.CellAt(x,y)-1,false);
            if(game.State==SnackRoundState.Playing||game.State==SnackRoundState.Paused){int landing=game.GhostY;var cells=game.Cells;foreach(var c in cells)art.Tile(g,left+(game.X+c.X)*cell,top+(landing+c.Y)*cell,cell,(int)game.Kind,true);foreach(var c in cells)art.Tile(g,left+(game.X+c.X)*cell,top+(game.Y+c.Y)*cell,cell,(int)game.Kind,false);}if(game.Flash>0){using(var glow=new SolidBrush(Color.FromArgb((int)(game.Flash/.45*150),255,240,158)))foreach(int row in game.ClearedRows)g.FillRectangle(glow,left,top+row*cell,MiniGameRules.BlocksColumns*cell,cell);}g.Restore(saved);
            MiniGameArt.Text(g,"다음 상몬",16,Design.Ink,640,109,true);var next=game.Next;for(int n=0;n<Math.Min(3,next.Length);n++){float y=146+n*88;MiniGameArt.Card(g,new RectangleF(631,y,293,82),Color.FromArgb(255,254,250));MiniGameArt.Text(g,(n+1).ToString("00"),10,Design.Muted,650,y+16,true);foreach(var c in BlockGame.CellsFor(next[n],0))art.Tile(g,708+c.X*22,y+15+c.Y*22,22,(int)next[n],false);}
            MiniGameArt.Card(g,new RectangleF(631,425,293,100),Color.FromArgb(255,254,250));MiniGameArt.Text(g,"나의 최고 기록",10,Design.Muted,650,436,true);MiniGameArt.Text(g,records.Best("blocks").ToString("N0")+"점",25,Design.Olive,650,456,true);MiniGameArt.Text(g,"BEST SCORE · 완료한 판을 저장해요",8,Design.Muted,650,502);MiniGameArt.Text(g,game.Pieces+"개 블록 · "+MiniGameRules.BlocksLinesPerLevel+"줄마다 속도 UP",10,Design.Muted,640,534);MiniGameArt.Text(g,game.Message,11,Design.Ink,640,561,true);MiniGameArt.Text(g,"P / Esc 정지 · R 다시 시작 · 옅은 블록은 착지 위치",10,Design.Muted,301,582);
            if(game.State==SnackRoundState.Ready)MiniGameArt.Overlay(g,"상몬 블록을 쌓아볼까요?","일곱 가지 상몬 블록으로 가로 한 줄을 채워요.","게임 시작 / Space · ↑ 회전 · Space 바로 내리기");else if(game.State==SnackRoundState.Paused)MiniGameArt.Overlay(g,"블록도 잠깐 쉬는 중","시간과 블록이 함께 멈췄어요.","P / 계속하기 · 키를 새로 눌러 움직여요.");else if(game.State==SnackRoundState.Finished)MiniGameArt.Overlay(g,"상몬이 가득 모였어요!",result==null?"":result.Score+"점 · "+result.Details,"결과 카드 저장 · R로 다시 쌓기");
        }
        private static void DrawControl(Graphics g,RectangleF box,string text){MiniGameArt.Card(g,box,Color.FromArgb(238,231,215));MiniGameArt.Text(g,text,11,Design.Ink,box.X+12,box.Y+10,true);}
        internal void SavePreview(string path){using(var image=new Bitmap(960,600))using(var g=Graphics.FromImage(image)){DrawScene(g);image.Save(path);}}
        protected override void Dispose(bool disposing){if(disposing){timer.Dispose();art.Dispose();}base.Dispose(disposing);}
    }
    internal sealed class BlockArt : IDisposable
    {
        private readonly Bitmap[] tiles=new Bitmap[7];
        private readonly ImageAttributes ghost=new ImageAttributes();
        public BlockArt()
        {
            Color[] colors={Color.FromArgb(153,211,205),Color.FromArgb(162,187,225),Color.FromArgb(239,177,133),Color.FromArgb(235,210,118),Color.FromArgb(166,203,139),Color.FromArgb(194,167,220),Color.FromArgb(234,160,173)};
            using(var original=new Bitmap(96,96)){
                using(var g=Graphics.FromImage(original)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;var pose=MonsterPose.ForActivity(.5,PetActivity.Rest,.5,0,0);pose.Bob=pose.Tilt=0;pose.Closed=false;MonsterPainter.Draw(g,new Rectangle(1,1,94,94),pose,true);}
                for(int i=0;i<tiles.Length;i++){tiles[i]=new Bitmap(96,96);using(var g=Graphics.FromImage(tiles[i]))using(var tint=new ImageAttributes()){var color=new ColorMatrix();color.Matrix00=colors[i].R/255f;color.Matrix11=colors[i].G/255f;color.Matrix22=colors[i].B/255f;tint.SetColorMatrix(color);g.DrawImage(original,new Rectangle(0,0,96,96),0,0,96,96,GraphicsUnit.Pixel,tint);}}
            }
            var matrix=new ColorMatrix();matrix.Matrix33=.22f;ghost.SetColorMatrix(matrix);
        }
        public void Tile(Graphics g,float x,float y,float size,int kind,bool landing){if(landing){g.DrawImage(tiles[kind],new Rectangle((int)x,(int)y,(int)size,(int)size),0,0,96,96,GraphicsUnit.Pixel,ghost);using(var pen=new Pen(Color.FromArgb(149,153,124),1)){pen.DashStyle=DashStyle.Dot;g.DrawRectangle(pen,x+2,y+2,size-4,size-4);}}else g.DrawImage(tiles[kind],x,y,size,size);}
        public void Dispose(){foreach(var image in tiles)image.Dispose();ghost.Dispose();}
    }
}
