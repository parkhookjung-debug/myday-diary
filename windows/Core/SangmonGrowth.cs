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
    }

    public static class SangmonGrowth
    {
        public const int DailyXP=10, LevelXP=50;
        public static readonly MonsterVariant[] Rewards={MonsterVariant.Apprentice,MonsterVariant.Bookmark,MonsterVariant.JournalKnight,
            MonsterVariant.MemoryMage,MonsterVariant.StarExplorer,MonsterVariant.JournalAlchemist,MonsterVariant.PageSovereign,MonsterVariant.MemoryHero};
        private static readonly int[] Levels={2,3,4,5,6,7,8,10};
        public static int XP(SangmonProgress progress) { return progress==null?0:progress.AwardedDays.Count*DailyXP; }
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
        public static SangmonProgress Award(SangmonProgress current,DiaryEntry entry,DateTime actualDay)
        {
            string key=DiaryStore.Key(actualDay.Date);
            if(!HasRecord(entry) || current!=null && current.AwardedDays.Contains(key)) return current;
            if(current!=null && current.AwardedDays.Count>=50000) return current;
            var result=new SangmonProgress();
            if(current!=null) result.AwardedDays.AddRange(current.AwardedDays);
            result.AwardedDays.Add(key); result.AwardedDays.Sort(StringComparer.Ordinal); return result;
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
        }
        public static SangmonProgress Merge(SangmonProgress current,SangmonProgress incoming)
        {
            Validate(current); Validate(incoming);
            if(current==null && incoming==null) return null;
            var result=new SangmonProgress { AwardedDays=(current==null?new string[0]:current.AwardedDays.ToArray())
                .Union(incoming==null?new string[0]:incoming.AwardedDays.ToArray(),StringComparer.Ordinal).OrderBy(s=>s,StringComparer.Ordinal).ToList() };
            Validate(result); return result;
        }
    }
}
