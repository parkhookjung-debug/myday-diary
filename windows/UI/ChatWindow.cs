using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDay.Windows.Chat;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class ChatWindow : Form
    {
        internal readonly TextBox ServerAddress=new TextBox {Text="http://127.0.0.1:8765/",AccessibleName="채팅 서버 주소"};
        internal readonly TextBox Nickname=new TextBox {MaxLength=DesktopContract.ChatNicknameLimit,AccessibleName="채팅 닉네임"};
        internal readonly TextBox Invitation=new TextBox {UseSystemPasswordChar=true,MaxLength=200,AccessibleName="채팅 초대코드"};
        internal readonly TextBox Composer=new TextBox {Multiline=true,MaxLength=DesktopContract.ChatMaxText,ScrollBars=ScrollBars.Vertical,AccessibleName="보낼 메시지"};
        internal readonly Button ConnectButton=Design.Button("입장하기",true),DisconnectButton=Design.Button("나가기"),SendButton=Design.Button("보내기",true);
        internal readonly ChatTranscript Conversation=new ChatTranscript();
        private readonly Label status=Design.Label("서버 주소·닉네임·초대코드를 입력하고 입장하세요.",9),count=Design.Label("0 / "+DesktopContract.ChatMaxText,8);
        private readonly ListBox people=new ListBox {BorderStyle=BorderStyle.None,BackColor=Design.Paper,IntegralHeight=false,HorizontalScrollbar=true,AccessibleName="접속 중인 참여자"};
        private ChatClient client;
        private CancellationTokenSource lifetime;
        private bool connecting,sending,closing;
        private long cursor;
        private string nickname,pendingText,pendingId;
        internal ChatWindow()
        {
            Text="MyDay · 상몬 라운지";Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Backgrounds[0];AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(900,720);MinimumSize=Design.Size(760,600);
            var header=new Panel {Dock=DockStyle.Top,Height=Design.P(80),BackColor=Design.Paper};
            var title=Design.Label("함께 쓰는 작은 이야기",22,true);title.Location=Design.Point(24,12);header.Controls.Add(title);
            var hint=Design.Label("초대한 사람들과 이야기해요. 일기와 사진은 채팅에 자동으로 전송되지 않아요.",9);hint.Location=Design.Point(26,53);hint.ForeColor=Design.Muted;header.Controls.Add(hint);
            var connection=new TableLayoutPanel {Dock=DockStyle.Top,Height=Design.P(112),ColumnCount=4,RowCount=2,Padding=Design.Pad(22,10,22,8)};
            connection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,44));connection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,21));connection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));connection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(112)));
            connection.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(22)));connection.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            connection.Controls.Add(Design.Label("서버 주소",9),0,0);connection.Controls.Add(Design.Label("닉네임",9),1,0);connection.Controls.Add(Design.Label("초대코드",9),2,0);
            foreach(var field in new[]{ServerAddress,Nickname,Invitation}) {field.Dock=DockStyle.Top;field.Margin=Design.Pad(0,4,10,0);field.Font=Design.Font(10);}
            connection.Controls.Add(ServerAddress,0,1);connection.Controls.Add(Nickname,1,1);connection.Controls.Add(Invitation,2,1);
            var actions=new FlowLayoutPanel {Dock=DockStyle.Fill,Margin=Design.Pad(0),WrapContents=false,FlowDirection=FlowDirection.TopDown};
            ConnectButton.Size=DisconnectButton.Size=Design.Size(106,30);ConnectButton.Font=DisconnectButton.Font=Design.Font(9);actions.Controls.Add(ConnectButton);actions.Controls.Add(DisconnectButton);connection.Controls.Add(actions,3,0);connection.SetRowSpan(actions,2);
            var participants=new Panel {Dock=DockStyle.Right,Width=Design.P(150),Padding=Design.Pad(12),BackColor=Design.Paper};
            var presence=Design.Label("함께 있는 사람",10,true);presence.Dock=DockStyle.Top;presence.Height=Design.P(32);people.Dock=DockStyle.Fill;participants.Controls.Add(people);participants.Controls.Add(presence);
            var messages=new Panel {Dock=DockStyle.Fill,Padding=Design.Pad(24,10,18,12)};Conversation.Dock=DockStyle.Fill;Conversation.Font=Design.Font(10);messages.Controls.Add(Conversation);
            var footer=new Panel {Dock=DockStyle.Bottom,Height=Design.P(142),Padding=Design.Pad(24,8,24,8)};
            var compose=new TableLayoutPanel {Dock=DockStyle.Top,Height=Design.P(80),ColumnCount=2,RowCount=1};compose.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));compose.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(100)));
            Composer.Dock=DockStyle.Fill;Composer.Font=Design.Font(10);Composer.Margin=Design.Pad(0,0,10,0);SendButton.Dock=DockStyle.Fill;compose.Controls.Add(Composer,0,0);compose.Controls.Add(SendButton,1,0);
            status.Location=Design.Point(24,99);status.AutoSize=false;status.Size=Design.Size(660,30);status.ForeColor=Design.Muted;count.Anchor=AnchorStyles.Top|AnchorStyles.Right;count.Location=Design.Point(790,102);footer.Controls.Add(compose);footer.Controls.Add(status);footer.Controls.Add(count);
            footer.Resize+=delegate {status.Width=Math.Max(100,footer.ClientSize.Width-Design.P(140));count.Left=footer.ClientSize.Width-Design.P(105);};
            Controls.Add(messages);Controls.Add(participants);Controls.Add(footer);Controls.Add(connection);Controls.Add(header);
            ConnectButton.Click+=async delegate {await Connect();};DisconnectButton.Click+=async delegate {await Disconnect();};SendButton.Click+=async delegate {await Send();};
            Composer.TextChanged+=delegate {count.Text=Composer.Text.Length+" / "+DesktopContract.ChatMaxText;UpdateControls();};
            Composer.KeyDown+=async delegate(object sender,KeyEventArgs e) {if(e.KeyCode==Keys.Enter && !e.Shift){e.SuppressKeyPress=true;e.Handled=true;await Send();}};
            FormClosed+=async delegate {closing=true;await Disconnect();};UpdateControls();
        }
        private void UpdateControls()
        {
            bool connected=client!=null && !connecting;
            ConnectButton.Enabled=!connecting && client==null;DisconnectButton.Enabled=client!=null || connecting;
            ServerAddress.Enabled=Nickname.Enabled=Invitation.Enabled=!connecting && client==null;
            SendButton.Enabled=connected && !sending && !string.IsNullOrWhiteSpace(Composer.Text);Composer.ReadOnly=!connected;
        }
        private void Status(string message) {if(!closing && !IsDisposed)status.Text=message;}
        private async Task Connect()
        {
            if(connecting || client!=null)return;
            var cancel=new CancellationTokenSource();lifetime=cancel;ChatClient next=null;connecting=true;UpdateControls();Status("채팅방에 입장하고 있어요…");
            try {
                next=new ChatClient(ServerAddress.Text.Trim());
                var session=await next.Join(Nickname.Text,Invitation.Text,cancel.Token);
                cancel.Token.ThrowIfCancellationRequested();client=next;nickname=session.nickname;cursor=0;pendingId=pendingText=null;
                Conversation.Clear();people.Items.Clear();connecting=false;UpdateControls();Status(session.room+"에 들어왔어요. Enter 전송 · Shift+Enter 줄바꿈");
                Composer.Focus();await Receive(next,cancel.Token);
            } catch(OperationCanceledException) {Status("채팅방 연결을 종료했어요.");}
            catch(Exception ex) {Status(ex.Message);}
            if(ReferenceEquals(lifetime,cancel)){connecting=true;if(!closing)UpdateControls();}
            if(next!=null)try{await next.Leave();}catch(Exception){}
            if(ReferenceEquals(lifetime,cancel)){client=null;connecting=false;sending=false;lifetime=null;if(!closing)UpdateControls();}cancel.Dispose();
        }
        private async Task Receive(ChatClient active,CancellationToken cancel)
        {
            int failures=0;bool first=true;
            while(!cancel.IsCancellationRequested) {
                int delay=0;
                try {
                    var page=await active.Poll(cursor,cancel,first?0:20);first=false;
                    if(closing || cancel.IsCancellationRequested)return;
                    if(page.reset){Conversation.Clear();cursor=0;Status("서버의 새 대화 내역을 불러왔어요.");}
                    foreach(var message in page.messages)if(message.id>cursor) {Append(message);cursor=message.id;}
                    cursor=Math.Max(cursor,page.cursor);people.Items.Clear();foreach(string name in page.users)people.Items.Add(name+(name==nickname?" (나)":""));
                    if(failures>0)Status("서버에 다시 연결됐어요.");failures=0;
                } catch(ChatException ex) {if(ex.Status==401 || ex.Status==403 || ex.Status==503)throw;Status(ex.Message);delay=ex.Status==429?10000:2000;}
                catch(OperationCanceledException) {throw;}
                catch(Exception) {failures++;Status("연결을 확인하고 있어요. 서버가 켜져 있는지 확인해 주세요.");if(failures>=3)throw new System.IO.IOException("서버 연결이 끊겼어요. 다시 입장해 주세요.");delay=2000;}
                if(delay>0)await Task.Delay(delay,cancel);
            }
        }
        private void Append(ChatMessage message)
        {
            Conversation.Append(message,message.nickname==nickname);
        }
        private async Task Send()
        {
            if(client==null || connecting || sending || string.IsNullOrWhiteSpace(Composer.Text))return;
            var active=client;var generation=lifetime;var cancel=generation.Token;string text=Composer.Text;
            if(text!=pendingText){pendingId=Guid.NewGuid().ToString("N");pendingText=text;}
            sending=true;UpdateControls();Status("메시지를 보내고 있어요…");
            try {
                await active.Send(text,pendingId,cancel);
                if(closing || cancel.IsCancellationRequested)return;
                if(Composer.Text==text)Composer.Clear();pendingId=pendingText=null;Status("보냈어요. Enter 전송 · Shift+Enter 줄바꿈");
            } catch(OperationCanceledException) {Status("전송 확인이 중단됐어요. 대화 내역을 확인해 주세요.");}
            catch(Exception ex) {Status(ex.Message+" 입력한 메시지는 남겨뒀어요.");}
            finally {if(ReferenceEquals(lifetime,generation)){sending=false;if(!closing)UpdateControls();}}
        }
        private Task Disconnect()
        {
            client=null;var cancel=lifetime;if(cancel!=null)cancel.Cancel();
            connecting=cancel!=null;sending=false;if(!closing){people.Items.Clear();Status("채팅방 연결을 종료하고 있어요…");UpdateControls();}
            return Task.FromResult(0);
        }
    }
}
