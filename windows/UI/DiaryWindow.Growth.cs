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
            growth.Text="성장 · Lv."+SangmonGrowth.Level(book.Progress)+" · "+(SangmonGrowth.XP(book.Progress)%50)+"/50";
            if(pet!=null) pet.SetAvailability(v=>SangmonGrowth.CanUse(book.Progress,v));
        }
        private void OpenGrowth()
        {
            OpenDiary(); if(!FlushSave()) return;
            using(var window=new GrowthWindow(book,EquipReward,ClaimAttendance)) window.ShowDialog(this);
        }
        private bool ClaimAttendance()
        {
            if(!FlushSave())return false;
            var previous=book.Progress;int version=book.Version,level=SangmonGrowth.Level(previous);
            book.Progress=SangmonGrowth.CheckIn(previous,DateTime.Today);
            if(ReferenceEquals(previous,book.Progress))return true;
            try {store.Save(book);UpdateGrowth();status.Text="오늘 출석 +5 XP";if(SangmonGrowth.Level(book.Progress)>level && pet!=null)pet.Celebrate();return true;}
            catch(Exception ex){book.Progress=previous;book.Version=version;UpdateGrowth();if(testMode)throw new IOException("Attendance save failed",ex);MessageBox.Show(this,"출석을 저장하지 못했어요. 다시 시도해 주세요.\n"+ex.Message);return false;}
        }
        private bool EquipReward(MonsterVariant variant)
        {
            if(!SangmonGrowth.CanUse(book.Progress,variant)) return false;
            string previous=book.CharacterStyle;
            SelectVariant(variant);
            if(FlushSave()) return true;
            book.CharacterStyle=previous; ApplyVariant(); return false;
        }
        internal void VerifyGrowthForTest(string directory)
        {
            if(!testMode) throw new InvalidOperationException("Only isolated native verification may simulate writing days.");
            if(book.Progress==null || book.Progress.AwardedDays.Count!=1 || SangmonGrowth.XP(book.Progress)<10 || SangmonGrowth.XP(book.Progress)>35) throw new Exception("Writing should award one day and bounded missions despite edits and date navigation");
            var menu=pet.ContextMenuStrip.Items.OfType<ToolStripMenuItem>().First(i=>i.Text=="캐릭터 버전");
            if(((ToolStripMenuItem)menu.DropDownItems[(int)MonsterVariant.Apprentice]).Enabled) throw new Exception("Locked desktop reward is enabled");
            SelectVariant(MonsterVariant.MemoryHero);
            if(book.CharacterStyle==MonsterVariants.Id(MonsterVariant.MemoryHero)) throw new Exception("Locked reward selected programmatically");
            int before=SangmonGrowth.XP(book.Progress);
            SelectVariant(MonsterVariant.Original); FlushSaveAt(DateTime.Today.AddDays(1));
            if(SangmonGrowth.XP(book.Progress)!=before) throw new Exception("Character-only change awarded a new day");
            entry.Theme=(entry.Theme+1)%3; QueueSave(); FlushSaveAt(DateTime.Today.AddDays(1));
            if(SangmonGrowth.XP(book.Progress)!=before) throw new Exception("Theme-only change awarded XP for existing content");
            var snapshot=book.Progress;
            entry.Blocks.First(b=>b.Kind=="text").Text+=" 오늘의 충분한 글을 써서 글자 수 스무 개를 채우겠습니다";
            QueueSave();
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {
                bool failed=false; try { FlushSaveAt(DateTime.Today.AddDays(1)); } catch(IOException) { failed=true; }
                if(!failed || !ReferenceEquals(snapshot,book.Progress) || !entryDirty) throw new Exception("Failed write committed growth or dropped retry");
            }
            FlushSaveAt(DateTime.Today.AddDays(1));
            if(SangmonGrowth.XP(book.Progress)!=before+20 || !store.Load().Progress.AwardedDays.Contains(DiaryStore.Key(DateTime.Today.AddDays(1)))) throw new Exception("Save retry lost growth");
            int simulatedDay=2;
            while(SangmonGrowth.Level(book.Progress)<2){entry.Blocks[0].Text+=" 기록";QueueSave();FlushSaveAt(DateTime.Today.AddDays(simulatedDay++));}
            if(SangmonGrowth.Level(book.Progress)!=2 || !characterChoice.ItemEnabled((int)MonsterVariant.Apprentice) || characterChoice.ItemEnabled((int)MonsterVariant.Bookmark)) throw new Exception("Reward threshold or diary menu gate failed");
            using(var window=new GrowthWindow(book,EquipReward,ClaimAttendance)) {
                window.StartPosition=FormStartPosition.Manual; window.Location=new Point(-30000,-30000); window.Show(this); Application.DoEvents();
                if(!window.EquipButtons[0].Enabled || window.EquipButtons.Skip(1).Any(b=>b.Enabled)) throw new Exception("Growth cards do not match unlocked state");
                window.EquipButtons[0].PerformClick();
                if(pet.Variant!=MonsterVariant.Apprentice || avatar.Variant!=MonsterVariant.Apprentice || store.Load().CharacterStyle!=MonsterVariants.Id(MonsterVariant.Apprentice)) throw new Exception("Reward equip did not reach pet, diary and storage");
                using(var image=new Bitmap(window.Width,window.Height)) { window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size)); image.Save(Path.Combine(directory,"windows-sangmon-growth.png")); }
                window.Size=window.MinimumSize; window.PerformLayout(); Application.DoEvents();
                foreach(var button in window.EquipButtons) if(button.Right>button.Parent.ClientSize.Width || button.Bottom>button.Parent.ClientSize.Height) throw new Exception("Growth action clipped at minimum window size");
                using(var image=new Bitmap(window.Width,window.Height)) { window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size)); image.Save(Path.Combine(directory,"windows-sangmon-growth-compact.png")); }
                window.Close();
            }
            while(SangmonGrowth.Level(book.Progress)<10){entry.Blocks[0].Text+=" 기록";QueueSave();FlushSaveAt(DateTime.Today.AddDays(simulatedDay++));}
            int attendanceXP=SangmonGrowth.XP(book.Progress);var committed=book.Progress;int committedVersion=book.Version;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){bool failed=false;try{ClaimAttendance();}catch(IOException){failed=true;}if(!failed || !ReferenceEquals(committed,book.Progress) || book.Version!=committedVersion)throw new Exception("Failed attendance changed growth");}
            ClaimAttendance();ClaimAttendance();
            if(SangmonGrowth.XP(book.Progress)!=attendanceXP+5 || (store.Load().Progress.Activities.First(d=>d.Date==DiaryStore.Key(DateTime.Today)).Flags&SangmonGrowth.Attendance)==0)throw new Exception("Attendance duplicate or persistence failed");
            foreach(var v in SangmonGrowth.Rewards) {
                ((ToolStripMenuItem)menu.DropDownItems[(int)v]).PerformClick(); FlushSave();
                if(pet.Variant!=v || avatar.Variant!=v || store.Load().CharacterStyle!=MonsterVariants.Id(v)) throw new Exception("Unlocked desktop reward failed: "+v);
            }
            Console.WriteLine("Smoke: daily growth, failure rollback, locked menus, all eight reward equips and disk reload passed.");
        }
    }
}
