using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
using MyDay.Windows.Character;
namespace MyDay.Windows {
 internal static partial class Tests {
  private static void RunFocusedTests(string directory) {
   DiaryBook old;
   using(var input=File.OpenRead("app/src/test/resources/windows-v8.json"))old=DiaryStore.Read(input);
   DiaryStore.Validate(old);old.CharacterStyle="frost";old.Equipment=new SangmonEquipment();old.Equipment.HeadId="solar-crown";
   string style=old.CharacterStyle,head=old.Equipment.HeadId;int xp=SangmonGrowth.XP(old.Progress),actions=old.Collection.Actions.Count;
   var store=new DiaryStore(Path.Combine(directory,"focused-legacy"));store.Save(old);var restored=store.Load();
   Check(restored.CharacterStyle==style && restored.Equipment.HeadId==head && SangmonGrowth.XP(restored.Progress)==xp && restored.Collection.Actions.Count==actions,"Focused save preserves legacy style, outfits, collection history and XP without granting rewards");
   using(var original=new Bitmap(240,220))using(var legacy=new Bitmap(240,220)) {
    var pose=MonsterPose.At(.2,false,false,false);
    using(var g=Graphics.FromImage(original))MonsterPainter.Draw(g,new Rectangle(0,0,240,220),pose,true);
    using(var g=Graphics.FromImage(legacy))MonsterPainter.Draw(g,new Rectangle(0,0,240,220),pose,true,MonsterVariant.Frost,restored.Equipment);
    bool same=true;for(int y=0;y<220;y++)for(int x=0;x<240;x++)same &= original.GetPixel(x,y)==legacy.GetPixel(x,y);
    Check(same,"Focused runtime renders the original character even when the backup contains a retired skin and outfit");
   }
   Check(restored.CharacterStyle==style && restored.Collection.Actions.Count==actions,"Rendering does not modify legacy backup metadata");
  }
 }
}
