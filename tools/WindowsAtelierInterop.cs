using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

public static class WindowsAtelierInterop
{
    public static int Main(string[] args)
    {
        try {
            if(args[0]=="export") {
                var p=new SangmonProgress {AwardedDays=Enumerable.Range(0,1000).Select(i=>DiaryStore.Key(new DateTime(2020,1,1).AddDays(i))).ToList()};CollectionBook c=null;
                for(int i=0;i<9;i++)c=SangmonCollection.Draw(c,p,n=>0);
                c=SangmonCollection.Draw(c,p,n=>n>=9900?6500:0);c=SangmonCollection.Synthesize(c,p,0,n=>0);
                var s=SangmonCollection.State(c,p);c=SangmonCollection.Craft(c,p,SangmonItems.All.First(i=>SangmonCollection.Rarity(i.Id)==0 && !s.Owned.Contains(i.Id)).Id);
                var book=new DiaryBook {Progress=p,Collection=c};new DiaryStore(Path.GetDirectoryName(Path.GetFullPath(args[1]))).Export(args[1],book);
                Console.WriteLine("Windows real serializer: v8 tiered/synthesis/craft fixture exported.");return 0;
            }
            DiaryBook imported;using(var input=File.OpenRead(args[1]))imported=DiaryStore.Read(input);
            var state=SangmonCollection.State(imported.Collection,imported.Progress);
            if(imported.Version!=8 || SangmonGrowth.XP(imported.Progress)!=10000 || state.Coins!=10975 || state.Petals!=24 || state.Draws!=10 || state.Crafts!=1 || state.Syntheses!=2 || state.SynthesisSuccesses!=2 || state.Owned.Count!=11 || !state.Materials.SequenceEqual(new[]{1,1,1,0}) || !state.Milestones.SetEquals(new[]{10}))throw new Exception("Android v8 export lost collection or synthesis data.");
            Console.WriteLine("Windows real serializer: Android v8 tiered rewards, materials, synthesis, crafting and achievement passed.");return 0;
        } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
