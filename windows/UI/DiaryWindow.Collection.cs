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
            using(var window=new CollectionWindow(book,CollectItem,OpenAtelier))window.ShowDialog(this);
        }
        private void OpenAtelier() {using(var window=new AtelierWindow(book,g=>CommitCollection(c=>SangmonCollection.Synthesize(c,book.Progress,g)),id=>CommitCollection(c=>SangmonCollection.Craft(c,book.Progress,id))))window.ShowDialog(this);}
        private bool CollectItem(string exchangeId)
        {return CommitCollection(c=>exchangeId==null?SangmonCollection.Draw(c,book.Progress):SangmonCollection.Exchange(c,book.Progress,exchangeId));}
        private bool CommitCollection(Func<CollectionBook,CollectionBook> change)
        {
            if(!FlushSave())return false;
            var previous=book.Collection;int version=book.Version;
            try {
                book.Collection=change(previous);
                store.Save(book);status.Text="수집 저장됨";return true;
            } catch(Exception ex) {
                book.Collection=previous;book.Version=version;
                if(testMode)throw new IOException("Collection save failed",ex);
                MessageBox.Show(this,"작업을 완료하지 못했어요. 소품과 재료는 그대로예요.\n"+ex.Message,"선물 공방");return false;
            }
        }
        internal void VerifyCollectionForTest(string directory)
        {
            if(!testMode)throw new InvalidOperationException();
            var before=SangmonCollection.State(book.Collection,book.Progress);var equipment=book.Equipment;int xp=SangmonGrowth.XP(book.Progress);
            using(var window=new CollectionWindow(book,CollectItem,OpenAtelier)) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show(this);Application.DoEvents();window.DrawButton.PerformClick();
                var saved=store.Load();if(saved.Version!=8 || SangmonCollection.State(saved.Collection,saved.Progress).Coins!=before.Coins-20 || !ReferenceEquals(equipment,book.Equipment) || SangmonGrowth.XP(book.Progress)!=xp)throw new Exception("Gift draw did not persist independently of equipment and XP");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-collection.png"));}
                window.Size=window.MinimumSize;window.PerformLayout();Application.DoEvents();
                if(window.DrawButton.Right>window.DrawButton.Parent.ClientSize.Width)throw new Exception("Gift draw button clipped at minimum size");
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-collection-compact.png"));}
                window.Close();
            }
            var ledger=book.Collection;int oldVersion=book.Version;bool failed=false;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {try{CollectItem(null);}catch(IOException){failed=true;}}
            if(!failed || !ReferenceEquals(ledger,book.Collection) || oldVersion!=book.Version)throw new Exception("Failed gift save consumed currency or changed collection");
            VerifyAtelierForTest(directory);
            Console.WriteLine("Smoke: tiered gift draw, version-eight save, unchanged equipment/XP and failure rollback passed.");
        }
    }
}
