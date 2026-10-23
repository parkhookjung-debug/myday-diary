using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows {
    internal static partial class Tests {
        private static void RunJournalToolsTests(string directory) {
            var path=Path.Combine(directory,"journal-tools");var store=new DiaryStore(path);var book=new DiaryBook();
            var e=DiaryEntry.Empty();e.Mood="행복해요";e.Blocks.Add(DiaryBlock.Create("text"));e.Blocks[0].Text="복구할 소중한 하루";e.Blocks.Add(DiaryBlock.Create("habit"));e.Blocks[1].Text="산책";e.Blocks[1].Checked=true;
            book.Days["2024-02-28"]=e;store.Save(book);
            book.Days["2024-02-29"]=DiaryEntry.Empty();store.Save(book);
            var history=new JournalHistory(path);Check(history.List().Count==0,"No revision for new or unchanged diary");
            e.Blocks[0].Text="수정한 기록";store.Save(book);
            Check(history.List().Count==1 && history.List()[0].Entry.Blocks[0].Text=="복구할 소중한 하루","Old content is durably archived before overwrite");
            e.Blocks.RemoveAt(1);store.Save(book);
            Check(history.List().Count==2 && history.List()[0].Entry.Blocks.Any(b=>b.Kind=="habit" && b.Checked),"Destructive block removal bypasses checkpoint interval");
            store.Save(book);Check(history.List().Count==2,"Unchanged saves do not duplicate revisions");
            book.Days.Remove("2024-02-28");store.Save(book);Check(history.List().Count==3 && store.Load().Days.Count==1,"Whole diary deletion retains previous record in trash");
            var revision=history.List().First(x=>x.Entry.Blocks.Any(b=>b.Kind=="habit"));book.Days[revision.Date]=revision.Entry;store.Save(book);
            Check(store.Load().Days[revision.Date].Blocks.Any(b=>b.Checked) && book.Progress==null && book.Collection==null,"Restoration changes diary content without earning XP or currency");
            var next=DiaryEntry.Empty();next.Blocks.Add(DiaryBlock.Create("text"));next.Blocks[0].Text="윤년 마지막 날";book.Days["2024-02-29"]=next;
            var outside=DiaryEntry.Empty();outside.Blocks.Add(DiaryBlock.Create("text"));outside.Blocks[0].Text="다음 달";book.Days["2024-03-01"]=outside;
            var report=JournalReport.ForMonth(book,new DateTime(2024,2,1));
            Check(report.Days==2 && report.Streak==2 && report.Habits==1 && report.Completed==1 && report.Moods["행복해요"]==1,"Monthly report handles leap day, blank pages, streak and habit denominator");
            Check(JournalReport.ForMonth(book,new DateTime(2023,2,1)).Days==0,"Empty month has zero totals without division by zero");
            history.Remove(history.List()[0]);Check(history.List().Count==2,"Explicit deletion removes only selected revision");
            string blocked=Path.Combine(directory,"blocked-history");Directory.CreateDirectory(blocked);var badStore=new DiaryStore(blocked);badStore.Save(book);File.WriteAllText(Path.Combine(blocked,"journal-history"),"blocked");
            string before=File.ReadAllText(badStore.FilePath);book.Days[revision.Date].Blocks.Clear();bool rejected=false;
            try {badStore.Save(book);}catch(IOException){rejected=true;}
            Check(rejected && File.ReadAllText(badStore.FilePath)==before,"Archive write failure cannot overwrite or delete original diary");
            var prefs=new JournalPreferences {Enabled=true,Minute=1291};prefs.Save(path);var roundtrip=JournalPreferences.Load(path);Check(roundtrip.Enabled && roundtrip.Minute==1291,"Optional reminder settings survive atomic save");
        }
    }
}
