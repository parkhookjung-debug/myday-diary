using System;
namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private SnackGameWindow snackGame;
        private TetrisGameWindow tetrisGame;
        private void OpenSnackGame(){if(!ScreenPrivacy.Reveal())return;if(snackGame==null||snackGame.IsDisposed)snackGame=new SnackGameWindow();snackGame.Show(this);snackGame.Activate();}
        private void OpenTetrisGame(){if(!ScreenPrivacy.Reveal())return;if(tetrisGame==null||tetrisGame.IsDisposed)tetrisGame=new TetrisGameWindow();tetrisGame.Show(this);tetrisGame.Activate();}
    }
}
