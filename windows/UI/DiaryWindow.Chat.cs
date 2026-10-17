using System;

namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private ChatWindow chatWindow;
        private void OpenChat()
        {
            if(chatWindow==null || chatWindow.IsDisposed)chatWindow=new ChatWindow();
            chatWindow.Show(this);chatWindow.Activate();
        }
    }
}
