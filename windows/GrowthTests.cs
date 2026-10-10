using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunGrowthTests(string directory)
        {
            var day=new DateTime(2026,10,10); var entry=DiaryTemplates.NewPage();
            Check(SangmonGrowth.Level(null)==1 && SangmonGrowth.XP(null)==0,"Missing growth starts at level one with zero XP");
            Check(SangmonGrowth.Award(null,entry,day)==null,"Empty template does not award XP");
            entry.Blocks[0].Text=" \r\n "; Check(SangmonGrowth.Award(null,entry,day)==null,"Whitespace does not count as writing");
            entry.Blocks[0].Text="오늘의 한 줄"; var progress=SangmonGrowth.Award(null,entry,day);
            Check(progress.AwardedDays.SequenceEqual(new[] {"2026-10-10"}) && SangmonGrowth.XP(progress)==10,"Meaningful writing awards the actual local save day");
            var same=SangmonGrowth.Award(progress,entry,day.AddHours(23));
            Check(ReferenceEquals(progress,same) && SangmonGrowth.XP(same)==10,"Repeated saves and multiple diary dates cannot farm daily XP");
            var tomorrow=SangmonGrowth.Award(progress,entry,day.AddDays(1));
            Check(progress.AwardedDays.Count==1 && tomorrow.AwardedDays.Count==2,"Award candidate leaves the committed ledger untouched");
            var snapshot=SangmonGrowth.CaptureRecord(entry);
            entry.Theme=2; entry.Mood="행복해요"; DiaryLayout.EnableFree(entry,800); entry.Blocks.Add(DiaryBlock.Create("photo-slot"));
            Check(SangmonGrowth.SameRecord(snapshot,entry),"Changing mood, paper, positions or empty blocks does not count as new writing");
            entry.Blocks[0].Text+=" 새 내용";
            Check(!SangmonGrowth.SameRecord(snapshot,entry),"Editing diary content counts as activity independently of presentation");
            var checklist=DiaryEntry.FirstPage(); checklist.Blocks[1].Checked=true;
            Check(SangmonGrowth.HasRecord(checklist),"Completed todo counts as real diary activity");
            checklist.Blocks[1].Checked=false; checklist.Blocks[2].Checked=true;
            Check(SangmonGrowth.HasRecord(checklist),"Completed habit counts as real diary activity");
            checklist.Blocks.Clear(); checklist.Blocks.Add(DiaryBlock.Create("photo-slot"));
            Check(!SangmonGrowth.HasRecord(checklist),"An empty photo slot gives no XP");
            string photoPath=Path.Combine(directory,"growth-photo.png"); WritePhotoFixture(photoPath);
            checklist.Blocks[0].Kind="photo"; checklist.Blocks[0].Photo=DiaryPhoto.FromFile(photoPath);
            Check(SangmonGrowth.HasRecord(checklist),"An actual attached photo counts as a record");
            Check(MonsterVariants.All.Take(56).All(v=>SangmonGrowth.CanUse(null,v)),"Existing fifty-six appearances remain freely available");
            Check(SangmonGrowth.Rewards.Length==8 && SangmonGrowth.Rewards.All(v=>!SangmonGrowth.CanUse(null,v)),"Eight added rewards start locked");
            for(int i=1;i<5;i++) progress=SangmonGrowth.Award(progress,entry,day.AddDays(i));
            Check(SangmonGrowth.XP(progress)==50 && SangmonGrowth.Level(progress)==2 && SangmonGrowth.CanUse(progress,MonsterVariant.Apprentice) && !SangmonGrowth.CanUse(progress,MonsterVariant.Bookmark),"Five writing days unlock only the level-two reward");
            for(int i=5;i<45;i++) progress=SangmonGrowth.Award(progress,entry,day.AddDays(i));
            Check(SangmonGrowth.Level(progress)==10 && SangmonGrowth.Rewards.All(v=>SangmonGrowth.CanUse(progress,v)),"Forty-five days unlock all eight rewards");
            var afterBreak=SangmonGrowth.Award(progress,entry,day.AddYears(1));
            Check(SangmonGrowth.XP(afterBreak)==460,"A break never removes previous experience");
            var merged=SangmonGrowth.Merge(progress,tomorrow);
            Check(merged.AwardedDays.Count==45 && !ReferenceEquals(merged,progress) && tomorrow.AwardedDays.Count==2,"Backup merge unions dates without duplicate XP or source mutation");
            Check(SangmonGrowth.Merge(progress,null).AwardedDays.SequenceEqual(progress.AwardedDays) && SangmonGrowth.Merge(null,null)==null,"Legacy backup cannot reset growth or manufacture experience");
            var book=new DiaryBook {Progress=progress,CharacterStyle=MonsterVariants.Id(MonsterVariant.MemoryHero)}; book.Days["2000-01-01"]=entry;
            var store=new DiaryStore(Path.Combine(directory,"growth")); store.Save(book); var loaded=store.Load();
            Check(loaded.Version==3 && loaded.Progress.AwardedDays.SequenceEqual(progress.AwardedDays) && loaded.CharacterStyle==book.CharacterStyle,"Growth and equipped reward round-trip using diary version three");
            book.Layouts.Add(SavedLayouts.Capture(entry,"저장해 둔 형식",800)); store.Save(book);
            Check(store.Load().Version==3,"Saving reusable layouts does not downgrade growth format");
            string export=Path.Combine(directory,"growth-backup.json"); store.Export(export,book);
            using(var stream=File.OpenRead(export)) Check(DiaryStore.Read(stream).Progress.AwardedDays.Count==45,"Export includes all growth dates");
            book.Days.Clear(); store.Save(book);
            Check(SangmonGrowth.XP(store.Load().Progress)==450,"Diary removal leaves earned growth intact");
            var before=File.ReadAllBytes(store.FilePath); book.Progress=new SangmonProgress {AwardedDays=new List<string> {"bad-date"}};
            bool rejected=false; try {store.Save(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid growth date is rejected without overwriting storage");
            book.Progress.AwardedDays=new List<string> {"2026-10-10","2026-10-10"}; rejected=false;
            try {DiaryStore.Validate(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected,"Duplicate awarded dates are rejected");
            book.Progress.AwardedDays=null; rejected=false; try {DiaryStore.Validate(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected,"Null growth ledger is rejected");
            book.Progress.AwardedDays=Enumerable.Range(0,50001).Select(i=>DiaryStore.Key(new DateTime(2000,1,1).AddDays(i))).ToList();
            rejected=false; try {DiaryStore.Validate(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected,"Oversized growth ledger is rejected before saving");
            var legacy=new DiaryBook {CharacterStyle=MonsterVariants.Id(MonsterVariant.MemoryHero)}; legacy.Days["2000-01-01"]=entry; DiaryStore.Validate(legacy);
            Check(legacy.CharacterStyle=="original" && legacy.Days.Count==1 && legacy.Progress==null,"Locked imported appearance falls back without losing old writing");
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"Version\":2,\"Days\":[]}"))) Check(DiaryStore.Read(stream).Progress==null,"Older version-two backup remains readable without retroactive XP");
        }
    }
}
