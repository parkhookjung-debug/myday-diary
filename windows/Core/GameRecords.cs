using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace MyDay.Windows.Core
{
    [DataContract] public sealed class GameBest { [DataMember] public string Id; [DataMember] public int Best,Plays,Last; }
    [DataContract] internal sealed class GameScoreBook { [DataMember] public int Version=1; [DataMember] public List<GameBest> Entries=new List<GameBest>(); }
    public sealed class GameRecords
    {
        private readonly string file;
        private GameScoreBook book=new GameScoreBook();
        private bool readable=true;
        public string Problem {get;private set;}
        public bool Persistent {get{return file!=null;}}
        public GameRecords(string file=null){this.file=file;Refresh();}
        public static GameRecords Local(){return new GameRecords(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MyDay","Windows","Games","scores.json"));}
        private static bool ValidId(string id){DateTime day;return id=="snack"||id=="sling"||id=="fish"||id=="jump"||id=="bake"||id=="blocks"||(id!=null&&id.StartsWith("snack-daily-v1:",StringComparison.Ordinal)&&DateTime.TryParseExact(id.Substring(15),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day));}
        public static string DailyId(DateTime day){return "snack-daily-v1:"+day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
        private static GameScoreBook Read(string path){
            if(!File.Exists(path))return new GameScoreBook();
            var info=new FileInfo(path);if(info.Length>65536)throw new InvalidDataException("Game record too large");
            GameScoreBook result;using(var input=File.OpenRead(path))result=(GameScoreBook)new DataContractJsonSerializer(typeof(GameScoreBook)).ReadObject(input);
            if(result==null||result.Version!=1||result.Entries==null||result.Entries.Count>40)throw new InvalidDataException("Unsupported game records");
            if(result.Entries.Any(x=>x==null||!ValidId(x.Id)||x.Best<0||x.Best>1000000||x.Last<0||x.Last>1000000||x.Plays<0||x.Plays>1000000)||result.Entries.Select(x=>x.Id).Distinct().Count()!=result.Entries.Count)throw new InvalidDataException("Invalid game score");
            return result;
        }
        public void Refresh(){if(file==null)return;try{book=Read(file);readable=true;Problem=null;}catch(Exception ex){if(!(ex is IOException||ex is InvalidDataException||ex is SerializationException||ex is UnauthorizedAccessException||ex is ArgumentException))throw;readable=false;Problem="게임 기록을 읽지 못했어요. 기존 파일을 보존하고 임시 기록으로 진행해요.";}}
        public int Best(string id){var item=book.Entries.FirstOrDefault(x=>x.Id==id);return item==null?0:item.Best;}
        public int Plays(string id){var row=book.Entries.FirstOrDefault(x=>x.Id==id);return row==null?0:row.Plays;}
        private static void Add(GameScoreBook target,string id,int score){var row=target.Entries.FirstOrDefault(x=>x.Id==id);if(row==null){row=new GameBest{Id=id};target.Entries.Add(row);}row.Best=Math.Max(row.Best,score);row.Last=score;row.Plays=Math.Min(1000000,row.Plays+1);var old=target.Entries.Where(x=>x.Id.StartsWith("snack-daily-v1:",StringComparison.Ordinal)).OrderByDescending(x=>x.Id,StringComparer.Ordinal).Skip(31).ToArray();foreach(var item in old)target.Entries.Remove(item);}
        public bool Record(string id,int score){
            if(!ValidId(id)||score<0||score>1000000)throw new ArgumentException("Invalid game result");
            if(file==null){Add(book,id,score);return true;}
            if(!readable)return false;
            string temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using(var gate=new FileStream(file+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
                    // Reload under the lock so another game's higher score cannot be overwritten.
                    var next=Read(file);Add(next,id,score);
                    using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(GameScoreBook)).WriteObject(output,next);output.Flush(true);}
                    if(File.Exists(file))File.Replace(temporary,file,file+".previous");else File.Move(temporary,file);
                    book=next;Problem=null;
                }
                return true;
            }catch(Exception ex){if(!(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is SerializationException||ex is ArgumentException))throw;Problem="게임 기록을 저장하지 못했어요. 이번 점수는 결과 카드로 보관할 수 있어요.";return false;}
            finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
    }
    public sealed class MiniGameResult
    {
        public string Name,Details,Challenge;
        public int Score;
        public DateTime Day;
    }
}
