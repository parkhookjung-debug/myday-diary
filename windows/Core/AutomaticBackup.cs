using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace MyDay.Windows.Core {
    [DataContract] public sealed class AutomaticBackupState {
        [DataMember] public bool Enabled=true;
        [DataMember] public string LastSuccess="";
        [DataMember] public string Error="";
    }
    public sealed class AutomaticBackup {
        private readonly string root;
        public readonly string DirectoryPath;
        private string StatePath {get{return Path.Combine(root,"automatic-backup-state.json");}}
        private static readonly Dictionary<string,string> Errors=new Dictionary<string,string>();
        public AutomaticBackup(string root) {this.root=root;DirectoryPath=Path.Combine(root,"automatic-backups");}
        public AutomaticBackupState State() {var result=new AutomaticBackupState();try {if(File.Exists(StatePath))using(var s=File.OpenRead(StatePath))result=(AutomaticBackupState)new DataContractJsonSerializer(typeof(AutomaticBackupState)).ReadObject(s);}catch(Exception ex){result.Error=ex.Message;}lock(Errors){if(Errors.ContainsKey(root))result.Error=Errors[root];}return result;}
        private void WriteState(AutomaticBackupState state) {using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(AutomaticBackupState)).WriteObject(s,state);Atomic(StatePath,s.ToArray());}}
        public void SetEnabled(bool enabled) {var state=State();state.Enabled=enabled;WriteState(state);}
        public FileInfo[] Files() {return Directory.Exists(DirectoryPath)?new DirectoryInfo(DirectoryPath).GetFiles("daily-*.json").Where(f=>Regex.IsMatch(f.Name,@"^daily-\d{4}-\d{2}-\d{2}\.json$")).OrderByDescending(f=>f.Name).ToArray():new FileInfo[0];}
        public bool Capture(string committedFile,bool force=false,DateTime? day=null) {
            try {
                var state=State();if(!force && !state.Enabled)return false;
                string path=Path.Combine(DirectoryPath,"daily-"+DiaryStore.Key(day??DateTime.Today)+".json");
                if(!force && File.Exists(path))return true;
                byte[] bytes;
                using(var stream=File.OpenRead(committedFile)){DiaryStore.Read(stream);stream.Position=0;using(var output=new MemoryStream()){stream.CopyTo(output);bytes=output.ToArray();}}
                Directory.CreateDirectory(DirectoryPath);Atomic(path,bytes);
                var files=Files();long total=files.Sum(f=>f.Length);int count=files.Length;
                foreach(var old in files.Reverse()) {if(count<=JournalPolicy.AutomaticBackupCount && total<=JournalPolicy.AutomaticBackupLimitBytes)break;if(old.FullName==Path.GetFullPath(path))continue;long size=old.Length;old.Delete();total-=size;count--;}
                state.LastSuccess=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);state.Error="";WriteState(state);lock(Errors){Errors.Remove(root);}return true;
            }catch(Exception ex){lock(Errors){Errors[root]=ex.Message;}try{var state=State();state.Error=ex.Message;WriteState(state);}catch{}return false;}
        }
        private static void Atomic(string path,byte[] bytes) {Directory.CreateDirectory(Path.GetDirectoryName(path));string pending=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var s=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){s.Write(bytes,0,bytes.Length);s.Flush(true);}if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);}finally{if(File.Exists(pending))File.Delete(pending);}}
    }
}
