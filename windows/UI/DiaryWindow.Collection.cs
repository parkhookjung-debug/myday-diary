using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow
    {
        private void OpenCollection()
        {
            if(!FlushSave())return;
            using(var window=new CollectionWindow(book,CollectItem))window.ShowDialog(this);
        }
        private bool CollectItem(string exchangeId)
        {
            if(!FlushSave())return false;
            var previous=book.Collection;int version=book.Version;
            try {
                book.Collection=exchangeId==null?SangmonCollection.Draw(previous,book.Progress):SangmonCollection.Exchange(previous,book.Progress,exchangeId);
                store.Save(book);status.Text="수집 저장됨";return true;
            } catch(Exception ex) {
                book.Collection=previous;book.Version=version;
                if(testMode)throw new IOException("Collection save failed",ex);
                MessageBox.Show(this,"선물을 저장하지 못했어요. 클로버와 꽃잎은 그대로예요.\n"+ex.Message,"선물 상자");return false;
            }
        }
        internal void VerifyCollectionForTest(string directory)
        {
            if(!testMode)throw new InvalidOperationException();
            var before=SangmonCollection.State(book.Collection,book.Progress);var equipment=book.Equipment;int xp=SangmonGrowth.XP(book.Progress);
            using(var window=new CollectionWindow(book,CollectItem)) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show(this);Application.DoEvents();window.DrawButton.PerformClick();
                var saved=store.Load();if(saved.Version!=6 || SangmonCollection.State(saved.Collection,saved.Progress).Coins!=before.Coins-20 || !ReferenceEquals(equipment,book.Equipment) || SangmonGrowth.XP(book.Progress)!=xp)throw new Exception("Gift draw did not persist independently of equipment and XP");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-collection.png"));}
                window.Size=window.MinimumSize;window.PerformLayout();Application.DoEvents();
                if(window.DrawButton.Right>window.DrawButton.Parent.ClientSize.Width)throw new Exception("Gift draw button clipped at minimum size");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-collection-compact.png"));}
                window.Close();
            }
            var ledger=book.Collection;int oldVersion=book.Version;bool failed=false;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {try{CollectItem(null);}catch(IOException){failed=true;}}
            if(!failed || !ReferenceEquals(ledger,book.Collection) || oldVersion!=book.Version)throw new Exception("Failed gift save consumed currency or changed collection");
            Console.WriteLine("Smoke: gift draw, version-six save, unchanged equipment/XP and failure rollback passed.");
        }
    }
}
