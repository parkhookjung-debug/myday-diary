using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyDay.Windows.Chat;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunChatAddressTests()
        {
            Check(ChatClient.ParseAddress("http://127.0.0.1:8765/").IsLoopback && ChatClient.ParseAddress("http://192.168.0.10:8765/").Port==8765,"Chat accepts loopback and private-network development servers");
            Check(ChatClient.ParseAddress("https://chat.example.invalid/").Scheme=="https","Internet chat uses HTTPS");
            foreach(string address in new[]{"http://example.com/","file:///C:/private","https://name:secret@example.com/","https://example.com/?token=secret","https://example.com/diary"}) {
                bool rejected=false;try{ChatClient.ParseAddress(address);}catch(ArgumentException){rejected=true;}Check(rejected,"Chat rejects unsafe or ambiguous server address: "+address.Split('?')[0]);
            }
            using(var transcript=new ChatTranscript {Size=new Size(300,200),Font=Design.Font(10)}) {
                transcript.Append(new ChatMessage {id=1,nickname="테스트",text=new string('a',2000),sentAt="2026-10-17T00:00:00Z"},false);
                Check(transcript.AutoScrollMinSize.Height>transcript.ClientSize.Height,"Long unbroken chat text wraps into a scrollable message");
            }
        }
        internal static async Task ChatTransport(string address,string code)
        {
            var a=new ChatClient(address);var b=new ChatClient(address);Exception failure=null;
            try {
                var session=await a.Join("연결검사 가",code,CancellationToken.None);await b.Join("연결검사 나",code,CancellationToken.None);
                Check(session.nickname=="연결검사 가","Windows client authenticates against real Python server");
                bool rejected=false;try{await new ChatClient(address).Join("연결검사 가",code,CancellationToken.None);}catch(ChatException e){rejected=e.Status==409;}Check(rejected,"Windows reports duplicate nickname");
                rejected=false;try{await new ChatClient(address).Join("잘못된 코드","invalid-code",CancellationToken.None);}catch(ChatException e){rejected=e.Status==403;}Check(rejected,"Windows reports invalid invitation without connecting");
                string nonce=Guid.NewGuid().ToString("N");var sent=await a.Send("안녕하세요 🌱\r\n함께 기록해요",nonce,CancellationToken.None);var retry=await a.Send("안녕하세요 🌱\r\n함께 기록해요",nonce,CancellationToken.None);
                var page=await b.Poll(0,CancellationToken.None,0);
                Check(sent.id==retry.id && page.messages.Count(m=>m.clientId==nonce)==1 && page.messages.Any(m=>m.text=="안녕하세요 🌱\n함께 기록해요"),"Unicode/newlines and retried sends arrive exactly once at second Windows client");
                Check(page.users.Count==2,"Windows receives server presence");
                var waiting=b.Poll(page.cursor,CancellationToken.None,3);await Task.Delay(80);var second=await a.Send("새 메시지",Guid.NewGuid().ToString("N"),CancellationToken.None);var arrived=await waiting;
                Check(arrived.messages.Any(m=>m.id==second.id),"Windows receives a message while long poll is waiting");
                using(var cancel=new CancellationTokenSource()) {
                    var pending=b.Poll(arrived.cursor,cancel.Token);cancel.CancelAfter(60);bool cancelled=false;try{await pending;}catch(OperationCanceledException){cancelled=true;}Check(cancelled,"Leaving can cancel an in-flight long poll");
                }
                await b.Leave();rejected=false;try{await b.Poll(0,CancellationToken.None,0);}catch(ChatException e){rejected=e.Status==401;}Check(rejected,"Logged-out Windows client cannot read room history");
            } catch(Exception ex) {failure=ex;}
            try{await a.Leave();}catch(Exception){}try{await b.Leave();}catch(Exception){}
            if(failure!=null)throw new Exception("Chat transport integration failed",failure);
            Console.WriteLine("Chat transport integration passed.");
        }
        private static async Task WaitForChat(Func<bool> condition)
        {
            var timer=Stopwatch.StartNew();while(!condition()){if(timer.Elapsed.TotalSeconds>12)throw new Exception("Chat window did not reach expected state");await Task.Delay(30);}
        }
        internal static void ChatSmoke(string address,string code,string directory)
        {
            Directory.CreateDirectory(directory);Exception failure=null;
            using(var window=new ChatWindow()) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);
                window.Shown+=async delegate {
                    var other=new ChatClient(address);
                    try {
                        window.ServerAddress.Text=address;window.Nickname.Text="상몬 기록가";window.Invitation.Text=code;window.ConnectButton.PerformClick();
                        await WaitForChat(()=>window.DisconnectButton.Enabled && !window.Composer.ReadOnly);
                        await other.Join("작은 친구",code,CancellationToken.None);await other.Send("오늘은 어떤 하루였어? 🌷",Guid.NewGuid().ToString("N"),CancellationToken.None);
                        await WaitForChat(()=>window.Conversation.Text.Contains("오늘은 어떤 하루였어?"));
                        window.Composer.Text="조금씩 만들다 보니 즐거운 하루가 됐어!";window.SendButton.PerformClick();await WaitForChat(()=>window.Composer.TextLength==0);
                        var page=await other.Poll(0,CancellationToken.None,0);Check(page.messages.Any(m=>m.text.Contains("조금씩 만들다")),"Real Windows chat send button delivers to another client");
                        await WaitForChat(()=>window.Conversation.Text.Contains("조금씩 만들다"));
                        using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-chat.png"));}
                        window.Size=window.MinimumSize;window.PerformLayout();Application.DoEvents();
                        Check(window.SendButton.Right<=window.SendButton.Parent.ClientSize.Width,"Chat send button fits minimum Windows window");
                        using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-chat-compact.png"));}
                        window.DisconnectButton.PerformClick();await WaitForChat(()=>window.ConnectButton.Enabled && !window.SendButton.Enabled);
                        window.ConnectButton.PerformClick();await WaitForChat(()=>!window.Composer.ReadOnly);Check(!window.Composer.ReadOnly,"Same nickname can reconnect after leaving");window.DisconnectButton.PerformClick();await WaitForChat(()=>window.ConnectButton.Enabled);
                        Console.WriteLine("Native chat window integration passed.");
                    } catch(Exception ex) {failure=ex;}
                    // C# 5 does not allow await inside finally.
                    try{await other.Leave();}catch(Exception){}window.Close();
                };
                Application.Run(window);
            }
            if(failure!=null)throw new Exception("Native chat smoke failed",failure);
        }
    }
}
