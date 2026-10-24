using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MyDay.Windows.Core {
    public sealed class DeviceLink:IDisposable {
        public readonly string Token,Origin;
        public string Invitation {get{return Origin+"/#"+Token;}}
        private readonly TcpListener listener;
        private readonly byte[] snapshot;
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly object gate=new object();
        private readonly HashSet<TcpClient> clients=new HashSet<TcpClient>();
        private byte[] received;
        private bool closed;
        private readonly System.Threading.Timer expiry;
        public bool Active {get{lock(gate)return !closed && clock.Elapsed.TotalSeconds<JournalPolicy.TransferLifetimeSeconds;}}
        public int Remaining {get{return Math.Max(0,JournalPolicy.TransferLifetimeSeconds-(int)clock.Elapsed.TotalSeconds);}}
        public DeviceLink(IPAddress address,byte[] data) {
            if(!Private(address))throw new ArgumentException("같은 Wi-Fi의 IPv4 주소를 선택해 주세요.");Validate(data);snapshot=data;
            var random=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(random);Token=Convert.ToBase64String(random).TrimEnd('=').Replace('+','-').Replace('/','_');
            listener=new TcpListener(address,0);listener.Start(4);Origin="http://"+address+":"+((IPEndPoint)listener.LocalEndpoint).Port;
            expiry=new System.Threading.Timer(delegate {Dispose();},null,JournalPolicy.TransferLifetimeSeconds*1000,Timeout.Infinite);
            var thread=new Thread(Accept){IsBackground=true,Name="MyDay device link"};thread.Start();
        }
        public static bool Private(IPAddress ip){var b=ip.GetAddressBytes();return b.Length==4&&(b[0]==127||b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31);}
        public static IPAddress[] Addresses(){return NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(a=>Private(a)&&!IPAddress.IsLoopback(a)).Distinct().ToArray();}
        public static Uri Parse(string input) {Uri uri;IPAddress ip;if(input==null||input.Length>256||!Uri.TryCreate(input.Trim(),UriKind.Absolute,out uri)||uri.Scheme!="http"||!IPAddress.TryParse(uri.Host,out ip)||!Private(ip)||uri.UserInfo!=""||uri.AbsolutePath!="/"||uri.Query!=""||!Regex.IsMatch(uri.Fragment,@"^#[A-Za-z0-9_-]{43}$"))throw new ArgumentException("MyDay의 같은 Wi-Fi 연결 주소를 QR 또는 복사로 입력해 주세요.");return uri;}
        public static DiaryBook Validate(byte[] bytes){using(var stream=new MemoryStream(bytes))return DiaryStore.Read(stream);}
        public static byte[] Encode(DiaryBook book){DiaryStore.Validate(book);using(var stream=new MemoryStream()){new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(stream,book);if(stream.Length>DiaryStore.MaxFileBytes)throw new IOException("백업이 128MB를 넘어요.");return stream.ToArray();}}
        public byte[] Incoming(){lock(gate)return received;}
        public static byte[] Download(string invitation){var uri=Parse(invitation);var request=(HttpWebRequest)WebRequest.Create(uri.GetLeftPart(UriPartial.Authority)+"/download");request.AllowAutoRedirect=false;request.Proxy=null;request.Timeout=30000;request.ReadWriteTimeout=30000;request.Headers[HttpRequestHeader.Authorization]="Bearer "+uri.Fragment.Substring(1);using(var response=(HttpWebResponse)request.GetResponse()){if(response.StatusCode!=HttpStatusCode.OK||response.ContentLength>DiaryStore.MaxFileBytes)throw new IOException("백업 응답을 확인해 주세요.");using(var input=response.GetResponseStream())using(var output=new MemoryStream()){var buffer=new byte[8192];int n;while((n=input.Read(buffer,0,buffer.Length))>0){if(output.Length+n>DiaryStore.MaxFileBytes)throw new IOException("백업이 너무 커요.");output.Write(buffer,0,n);}var bytes=output.ToArray();Validate(bytes);return bytes;}}}
        private void Accept(){while(Active){try{var client=listener.AcceptTcpClient();lock(gate){if(closed||clients.Count>=3){client.Close();continue;}clients.Add(client);}ThreadPool.QueueUserWorkItem(delegate{Handle(client);});}catch(SocketException){break;}catch(ObjectDisposedException){break;}}}
        private static bool Equal(string a,string b){if(a.Length!=b.Length)return false;int difference=0;for(int i=0;i<a.Length;i++)difference|=a[i]^b[i];return difference==0;}
        private void Handle(TcpClient client){try {client.ReceiveTimeout=30000;client.SendTimeout=30000;using(var stream=client.GetStream()){
            var header=new List<byte>();int ch;while((ch=stream.ReadByte())>=0){header.Add((byte)ch);if(header.Count>16384){Reply(stream,431,"요청이 너무 커요.");return;}int n=header.Count;if(n>=4&&header[n-4]==13&&header[n-3]==10&&header[n-2]==13&&header[n-1]==10)break;}
            var lines=Encoding.ASCII.GetString(header.ToArray()).Split(new[]{"\r\n"},StringSplitOptions.None);var first=lines[0].Split(' ');if(first.Length!=3){Reply(stream,400,"요청을 확인해 주세요.");return;}
            var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var line in lines.Skip(1).Where(l=>l.Length>0)){int i=line.IndexOf(':');if(i<=0||headers.ContainsKey(line.Substring(0,i))){Reply(stream,400,"요청을 확인해 주세요.");return;}headers[line.Substring(0,i)]=line.Substring(i+1).Trim();}
            if(!Active){Reply(stream,410,"연결 시간이 끝났어요.");return;}
            string value;if(!headers.TryGetValue("Host",out value)||value!=Origin.Substring(7)||headers.TryGetValue("Origin",out value)&&value!=Origin){Reply(stream,403,"이 연결에서는 사용할 수 없어요.");return;}
            if(first[0]=="GET"&&first[1]=="/"){Reply(stream,200,DeviceLinkPage.Html,"text/html; charset=utf-8");return;}
            if(!headers.TryGetValue("Authorization",out value)||!Equal(value,"Bearer "+Token)){Reply(stream,401,"연결 주소를 다시 확인해 주세요.");return;}
            if(first[0]=="GET"&&first[1]=="/download"){Reply(stream,200,snapshot,"application/json",true);return;}
            if(first[0]!="POST"||first[1]!="/upload"){Reply(stream,404,"요청을 찾지 못했어요.");return;}
            int length;if(headers.ContainsKey("Transfer-Encoding")||!headers.TryGetValue("Content-Type",out value)||value.Split(';')[0]!="application/json"||!headers.TryGetValue("Content-Length",out value)||!Int32.TryParse(value,out length)||length<=0||length>DiaryStore.MaxFileBytes){Reply(stream,413,"공통 백업 JSON은 128MB 이하로 보내 주세요.");return;}
            var bytes=new byte[length];int offset=0;while(offset<length){int n=stream.Read(bytes,offset,Math.Min(8192,length-offset));if(n<=0)throw new IOException("전송이 끊겼어요.");offset+=n;}Validate(bytes);
            lock(gate){if(closed){Reply(stream,410,"연결이 닫혔어요.");return;}if(received!=null){Reply(stream,409,"이미 도착한 백업이 있어요. 새 연결을 열어 주세요.");return;}received=bytes;}Reply(stream,200,"앱에서 도착한 기록을 확인해 주세요.");
        }}catch(Exception){try{if(client.Connected)Reply(client.GetStream(),400,"올바른 공통 백업인지 확인해 주세요.");}catch{}}finally{lock(gate)clients.Remove(client);client.Close();}}
        private static void Reply(Stream stream,int status,string body,string type="text/plain; charset=utf-8"){Reply(stream,status,Encoding.UTF8.GetBytes(body),type,false);}
        private static void Reply(Stream stream,int status,byte[] bytes,string type,bool download){string header="HTTP/1.1 "+status+" Result\r\nContent-Type: "+type+"\r\nContent-Length: "+bytes.Length+"\r\nCache-Control: no-store\r\nReferrer-Policy: no-referrer\r\nX-Content-Type-Options: nosniff\r\nContent-Security-Policy: default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'\r\nConnection: close\r\n"+(download?"Content-Disposition: attachment; filename=\"MyDay-transfer.json\"\r\n":"")+"\r\n";byte[] h=Encoding.ASCII.GetBytes(header);stream.Write(h,0,h.Length);stream.Write(bytes,0,bytes.Length);stream.Flush();}
        public void Dispose(){lock(gate){if(closed)return;closed=true;listener.Stop();foreach(var c in clients)c.Close();received=null;}if(expiry!=null)expiry.Dispose();}
    }
}
