using System;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI {
    public sealed partial class DiaryWindow {
        private readonly Timer reminderTimer=new Timer {Interval=30000};
        private DateTime lastReaction=DateTime.MinValue;
        private void OpenJournalTools() {
            board.CancelPlacement();
            // Recovery remains accessible when an archive quota or disk error blocks saving.
            if(dirty) FlushSave();
            try {using(var tools=new JournalToolsWindow(store,book,delegate(JournalRevision revision) {
                if(!FlushSave())throw new IOException("먼저 필요 없는 이력을 정리하고 현재 일기를 다시 저장해 주세요.");
                DiaryEntry current;if(book.Days.TryGetValue(revision.Date,out current))new JournalHistory(Path.GetDirectoryName(store.FilePath)).Capture(revision.Date,current,"복원 전",true);
                DiaryEntry old;bool existed=book.Days.TryGetValue(revision.Date,out old);book.Days[revision.Date]=revision.Entry;
                try {store.Save(book);} catch {if(existed)book.Days[revision.Date]=old;else book.Days.Remove(revision.Date);throw;}
                ChangeDate(DateTime.ParseExact(revision.Date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture));LoadDate();
            },AcceptIncomingBook)) {tools.ShowDialog(this);if(tools.SelectedAlbumDate.HasValue)ChangeDate(tools.SelectedAlbumDate.Value);}} catch(Exception ex) {MessageBox.Show(this,"기록 도구를 열지 못했어요.\n"+ex.Message,"기록 도구");}
        }
        private void DeleteCurrentDay() {
            if(!FlushSave() || !book.Days.ContainsKey(DiaryStore.Key(date)))return;
            if(MessageBox.Show("이 날짜의 일기를 휴지통으로 옮길까요? 글과 사진은 기록 도구에서 복원할 수 있어요.","일기 삭제",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            string key=DiaryStore.Key(date);var old=book.Days[key];book.Days.Remove(key);
            try {store.Save(book);LoadDate();}catch(Exception ex){book.Days[key]=old;MessageBox.Show(ex.Message);}
        }
        internal void VerifyJournalToolsForTest(string directory) {
            using(var tools=new JournalToolsWindow(store,book,delegate {})) {
                tools.StartPosition=FormStartPosition.Manual;tools.Location=new System.Drawing.Point(-30000,-30000);tools.Show();Application.DoEvents();
                var tabs=(TabControl)tools.Controls[0];
                for(int i=0;i<tabs.TabCount;i++) {tabs.SelectedIndex=i;Application.DoEvents();using(var bitmap=new System.Drawing.Bitmap(tools.Width,tools.Height)){tools.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,tools.Size));bitmap.Save(Path.Combine(directory,"windows-journal-tools-"+i+".png"),System.Drawing.Imaging.ImageFormat.Png);}}
                tools.Close();
            }
            using(var album=new PhotoAlbumWindow(book,date)){
                album.StartPosition=FormStartPosition.Manual;album.Location=new System.Drawing.Point(-30000,-30000);album.Show();Application.DoEvents();
                using(var bitmap=new System.Drawing.Bitmap(album.Width,album.Height)){album.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,album.Size));bitmap.Save(Path.Combine(directory,"windows-photo-album.png"),System.Drawing.Imaging.ImageFormat.Png);}album.Close();
            }
            using(var link=new DeviceLinkWindow(book)){
                link.StartPosition=FormStartPosition.Manual;link.Location=new System.Drawing.Point(-30000,-30000);link.Show();link.StartForTest();Application.DoEvents();
                using(var bitmap=new System.Drawing.Bitmap(link.Width,link.Height)){link.DrawToBitmap(bitmap,new System.Drawing.Rectangle(System.Drawing.Point.Empty,link.Size));bitmap.Save(Path.Combine(directory,"windows-device-link.png"),System.Drawing.Imaging.ImageFormat.Png);}link.Close();
            }
        }
        private void StartReminder() {
            if(testMode)return;
            reminderTimer.Tick+=delegate {
                try {var preferences=JournalPreferences.Load(Path.GetDirectoryName(store.FilePath));var now=DateTime.Now;string key=DiaryStore.Key(now);
                    int elapsed=now.Hour*60+now.Minute-preferences.Minute;
                    if(!preferences.Enabled || preferences.LastDay==key || elapsed<0 || elapsed>=10)return;
                    preferences.LastDay=key;preferences.Save(Path.GetDirectoryName(store.FilePath));
                    tray.BalloonTipTitle="상몬과 한 줄 일기";tray.BalloonTipText="오늘의 마음을 잠깐 남겨 볼까?";tray.ShowBalloonTip(6000);
                }catch {reminderTimer.Stop();status.Text="알림 설정을 읽지 못했어요. 기록 도구에서 확인해 주세요.";}
            };
            tray.BalloonTipClicked+=delegate {OpenDiary();};reminderTimer.Start();
        }
        private void ReactToSavedRecord(System.Collections.Generic.List<DiaryBlock> before) {
            if(DateTime.Now-lastReaction<TimeSpan.FromSeconds(4))return;
            bool habit=entry.Blocks.Exists(b=>b.Kind=="habit" && b.Checked && !before.Exists(o=>o.Id==b.Id && o.Checked));
            bool text=entry.Blocks.Exists(b=>(!String.IsNullOrWhiteSpace(b.Text)||b.Photo!=null) && !before.Exists(o=>o.Id==b.Id && o.Text==b.Text && o.Photo==b.Photo));
            if(!habit && !text)return;
            lastReaction=DateTime.Now;avatar.Celebrate();if(pet!=null)pet.Celebrate();status.Text=habit?JournalPolicy.HabitReaction:JournalPolicy.RecordReaction;
        }
    }
}
