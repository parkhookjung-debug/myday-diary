using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
namespace MyDay.Windows.Core {
 [DataContract] public sealed class RoomItem {[DataMember] public string Id=Guid.NewGuid().ToString("N");[DataMember] public string Kind="plant";[DataMember] public int X=400;[DataMember] public int Y=500;}
 [DataContract] public sealed class StudioNote {[DataMember] public string Date="";[DataMember] public List<string> Tags=new List<string>();[DataMember] public bool Favourite;}
 [DataContract] public sealed class Anniversary {[DataMember] public string Id=Guid.NewGuid().ToString("N");[DataMember] public string Name="";[DataMember] public string Date="";[DataMember(IsRequired=true)] public bool RepeatYearly=true;}
 [DataContract] public sealed class StudioData {
  [DataMember] public int RoomTheme;[DataMember] public List<RoomItem> Furniture=new List<RoomItem>();[DataMember] public List<StudioNote> Pages=new List<StudioNote>();[DataMember(EmitDefaultValue=false)] public List<Anniversary> Anniversaries=new List<Anniversary>();
  [OnDeserializing] private void BeforeRead(StreamingContext context){Anniversaries=new List<Anniversary>();}
  [OnDeserialized] private void AfterRead(StreamingContext context){if(Anniversaries==null)Anniversaries=new List<Anniversary>();}
  public static void Validate(StudioData value){if(value==null)return;if(value.RoomTheme<0||value.RoomTheme>2||value.Furniture==null||value.Furniture.Count>24||value.Pages==null||value.Pages.Count>50000||value.Anniversaries==null||value.Anniversaries.Count>StudioCatalog.AnniversaryLimit)throw new InvalidDataException("꾸미기 자료의 크기나 배경이 올바르지 않아요.");var ids=new HashSet<string>();foreach(var f in value.Furniture){if(f==null||string.IsNullOrEmpty(f.Id)||f.Id.Length>64||!ids.Add(f.Id)||!StudioCatalog.FurnitureKinds.Contains(f.Kind)||f.X<0||f.X>900||f.Y<0||f.Y>900)throw new InvalidDataException("가구의 종류나 위치를 확인해 주세요.");}ids.Clear();foreach(var p in value.Pages){DateTime d;if(p==null||!DateTime.TryParseExact(p.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d)||!ids.Add(p.Date)||p.Tags==null||p.Tags.Count>10||p.Tags.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=p.Tags.Count||p.Tags.Any(t=>string.IsNullOrWhiteSpace(t)||t.Length>20||t.Any(char.IsControl)))throw new InvalidDataException("태그는 날짜당 최대 10개, 각 20자예요.");}ids.Clear();foreach(var a in value.Anniversaries){DateTime d;if(a==null||string.IsNullOrWhiteSpace(a.Id)||a.Id.Length>64||!ids.Add(a.Id)||string.IsNullOrWhiteSpace(a.Name)||a.Name.Length>StudioCatalog.AnniversaryNameLength||a.Name.Any(char.IsControl)||a.Id.Any(char.IsControl)||!DateTime.TryParseExact(a.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))throw new InvalidDataException("기념일 이름이나 날짜를 확인해 주세요.");}}
  public static StudioData Merge(StudioData current,StudioData incoming){if(incoming==null)return current;Validate(current);Validate(incoming);var result=new StudioData {RoomTheme=incoming.RoomTheme,Furniture=incoming.Furniture.Select(f=>new RoomItem{Id=f.Id,Kind=f.Kind,X=f.X,Y=f.Y}).ToList(),Anniversaries=((current==null?new List<Anniversary>():current.Anniversaries).Concat(incoming.Anniversaries)).GroupBy(a=>a.Id).Select(g=>g.Last()).Select(a=>new Anniversary{Id=a.Id,Name=a.Name,Date=a.Date,RepeatYearly=a.RepeatYearly}).ToList()};var notes=(current==null?new List<StudioNote>():current.Pages).ToDictionary(p=>p.Date);foreach(var p in incoming.Pages)notes[p.Date]=p;result.Pages=notes.Values.OrderBy(p=>p.Date).Select(p=>new StudioNote{Date=p.Date,Tags=p.Tags.ToList(),Favourite=p.Favourite}).ToList();Validate(result);return result;}
  public StudioNote Note(string date){return Pages.FirstOrDefault(p=>p.Date==date)??new StudioNote{Date=date};}
  public void SetNote(StudioNote note){var next=Pages.Where(p=>p.Date!=note.Date).ToList();next.Add(note);var old=Pages;Pages=next;try{Validate(this);}catch{Pages=old;throw;}}
  public static string Question(DateTime date){return StudioCatalog.Questions[(int)(date.Date-new DateTime(1,1,1)).TotalDays%StudioCatalog.Questions.Length];}
 }
}
