using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static SangmonProgress AtelierProgress() {return new SangmonProgress {AwardedDays=Enumerable.Range(0,1000).Select(i=>DiaryStore.Key(new DateTime(2020,1,1).AddDays(i))).ToList()};}
        private static CollectionBook AtelierDraws(SangmonProgress p,int grade,int count)
        {
            CollectionBook c=null;int ticket=CollectionPolicy.Weights.Take(grade).Sum();
            for(int i=0;i<count;i++)c=SangmonCollection.Draw(c,p,n=>n>=9900?Math.Min(ticket,n-1):0);return c;
        }
        private static void RunAtelierTests(string directory)
        {
            var p=AtelierProgress();
            Check(SangmonItems.All.All(i=>SangmonCollection.Rarity(i.Id)>=0 && SangmonCollection.Rarity(i.Id)<4) && SangmonCollection.Starters.All(i=>SangmonCollection.Rarity(i)==0),"All 300 items have stable tiers and eight starters stay ordinary");
            int[] tickets={0,6499,6500,8999,9000,9899,9900,9999},grades={0,0,1,1,2,2,3,3};
            Check(tickets.Select(t=>SangmonCollection.Rarity(SangmonCollection.Draw(null,p,n=>n==10000?t:0).Actions.Last().ItemId)).SequenceEqual(grades),"Weighted draw boundaries exactly implement 65/25/9/1 percent");
            for(int g=0;g<4;g++) {
                var c=AtelierDraws(p,g,6);var s=SangmonCollection.State(c,p);
                Check(s.Materials[g]==6 && s.Petals==5*CollectionPolicy.DuplicatePetals[g],"Tier "+g+" grants one fragment per draw and grade-specific duplicate petals");
                if(g>=3)continue;
                var success=SangmonCollection.State(SangmonCollection.Synthesize(c,p,g,n=>CollectionPolicy.SynthesisPercent[g]-1),p);
                var fail=SangmonCollection.State(SangmonCollection.Synthesize(c,p,g,n=>CollectionPolicy.SynthesisPercent[g]),p);
                Check(success.Materials[g]==3 && success.Materials[g+1]==1 && success.SynthesisSuccesses==1 && fail.Materials[g]==4 && fail.Materials[g+1]==0 && fail.SynthesisSuccesses==0 && fail.Syntheses==1,"Synthesis "+g+" has the exact probability boundary, consumes three and refunds one only on failure");
                Check(s.Owned.SetEquals(success.Owned) && s.Owned.SetEquals(fail.Owned) && s.Coins==success.Coins && s.Coins==fail.Coins,"Synthesis never consumes items, equipment or clovers");
            }
            bool rejected=false,called=false;try{SangmonCollection.Synthesize(null,p,0,n=>{called=true;return 0;});}catch(InvalidOperationException){rejected=true;}
            Check(rejected && !called,"Insufficient synthesis materials are rejected before randomness");
            var source=AtelierDraws(p,0,9);var prior=SangmonCollection.State(source,p);var target=SangmonItems.All.First(i=>SangmonCollection.Rarity(i.Id)==0 && !prior.Owned.Contains(i.Id));
            var crafted=SangmonCollection.Craft(source,p,target.Id);var state=SangmonCollection.State(crafted,p);
            Check(state.Crafts==1 && state.Materials[0]==4 && state.Materials[1]==2 && state.Owned.Contains(target.Id) && state.Milestones.SetEquals(new[]{10}) && state.Draws==prior.Draws,"Five same-grade fragments craft the chosen item and unlock the ten-item achievement once");
            var repeated=SangmonCollection.State(SangmonCollection.Merge(crafted,crafted),p);
            Check(repeated.Materials.SequenceEqual(state.Materials) && repeated.Milestones.SetEquals(state.Milestones),"Repeated backup merges cannot mint achievement fragments");
            var old=new CollectionBook {Actions=source.Actions.Select(a=>new CollectionAction {Id=a.Id,Kind=a.Kind=="draw-tiered"?"draw":a.Kind,ItemId=a.ItemId}).ToList()};var legacy=SangmonCollection.State(old,p);
            Check(legacy.Coins==prior.Coins && legacy.Petals==prior.Petals && legacy.Owned.SetEquals(prior.Owned) && legacy.Materials.Sum()==0,"Old uniform draw ledgers preserve exact spending, duplicates and ownership");
            var store=new DiaryStore(Path.Combine(directory,"atelier"));var book=new DiaryBook {Progress=p,Collection=crafted};store.Save(book);
            var loaded=store.Load();Check(loaded.Version==8 && SangmonCollection.State(loaded.Collection,loaded.Progress).Materials.SequenceEqual(state.Materials),"Version-eight backups replay tiers, crafting, fragments and achievements");
            var bytes=File.ReadAllBytes(store.FilePath);book.Collection.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind="synth",ItemId="2:up"});rejected=false;
            try{store.Save(book);}catch(InvalidDataException){rejected=true;}
            Check(rejected && bytes.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Forged or unfunded synthesis cannot overwrite committed data");
            var up=SangmonCollection.Synthesize(source,p,0,n=>0);var stay=SangmonCollection.Synthesize(source,p,0,n=>99);rejected=false;
            try{SangmonCollection.Merge(up,stay);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"Divergent synthesis histories cannot roll back spending through backup merge");
            var guaranteed=source;guaranteed=SangmonCollection.Draw(guaranteed,p,n=>0);Check(SangmonCollection.State(guaranteed,p).Owned.Count==10,"Tiered draws retain the tenth-draw uncollected item guarantee");
            var noTop=new CollectionBook {Actions=source.Actions.ToList()};
            foreach(var i in SangmonItems.All.Where(i=>SangmonCollection.Rarity(i.Id)==3))noTop.Actions.Add(new CollectionAction {Id=Guid.NewGuid().ToString("N"),Kind="legacy",ItemId=i.Id});
            var oldOwned=SangmonCollection.State(noTop,p).Owned;var remaining=SangmonCollection.Draw(noTop,p,n=>n-1).Actions.Last().ItemId;
            Check(SangmonCollection.Rarity(remaining)==2 && !oldOwned.Contains(remaining),"Pity reweights empty tiers and still returns an uncollected item");
        }
    }
}
