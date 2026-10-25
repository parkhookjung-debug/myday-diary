using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
namespace MyDay.Windows.Core {
 [DataContract] public sealed class PinState {[DataMember]public string Salt="";[DataMember]public string Hash="";[DataMember]public int Failures;[DataMember]public long RetryAfter;}
 public sealed class PinVault {
  private readonly string path;public PinVault(string directory){path=Path.Combine(directory,"screen-lock.json");}
  public PinState State(){if(!File.Exists(path))return null;using(var s=File.OpenRead(path)){if(s.Length>4096)throw new IOException("잠금 설정을 읽지 못했어요.");var state=(PinState)new DataContractJsonSerializer(typeof(PinState)).ReadObject(s);if(state==null||Convert.FromBase64String(state.Salt).Length!=16||Convert.FromBase64String(state.Hash).Length!=32||state.Failures<0||state.Failures>5||state.RetryAfter<0)throw new IOException("잠금 설정이 손상되었어요. 기록은 열지 않았어요.");return state;}}
  public bool Enabled {get{return State()!=null;}}
  public static byte[] Derive(string pin,byte[] salt){using(var k=new Rfc2898DeriveBytes(pin,salt,100000,HashAlgorithmName.SHA256))return k.GetBytes(32);}
  public static bool Valid(string pin){return pin!=null&&pin.Length>=6&&pin.Length<=12&&pin.All(c=>c>='0'&&c<='9');}
  public bool Verify(string pin){var s=State();if(s==null)return true;if(DateTime.UtcNow.Ticks<s.RetryAfter)throw new IOException("잠시 기다렸다가 다시 시도해 주세요. 5회 실패하면 30초 쉬어요.");var expected=Convert.FromBase64String(s.Hash);var got=Derive(pin??"",Convert.FromBase64String(s.Salt));int difference=0;for(int i=0;i<32;i++)difference|=expected[i]^got[i];if(difference==0){s.Failures=0;s.RetryAfter=0;Save(s);return true;}s.Failures++;if(s.Failures>=5){s.Failures=0;s.RetryAfter=DateTime.UtcNow.AddSeconds(30).Ticks;}Save(s);return false;}
  public void Set(string oldPin,string pin){if(!Verify(oldPin))throw new IOException("현재 PIN이 맞지 않아요.");if(!Valid(pin))throw new IOException("PIN은 숫자 6~12자리로 설정해 주세요.");var salt=new byte[16];using(var random=RandomNumberGenerator.Create())random.GetBytes(salt);Save(new PinState{Salt=Convert.ToBase64String(salt),Hash=Convert.ToBase64String(Derive(pin,salt))});}
  public void Disable(string oldPin){if(!Verify(oldPin))throw new IOException("현재 PIN이 맞지 않아요.");if(File.Exists(path))File.Delete(path);}
  private void Save(PinState state){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var s=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(PinState)).WriteObject(s,state);s.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 }
}
