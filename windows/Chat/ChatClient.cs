using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyDay.Windows.Chat
{
    [DataContract] internal sealed class ChatJoinRequest { [DataMember] public string nickname,joinCode; }
    [DataContract] internal sealed class ChatSendRequest { [DataMember] public string text,clientId; }
    [DataContract] internal sealed class ChatSession {
        [DataMember] public string token {get;set;}
        [DataMember] public string nickname {get;set;}
        [DataMember] public string room {get;set;}
        [DataMember] public int maxText {get;set;}
    }
    [DataContract] internal sealed class ChatMessage {
        [DataMember] public long id {get;set;}
        [DataMember] public string nickname {get;set;}
        [DataMember] public string text {get;set;}
        [DataMember] public string sentAt {get;set;}
        [DataMember] public string clientId {get;set;}
    }
    [DataContract] internal sealed class ChatPage {
        [DataMember] public List<ChatMessage> messages {get;set;}
        [DataMember] public List<string> users {get;set;}
        [DataMember] public long cursor {get;set;}
        [DataMember] public bool reset {get;set;}
    }
    [DataContract] internal sealed class ChatErrorBody { [DataMember] public string error {get;set;} }
    [DataContract] internal sealed class ChatOk { [DataMember] public bool ok {get;set;} }
    internal sealed class ChatException : IOException
    {
        public readonly int Status;
        public ChatException(int status,string message):base(message) {Status=status;}
    }

    // No diary model, journal path or photo data enters the transport API.
    internal sealed class ChatClient
    {
        public readonly Uri Address;
        private string token;
        public ChatClient(string address) {Address=ParseAddress(address);}
        internal static Uri ParseAddress(string text)
        {
            Uri address;
            if(!Uri.TryCreate(text,UriKind.Absolute,out address) || (address.Scheme!="http" && address.Scheme!="https") || address.UserInfo!="" || address.Query!="" || address.Fragment!="" || address.AbsolutePath!="/")
                throw new ArgumentException("서버 주소는 https://주소/ 형식으로 입력해 주세요. 로컬 테스트는 http://127.0.0.1:8765/예요.");
            IPAddress ip;bool local=address.DnsSafeHost.Equals("localhost",StringComparison.OrdinalIgnoreCase);
            if(IPAddress.TryParse(address.DnsSafeHost,out ip)) {
                var bytes=ip.GetAddressBytes();local=IPAddress.IsLoopback(ip) || (bytes.Length==4 && (bytes[0]==10 || bytes[0]==192 && bytes[1]==168 || bytes[0]==172 && bytes[1]>=16 && bytes[1]<=31));
            }
            if(address.Scheme=="http" && !local)throw new ArgumentException("인터넷 서버에는 HTTPS 주소를 사용해 주세요. HTTP는 로컬·사설 IP 테스트에 사용할 수 있어요.");
            return address;
        }
        private static byte[] Encode(object data)
        {
            using(var stream=new MemoryStream()) {new DataContractJsonSerializer(data.GetType()).WriteObject(stream,data);return stream.ToArray();}
        }
        private static T Decode<T>(Stream input)
        {
            using(var buffer=new MemoryStream()) {
                var bytes=new byte[8192];int read;
                while((read=input.Read(bytes,0,bytes.Length))>0) {if(buffer.Length+read>1024*1024)throw new IOException("서버 응답이 너무 커요.");buffer.Write(bytes,0,read);}
                buffer.Position=0;return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(buffer);
            }
        }
        private async Task<T> Request<T>(string path,string method,object data,CancellationToken cancel,int timeout=30000)
        {
            var request=(HttpWebRequest)WebRequest.Create(new Uri(Address,path));
            request.Method=method;request.AllowAutoRedirect=false;request.KeepAlive=false;if(Address.Scheme=="http")request.Proxy=null;
            request.Timeout=timeout;request.ReadWriteTimeout=timeout;request.ServicePoint.ConnectionLimit=4;
            request.Accept="application/json";
            if(token!=null)request.Headers[HttpRequestHeader.Authorization]="Bearer "+token;
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel)) {
                deadline.CancelAfter(timeout);
                using(deadline.Token.Register(request.Abort)) {
                    try {
                        if(data!=null) {
                            byte[] body=Encode(data);request.ContentType="application/json; charset=utf-8";request.ContentLength=body.Length;
                            using(var output=await request.GetRequestStreamAsync().ConfigureAwait(false))await output.WriteAsync(body,0,body.Length,deadline.Token).ConfigureAwait(false);
                        }
                        using(var response=(HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false)) {
                            if((int)response.StatusCode<200 || (int)response.StatusCode>=300)throw new ChatException((int)response.StatusCode,"서버가 요청을 다른 주소로 보냈어요. 채팅 서버 주소를 확인해 주세요.");
                            if(!response.ContentType.StartsWith("application/json",StringComparison.OrdinalIgnoreCase))throw new IOException("채팅 API 응답이 아니에요. 서버 주소를 확인해 주세요.");
                            using(var input=response.GetResponseStream())return Decode<T>(input);
                        }
                    } catch(WebException ex) {
                        cancel.ThrowIfCancellationRequested();
                        if(deadline.IsCancellationRequested)throw new IOException("서버 응답 시간이 초과됐어요.",ex);
                        var response=ex.Response as HttpWebResponse;
                        if(response!=null)using(response) {
                            string message="서버가 요청을 처리하지 못했어요 ("+(int)response.StatusCode+").";
                            try {using(var input=response.GetResponseStream()){var error=Decode<ChatErrorBody>(input);if(error!=null && !string.IsNullOrWhiteSpace(error.error) && error.error.Length<=500)message=error.error;}}catch(SerializationException){}catch(IOException){}
                            throw new ChatException((int)response.StatusCode,message);
                        }
                        throw new IOException("채팅 서버에 연결할 수 없어요. 주소와 서버 실행 상태를 확인해 주세요.",ex);
                    }
                }
            }
        }
        public async Task<ChatSession> Join(string nickname,string code,CancellationToken cancel)
        {
            if(token!=null)throw new InvalidOperationException("이미 입장한 연결이에요.");
            var session=await Request<ChatSession>("api/sessions","POST",new ChatJoinRequest {nickname=nickname,joinCode=code},cancel).ConfigureAwait(false);
            if(session==null || string.IsNullOrEmpty(session.token) || session.token.Length>100 || string.IsNullOrWhiteSpace(session.nickname) || session.nickname.Length>20 || session.maxText!=2000)
                throw new IOException("채팅 서버의 입장 응답이 올바르지 않아요.");
            token=session.token;return session;
        }
        public async Task<ChatPage> Poll(long cursor,CancellationToken cancel,int wait=20)
        {
            var page=await Request<ChatPage>("api/messages?after="+cursor+"&wait="+wait,"GET",null,cancel).ConfigureAwait(false);
            if(page==null || page.messages==null || page.users==null || page.messages.Count>100 || page.users.Count>64 || page.cursor<0)throw new IOException("메시지 목록이 올바르지 않아요.");
            foreach(var message in page.messages)ValidateMessage(message);
            foreach(string name in page.users)if(name==null || name.Length>20)throw new IOException("참여자 목록이 올바르지 않아요.");
            return page;
        }
        private static void ValidateMessage(ChatMessage message)
        {
            if(message==null || message.id<1 || message.nickname==null || message.nickname.Length>20 || message.text==null || message.text.Length>4000 || message.sentAt==null || message.sentAt.Length>40)throw new IOException("메시지 형식이 올바르지 않아요.");
        }
        public async Task<ChatMessage> Send(string text,string clientId,CancellationToken cancel)
        {
            var message=await Request<ChatMessage>("api/messages","POST",new ChatSendRequest {text=text,clientId=clientId},cancel).ConfigureAwait(false);ValidateMessage(message);return message;
        }
        public async Task Leave()
        {
            if(token==null)return;
            try {await Request<ChatOk>("api/session","DELETE",null,CancellationToken.None,3000).ConfigureAwait(false);}finally {token=null;}
        }
    }
}
