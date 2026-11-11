using System;
namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private SnackGameWindow snackGame;
        private void OpenSnackGame(){if(!ScreenPrivacy.Reveal())return;if(snackGame==null||snackGame.IsDisposed)snackGame=new SnackGameWindow();snackGame.Show(this);snackGame.Activate();}
    }
}
