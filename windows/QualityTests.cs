using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
namespace MyDay.Windows { internal static partial class Tests {
 private static void RunQualityTests(string directory){
    var folder=Path.Combine(directory,"quality");var store=new DiaryStore(folder);var book=StudioFixture();store.Save(book);
    var first=File.ReadAllBytes(store.FilePath);book.Days.Values.First().Blocks[0].Text="두 번째 저장";store.Save(book);
    Check(RecoveryStore.Available(store),"Recovery validates a durable prior generation");
    File.WriteAllText(store.FilePath,"damaged current");bool rejected=false;try{store.Load();}catch{rejected=true;}
    Check(rejected && File.ReadAllText(store.FilePath)=="damaged current","Damaged current is never silently replaced");
    string archived=RecoveryStore.Restore(store);var restored=store.Load();
    Check(File.ReadAllText(archived)=="damaged current" && first.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Explicit recovery archives damaged bytes and restores exact prior generation");
    Check(restored.Studio.Furniture.Count==6 && restored.Studio.Pages[0].Favourite && restored.Collection.Actions.Count==book.Collection.Actions.Count,"Recovery keeps room, tags and economic ledger without extra rewards");
    File.WriteAllText(store.FilePath+".bak","bad prior");var before=File.ReadAllBytes(store.FilePath);rejected=false;try{RecoveryStore.Restore(store);}catch{rejected=true;}
    Check(!RecoveryStore.Available(store)&&rejected&&before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid recovery candidate cannot overwrite current diary");
    var target=Path.Combine(folder,"export.json");File.WriteAllText(target,"keep existing export");book.Days.Values.First().Blocks[0].Kind="bad";
    rejected=false;try{store.Export(target,book);}catch{rejected=true;}
    Check(rejected&&File.ReadAllText(target)=="keep existing export","Invalid export does not truncate selected destination");book.Days.Values.First().Blocks[0].Kind="text";
    using(var locked=new FileStream(target,FileMode.Open,FileAccess.Read,FileShare.None)){rejected=false;try{store.Export(target,book);}catch(IOException){rejected=true;}}
    Check(rejected&&File.ReadAllText(target)=="keep existing export","Failed atomic export retains existing destination");
    Check(Directory.GetFiles(folder,"*.tmp").Length==0,"Interrupted export leaves no pending file");
    var raw=new DiaryBook{Studio=new StudioData()};var portable=Path.Combine(folder,"studio-new.json");store.Export(portable,raw);using(var s=File.OpenRead(portable))Check(DiaryStore.Read(s).Version==9,"Fresh studio backup upgrades ownership before version-nine serialization");
    var fresh=Path.Combine(directory,"new-welcome");Check(WelcomeState.ShouldShow(fresh,false)&&!WelcomeState.ShouldShow(fresh,true),"Welcome appears only for new empty users");WelcomeState.Complete(fresh);Check(!WelcomeState.ShouldShow(fresh,false),"Welcome completion stays local across restarts");
    Check(RewardOverview.Text(new DiaryBook()).Contains("2회 뽑기 가능"),"Reward overview uses actual earned starting currency");
 }
}}
