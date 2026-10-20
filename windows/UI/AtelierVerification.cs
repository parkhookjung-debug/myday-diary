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
        private void VerifyAtelierForTest(string directory)
        {
            for(int i=0;i<9;i++)CommitCollection(c=>SangmonCollection.Draw(c,book.Progress,n=>0));
            var owned=SangmonCollection.State(book.Collection,book.Progress).Owned.ToArray();
            using(var window=new AtelierWindow(book,g=>CommitCollection(c=>SangmonCollection.Synthesize(c,book.Progress,g,n=>0)),id=>CommitCollection(c=>SangmonCollection.Craft(c,book.Progress,id)))) {
                window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-30000,-30000);window.Show(this);Application.DoEvents();
                if(!window.SynthesisButtons[0].Enabled)throw new Exception("Funded synthesis button is disabled");window.SynthesisButtons[0].PerformClick();
                if(SangmonCollection.State(store.Load().Collection,book.Progress).SynthesisSuccesses!=1 || !SangmonCollection.State(book.Collection,book.Progress).Owned.SetEquals(owned))throw new Exception("Native synthesis lost items or failed to save");
                if(!window.CraftButton.Enabled)throw new Exception("Funded item crafting is disabled");window.CraftButton.PerformClick();
                if(SangmonCollection.State(store.Load().Collection,book.Progress).Crafts!=1)throw new Exception("Chosen item craft was not persisted");
                var scroll=(ScrollableControl)window.AchievementSection.Parent;scroll.AutoScrollPosition=Point.Empty;
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-atelier.png"));}
                window.Size=window.MinimumSize;window.PerformLayout();Application.DoEvents();
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-atelier-compact.png"));}
                scroll.ScrollControlIntoView(window.AchievementSection);Application.DoEvents();
                using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-atelier-achievements.png"));}
                window.Close();
            }
            for(int i=0;i<3;i++)CommitCollection(c=>SangmonCollection.Draw(c,book.Progress,n=>0));
            var before=book.Collection;int version=book.Version;bool failed=false;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){try{CommitCollection(c=>SangmonCollection.Synthesize(c,book.Progress,0,n=>99));}catch(IOException){failed=true;}}
            if(!failed || !ReferenceEquals(before,book.Collection) || book.Version!=version)throw new Exception("Failed synthesis save changed materials");
            CommitCollection(c=>SangmonCollection.Synthesize(c,book.Progress,0,n=>99));
            if(SangmonCollection.State(store.Load().Collection,book.Progress).Syntheses!=2)throw new Exception("Failed random outcome was not saved exactly once");
            Console.WriteLine("Smoke: atelier synthesis, item preservation, failure refund, disk rollback and compact layout passed.");
        }
    }
}
