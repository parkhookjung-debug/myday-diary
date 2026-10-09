using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class JournalBrowser
    {
        private void CaptureBrowser(string path)
        {
            PerformLayout(); Application.DoEvents(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(path,System.Drawing.Imaging.ImageFormat.Png); }
        }
        internal void VerifyAndRender(string directory,DateTime photoDate)
        {
            var photoDay=calendar.FindDay(photoDate);
            if(!photoDay.Day.HasEntry || !photoDay.Day.HasPhoto || !photoDay.AccessibleName.Contains("사진 있음")) throw new Exception("Photo calendar marker missing");
            CaptureBrowser(Path.Combine(directory,"windows-calendar.png"));
            var originalMonth=calendar.Month; calendar.Previous.PerformClick();
            if(calendar.Month!=originalMonth.AddMonths(-1)) throw new Exception("Previous month action failed");
            calendar.Next.PerformClick(); if(calendar.Month!=originalMonth) throw new Exception("Next month action failed");
            calendar.SetMonth(min); if(calendar.Previous.Enabled) throw new Exception("Calendar goes before the supported date range");
            calendar.Previous.PerformClick(); calendar.SetMonth(max); if(calendar.Next.Enabled) throw new Exception("Calendar goes after the supported date range");
            calendar.Next.PerformClick(); calendar.SetMonth(originalMonth.AddMonths(-1));
            search.Text=" 바닷가 "; OnSearchKey(search,new KeyEventArgs(Keys.Enter));
            if(matches.Count!=1 || matches[0].Date!=photoDate || matches[0].Kind!="photo" || !count.Text.StartsWith("전체 기록")) throw new Exception("Caption search failed across months");
            calendar.SetMonth(photoDate); CaptureBrowser(Path.Combine(directory,"windows-diary-search.png"));
            search.Text="no-such-diary-native-fixture"; RunSearch();
            if(matches.Count!=0 || results.Controls.OfType<HistoryRow>().Any() || previous.Enabled || next.Enabled) throw new Exception("No-results view retained stale selectable entries");
            var enter=new KeyEventArgs(Keys.Enter); OnSearchKey(search,enter);
            if(!enter.SuppressKeyPress || SelectedDate.HasValue || IsDisposed) throw new Exception("Search Enter unexpectedly opened an entry or closed the browser");
            CaptureBrowser(Path.Combine(directory,"windows-diary-search-empty.png"));
            search.Focus(); var escapeMessage=new Message();
            if(!ProcessCmdKey(ref escapeMessage,Keys.Escape) || search.Text.Length!=0 || SelectedDate.HasValue) throw new Exception("Escape did not clear the query before closing the browser");
            search.Text="바닷가"; RunSearch();
            clear.PerformClick(); RunSearch(); if(search.Text.Length!=0 || matches.Count!=DiaryBrowse.Find(book,"",calendar.Month).Count) throw new Exception("Clear search failed to restore monthly entries");
            var snapshot=book.Days.Keys.ToArray();
            try {
                for(int i=0;i<121;i++) {
                    var fixture=DiaryEntry.Empty(); var block=DiaryBlock.Create("text"); block.Text="paging-fixture-for-native-browser "+i; fixture.Blocks.Add(block);
                    book.Days[DiaryStore.Key(new DateTime(2023,1,1).AddDays(i))]=fixture;
                }
                search.Text="paging-fixture-for-native-browser"; RunSearch();
                if(matches.Count!=121 || results.Controls.OfType<HistoryRow>().Count()!=50 || previous.Enabled || !next.Enabled) throw new Exception("Search paging did not bound the initial page");
                next.PerformClick(); if(page!=1 || results.Controls.OfType<HistoryRow>().Count()!=50 || !previous.Enabled) throw new Exception("Search next page failed");
                next.PerformClick(); if(page!=2 || results.Controls.OfType<HistoryRow>().Count()!=21 || next.Enabled) throw new Exception("Search last page failed");
                previous.PerformClick(); if(page!=1) throw new Exception("Search previous page failed");
            } finally { foreach(var key in book.Days.Keys.Except(snapshot).ToArray()) book.Days.Remove(key); }
            search.Text="바닷가"; RunSearch();
            var full=Size; Size=MinimumSize; PerformLayout(); Application.DoEvents();
            CaptureBrowser(Path.Combine(directory,"windows-calendar-compact.png"));
            if(results.HorizontalScroll.Visible || results.Controls.OfType<HistoryRow>().Any(row=>row.Right>results.ClientSize.Width) ||
                calendar.FindDay(photoDate).Bottom>calendar.Height || search.Right>clear.Left) throw new Exception("Compact browser clips results or search controls: horizontal="+results.HorizontalScroll.Visible+", client="+results.ClientSize+", display="+results.DisplayRectangle+", children="+string.Join(";",results.Controls.Cast<Control>().Select(c=>c.Bounds+" margin="+c.Margin))+", search="+search.Right+"/"+clear.Left);
            Size=full; search.Clear(); RunSearch();
            var emptyDate=calendar.FindDay(photoDate.AddDays(1));
            if(emptyDate.Day.HasEntry) throw new Exception("Expected a blank calendar date in fixture");
            // Opening a blank day is verified separately; this pass opens the caption result.
            search.Text="바닷가"; RunSearch(); results.Controls.OfType<HistoryRow>().Single().PerformClick();
            if(SelectedDate!=photoDate || DialogResult!=DialogResult.OK) throw new Exception("Search result did not select its diary date");
        }
        internal void SelectDayForTest(DateTime target)
        {
            calendar.SetMonth(target); calendar.FindDay(target).PerformClick();
            if(SelectedDate!=target.Date) throw new Exception("Calendar click did not select its date");
        }
    }
}
