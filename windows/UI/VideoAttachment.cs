using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
    internal sealed class VideoAttachment:Panel {
        public VideoAttachment(DiaryBlock block,Action changed){
            BackColor=Color.Transparent;
            var info=Design.Label("",8);info.AutoSize=false;info.Dock=DockStyle.Fill;
            var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(36),WrapContents=false};
            var choose=Design.Button("영상 선택");choose.Width=Design.P(100);choose.AccessibleName="영상 원본 선택";
            var play=Design.Button("재생");play.Width=Design.P(62);play.AccessibleName="영상 원본 재생";
            Action refresh=delegate {info.Text=block.VideoPath==null?"MP4·MOV 원본을 연결해요.\n영상은 백업에 포함되지 않아요.":"영상이 연결되어 있어요.\n다른 기기에서는 원본을 다시 선택해요.";choose.Text=block.VideoPath==null?"영상 선택":"영상 바꾸기";play.Enabled=block.VideoPath!=null;};
            choose.Click+=delegate {using(var dialog=new OpenFileDialog{Title="영상 원본 선택 · 2GB 이하",Filter="영상 (*.mp4;*.mov;*.m4v;*.webm;*.avi;*.mkv)|*.mp4;*.mov;*.m4v;*.webm;*.avi;*.mkv",CheckFileExists=true}){if(dialog.ShowDialog(FindForm())!=DialogResult.OK)return;try{string path=Path.GetFullPath(dialog.FileName);if(!DiaryVideo.LocalPlayable(path)||new FileInfo(path).Length>2000000000L)throw new InvalidDataException("이 컴퓨터의 2GB 이하 영상 파일을 선택해 주세요.");block.VideoPath=path;changed();refresh();}catch(Exception ex){MessageBox.Show(FindForm(),ex.Message,"영상 선택");}}};
            play.Click+=delegate {try{if(!DiaryVideo.LocalPlayable(block.VideoPath))throw new FileNotFoundException("원본 영상을 찾지 못했어요. 이 컴퓨터의 영상을 다시 선택해 주세요.");Process.Start(new ProcessStartInfo(block.VideoPath){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(FindForm(),ex.Message,"영상 재생");}};
            buttons.Controls.AddRange(new Control[]{choose,play});Controls.Add(info);Controls.Add(buttons);refresh();
        }
    }
}
