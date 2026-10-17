using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunSavedLayoutTests(string directory)
        {
            var source=DiaryEntry.FirstPage(); source.LayoutMode="free"; source.Theme=2; source.PageStyle="dots";
            source.Blocks[0].Title="나만의 하루"; source.Blocks[0].Prompt="오늘 가장 좋았던 순간은?"; source.Blocks[0].Text="private-body-marker";
            source.Blocks[1].Checked=true; source.Blocks[1].Text="private-checklist-marker";
            var photo=DiaryBlock.Create("photo"); photo.Text="private-caption-marker"; string file=Path.Combine(directory,"layout-photo.png"); WritePhotoFixture(file); photo.Photo=DiaryPhoto.FromFile(file); source.Blocks.Add(photo);
            for(int i=0;i<source.Blocks.Count;i++) DiaryLayout.SetBounds(source.Blocks[i],new Rectangle(i%2*360+12,i/2*320+8,340,300));
            var sourceBook=new DiaryBook(); sourceBook.Days["2026-10-09"]=source; byte[] before=BrowseSnapshot(sourceBook);
            var layout=SavedLayouts.Capture(source," 나의 루틴 ",800);
            Check(before.SequenceEqual(BrowseSnapshot(sourceBook)) && layout.Name=="나의 루틴" && layout.Blocks.Select(b=>b.Bounds).SequenceEqual(source.Blocks.Select(DiaryLayout.Bounds)),"Capturing a layout preserves source diary and exact free coordinates");
            string serialized; using(var stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(SavedLayout)).WriteObject(stream,layout); serialized=Encoding.UTF8.GetString(stream.ToArray()); }
            Check(!serialized.Contains("private-") && !serialized.Contains(photo.Photo.Substring(0,60)) && layout.Blocks.Last().Kind=="photo-slot","Reusable layouts omit journal text, checklist state and image bytes");
            var first=DiaryTemplates.NewPage(); SavedLayouts.Apply(first,layout);
            Check(first.Blocks.Count==5 && first.Theme==2 && first.PageStyle=="dots" && first.LayoutMode=="free" && first.Blocks.Select(DiaryLayout.Bounds).SequenceEqual(layout.Blocks.Select(b=>b.Bounds)) && first.Blocks[0].Title=="나만의 하루" && first.Blocks[0].Prompt==source.Blocks[0].Prompt,"Applying to a fresh page replaces only the empty starter and restores style and geometry");
            Check(first.Blocks.All(b=>b.Text=="" && !b.Checked && b.Photo==null) && !first.Blocks.Select(b=>b.Id).Intersect(source.Blocks.Select(b=>b.Id)).Any(),"Applying creates independent empty blocks and a photo slot");
            var second=DiaryEntry.Empty(); SavedLayouts.Apply(second,layout); first.Blocks[0].Text="new first-day text"; first.Blocks[1].Checked=true; first.Blocks[0].X=300;
            Check(second.Blocks[0].Text=="" && !second.Blocks[1].Checked && second.Blocks[0].X==layout.Blocks[0].X && !first.Blocks.Select(b=>b.Id).Intersect(second.Blocks.Select(b=>b.Id)).Any(),"Different dates do not share text, checks, IDs or placement after applying a layout");
            var existing=DiaryEntry.FirstPage(); existing.LayoutMode="free"; existing.Blocks[0].Text="keep this text"; existing.Blocks[1].Checked=true; existing.Theme=1; existing.PageStyle="paper";
            DiaryLayout.ArrangeFree(existing,800); var oldIds=existing.Blocks.Select(b=>b.Id).ToArray(); var oldBounds=existing.Blocks.Select(DiaryLayout.Bounds).ToArray(); int bottom=oldBounds.Max(r=>r.Bottom)+16;
            SavedLayouts.Apply(existing,layout);
            Check(existing.Blocks.Take(4).Select(b=>b.Id).SequenceEqual(oldIds) && existing.Blocks.Take(4).Select(DiaryLayout.Bounds).SequenceEqual(oldBounds) && existing.Blocks[0].Text=="keep this text" && existing.Blocks[1].Checked && existing.Theme==1 && existing.PageStyle=="paper" && existing.Blocks.Skip(4).All(b=>b.Y>=bottom),"Adding to an existing diary preserves content, checks, style and placement and appends below free blocks");
            var book=new DiaryBook(); book.Layouts=SavedLayouts.Add(book.Layouts,layout); book.Days["2026-10-10"]=second;
            var store=new DiaryStore(Path.Combine(directory,"saved-layouts")); store.Save(book); var restored=store.Load();
            Check(restored.Version==2 && restored.Layouts.Single().Name==layout.Name && restored.Layouts[0].Blocks.Select(b=>b.Bounds).SequenceEqual(layout.Blocks.Select(b=>b.Bounds)) && restored.Days["2026-10-10"].Blocks.Last().Kind=="photo-slot","Layouts and photo slots survive reload with the protected version-two format");
            string backup=Path.Combine(directory,"saved-layout-backup.json"); store.Export(backup,book);
            using(var stream=File.OpenRead(backup)) Check(DiaryStore.Read(stream).Layouts[0].Blocks.Last().Kind=="photo-slot","JSON backup includes reusable layouts and empty photo slots");
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"Version\":1,\"Days\":[]}"))) Check(DiaryStore.Read(stream).Layouts.Count==0,"Version-one diaries without custom layouts remain readable");
            var renamed=SavedLayouts.Rename(book.Layouts,layout.Id,"주말 루틴");
            Check(renamed[0].Id==layout.Id && renamed[0].Name=="주말 루틴" && book.Layouts[0].Name=="나의 루틴","Rename retains identity without mutating the original list before save");
            var duplicate=SavedLayouts.Copy(layout,"토요일"); duplicate.Id=Guid.NewGuid().ToString("N"); var two=SavedLayouts.Add(renamed,duplicate); bool rejected=false;
            try { SavedLayouts.Rename(two,duplicate.Id,"주말 루틴"); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && two[1].Name=="토요일" && two[0].Name=="주말 루틴","Conflicting names are rejected without changing saved layouts");
            var conflict=SavedLayouts.Copy(layout,"주말 루틴"); conflict.Id=Guid.NewGuid().ToString("N");
            var merged=SavedLayouts.Merge(renamed,new[] {conflict});
            Check(merged.Count==2 && merged[0].Name=="주말 루틴" && merged[1].Name=="주말 루틴 (2)","Import preserves both layouts when distinct IDs have the same name");
            var changed=SavedLayouts.Copy(layout,"백업 배치"); var replaced=SavedLayouts.Merge(renamed,new[] {changed});
            Check(replaced.Count==1 && replaced[0].Id==layout.Id && replaced[0].Name=="백업 배치" && renamed[0].Name=="주말 루틴","Import updates matching IDs without changing the existing list during preparation");
            Check(SavedLayouts.Merge(renamed,new SavedLayout[0]).Count==1,"Importing an old diary retains existing custom layouts");
            book.Layouts=two; store.Save(book); var savedBytes=File.ReadAllBytes(store.FilePath); int width=two[0].Blocks[0].Width; two[0].Blocks[0].Width=1; rejected=false;
            try { store.Save(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && savedBytes.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid layout geometry cannot overwrite the stored diary"); two[0].Blocks[0].Width=width;
            var full=DiaryEntry.Empty(); for(int i=0;i<199;i++) full.Blocks.Add(DiaryBlock.Create("text"));
            var fullBook=new DiaryBook(); fullBook.Days["2026-10-11"]=full; before=BrowseSnapshot(fullBook); rejected=false;
            try { SavedLayouts.Apply(full,layout); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && before.SequenceEqual(BrowseSnapshot(fullBook)),"Applying beyond the block limit leaves the complete diary unchanged");
            var low=DiaryEntry.Empty(); low.LayoutMode="free"; var last=DiaryBlock.Create("text"); DiaryLayout.SetBounds(last,new Rectangle(0,10000,300,1400)); low.Blocks.Add(last);
            var lowBook=new DiaryBook(); lowBook.Days["2026-10-12"]=low; before=BrowseSnapshot(lowBook); rejected=false;
            try { SavedLayouts.Apply(low,layout); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && before.SequenceEqual(BrowseSnapshot(lowBook)),"Insufficient free-canvas space rejects a layout before adding any blocks");
            var fifty=new List<SavedLayout>();
            for(int i=0;i<50;i++) { var item=SavedLayouts.Copy(layout,"배치 "+i); item.Id=Guid.NewGuid().ToString("N"); fifty.Add(item); }
            rejected=false; try { SavedLayouts.Add(fifty,layout); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && fifty.Count==50,"Custom layout storage is limited without modifying the existing list");
            rejected=false; try { SavedLayouts.Capture(DiaryEntry.Empty(),"빈 배치",800); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Empty layouts cannot be saved");
            var automatic=DiaryEntry.FirstPage(); var auto=SavedLayouts.Capture(automatic,"자동 배치",800); var autoPage=DiaryEntry.Empty(); SavedLayouts.Apply(autoPage,auto);
            Check(autoPage.LayoutMode=="cards" && DiaryLayout.AutoArrange(autoPage.Blocks,500).All(r=>r.Right<=500) && automatic.Blocks.All(b=>b.Width==0),"Automatic layouts remain responsive and capturing does not write geometry into the source");
            var longAuto=DiaryEntry.Empty(); for(int i=0;i<200;i++) { var block=DiaryBlock.Create("text"); block.Wide=true; longAuto.Blocks.Add(block); }
            var longSaved=SavedLayouts.Capture(longAuto,"긴 자동 일기",3000); var longPage=DiaryEntry.Empty(); SavedLayouts.Apply(longPage,longSaved);
            Check(longPage.Blocks.Count==200 && longSaved.Blocks.All(b=>b.Width<=DiaryLayout.MaxWidth) && DiaryLayout.AutoArrange(longPage.Blocks,500).All(r=>r.Right<=500) && longPage.Blocks.All(b=>b.Width==0),"Large automatic layouts are saved at bounded width and remain responsive when reused");
            string emojiName=new string('가',35)+"🌱"+"일기장";
            var emoji=SavedLayouts.Copy(layout,emojiName); var emojiOther=SavedLayouts.Copy(emoji); emojiOther.Id=Guid.NewGuid().ToString("N");
            var emojiMerged=SavedLayouts.Merge(new[] {emoji},new[] {emojiOther});
            using(var stream=new MemoryStream()) new DataContractJsonSerializer(typeof(SavedLayout)).WriteObject(stream,emojiMerged[1]);
            Check(emojiMerged[1].Name.Length<=40 && emojiMerged[1].Name.EndsWith("(2)"),"Import name suffixes do not split emoji surrogate pairs at the name limit");
            book.Layouts=book.Layouts.Where(l=>l.Id!=layout.Id).ToList(); store.Save(book);
            Check(store.Load().Layouts.Count==1 && store.Load().Days["2026-10-10"].Blocks.Count==5,"Deleting a saved layout retains the diaries already created from it");
        }
    }
}
