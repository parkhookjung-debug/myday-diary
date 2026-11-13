using System;
namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private GameHubWindow gameHub;
        private void OpenGames(){if(!ScreenPrivacy.Reveal())return;if(gameHub==null||gameHub.IsDisposed)gameHub=new GameHubWindow(testMode?new MyDay.Windows.Core.GameRecords():MyDay.Windows.Core.GameRecords.Local());gameHub.Show(this);gameHub.Activate();}
    }
}
