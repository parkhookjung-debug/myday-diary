using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private void UpdateGrowth()
        {
            growth.Text=SangmonGrowth.Level(book.Progress)>=SangmonGrowth.MaxLevel?"Lv.100 · 만렙":"성장 · Lv."+SangmonGrowth.Level(book.Progress)+" · "+SangmonGrowth.IntoLevel(book.Progress)+"/"+SangmonGrowth.NeededXP(SangmonGrowth.Level(book.Progress));
        }
        private void OpenGrowth()
        {
            if(!ScreenPrivacy.Reveal())return;
            OpenDiary(); if(!FlushSave()) return;
            using(var window=new GrowthWindow(book,ClaimAttendance)) window.ShowDialog(this);
        }
        private bool ClaimAttendance()
        {
            if(!FlushSave())return false;
            var previous=book.Progress;int version=book.Version,level=SangmonGrowth.Level(previous);
            book.Progress=SangmonGrowth.CheckIn(previous,DateTime.Today);
            if(ReferenceEquals(previous,book.Progress))return true;
            try {store.Save(book);UpdateGrowth();status.Text="오늘 출석 +"+(SangmonGrowth.XP(book.Progress)-SangmonGrowth.XP(previous))+" XP";if(SangmonGrowth.Level(book.Progress)>level && pet!=null)pet.Celebrate();return true;}
            catch(Exception ex){book.Progress=previous;book.Version=version;UpdateGrowth();if(testMode)throw new IOException("Attendance save failed",ex);MessageBox.Show(this,"출석을 저장하지 못했어요. 다시 시도해 주세요.\n"+ex.Message);return false;}
        }
        internal void VerifyGrowthForTest(string directory)
        {
            if(!testMode)throw new InvalidOperationException();
            int before=SangmonGrowth.XP(book.Progress);var previous=book.Progress;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){bool failed=false;try{ClaimAttendance();}catch(IOException){failed=true;}if(!failed||!ReferenceEquals(previous,book.Progress))throw new Exception("Failed attendance changed growth");}
            ClaimAttendance();int once=SangmonGrowth.XP(book.Progress);ClaimAttendance();
            if(once!=before+5||SangmonGrowth.XP(store.Load().Progress)!=once)throw new Exception("Attendance was lost or awarded twice");
            using(var window=new GrowthWindow(book,ClaimAttendance)){window.Show(this);Application.DoEvents();if(window.Attendance.Enabled)throw new Exception("Repeated check-in remains enabled");using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-sangmon-growth.png"));}window.Close();}
            var cappedBook=new DiaryBook {Progress=new SangmonProgress {AwardedDays=Enumerable.Range(0,359).Select(i=>DiaryStore.Key(new DateTime(2025,1,1).AddDays(i))).ToList()}};
            using(var capped=new GrowthWindow(cappedBook)){if(capped.Meter.Value!=capped.Meter.Maximum||SangmonGrowth.Level(cappedBook.Progress)!=100)throw new Exception("Maximum level meter is not full");}
            Console.WriteLine("Smoke: focused growth screen, attendance persistence, duplicate prevention, file-lock rollback and max level passed.");
        }
    }
}
