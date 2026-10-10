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
            using(var window=new GrowthWindow(book,EquipReward)) window.ShowDialog(this);
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
            if(book.Progress==null || SangmonGrowth.XP(book.Progress)!=10) throw new Exception("Writing should award exactly one day despite edits and date navigation");
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
            entry.Blocks.First(b=>!string.IsNullOrWhiteSpace(b.Text)).Text+=" 새 기록";
            QueueSave();
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {
                bool failed=false; try { FlushSaveAt(DateTime.Today.AddDays(1)); } catch(IOException) { failed=true; }
                if(!failed || !ReferenceEquals(snapshot,book.Progress) || !entryDirty) throw new Exception("Failed write committed growth or dropped retry");
            }
            FlushSaveAt(DateTime.Today.AddDays(1));
            if(SangmonGrowth.XP(book.Progress)!=before+10 || !store.Load().Progress.AwardedDays.Contains(DiaryStore.Key(DateTime.Today.AddDays(1)))) throw new Exception("Save retry lost growth");
            for(int i=2;i<5;i++) { entry.Blocks[0].Text+=" 기록"; QueueSave(); FlushSaveAt(DateTime.Today.AddDays(i)); }
            if(SangmonGrowth.Level(book.Progress)!=2 || !characterChoice.ItemEnabled((int)MonsterVariant.Apprentice) || characterChoice.ItemEnabled((int)MonsterVariant.Bookmark)) throw new Exception("Reward threshold or diary menu gate failed");
            using(var window=new GrowthWindow(book,EquipReward)) {
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
            for(int i=5;i<45;i++) { entry.Blocks[0].Text+=" 기록"; QueueSave(); FlushSaveAt(DateTime.Today.AddDays(i)); }
            foreach(var v in SangmonGrowth.Rewards) {
                ((ToolStripMenuItem)menu.DropDownItems[(int)v]).PerformClick(); FlushSave();
                if(pet.Variant!=v || avatar.Variant!=v || store.Load().CharacterStyle!=MonsterVariants.Id(v)) throw new Exception("Unlocked desktop reward failed: "+v);
            }
            Console.WriteLine("Smoke: daily growth, failure rollback, locked menus, all eight reward equips and disk reload passed.");
        }
    }
}
