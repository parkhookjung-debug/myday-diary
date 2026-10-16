using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private void ApplyEquipment() {avatar.Equipment=book.Equipment;if(pet!=null)pet.Equipment=book.Equipment;}
        private void OpenItems()
        {
            OpenDiary();if(!FlushSave())return;
            using(var window=new EquipmentWindow(book,EquipItem))window.ShowDialog(this);
        }
        private bool EquipItem(string slot,string id)
        {
            if(!FlushSave())return false;
            var previous=book.Equipment;int version=book.Version;
            var candidate=SangmonItems.Select(previous,slot,id);book.Equipment=candidate;
            try {store.Save(book);ApplyEquipment();status.Text="소품 저장됨";status.ForeColor=Design.Accent;return true;}
            catch(Exception ex) {
                book.Equipment=previous;book.Version=version;ApplyEquipment();
                if(testMode)throw new IOException("Isolated equipment save failed.",ex);
                MessageBox.Show(this,"소품을 저장하지 못했어요. 기존 모습을 유지했어요.\n\n"+ex.Message,"작은 옷장",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;
            }
        }
        internal void VerifyItemsForTest(string directory)
        {
            if(!testMode)throw new InvalidOperationException("Only isolated fixtures may verify items.");
            FlushSave();int xp=SangmonGrowth.XP(book.Progress);
            using(var window=new EquipmentWindow(book,EquipItem)) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show(this);Application.DoEvents();
                if(window.EquipButtons.Count!=12)throw new Exception("Default weapon page did not show twelve items");
                window.EquipButtons["frost-sword"].PerformClick();window.Category.SelectedIndex=1;window.EquipButtons["solar-orb"].PerformClick();
                if(avatar.Equipment.WeaponId!="frost-sword" || pet.Equipment.CharmId!="solar-orb" || store.Load().Equipment.CharmId!="solar-orb")throw new Exception("Dual-slot equip missed avatar, desktop or storage");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-item-orbs.png"));}
                window.ClearWeapon.PerformClick();if(book.Equipment.WeaponId!="none" || book.Equipment.CharmId!="solar-orb")throw new Exception("Unequipping weapon removed charm");
                window.ClearCharm.PerformClick();if(book.Equipment.CharmId!="none")throw new Exception("Charm did not unequip");
                window.Category.SelectedIndex=8;if(window.EquipButtons.Count!=12)throw new Exception("All items page count is wrong");
                foreach(string slot in SangmonItems.Slots) {var item=SangmonItems.All.First(i=>i.Slot==slot);window.Search.Text=item.Name;window.EquipButtons[item.Id].PerformClick();if(SangmonItems.Get(store.Load().Equipment,slot)!=item.Id)throw new Exception("Item not persisted: "+item.Id);}
                window.Search.Text="zz-no-item";if(window.EquipButtons.Count!=0)throw new Exception("Search empty state failed");
                window.Search.Text="";window.Category.SelectedIndex=0;window.Next.PerformClick();if(window.EquipButtons.Count!=12)throw new Exception("Next equipment page failed");window.Previous.PerformClick();window.EquipButtons["frost-sword"].PerformClick();
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-items.png"));}
                window.Size=window.MinimumSize;window.PerformLayout();Application.DoEvents();
                foreach(var button in window.EquipButtons.Values)if(button.Right>button.Parent.ClientSize.Width || button.Bottom>button.Parent.ClientSize.Height)throw new Exception("Equipment button clipped at minimum size");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-items-compact.png"));}
                window.Close();
            }
            if(SangmonGrowth.XP(book.Progress)!=xp)throw new Exception("Item changes awarded XP");
            var old=book.Equipment;int oldVersion=book.Version;bool failed=false;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {try{EquipItem("weapon","solar-sword");}catch(IOException){failed=true;}}
            if(!failed || !ReferenceEquals(old,book.Equipment) || book.Version!=oldVersion || avatar.Equipment.WeaponId!=old.WeaponId || pet.Equipment.WeaponId!=old.WeaponId)throw new Exception("Failed equipment save changed selected items");
            var dateBefore=date;ChangeDate(date.AddDays(1));ChangeDate(dateBefore);
            if(avatar.Equipment.WeaponId!=old.WeaponId || book.Equipment.CharmId!=old.CharmId)throw new Exception("Date change reset equipment");
            string backup=Path.Combine(directory,"item-backup.json");store.Export(backup,book);
            using(var stream=File.OpenRead(backup))if(DiaryStore.Read(stream).Equipment.WeaponId!=old.WeaponId)throw new Exception("Equipment missing from backup");
            File.Delete(backup);
            Console.WriteLine("Smoke: 300-item paging/search, eight slots, unequip, no XP, file-lock rollback, small window and backup passed.");
        }
    }
}
