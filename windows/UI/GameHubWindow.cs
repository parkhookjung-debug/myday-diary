using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;
namespace MyDay.Windows.UI
{
    public sealed class GameHubWindow : Form
    {
        private readonly GameRecords records;
        private readonly Action openFetch;
        private readonly List<Action> arrangeCards=new List<Action>();
        private DesktopPet ownPet;
        private readonly Label[] best=new Label[6];
        private readonly Label status=Design.Label("",9);
        public GameHubWindow(GameRecords records=null,Action openFetch=null)
        {
            this.records=records??GameRecords.Local();this.openFetch=openFetch;Text="MyDay · 상몬 플레이룸";BackColor=Design.Paper;Font=Design.Font(10);StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(1120,710);MinimumSize=Design.Size(740,600);
            var header=new Panel{Dock=DockStyle.Top,Height=Design.P(96)};var title=Design.Label("상몬 플레이룸",25,true);title.Location=Design.Point(28,16);header.Controls.Add(title);var intro=Design.Label("가볍게 한 판, 상몬과 쉬어가요. · 점수는 이 컴퓨터에 보관해요.",10);intro.ForeColor=Design.Muted;intro.Location=Design.Point(30,62);header.Controls.Add(intro);
            var footer=new Panel{Dock=DockStyle.Bottom,Height=Design.P(94)};var fetch=Design.Button("바탕화면 공놀이");fetch.Size=Design.Size(200,34);fetch.Location=Design.Point(28,4);fetch.Click+=delegate{StartFetchOnDesktop();};footer.Controls.Add(fetch);status.Location=Design.Point(28,47);status.MaximumSize=Design.Size(1040,42);footer.Controls.Add(status);
            var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,Padding=Design.Pad(20,6,20,8),BackColor=Design.Paper};for(int i=0;i<3;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));for(int i=0;i<2;i++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,50));
            string[] titles={"간식 산책","상몬 슬링샷","느긋한 낚시","통통 점프","불꽃 요리","상몬 블록 쌓기"},notes={"간식은 냠냠, 장애물은 쏙!\n5콤보 보너스와 오늘의 도전.","상몬을 쭉 당겼다 놓기!\n10번의 기회, 5개의 고리.","찌가 움직이면 한 번,\n초록 칸에서 한 번 더!","Space / ↑ / 클릭으로 폴짝!\n장애물을 넘고 간식을 먹어요.","누르고 있다가 놓기!\n황금색에서 노릇노릇하게.","상몬 블록을 차곡차곡!\n한 줄을 채우면 사르르."};
            for(int i=0;i<6;i++){
                int index=i;var card=new CardPanel{Dock=DockStyle.Fill,Margin=Design.Pad(7),Fill=Color.FromArgb(255,254,249),Padding=Design.Pad(12)};var art=new GameIllustration(i){Dock=DockStyle.Left,Width=Design.P(118)};var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,BackColor=Color.Transparent,Padding=Design.Pad(10,0,0,0)};
                var name=Design.Label(titles[i],17,true);name.Margin=Design.Pad(0,0,0,8);var note=Design.Label(notes[i],9);note.ForeColor=Design.Muted;note.Margin=Design.Pad(0,0,0,8);var score=Design.Label("",10,true);best[i]=score;score.ForeColor=Design.Olive;score.Margin=Design.Pad(0,0,0,9);var play=Design.Button("놀러 가기",true);play.Size=Design.Size(220,31);play.Margin=Design.Pad(0,0,0,7);play.Click+=delegate{OpenGame(index,false);};stack.Controls.AddRange(new Control[]{name,note,score,play});Button daily=null;
                if(i==0){daily=Design.Button("오늘의 같은 배치 도전");daily.Size=Design.Size(220,30);daily.Margin=Design.Pad(0);daily.Click+=delegate{OpenGame(0,true);};stack.Controls.Add(daily);}var dailyButton=daily;stack.Resize+=delegate{int width=Math.Max(Design.P(100),stack.ClientSize.Width-stack.Padding.Horizontal-Design.P(8));play.Width=width;if(dailyButton!=null)dailyButton.Width=width;note.MaximumSize=score.MaximumSize=new Size(width,0);};arrangeCards.Add(delegate{bool compact=ClientSize.Width<Design.P(1100)||ClientSize.Height<Design.P(670);art.Visible=note.Visible=!compact;name.Font=Design.Font(compact?14:17,true);stack.PerformLayout();});card.Controls.Add(stack);card.Controls.Add(art);grid.Controls.Add(card,i%3,i/3);
            }
            Controls.Add(grid);Controls.Add(header);Controls.Add(footer);Activated+=delegate{RefreshBest();};Resize+=delegate{foreach(var arrange in arrangeCards)arrange();};Shown+=delegate{foreach(var arrange in arrangeCards)arrange();};foreach(var arrange in arrangeCards)arrange();RefreshBest();
        }
        private void RestoreHub(){Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();}
        internal void StartFetchOnDesktop(){if(openFetch!=null)openFetch();else{if(ownPet==null||ownPet.IsDisposed){ownPet=new DesktopPet(RestoreHub,null,RestoreHub);ownPet.AccessibleName="상몬 공놀이. 상몬을 클릭하면 플레이룸으로 돌아옵니다.";}ownPet.StartFetch();}WindowState=FormWindowState.Minimized;}
        internal void VerifyCardsForTest(){foreach(var button in FindButtons(this))if((button.Text=="바탕화면 공놀이"||button.Text=="놀러 가기"||button.Text=="오늘의 같은 배치 도전")&&button.Visible&&!button.Parent.ClientRectangle.Contains(button.Bounds))throw new InvalidOperationException("A game launch button is clipped: "+button.Bounds+" / "+button.Parent.ClientRectangle);}
        private static IEnumerable<Button> FindButtons(Control root){foreach(Control child in root.Controls){var button=child as Button;if(button!=null)yield return button;foreach(var nested in FindButtons(child))yield return nested;}}
        internal void RefreshBest(){records.Refresh();string[] ids={"snack","sling","fish","jump","bake","blocks"};for(int i=0;i<6;i++)best[i].Text="나의 최고 "+records.Best(ids[i])+"점";status.Text=records.Problem??"공은 드래그해서 던져요. 끝내기는 상몬/공 우클릭 · 게임 점수와 일기 경험치는 각각 관리해요.";}
        internal void OpenGame(int kind,bool daily,IWin32Window owner=null,Action<Form> shown=null){Form window;if(kind==0)window=new SnackGameWindow(records,daily);else if(kind==5)window=new BlockGameWindow(records);else if(kind==4)window=new CookingGameWindow(records);else if(kind==3)window=new JumpGameWindow(records);else window=new ArcadeGameWindow(kind==1?ArcadeKind.Slingshot:ArcadeKind.Fishing,records);using(window){if(shown!=null)window.Shown+=delegate{window.BeginInvoke((MethodInvoker)delegate{shown(window);});};window.ShowDialog(owner??this);}RefreshBest();}
        protected override void Dispose(bool disposing){if(disposing&&ownPet!=null)ownPet.Dispose();base.Dispose(disposing);}
    }
    internal sealed class GameIllustration : Control
    {
        private readonly int kind;
        private readonly BlockArt blocks;
        public GameIllustration(int kind){this.kind=kind;if(kind==5)blocks=new BlockArt();DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.FromArgb(255,254,249));float scale=Math.Min(Width/235f,Height/150f);var state=e.Graphics.Save();e.Graphics.TranslateTransform((Width-235*scale)/2,(Height-150*scale)/2);e.Graphics.ScaleTransform(scale,scale);float x=117,y=75;
            if(kind==0){MiniGameArt.Pet(e.Graphics,x,y+18,140,.4f);SnackGameCanvas.DrawItem(e.Graphics,SnackKind.Strawberry,x-60,y-35,1.1f);SnackGameCanvas.DrawItem(e.Graphics,SnackKind.Cookie,x+60,y-10,1.1f);}else if(kind==1){MiniGameArt.Pet(e.Graphics,x-15,y+10,145,.4f,.35f,-12);using(var pen=new Pen(Color.FromArgb(214,176,111),5))e.Graphics.DrawEllipse(pen,x+43,y-47,45,45);}else if(kind==2){using(var water=new SolidBrush(Color.FromArgb(200,228,218)))e.Graphics.FillEllipse(water,x-90,y+25,180,40);MiniGameArt.Pet(e.Graphics,x-20,y+5,140,.3f);MiniGameArt.Fish(e.Graphics,x+65,y+20,19);}else if(kind==3){using(var grass=new Pen(Color.FromArgb(164,189,133),3))e.Graphics.DrawLine(grass,25,135,220,135);MiniGameArt.Pet(e.Graphics,x,y-5,155,.2f,-.1f,-5,false,1.1f);using(var rock=new SolidBrush(Color.FromArgb(155,171,148)))e.Graphics.FillRectangle(rock,190,110,25,25);}
            if(kind==4){MiniGameArt.Pet(e.Graphics,x-30,y+20,120,.5f,0,3,true);CookingArt.Food(e.Graphics,0,65,x+65,y-15,.42f);}else if(kind==5){foreach(var c in BlockGame.CellsFor(BlockKind.T,0)){blocks.Tile(e.Graphics,50+c.X*43,20+c.Y*43,43,(int)BlockKind.T,false);}}
            e.Graphics.Restore(state);base.OnPaint(e);}
        protected override void Dispose(bool disposing){if(disposing&&blocks!=null)blocks.Dispose();base.Dispose(disposing);}
    }
}
