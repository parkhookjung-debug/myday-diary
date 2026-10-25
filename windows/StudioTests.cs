using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
namespace MyDay.Windows {
 internal static partial class Tests {
  internal static DiaryBook StudioFixture(){var book=new DiaryBook();SangmonCollection.Migrate(book);book.Studio=new StudioData{RoomTheme=1};for(int i=0;i<6;i++)book.Studio.Furniture.Add(new RoomItem{Id="room-"+i,Kind=StudioCatalog.FurnitureKinds[i],X=100+i*100,Y=300});book.Studio.Pages.Add(new StudioNote{Date="2026-10-25",Tags=new System.Collections.Generic.List<string>{"산책","학교"},Favourite=true});var day=DiaryEntry.Empty();var b=DiaryBlock.Create("text");b.Text="사진과 상몬의 방을 꾸민 날";day.Blocks.Add(b);book.Days["2026-10-25"]=day;return book;}
  public static void ExportStudio(string path){var book=StudioFixture();new DiaryStore(Path.GetDirectoryName(path)).Export(path,book);}
  private static void RunStudioTests(string directory){
   var folder=Path.Combine(directory,"studio");var store=new DiaryStore(folder);var book=StudioFixture();store.Save(book);var loaded=store.Load();Check(loaded.Version==9&&loaded.Studio.RoomTheme==1&&loaded.Studio.Furniture.Count==6&&loaded.Studio.Pages[0].Tags.SequenceEqual(new[]{"산책","학교"})&&loaded.Studio.Pages[0].Favourite,"Version-nine studio round trip keeps room positions, tags and favourites");
   var before=File.ReadAllBytes(store.FilePath);book.Studio.Furniture[0].X=901;bool rejected=false;try{store.Save(book);}catch(InvalidDataException){rejected=true;}Check(rejected&&before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid studio cannot overwrite saved diary");book.Studio.Furniture[0].X=100;
   var old=new StudioData();old.Pages.Add(new StudioNote{Date="2026-10-24",Tags=new System.Collections.Generic.List<string>{"이전"}});var merged=StudioData.Merge(old,loaded.Studio);Check(merged.Pages.Count==2&&merged.Note("2026-10-25").Favourite&&merged.Note("2026-10-24").Tags[0]=="이전","Studio import merges notes by date and uses incoming room without erasing other dates");
   Check(StudioData.Question(new DateTime(2026,10,25))==StudioCatalog.Questions[(new DateTime(2026,10,25)-new DateTime(1,1,1)).Days%12],"Daily prompt is stable across native platforms");
   using(var image=new Bitmap(400,200)){using(var g=Graphics.FromImage(image))g.Clear(Color.Coral);string original=PhotoDecor.Encode(image);using(var square=PhotoDecor.Crop(image,1)){Check(square.Width==200&&square.Height==200&&image.Width==400,"Photo crop edits a separate bitmap and retains original");using(var frame=PhotoDecor.Frame(square,0)){Check(frame.Width>square.Width&&frame.Height>square.Height,"Photo frame includes full photo");}}Check(original==PhotoDecor.Encode(image),"Preview transformations do not replace source before applying");}
   for(int i=0;i<4;i++)using(var sticker=PhotoDecor.Sticker(i)){var b=DiaryBlock.Create("photo");b.Photo=PhotoDecor.Encode(sticker);DiaryPhoto.Validate(b);Check(b.Photo.Length>0,"Offline sticker is a portable normalized diary photo "+i);}
   foreach(string kind in RoomArt.Kinds)using(var picture=RoomArt.Image(kind))Check(picture.Width==240&&picture.Height==240,"First-party cozy furniture renders: "+kind);
   foreach(var size in new[]{new Size(650,360),new Size(360,330),new Size(900,600)})using(var canvas=new MyDay.Windows.UI.RoomCanvas(new StudioData(),StudioFixture(),()=>true)){canvas.Size=size;foreach(int x in new[]{0,900})foreach(int y in new[]{0,900})Check(canvas.ClientRectangle.Contains(canvas.BoundsOf(new RoomItem{X=x,Y=y})),"Furniture remains inside native room at every corner and viewport");}
   var vault=new PinVault(Path.Combine(directory,"pin"));Check(!vault.Enabled,"Screen lock is opt-in");vault.Set("","123456");Check(vault.Enabled&&vault.Verify("123456")&&!vault.Verify("654321"),"PIN validates correct input and rejects wrong input");Check(!File.ReadAllText(Path.Combine(directory,"pin/screen-lock.json")).Contains("123456"),"PIN plaintext is never stored");vault.Set("123456","234567");Check(vault.Verify("234567")&&!vault.Verify("123456"),"Changing PIN requires the current PIN");vault.Disable("234567");Check(!vault.Enabled,"Disabling lock verifies PIN");
   var blocked=new PinVault(Path.Combine(directory,"blocked-pin"));blocked.Set("","123456");for(int i=0;i<5;i++)blocked.Verify("000000");rejected=false;try{new PinVault(Path.Combine(directory,"blocked-pin")).Verify("123456");}catch(IOException){rejected=true;}Check(rejected,"Five failed PINs enforce persistent cooldown across restart");
   string hash=string.Concat(PinVault.Derive("123456",Enumerable.Range(0,16).Select(i=>(byte)i).ToArray()).Select(b=>b.ToString("x2")));Check(hash=="3e3d2422f00f2cc1d1bad045819bfb8360117d59c588035c4294f3403ac097a5","PIN derivation matches independent PBKDF2-SHA256 reference");
  }
 }
}
