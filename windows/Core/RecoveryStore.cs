using System;
using System.IO;
namespace MyDay.Windows.Core {
    // Explicit recovery: never guess which snapshot the user wants to keep.
    public static class RecoveryStore {
        public static bool Available(DiaryStore store) {try {using(var s=File.OpenRead(store.FilePath+".bak"))DiaryStore.Read(s);return true;}catch{return false;}}
        public static string Restore(DiaryStore store) {
            if(new FileInfo(store.FilePath+".bak").Length>DiaryStore.MaxFileBytes)throw new InvalidDataException("직전 저장본의 크기가 너무 커요.");
            byte[] bytes=File.ReadAllBytes(store.FilePath+".bak");
            using(var s=new MemoryStream(bytes))DiaryStore.Read(s);
            string archive=store.FilePath+".damaged-"+Guid.NewGuid().ToString("N");
            if(File.Exists(store.FilePath))File.Copy(store.FilePath,archive,false);
            AtomicWrite(store.FilePath,bytes);return archive;
        }
        public static void AtomicWrite(string path,byte[] bytes) {
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {using(var s=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){s.Write(bytes,0,bytes.Length);s.Flush(true);}
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
