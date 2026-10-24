using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MyDay.Windows.Core
{
    public sealed class CalendarDay
    {
        public DateTime? Date;
        public bool InMonth,HasEntry,HasPhoto;
    }
    public sealed class DiaryMatch
    {
        public DateTime Date;
        public string Preview,Kind;
        public bool HasPhoto;
    }
    public static class DiaryBrowse
    {
        public static bool HasPhoto(DiaryEntry entry) { return entry.Blocks.Any(b=>b.Kind=="photo" && !string.IsNullOrEmpty(b.Photo)); }
        public static List<CalendarDay> Month(DiaryBook book,DateTime month)
        {
            month=new DateTime(month.Year,month.Month,1);
            int offset=(int)month.DayOfWeek;
            var result=new List<CalendarDay>();
            for(int i=0;i<42;i++) {
                long ticks=month.Ticks+(i-offset)*TimeSpan.TicksPerDay;
                var cell=new CalendarDay();
                if(ticks>=DateTime.MinValue.Ticks && ticks<=DateTime.MaxValue.Ticks) {
                    var date=new DateTime(ticks); cell.Date=date; cell.InMonth=date.Year==month.Year && date.Month==month.Month;
                    DiaryEntry entry; cell.HasEntry=book.Days.TryGetValue(DiaryStore.Key(date),out entry);
                    cell.HasPhoto=cell.HasEntry && HasPhoto(entry);
                }
                result.Add(cell);
            }
            return result;
        }
        public static List<DiaryMatch> Find(DiaryBook book,string query,DateTime? month=null)
        {
            query=(query??"").Trim(); var result=new List<DiaryMatch>();
            foreach(var pair in book.Days.OrderByDescending(p=>p.Key,StringComparer.Ordinal)) {
                var date=DateTime.ParseExact(pair.Key,"yyyy-MM-dd",CultureInfo.InvariantCulture);
                if(month.HasValue && (date.Year!=month.Value.Year || date.Month!=month.Value.Month)) continue;
                // Only user-written text is searchable; prompts and image bytes are excluded.
                var block=pair.Value.Blocks.FirstOrDefault(b=>!string.IsNullOrWhiteSpace(b.Text) &&
                    (query.Length==0 || b.Text.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));
                if(query.Length>0 && block==null) continue;
                result.Add(new DiaryMatch {Date=date,HasPhoto=HasPhoto(pair.Value),Kind=block==null?null:block.Kind,
                    Preview=block==null?"저장한 블록 "+pair.Value.Blocks.Count+"개":Snippet(block.Text,query)});
            }
            return result;
        }
        public static string Snippet(string text,string query)
        {
            int match=string.IsNullOrEmpty(query)?0:text.IndexOf(query,StringComparison.OrdinalIgnoreCase);
            int start=Math.Max(0,match-32);
            if(start>0 && char.IsLowSurrogate(text[start])) start--;
            int length=Math.Min(140,text.Length-start);
            if(start+length<text.Length && length>0 && char.IsHighSurrogate(text[start+length-1])) length--;
            return (start>0?"… ":"")+text.Substring(start,length).Replace("\r"," ").Replace("\n"," ").Replace("\t"," ")+(start+length<text.Length?" …":"");
        }
    }
}
