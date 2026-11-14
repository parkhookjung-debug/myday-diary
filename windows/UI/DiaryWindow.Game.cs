using System;
using System.Windows.Forms;
namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private SnackGameWindow snackGame;
        private TetrisGameWindow tetrisGame;
        private ContextMenuStrip gamesMenu;
        private void ShowGamesMenu(Control anchor){if(gamesMenu==null){gamesMenu=new ContextMenuStrip{ShowImageMargin=false,Font=Design.Font(10)};var snack=new ToolStripMenuItem("간식 산책");snack.Click+=delegate{OpenSnackGame();};var tetris=new ToolStripMenuItem("테트리스 · 점수 도전");tetris.Click+=delegate{OpenTetrisGame();};gamesMenu.Items.AddRange(new ToolStripItem[]{snack,tetris});}gamesMenu.Show(anchor,new System.Drawing.Point(0,anchor.Height));}
        private void OpenSnackGame(){if(!ScreenPrivacy.Reveal())return;if(snackGame==null||snackGame.IsDisposed)snackGame=new SnackGameWindow();snackGame.Show(this);snackGame.Activate();}
        private void OpenTetrisGame(){if(!ScreenPrivacy.Reveal())return;if(tetrisGame==null||tetrisGame.IsDisposed)tetrisGame=new TetrisGameWindow();tetrisGame.Show(this);tetrisGame.Activate();}
    }
}
