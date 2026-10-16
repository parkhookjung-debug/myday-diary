using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunCollectionTests(string directory)
        {
            int[] xp={0,9,10,89,90,109,110,489,490,1089,1090,1140},levels={1,1,2,9,10,10,11,29,30,49,50,51};
            Check(xp.Select(SangmonGrowth.LevelFromXP).SequenceEqual(levels),"Granular levels use exact 10/20/30/50-XP boundaries");
            Check(Enumerable.Range(1,200).All(l=>SangmonGrowth.LevelFromXP(SangmonGrowth.FloorXP(l))==l && SangmonGrowth.LevelFromXP(SangmonGrowth.FloorXP(l)+SangmonGrowth.NeededXP(l)-1)==l),"Every level floor and final XP agrees with its progress meter");
            var progress=new SangmonProgress();var day=new DateTime(2026,1,1);
            for(int i=0;i<20;i++)progress.AwardedDays.Add(DiaryStore.Key(day.AddDays(i)));
            var nearReward=new SangmonProgress {AwardedDays=progress.AwardedDays.Take(9).ToList()};
            Check(!SangmonGrowth.CanUse(nearReward,Character.MonsterVariant.Bookmark) && SangmonGrowth.Level(nearReward)==10,"Reaching the reward's displayed level does not unlock below its original 100-XP threshold");
            nearReward.AwardedDays.Add(DiaryStore.Key(day.AddDays(9)));Check(SangmonGrowth.CanUse(nearReward,Character.MonsterVariant.Bookmark),"Existing reward still unlocks at exactly 100 XP");
            var attended=new SangmonProgress();for(int i=0;i<14;i++)attended=SangmonGrowth.CheckIn(attended,day.AddDays(i));
            Check(SangmonGrowth.AttendanceWeeks(attended)==2 && SangmonCollection.Earned(attended)==40+70+7*5+20,"Two consecutive attendance weeks add two clover bonuses");
            var broken=new SangmonProgress();foreach(int i in new[]{0,1,2,3,4,5,7})broken=SangmonGrowth.CheckIn(broken,day.AddDays(i));
            Check(SangmonGrowth.AttendanceWeeks(broken)==0,"A missed attendance day breaks only the streak bonus");
            var first=SangmonCollection.Draw(null,null,n=>0);var second=SangmonCollection.Draw(first,null,n=>0);var empty=SangmonCollection.State(second,null);
            Check(empty.Coins==0 && empty.Draws==2 && empty.Owned.Count==9 && empty.Petals==3,"Welcome clovers buy two draws; duplicate becomes three petals");
            bool failed=false;try{SangmonCollection.Draw(second,null,n=>0);}catch(InvalidOperationException){failed=true;}
            Check(failed && second.Actions.Count==3,"Insufficient funds do not append a draw");
            CollectionBook collection=null;for(int i=0;i<10;i++)collection=SangmonCollection.Draw(collection,progress,n=>0);
            var state=SangmonCollection.State(collection,progress);Check(state.Draws==10 && state.Owned.Count==10 && state.Petals==24,"Tenth draw guarantees an uncollected item even after nine identical choices");
            var selected=SangmonItems.All.First(i=>!state.Owned.Contains(i.Id));var exchanged=SangmonCollection.Exchange(collection,progress,selected.Id);
            var after=SangmonCollection.State(exchanged,progress);Check(after.Petals==9 && after.Owned.Count==11 && after.Draws==10 && after.Coins==state.Coins,"Selected exchange costs fifteen petals without spending clovers or changing pity");
            failed=false;try{SangmonCollection.Exchange(exchanged,progress,selected.Id);}catch(InvalidDataException){failed=true;}
            Check(failed && collection.Actions.Count==11,"Already collected exchange is rejected without source mutation");
            Check(SangmonCollection.Merge(exchanged,collection).Actions.Count==12 && SangmonCollection.Merge(exchanged,exchanged).Actions.Count==12 && SangmonCollection.Merge(exchanged,null).Actions.Count==12,"Prefix and repeated backup imports retain spending exactly once");
            failed=false;try{SangmonCollection.Merge(first,SangmonCollection.Draw(null,null,n=>1));}catch(InvalidDataException){failed=true;}
            Check(failed,"Divergent offline draw histories are rejected instead of minting currency");
            var store=new DiaryStore(Path.Combine(directory,"collection"));var book=new DiaryBook {Progress=progress,Collection=exchanged};store.Save(book);
            var loaded=store.Load();Check(loaded.Version==7 && SangmonCollection.State(loaded.Collection,loaded.Progress).Petals==9 && loaded.Collection.Actions.Select(a=>a.Id).SequenceEqual(exchanged.Actions.Select(a=>a.Id)),"Version-seven storage preserves all draws, exchanges and balances");
            var bytes=File.ReadAllBytes(store.FilePath);book.Collection.Actions.Add(book.Collection.Actions[0]);failed=false;try{store.Save(book);}catch(InvalidDataException){failed=true;}
            Check(failed && bytes.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Duplicate transaction ID cannot overwrite a valid collection");
        }
    }
}
