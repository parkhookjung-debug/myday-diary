using System;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class PreferencesWindow : Form
    {
        public PreferencesWindow(int currentTheme,Action<int> setTheme,Action export,Action import,Action pet)
        {
            Text=AppShell.Titles[4];Size=Design.Size(560,540);MinimumSize=Design.Size(440,440);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Sidebar;ForeColor=Design.Ink;
            var stack=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=Design.Pad(24)};
            var title=Design.Label("오늘의 분위기",18,true);title.Margin=Design.Pad(0,0,0,16);stack.Controls.Add(title);
            var themes=new ThemePicker {Size=Design.Size(340,62),SelectedIndex=currentTheme};themes.SelectedIndexChanged+=delegate {setTheme(themes.SelectedIndex);};stack.Controls.Add(themes);
            var note=Design.Label("일기·사진·배치·성장·소품·뽑기 기록을\nWindows·Mac·Android 사이에서 JSON 파일로 옮겨요.",10);note.Margin=Design.Pad(0,20,0,12);stack.Controls.Add(note);
            string[] names={"↓ 공통 백업 다운로드","↥ 백업 복원","바탕화면 상몬 표시 / 숨기기"};Action[] actions={export,import,pet};
            for(int i=0;i<names.Length;i++){var button=Design.Button(names[i]);button.Size=Design.Size(340,38);var action=actions[i];button.Click+=delegate {action();};stack.Controls.Add(button);}
            var close=Design.Button("닫기");close.Size=Design.Size(340,34);close.Click+=delegate {Close();};stack.Controls.Add(close);Controls.Add(stack);
        }
    }
}
