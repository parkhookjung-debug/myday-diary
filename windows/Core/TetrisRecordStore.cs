using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
namespace MyDay.Windows.Core {
 [DataContract] internal sealed class TetrisRecord { [DataMember] public int HighScore; }
 public sealed class TetrisRecordStore {
  private readonly string path;
  public TetrisRecordStore(string directory){path=Path.Combine(directory,"tetris-record.json");}
  public int Load(){if(!File.Exists(path))return 0;var info=new FileInfo(path);if(info.Length>1024)throw new InvalidDataException("테트리스 기록 파일이 너무 커요.");TetrisRecord value;using(var stream=File.OpenRead(path))value=(TetrisRecord)new DataContractJsonSerializer(typeof(TetrisRecord)).ReadObject(stream);if(value==null||value.HighScore<0)throw new InvalidDataException("테트리스 최고 점수 기록이 올바르지 않아요.");return value.HighScore;}
  public int SaveIfHigher(int score){if(score<0)throw new ArgumentOutOfRangeException("score");int best=Load();if(score<=best)return best;using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(TetrisRecord)).WriteObject(stream,new TetrisRecord{HighScore=score});RecoveryStore.AtomicWrite(path,stream.ToArray());}return score;}
 }
}
