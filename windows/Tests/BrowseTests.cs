using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static byte[] BrowseSnapshot(DiaryBook book)
        {
            using(var stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(stream,book); return stream.ToArray(); }
        }
        private static void RunBrowseTests(string directory)
        {
            var book=new DiaryBook(); var feb=DiaryEntry.Empty();
            var text=DiaryBlock.Create("text"); text.Text="My Quiet Walk · 봄을 기다리며 산책했다."; text.Title="title-only-marker"; text.Prompt="prompt-only-marker"; feb.Blocks.Add(text);
            var caption=DiaryBlock.Create("photo"); caption.Text="바닷가 산책 사진";
            string file=Path.Combine(directory,"browse-photo.png"); WritePhotoFixture(file); caption.Photo=DiaryPhoto.FromFile(file);
            var photo=DiaryEntry.Empty(); photo.Blocks.Add(caption);
            book.Days["2024-02-01"]=feb; book.Days["2024-02-29"]=photo;
            var march=DiaryEntry.Empty(); var todo=DiaryBlock.Create("todo"); todo.Text="산책 운동 20분"; todo.Checked=true; march.Blocks.Add(todo); book.Days["2024-03-01"]=march;
            book.Days["2024-02-10"]=DiaryEntry.Empty();
            var before=BrowseSnapshot(book);
            var cells=DiaryBrowse.Month(book,new DateTime(2024,2,29));
            Check(cells.Count==42 && cells.Count(c=>c.InMonth)==29 && cells[0].Date==new DateTime(2024,1,28) && cells.Last(c=>c.InMonth).Date==new DateTime(2024,2,29),"Calendar aligns Sunday-first weeks and leap day without dropping dates");
            Check(cells.First(c=>c.Date==new DateTime(2024,2,29)).HasPhoto && cells.First(c=>c.Date==new DateTime(2024,2,1)).HasEntry && !cells.First(c=>c.Date==new DateTime(2024,2,1)).HasPhoto && !cells.First(c=>c.Date==new DateTime(2024,2,2)).HasEntry,"Calendar distinguishes saved days, photo days and empty dates");
            Check(cells.First(c=>c.Date==new DateTime(2024,2,10)).HasEntry,"An explicitly saved empty diary remains visible in the calendar");
            var december=DiaryBrowse.Month(book,new DateTime(2024,12,1));
            Check(december.Any(c=>c.Date==new DateTime(2025,1,1) && !c.InMonth),"Calendar crossing December retains next-year dates");
            Check(DiaryBrowse.Month(book,DateTime.MinValue).Count==42 && DiaryBrowse.Month(book,DateTime.MaxValue).Any(c=>!c.Date.HasValue),"Calendar handles DateTime limits without date overflow");
            var matches=DiaryBrowse.Find(book," 산책 ");
            Check(matches.Select(m=>m.Date).SequenceEqual(new[] {new DateTime(2024,3,1),new DateTime(2024,2,29),new DateTime(2024,2,1)}),"Search finds user text across months, trims input and returns newest first");
            Check(DiaryBrowse.Find(book,"quiet WALK").Single().Date==new DateTime(2024,2,1),"Latin search is case-insensitive alongside Korean text");
            var photoMatch=DiaryBrowse.Find(book,"바닷가").Single();
            Check(photoMatch.Kind=="photo" && photoMatch.HasPhoto && photoMatch.Preview.Contains("바닷가"),"Photo captions participate in search with photo metadata");
            Check(DiaryBrowse.Find(book,"title-only-marker").Count==0 && DiaryBrowse.Find(book,"prompt-only-marker").Count==0 && DiaryBrowse.Find(book,caption.Photo.Substring(0,60)).Count==0,"Search excludes template headings, prompts and image bytes");
            Check(DiaryBrowse.Find(book,"운동").Single().Kind=="todo","User-written checklist text is searchable");
            Check(DiaryBrowse.Find(book,"",new DateTime(2024,2,1)).Count==3 && DiaryBrowse.Find(book,"   ",new DateTime(2024,1,1)).Count==0,"Month browsing lists saved entries only and handles an empty month");
            string longText=new string('가',400)+"\r\n찾는 기억 🌱"+new string('나',400);
            string snippet=DiaryBrowse.Snippet(longText,"찾는 기억");
            Check(snippet.Contains("찾는 기억") && snippet.Length<150 && !snippet.Contains("\n") && snippet.StartsWith("… "),"Long diary previews show the matching passage with bounded, single-line text");
            Check(before.SequenceEqual(BrowseSnapshot(book)),"Calendar and search leave all diary data untouched");
            var store=new DiaryStore(Path.Combine(directory,"browse")); store.Save(book); var loaded=store.Load();
            Check(DiaryBrowse.Find(loaded,"산책").Count==3 && DiaryBrowse.Month(loaded,new DateTime(2024,2,1)).First(c=>c.Date==new DateTime(2024,2,29)).HasPhoto,"Calendar markers and caption search work after a disk reload");
            caption.Text="새로 입력한 여행";
            Check(DiaryBrowse.Find(book,"바닷가").Count==0 && DiaryBrowse.Find(book,"새로 입력한").Single().Kind=="photo","Search reflects edited captions without a stale index");
        }
    }
}
