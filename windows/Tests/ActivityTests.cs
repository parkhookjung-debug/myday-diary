using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunActivityTests(string directory)
        {
            var day=new DateTime(2026,10,16);var entry=DiaryEntry.Empty();
            entry.Blocks.Add(new DiaryBlock {Id="text",Kind="text",Text="공백을 제외하고 스무 글자가 넘는 오늘의 충분한 일기를 기록합니다"});
            entry.Blocks.Add(new DiaryBlock {Id="photo",Kind="photo",Text="",Photo="photo-bytes"});
            entry.Blocks.Add(new DiaryBlock {Id="todo",Kind="todo",Text="할 일",Checked=true});
            entry.Blocks.Add(new DiaryBlock {Id="habit",Kind="habit",Text="습관",Checked=true});
            var attendance=SangmonGrowth.CheckIn(null,day);var all=SangmonGrowth.Award(attendance,entry,day);
            Check(SangmonGrowth.XP(attendance)==5 && attendance.AwardedDays.Count==0 && SangmonGrowth.XP(all)==40 && SangmonGrowth.TodayXP(all,day)==40,"Attendance and all five record missions total at most 40 XP per local day");
            Check(ReferenceEquals(all,SangmonGrowth.CheckIn(all,day)) && ReferenceEquals(all,SangmonGrowth.Award(all,entry,day)),"Repeated attendance, edits and checks never duplicate daily mission credit");
            var baseline=SangmonGrowth.CaptureRecord(entry);entry.Blocks[0].Text+=" 다음 날 수정";
            var tomorrow=SangmonGrowth.Award(all,entry,day.AddDays(1),baseline);
            Check(SangmonGrowth.XP(tomorrow)==60 && SangmonGrowth.Flags(tomorrow,day.AddDays(1))==SangmonGrowth.Writing,"Editing text tomorrow does not re-credit old photos or completed checkboxes");
            var merged=SangmonGrowth.Merge(all,SangmonGrowth.CheckIn(null,day));
            Check(SangmonGrowth.XP(merged)==40 && SangmonGrowth.XP(SangmonGrowth.Merge(merged,all))==40 && SangmonGrowth.XP(attendance)==5,"Backup merge unions mission flags without duplicate XP or source mutation");
            var book=new DiaryBook {Progress=all};var store=new DiaryStore(Path.Combine(directory,"activities"));store.Save(book);
            Check(store.Load().Version==5 && SangmonGrowth.XP(store.Load().Progress)==40 && store.Load().Days.Count==0,"Version five preserves attendance and missions even without diary entries");
            var before=File.ReadAllBytes(store.FilePath);book.Progress.Activities[0].Flags=32;bool failed=false;
            try{store.Save(book);}catch(InvalidDataException){failed=true;}
            Check(failed && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid mission bits cannot overwrite a valid ledger");
        }
    }
}
