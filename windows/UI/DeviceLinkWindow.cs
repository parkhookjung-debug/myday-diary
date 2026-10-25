using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI {
    public sealed class DeviceLinkWindow:Form {
        public DiaryBook Incoming;
        private DeviceLink server;
        private readonly DiaryBook book;
        private readonly ComboBox address=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,Width=Design.P(340)};
        private readonly TextBox invitation=new TextBox {Width=Design.P(540),ReadOnly=true};
        private readonly TextBox remote=new TextBox {Width=Design.P(540)};
        private readonly PictureBox qr=new PictureBox {Size=Design.Size(245,245),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};
        private readonly Label state=Design.Label("연결할 때만 같은 Wi-Fi에 임시로 열려요.",10);
        private readonly Timer timer=new Timer {Interval=1000};
        private readonly Button start=Design.Button("이 기기의 연결 QR 만들기",true);
        public DeviceLinkWindow(DiaryBook book) {
            this.book=book;Text="기기 연결";Size=Design.Size(660,760);MinimumSize=Design.Size(540,500);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Sidebar;
            var stack=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=Design.Pad(20)};Controls.Add(stack);
            var note=Design.Label("같은 Wi-Fi에서 QR 또는 주소로 기록을 받아요.\n받은 기록은 날짜·사진 수와 겹치는 날짜를 확인한 뒤 적용해요.",10);stack.Controls.Add(note);
            foreach(var ip in DeviceLink.Addresses())address.Items.Add(ip);if(address.Items.Count>0)address.SelectedIndex=0;
            stack.Controls.Add(address);start.Size=Design.Size(340,34);start.Click+=delegate {try {Stop();if(address.SelectedItem==null)throw new IOException("Wi-Fi 또는 유선 네트워크의 사설 IPv4 주소를 찾지 못했어요.");server=new DeviceLink((IPAddress)address.SelectedItem,DeviceLink.Encode(book));invitation.Text=server.Invitation;qr.Image=new DeviceQr(server.Invitation).Image();timer.Start();}catch(Exception ex){state.Text=ex.Message;}};stack.Controls.Add(start);stack.Controls.Add(qr);stack.Controls.Add(invitation);
            Add(stack,"연결 주소 복사",delegate {if(server!=null&&server.Active)Clipboard.SetText(server.Invitation);});
            Add(stack,"도착한 기록 확인",delegate {if(server==null||server.Incoming()==null){state.Text="아직 도착한 백업이 없어요.";return;}Review(DeviceLink.Validate(server.Incoming()));});
            Add(stack,"연결 닫기",Stop);
            stack.Controls.Add(Design.Label("다른 기기의 연결 주소로 받기",12,true));stack.Controls.Add(remote);
            var receive=Add(stack,"이 주소에서 기록 받기",delegate {});
            receive.Click+=async delegate {receive.Enabled=false;try {string link=remote.Text;var bytes=await Task.Run(()=>DeviceLink.Download(link));if(!IsDisposed)Review(DeviceLink.Validate(bytes));}catch(Exception ex){if(!IsDisposed)state.Text="받지 못했어요. "+ex.Message;}finally {if(!IsDisposed)receive.Enabled=true;}};
            state.MaximumSize=Design.Size(550,0);stack.Controls.Add(state);timer.Tick+=delegate {if(server!=null){state.Text=server.Active?"남은 연결 시간 "+server.Remaining/60+":"+(server.Remaining%60).ToString("D2")+(server.Incoming()!=null?" · 기록이 도착했어요":""):"연결 시간이 끝났어요. 새 QR을 만들어 주세요.";if(!server.Active)Stop();}};
            FormClosed+=delegate {Stop();timer.Dispose();};
        }
        private static Button Add(Control page,string title,Action action){var button=Design.Button(title);button.Size=Design.Size(340,34);button.Click+=delegate {try{action();}catch(Exception ex){MessageBox.Show(ex.Message);}};page.Controls.Add(button);return button;}
        private void Review(DiaryBook incoming){if(ScreenPrivacy.IsLocked)return;int photos=incoming.Days.Values.Sum(e=>e.Blocks.Count(b=>b.Photo!=null));int conflicts=incoming.Days.Keys.Count(book.Days.ContainsKey);if(MessageBox.Show(this,incoming.Days.Count+"일 · 사진 "+photos+"장\n겹치는 날짜 "+conflicts+"일\n가져오기 검토를 계속할까요? 아직 기록을 변경하지 않았어요.","도착한 기록",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;Incoming=incoming;DialogResult=DialogResult.OK;}
        internal void StartForTest(){address.Items.Clear();address.Items.Add(IPAddress.Loopback);address.SelectedIndex=0;server=new DeviceLink(IPAddress.Loopback,DeviceLink.Encode(book));invitation.Text=server.Invitation;qr.Image=new DeviceQr(server.Invitation).Image();timer.Start();}
        private void Stop(){timer.Stop();if(server!=null){server.Dispose();server=null;}invitation.Text="";if(qr.Image!=null){qr.Image.Dispose();qr.Image=null;}state.Text="기기 연결을 닫았어요.";}
    }
}
