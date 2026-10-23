using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace MyDay.Windows.Core
{
    [DataContract] public sealed class JournalRevision
    {
        [DataMember] public string Date;
        [DataMember] public string Captured;
        [DataMember] public string Reason;
        [DataMember] public DiaryEntry Entry;
        public string Path;
        public override string ToString() { return Date+" · "+Reason+" · "+Captured; }
    }
    public sealed class JournalHistory
    {
        private readonly string directory;
        public JournalHistory(string root) {directory=System.IO.Path.Combine(root,"journal-history");}
        private static byte[] Encode(object value,Type type) {using(var stream=new MemoryStream()) {new DataContractJsonSerializer(type).WriteObject(stream,value);return stream.ToArray();}}
        public static bool Same(DiaryEntry a,DiaryEntry b) {return a!=null && b!=null && Encode(a,typeof(DiaryEntry)).SequenceEqual(Encode(b,typeof(DiaryEntry)));}
        public void BeforeSave(DiaryBook previous,DiaryBook next)
        {
            foreach(var pair in previous.Days) {
                DiaryEntry target; next.Days.TryGetValue(pair.Key,out target);
                if(Same(pair.Value,target)) continue;
                bool destructive=target==null || pair.Value.Blocks.Any(b=>!target.Blocks.Any(n=>n.Id==b.Id && n.Photo==b.Photo));
                Capture(pair.Key,pair.Value,destructive?"삭제·교체 전":"수정 전",destructive);
            }
        }
        public void Capture(string date,DiaryEntry entry,string reason,bool force)
        {
            if(!JournalReport.Meaningful(entry)) return;
            var files=Directory.Exists(directory)?new DirectoryInfo(directory).GetFiles("*.json"):new FileInfo[0];
            var latest=files.Where(f=>f.Name.EndsWith("-"+date+".json",StringComparison.Ordinal)).OrderByDescending(f=>f.Name).FirstOrDefault();
            if(latest!=null) {
                var old=Read(latest.FullName); if(Same(old.Entry,entry)) return;
                if(!force && DateTime.UtcNow-latest.LastWriteTimeUtc<TimeSpan.FromSeconds(JournalPolicy.HistoryIntervalSeconds))return;
            }
            var revision=new JournalRevision {Date=date,Captured=DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'",CultureInfo.InvariantCulture),Reason=reason,Entry=entry};
            var bytes=Encode(revision,typeof(JournalRevision));
            if(files.Length>=JournalPolicy.HistoryLimitFiles || files.Sum(f=>f.Length)+bytes.Length>JournalPolicy.HistoryLimitBytes)
                throw new IOException("복구 보관함이 가득 찼어요. 설정의 휴지통·변경 이력에서 필요 없는 이전 기록을 정리한 뒤 다시 저장해 주세요.");
            Directory.CreateDirectory(directory);
            var path=System.IO.Path.Combine(directory,DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")+"-"+date+".json");
            var pending=path+".tmp";
            try {using(var output=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes,0,bytes.Length);output.Flush(true);}File.Move(pending,path);}
            finally {if(File.Exists(pending))File.Delete(pending);}
        }
        public List<JournalRevision> List() {return Directory.Exists(directory)?Directory.GetFiles(directory,"*.json").OrderByDescending(p=>p).Select(Read).ToList():new List<JournalRevision>();}
        public JournalRevision Read(string path)
        {
            using(var input=File.OpenRead(path)) {
                if(input.Length>DiaryStore.MaxFileBytes)throw new InvalidDataException("이력 파일이 너무 커요.");
                var item=(JournalRevision)new DataContractJsonSerializer(typeof(JournalRevision)).ReadObject(input);
                var book=new DiaryBook();book.Days[item.Date]=item.Entry;DiaryStore.Validate(book);item.Path=path;return item;
            }
        }
        public void Remove(JournalRevision item) {
            if(!List().Any(x=>x.Path==item.Path))throw new IOException("이력을 찾지 못했어요.");File.Delete(item.Path);
        }
    }
    public sealed class JournalReport
    {
        public int Days,Photos,Habits,Completed,Streak;
        public readonly Dictionary<string,int> Moods=new Dictionary<string,int>();
        public static bool Meaningful(DiaryEntry e) {return e!=null && e.Blocks.Any(b=>!String.IsNullOrWhiteSpace(b.Text) || b.Photo!=null || b.Checked);}
        public static JournalReport ForMonth(DiaryBook book,DateTime month) {
            var result=new JournalReport();int streak=0;
            for(int i=1;i<=DateTime.DaysInMonth(month.Year,month.Month);i++) {
                DiaryEntry e;
                if(!book.Days.TryGetValue(DiaryStore.Key(new DateTime(month.Year,month.Month,i)),out e) || !Meaningful(e)) {streak=0;continue;}
                result.Days++;result.Streak=Math.Max(result.Streak,++streak);result.Photos+=e.Blocks.Count(b=>b.Photo!=null);
                var habits=e.Blocks.Where(b=>b.Kind=="habit" && (!String.IsNullOrWhiteSpace(b.Text)||b.Checked));result.Habits+=habits.Count();result.Completed+=habits.Count(b=>b.Checked);
                string mood=e.Mood??"평온해요";result.Moods[mood]=(result.Moods.ContainsKey(mood)?result.Moods[mood]:0)+1;
            }return result;
        }
        public override string ToString() {return "기록한 날  "+Days+"일\n가장 길게 이어 쓴 날  "+Streak+"일\n사진  "+Photos+"장\n습관 실천  "+Completed+" / "+Habits+(Habits==0?"":" ("+(100*Completed/Habits)+"%)")+"\n\n오늘의 마음\n"+String.Join("\n",Moods.OrderByDescending(p=>p.Value).Select(p=>p.Key+"  "+p.Value+"일"))+"\n\n빈 페이지는 기록한 날에서 제외해요.\n감정은 직접 고른 값만 집계해요.";}
    }
}
