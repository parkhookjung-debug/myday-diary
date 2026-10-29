using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MyDay.Windows.Core;

namespace MyDay.Windows {
    internal static partial class Tests {
        private static void RunDeviceToolsTests(string directory) {
            var matrix=new DeviceQr("http://127.0.0.1:12345/#"+new string('a',43)).Matrix();string hash;
            using(var sha=SHA256.Create())hash=String.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(matrix)).Select(b=>b.ToString("x2")));
            Check(hash=="a012621192ba27098623640ff5a0d505348b71a3aa4df275a4cc8a93ac19e661","Native QR matches independent Nayuki v6-L reference matrix");
            foreach(string host in new[]{"10.0.0.1","192.168.0.5","172.16.0.1","127.0.0.1"})Check(DeviceLink.Private(IPAddress.Parse(host)),"Private device address accepted: "+host);
            foreach(string text in new[]{"http://8.8.8.8/#"+new string('a',43),"https://192.168.0.1/#"+new string('a',43),"http://localhost/#"+new string('a',43),"http://192.168.0.1/path#"+new string('a',43),"http://user@192.168.0.1/#"+new string('a',43)}){bool rejected=false;try{DeviceLink.Parse(text);}catch(ArgumentException){rejected=true;}Check(rejected,"Device invitation rejects untrusted destination");}
            var book=new DiaryBook();var entry=DiaryEntry.Empty();entry.Blocks.Add(DiaryBlock.Create("text"));entry.Blocks[0].Text="기기 사이 한글과 사진";book.Days["2026-10-24"]=entry;
            using(var link=new DeviceLink(IPAddress.Loopback,DeviceLink.Encode(book))){var bytes=DeviceLink.Download(link.Invitation);Check(DeviceLink.Validate(bytes).Days["2026-10-24"].Blocks[0].Text==entry.Blocks[0].Text,"Native in-app handoff downloads real diary without Python");
                var request=(HttpWebRequest)WebRequest.Create(link.Origin+"/upload");request.Proxy=null;request.Method="POST";request.ContentType="application/json";request.Headers[HttpRequestHeader.Authorization]="Bearer "+link.Token;request.ContentLength=bytes.Length;using(var output=request.GetRequestStream())output.Write(bytes,0,bytes.Length);using(var response=request.GetResponse()){};
                Check(link.Incoming()!=null && DeviceLink.Validate(link.Incoming()).Days.Count==1,"Uploaded handoff waits in memory for explicit import");
                request=(HttpWebRequest)WebRequest.Create(link.Origin+"/download");request.Proxy=null;bool denied=false;try{using(var response=request.GetResponse()){}}catch(WebException ex){denied=((HttpWebResponse)ex.Response).StatusCode==HttpStatusCode.Unauthorized;}Check(denied,"Device handoff denies requests without invitation token");
                link.Dispose();Check(!link.Active && link.Incoming()==null,"Closing device link revokes access and clears pending upload");
            }
            string root=Path.Combine(directory,"automatic-test");var store=new DiaryStore(root);store.Save(book);var backup=new AutomaticBackup(root);
            Check(backup.Files().Length==1 && backup.State().LastSuccess!="","Successful diary save creates one daily complete backup");
            for(int i=1;i<=5;i++)backup.Capture(store.FilePath,false,new DateTime(2030,1,i));
            Check(backup.Files().Length==3 && backup.Files().All(f=>f.Name.CompareTo("daily-2030-01-03.json")>=0),"Automatic backups retain only the newest three generated days");
            foreach(var file in backup.Files())using(var stream=file.OpenRead())Check(DiaryStore.Read(stream).Days.Count==1,"Daily backup is a valid portable diary");
            File.WriteAllText(Path.Combine(backup.DirectoryPath,"my-own-copy.json"),"keep");backup.Capture(store.FilePath,true,new DateTime(2030,1,6));Check(File.Exists(Path.Combine(backup.DirectoryPath,"my-own-copy.json")),"Backup retention never deletes unrelated filenames");
            backup.SetEnabled(false);Check(!backup.Capture(store.FilePath,false,new DateTime(2030,1,7)) && backup.Files().Length==3,"Turning automatic backup off retains existing copies");
            string blocked=Path.Combine(directory,"automatic-failure");Directory.CreateDirectory(blocked);File.WriteAllText(Path.Combine(blocked,"automatic-backups"),"blocked");var safe=new DiaryStore(blocked);safe.Save(book);Check(safe.Load().Days.Count==1 && new AutomaticBackup(blocked).State().Error!="","Automatic backup failure never loses or rejects diary save");
        }
    }
}
