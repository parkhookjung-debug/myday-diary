using System;
using System.Windows.Forms;
namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private GameHubWindow gameHub;
        private ContextMenuStrip gamesMenu;
        private void StartDesktopFetch(){EnsurePet();pet.Show();pet.StartFetch();petToggle.Text="캐릭터 숨기기";}
        private void EnsureGames(){if(gameHub==null||gameHub.IsDisposed)gameHub=new GameHubWindow(testMode?new MyDay.Windows.Core.GameRecords():MyDay.Windows.Core.GameRecords.Local(),StartDesktopFetch);}
        private void OpenGames(){if(!ScreenPrivacy.Reveal())return;EnsureGames();gameHub.Show(this);gameHub.Activate();}
        private void OpenGameDirect(int kind,bool daily=false,Action<Form> shown=null){if(!ScreenPrivacy.Reveal())return;EnsureGames();gameHub.OpenGame(kind,daily,this,shown);}
        private ContextMenuStrip BuildGamesMenu(){if(gamesMenu!=null)return gamesMenu;gamesMenu=new ContextMenuStrip{ShowImageMargin=false,Font=Design.Font(10)};string[] names={"간식 산책","상몬 슬링샷","느긋한 낚시","통통 점프","불꽃 요리","상몬 블록 쌓기"};for(int i=0;i<names.Length;i++){int kind=i;var item=new ToolStripMenuItem(names[i]);item.Click+=delegate{OpenGameDirect(kind);};gamesMenu.Items.Add(item);}var daily=new ToolStripMenuItem("오늘의 같은 배치 도전");daily.Click+=delegate{OpenGameDirect(0,true);};gamesMenu.Items.Add(daily);gamesMenu.Items.Add(new ToolStripSeparator());var all=new ToolStripMenuItem("플레이룸 전체 보기");all.Click+=delegate{OpenGames();};gamesMenu.Items.Add(all);var fetch=new ToolStripMenuItem("바탕화면 공놀이");fetch.Click+=delegate{if(ScreenPrivacy.Reveal())StartDesktopFetch();};gamesMenu.Items.Add(fetch);return gamesMenu;}
        private void ShowGamesMenu(Control anchor){if(ScreenPrivacy.Reveal())BuildGamesMenu().Show(anchor,new System.Drawing.Point(0,anchor.Height));}
        internal ContextMenuStrip GamesMenuForTest(){return BuildGamesMenu();}
        internal void OpenBlockFromMenuForTest(Action<Form> shown){OpenGameDirect(5,false,shown);}
    }
}
