using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunGrowthPaceTests(string directory)
        {
            var start=new DateTime(2026,1,1);
            foreach(var profile in DesktopFixtures.GrowthProfiles) {
                SangmonProgress progress=null;int days=0,previous=0;
                while(SangmonGrowth.Level(progress)<SangmonGrowth.MaxLevel && days<1000) {
                    int mask=profile[1+days%7];var day=start.AddDays(days++);previous=SangmonGrowth.XP(progress);
                    if((mask&SangmonGrowth.Attendance)!=0)progress=SangmonGrowth.CheckIn(progress,day);
                    if((mask&32)!=0) {
                        var entry=DiaryEntry.Empty();
                        entry.Blocks.Add(new DiaryBlock {Id="text",Kind="text",Text=(mask&SangmonGrowth.Writing)!=0?"오늘은 산책을 하고 하루를 돌아보며 스무 글자가 넘는 기록을 남겼어요":"작은 기록"});
                        if((mask&SangmonGrowth.Photo)!=0)entry.Blocks.Add(new DiaryBlock {Id="photo",Kind="photo",Photo="fixture"});
                        if((mask&SangmonGrowth.Todo)!=0)entry.Blocks.Add(new DiaryBlock {Id="todo",Kind="todo",Checked=true});
                        if((mask&SangmonGrowth.Habit)!=0)entry.Blocks.Add(new DiaryBlock {Id="habit",Kind="habit",Checked=true});
                        progress=SangmonGrowth.Award(progress,entry,day);
                    }
                }
                Check(days==profile[0] && previous<SangmonGrowth.MaxXP && SangmonGrowth.XP(progress)>=SangmonGrowth.MaxXP,"Real saved activity profile reaches maximum on day "+profile[0]);
                Console.WriteLine("Growth pace: "+days+" calendar days, "+SangmonGrowth.XP(progress)+" XP.");
            }
            Check(SangmonGrowth.LevelFromXP(3589)==99 && SangmonGrowth.LevelFromXP(3590)==100 && SangmonGrowth.LevelFromXP(int.MaxValue)==100,"Maximum boundary is exactly Lv.100 at 3590 XP");
            var old=new SangmonProgress {AwardedDays=Enumerable.Range(0,1000).Select(i=>DiaryStore.Key(start.AddDays(i))).ToList()};
            Check(SangmonGrowth.XP(old)==10000 && SangmonGrowth.IntoLevel(old)==SangmonGrowth.NeededXP(100) && SangmonCollection.Earned(old)==11175,"High-XP legacy saves retain XP and all historical clover steps with a full max meter");
            var week=new SangmonProgress();for(int i=0;i<7;i++)week=SangmonGrowth.CheckIn(week,start.AddDays(i));
            Check(SangmonGrowth.XP(week)==45 && SangmonGrowth.TodayXP(week,start.AddDays(6))==15 && SangmonGrowth.TodayActivityXP(week,start.AddDays(6))==5,"Seventh consecutive attendance gives a separate 10-XP bonus");
            var merged=SangmonGrowth.Merge(week,week);
            Check(SangmonGrowth.XP(merged)==45 && SangmonGrowth.XP(SangmonGrowth.CheckIn(merged,start.AddDays(6)))==45,"Repeated imports and check-in cannot duplicate a weekly bonus");
            var broken=SangmonGrowth.CheckIn(week,start.AddDays(8));
            Check(SangmonGrowth.XP(broken)==50 && SangmonGrowth.Streak(broken,start.AddDays(8))==1,"A missed day preserves earned XP and restarts only the attendance streak");
            var store=new DiaryStore(Path.Combine(directory,"growth-pace"));var book=new DiaryBook {Progress=week};store.Save(book);
            Check(SangmonGrowth.XP(store.Load().Progress)==45,"Existing growth ledger round-trips the weekly bonus without a new backup version");
            var after=SangmonCollection.Draw(null,old,n=>0);var balance=SangmonCollection.State(after,old).Coins;
            var next=SangmonGrowth.CheckIn(old,start.AddDays(1100));
            Check(SangmonGrowth.Level(next)==100 && SangmonCollection.State(after,next).Coins>balance,"Maximum level keeps awarding clovers and accepts existing draw spending");
        }
    }
}
