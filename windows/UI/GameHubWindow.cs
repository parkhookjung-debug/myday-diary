using System;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI
{
    public sealed class GameHubWindow : Form
    {
        private readonly GameRecords records;
        private readonly Label[] best=new Label[3];
        private readonly Label status=Design.Label("",9);
        public GameHubWindow(GameRecords records=null)
        {
            this.records=records??GameRecords.Local();Text="MyDay · 상몬 플레이룸";BackColor=Design.Paper;Font=Design.Font(10);StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(960,650);MinimumSize=Design.Size(700,530);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(100)};var title=Design.Label("상몬 플레이룸",25,true);title.Location=Design.Point(28,18);header.Controls.Add(title);var intro=Design.Label("가볍게 한 판, 상몬과 쉬어가요.  ·  점수는 이 컴퓨터에 보관해요.",10);intro.ForeColor=Design.Muted;intro.Location=Design.Point(30,65);header.Controls.Add(intro);
            var footer=new Panel{Dock=DockStyle.Bottom,Height=Design.P(54)};status.Location=Design.Point(28,14);status.MaximumSize=Design.Size(880,40);footer.Controls.Add(status);
            var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Padding=Design.Pad(20,12,20,8),BackColor=Design.Paper};for(int i=0;i<3;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            string[] titles={"간식 산책","상몬 슬링샷","느긋한 낚시"},notes={"맛있는 간식은 냠냠!\n돌과 가시는 쏙쏙.\n5콤보마다 보너스 +10점.","상몬을 쭉 당겼다 놓기!\n점선을 따라 고리에 쏙.\n10번의 기회, 5개의 고리.","물가에서 잠깐 쉬어요.\n찌가 움직이면 한 번,\n초록 칸에서 한 번 더!"};
            for(int i=0;i<3;i++){
                int index=i;var card=new CardPanel{Dock=DockStyle.Fill,Margin=Design.Pad(7),Fill=Color.FromArgb(255,254,249),Padding=Design.Pad(16)};var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.Transparent};
                var art=new GameIllustration(i){Size=Design.Size(235,150),Margin=Design.Pad(0,0,0,8)};var name=Design.Label(titles[i],18,true);name.Margin=Design.Pad(0,2,0,14);var note=Design.Label(notes[i],10);note.ForeColor=Design.Muted;note.Margin=Design.Pad(0,0,0,14);best[i]=Design.Label("",11,true);best[i].ForeColor=Design.Olive;best[i].Margin=Design.Pad(0,0,0,18);var play=Design.Button("놀러 가기",true);play.Size=Design.Size(230,38);play.Margin=Design.Pad(0,0,0,10);play.Click+=delegate{OpenGame(index,false);};stack.Controls.AddRange(new Control[]{art,name,note,best[i],play});
                Button daily=null;if(i==0){daily=Design.Button("오늘의 같은 배치 도전");daily.Size=Design.Size(230,36);daily.Click+=delegate{OpenGame(0,true);};stack.Controls.Add(daily);}
                var dailyButton=daily;card.Resize+=delegate{int width=Math.Max(Design.P(150),stack.ClientSize.Width-Design.P(18)-SystemInformation.VerticalScrollBarWidth);art.Width=play.Width=width;if(dailyButton!=null)dailyButton.Width=width;note.MaximumSize=new Size(width,0);};card.Controls.Add(stack);grid.Controls.Add(card,i,0);
            }
            Controls.Add(grid);Controls.Add(header);Controls.Add(footer);Activated+=delegate{RefreshBest();};RefreshBest();
        }
        internal void RefreshBest(){records.Refresh();string[] ids={"snack","sling","fish"};for(int i=0;i<3;i++)best[i].Text="나의 최고 "+records.Best(ids[i])+"점";status.Text=records.Problem??"한 판은 약 1분 · 게임 점수와 일기 경험치는 각각 관리해요.";}
        private void OpenGame(int kind,bool daily){if(kind==0){using(var game=new SnackGameWindow(records,daily))game.ShowDialog(this);}else{using(var game=new ArcadeGameWindow(kind==1?ArcadeKind.Slingshot:ArcadeKind.Fishing,records))game.ShowDialog(this);}RefreshBest();}
    }
    internal sealed class GameIllustration : Control
    {
        private readonly int kind;
        public GameIllustration(int kind){this.kind=kind;DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.FromArgb(255,254,249));float x=Width/2f,y=Height/2f;if(kind==0){MiniGameArt.Pet(e.Graphics,x,y+18,Design.P(135),.4f);SnackGameCanvas.DrawItem(e.Graphics,SnackKind.Strawberry,x-Design.P(55),y-Design.P(38),Design.P(20)/20f);SnackGameCanvas.DrawItem(e.Graphics,SnackKind.Cookie,x+Design.P(60),y-Design.P(10),Design.P(20)/20f);}
            else if(kind==1){MiniGameArt.Pet(e.Graphics,x-Design.P(15),y+10,Design.P(145),.4f,.35f,-12);using(var pen=new Pen(Color.FromArgb(214,176,111),Design.P(5)))e.Graphics.DrawEllipse(pen,x+Design.P(43),y-Design.P(47),Design.P(45),Design.P(45));}
            else {using(var water=new SolidBrush(Color.FromArgb(200,228,218)))e.Graphics.FillEllipse(water,x-Design.P(90),y+Design.P(25),Design.P(180),Design.P(40));MiniGameArt.Pet(e.Graphics,x-Design.P(20),y+5,Design.P(140),.3f);MiniGameArt.Fish(e.Graphics,x+Design.P(65),y+Design.P(20),Design.P(19));}base.OnPaint(e);}
    }
}
