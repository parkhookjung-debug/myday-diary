using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MyDay.Windows.Character;

namespace MyDay.Windows.Core
{
    [DataContract]
    public sealed class SangmonProgress
    {
        [DataMember] public List<string> AwardedDays=new List<string>();
        [DataMember(EmitDefaultValue=false)] public List<GrowthDay> Activities=new List<GrowthDay>();
    }
    [DataContract] public sealed class GrowthDay
    {
        [DataMember] public string Date;
        [DataMember] public int Flags;
    }

    public static class SangmonGrowth
    {
        public const int DailyXP=10, LevelXP=50;
        public const int Attendance=1, Writing=2, Photo=4, Todo=8, Habit=16;
        public static readonly int[] Goals={Attendance,Writing,Photo,Todo,Habit};
        public static readonly string[] GoalNames={"출석","글 20자 이상","사진 첨부","할 일 완료","습관 완료"};
        public static int Points(int goal) {return goal==Writing?10:5;}
        public static readonly MonsterVariant[] Rewards={MonsterVariant.Apprentice,MonsterVariant.Bookmark,MonsterVariant.JournalKnight,
            MonsterVariant.MemoryMage,MonsterVariant.StarExplorer,MonsterVariant.JournalAlchemist,MonsterVariant.PageSovereign,MonsterVariant.MemoryHero};
        private static readonly int[] Levels={2,3,4,5,6,7,8,10};
        public static int XP(SangmonProgress progress) { return progress==null?0:progress.AwardedDays.Count*DailyXP+(progress.Activities==null?0:progress.Activities.Sum(d=>Goals.Where(f=>(d.Flags&f)!=0).Sum(f=>Points(f)))); }
        public static int Flags(SangmonProgress progress,DateTime date) {var day=progress==null || progress.Activities==null?null:progress.Activities.FirstOrDefault(d=>d.Date==DiaryStore.Key(date));return day==null?0:day.Flags;}
        public static int TodayXP(SangmonProgress p,DateTime date) {return (p!=null && p.AwardedDays.Contains(DiaryStore.Key(date))?DailyXP:0)+Goals.Where(f=>(Flags(p,date)&f)!=0).Sum(f=>Points(f));}
        public static int Level(SangmonProgress progress) { return 1+XP(progress)/LevelXP; }
        public static int RequiredLevel(MonsterVariant variant) { int i=Array.IndexOf(Rewards,variant); return i<0?1:Levels[i]; }
        public static bool CanUse(SangmonProgress progress,MonsterVariant variant) { return RequiredLevel(variant)<=Level(progress); }
        public static bool HasRecord(DiaryEntry entry)
        {
            return entry!=null && entry.Blocks.Any(Meaningful);
        }
        private static bool Meaningful(DiaryBlock b) { return !string.IsNullOrWhiteSpace(b.Text) || b.Kind=="photo" && !string.IsNullOrEmpty(b.Photo) || (b.Kind=="todo" || b.Kind=="habit") && b.Checked; }
        internal static List<DiaryBlock> CaptureRecord(DiaryEntry entry)
        {
            return entry.Blocks.Where(Meaningful).Select(b=>new DiaryBlock {Id=b.Id,Kind=b.Kind,Text=b.Text,Checked=b.Checked,Photo=b.Photo}).ToList();
        }
        internal static bool SameRecord(List<DiaryBlock> saved,DiaryEntry entry)
        {
            var current=entry.Blocks.Where(Meaningful).ToDictionary(b=>b.Id);
            return saved.Count==current.Count && saved.All(b=>current.ContainsKey(b.Id) && current[b.Id].Kind==b.Kind && current[b.Id].Text==b.Text && current[b.Id].Checked==b.Checked && current[b.Id].Photo==b.Photo);
        }
        // Return a candidate; the caller commits it only with a successful diary save.
        public static SangmonProgress Award(SangmonProgress current,DiaryEntry entry,DateTime actualDay,List<DiaryBlock> baseline=null)
        {
            string key=DiaryStore.Key(actualDay.Date);
            if(!HasRecord(entry)) return current;
            var prior=(baseline??new List<DiaryBlock>()).ToDictionary(b=>b.Id);
            Func<DiaryBlock,bool> textChanged=b=>!prior.ContainsKey(b.Id) || prior[b.Id].Kind!=b.Kind || prior[b.Id].Text!=b.Text;
            int flags=0;
            var text=entry.Blocks.Where(b=>b.Kind=="text" || b.Kind=="emotion").ToList();
            if(text.Sum(b=>(b.Text??"").Count(c=>!char.IsWhiteSpace(c)))>=20 && text.Any(textChanged))flags|=Writing;
            if(entry.Blocks.Any(b=>b.Kind=="photo" && !string.IsNullOrEmpty(b.Photo) && (!prior.ContainsKey(b.Id) || prior[b.Id].Photo!=b.Photo)))flags|=Photo;
            if(entry.Blocks.Any(b=>b.Kind=="todo" && b.Checked && (!prior.ContainsKey(b.Id) || !prior[b.Id].Checked || prior[b.Id].Kind!=b.Kind)))flags|=Todo;
            if(entry.Blocks.Any(b=>b.Kind=="habit" && b.Checked && (!prior.ContainsKey(b.Id) || !prior[b.Id].Checked || prior[b.Id].Kind!=b.Kind)))flags|=Habit;
            var result=AddFlags(current,actualDay,flags);
            if(result!=null && result.AwardedDays.Contains(key) || current!=null && current.AwardedDays.Count>=50000)return result;
            if(ReferenceEquals(result,current))result=Copy(current);
            result.AwardedDays.Add(key);result.AwardedDays.Sort(StringComparer.Ordinal);return result;
        }
        private static SangmonProgress Copy(SangmonProgress p) {return new SangmonProgress {AwardedDays=p==null?new List<string>():p.AwardedDays.ToList(),Activities=p==null || p.Activities==null?new List<GrowthDay>():p.Activities.Select(d=>new GrowthDay {Date=d.Date,Flags=d.Flags}).ToList()};}
        public static SangmonProgress CheckIn(SangmonProgress p,DateTime day) {return AddFlags(p,day,Attendance);}
        private static SangmonProgress AddFlags(SangmonProgress current,DateTime day,int flags)
        {
            if(flags==0 || (Flags(current,day)&flags)==flags)return current;
            if(current!=null && current.Activities!=null && current.Activities.Count>=50000 && Flags(current,day)==0)return current;
            var result=Copy(current);string key=DiaryStore.Key(day);var found=result.Activities.FirstOrDefault(d=>d.Date==key);
            if(found==null)result.Activities.Add(new GrowthDay {Date=key,Flags=flags});else found.Flags|=flags;
            result.Activities=result.Activities.OrderBy(d=>d.Date,StringComparer.Ordinal).ToList();return result;
        }
        public static void Validate(SangmonProgress progress)
        {
            if(progress==null) return;
            if(progress.AwardedDays==null || progress.AwardedDays.Count>50000) throw new InvalidDataException("상몬 성장 기록이 올바르지 않습니다.");
            var dates=new HashSet<string>(StringComparer.Ordinal);
            foreach(string day in progress.AwardedDays) {
                DateTime parsed;
                if(!DateTime.TryParseExact(day,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed) || !dates.Add(day))
                    throw new InvalidDataException("상몬 성장 기록의 날짜가 잘못되었거나 중복됩니다.");
            }
            if(progress.Activities==null)progress.Activities=new List<GrowthDay>(); // v1-v4 migration
            if(progress.Activities.Count>50000)throw new InvalidDataException("활동 기록이 너무 큽니다.");
            dates.Clear();
            foreach(var activity in progress.Activities) {
                DateTime parsed;
                if(activity==null || !DateTime.TryParseExact(activity.Date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed) || !dates.Add(activity.Date) || activity.Flags<1 || (activity.Flags&~31)!=0)
                    throw new InvalidDataException("활동 기록 날짜 또는 점수가 올바르지 않습니다.");
            }
        }
        public static SangmonProgress Merge(SangmonProgress current,SangmonProgress incoming)
        {
            Validate(current); Validate(incoming);
            if(current==null && incoming==null) return null;
            var result=new SangmonProgress { AwardedDays=(current==null?new string[0]:current.AwardedDays.ToArray())
                .Union(incoming==null?new string[0]:incoming.AwardedDays.ToArray(),StringComparer.Ordinal).OrderBy(s=>s,StringComparer.Ordinal).ToList() };
            result.Activities=(current==null?new List<GrowthDay>():current.Activities).Concat(incoming==null?new List<GrowthDay>():incoming.Activities)
                .GroupBy(d=>d.Date,StringComparer.Ordinal).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>new GrowthDay {Date=g.Key,Flags=g.Aggregate(0,(f,d)=>f|d.Flags)}).ToList();
            Validate(result); return result;
        }
    }
}
