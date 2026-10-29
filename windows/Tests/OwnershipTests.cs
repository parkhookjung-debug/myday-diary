using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunOwnershipTests(string directory)
        {
            var book=new DiaryBook();SangmonCollection.Migrate(book);
            var state=SangmonCollection.State(book.Collection,book.Progress);
            Check(book.Version==7 && state.Owned.Count==8 && SangmonCollection.Starters.Select(id=>SangmonItems.Find(id).Slot).Distinct().Count()==8 && state.Coins==40,"New closet grants one free accessory per slot without spending currency");
            Check(SangmonCollection.CanUse(book,"none") && SangmonCollection.Starters.All(id=>SangmonCollection.CanUse(book,id)) && !SangmonCollection.CanUse(book,"solar-sword"),"Only free and acquired accessories can be equipped");
            var store=new DiaryStore(Path.Combine(directory,"ownership"));store.Save(book);var bytes=File.ReadAllBytes(store.FilePath);
            book.Equipment=new SangmonEquipment {WeaponId="solar-sword"};bool failed=false;try{store.Save(book);}catch(InvalidDataException){failed=true;}
            Check(failed && bytes.SequenceEqual(File.ReadAllBytes(store.FilePath)),"A version-seven backup cannot persist unowned equipment");book.Equipment=null;
            book.Collection=SangmonCollection.Draw(book.Collection,book.Progress,n=>0);var drawn=book.Collection.Actions.Last();
            Check(!SangmonCollection.Starters.Contains(drawn.ItemId) && SangmonCollection.CanUse(book,drawn.ItemId),"Drawing an accessory immediately unlocks it and excludes free starters");
            book.Equipment=SangmonItems.Select(null,SangmonItems.Find(drawn.ItemId).Slot,drawn.ItemId);store.Save(book);Check(SangmonCollection.CanUse(store.Load(),drawn.ItemId),"Ownership and equipped result survive reload");
            var legacy=new DiaryBook {Version=6,Equipment=new SangmonEquipment {WeaponId="ember-book",HeadId="shadow-beret"}};
            legacy.Collection=new CollectionBook();for(int i=0;i<2;i++)legacy.Collection.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind="draw",ItemId="frost-sword"});
            var before=SangmonCollection.State(legacy.Collection,legacy.Progress);SangmonCollection.Migrate(legacy);var after=SangmonCollection.State(legacy.Collection,legacy.Progress);
            Check(before.Coins==after.Coins && before.Petals==after.Petals && before.Draws==after.Draws && after.Petals==3 && SangmonCollection.CanUse(legacy,"ember-book") && SangmonCollection.CanUse(legacy,"shadow-beret"),"Legacy duplicate rewards and previously worn accessories migrate without retroactive currency changes");
            legacy.Equipment=null;store.Save(legacy);Check(SangmonCollection.CanUse(store.Load(),"ember-book"),"Removing an old equipped item does not lose its migrated ownership");
            int count=legacy.Collection.Actions.Count;SangmonCollection.Migrate(legacy);Check(count==legacy.Collection.Actions.Count,"Ownership migration is idempotent");
            var oldPrefix=new CollectionBook {Actions=legacy.Collection.Actions.Where(a=>a.Kind=="draw").ToList()};var merged=SangmonCollection.Merge(legacy.Collection,oldPrefix);
            Check(SangmonCollection.State(merged,legacy.Progress).Coins==0 && SangmonCollection.State(merged,legacy.Progress).Owned.Contains("ember-book"),"Importing an old prefix never removes grants or refunds draws");
            var fresh=new DiaryBook();SangmonCollection.Migrate(fresh);var imported=SangmonCollection.Merge(fresh.Collection,oldPrefix);
            Check(SangmonCollection.State(imported,null).Draws==2 && SangmonCollection.State(imported,null).Petals==3 && SangmonCollection.State(imported,null).Closet,"A fresh closet can import a pre-ownership draw ledger unchanged");
        }
    }
}
