using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunItemTests(string directory)
        {
            Check(SangmonItems.All.Length==12 && SangmonItems.All.Select(i=>i.Id).Distinct().Count()==12 && SangmonItems.All.Select(i=>i.Name).Distinct().Count()==12,"Twelve item IDs and names are distinct");
            Check(SangmonItems.All.Count(i=>i.Slot=="weapon")==6 && SangmonItems.All.Count(i=>i.Slot=="charm")==6 && SangmonItems.All.GroupBy(i=>i.Element).All(g=>g.Count()==2),"Six elements each provide one sword and one charm");
            var gear=SangmonItems.Select(null,"weapon","frost-sword");var pair=SangmonItems.Select(gear,"charm","solar-orb");
            Check(gear.CharmId=="none" && pair.WeaponId=="frost-sword" && pair.CharmId=="solar-orb","Two independent slots equip together without mutating the previous state");
            var clear=SangmonItems.Select(pair,"weapon","none");
            Check(clear.WeaponId=="none" && clear.CharmId==pair.CharmId && pair.WeaponId=="frost-sword","Removing a weapon preserves the charm and source state");
            Check(SangmonItems.Select(clear,"charm","none").CharmId=="none","Charm can be independently removed");
            bool rejected=false;try{SangmonItems.Select(pair,"weapon","solar-orb");}catch(ArgumentException){rejected=true;}
            Check(rejected && pair.WeaponId=="frost-sword","Wrong-slot item is rejected without changing equipment");
            rejected=false;try{SangmonItems.Select(pair,"hat","solar-sword");}catch(ArgumentException){rejected=true;}Check(rejected,"Unknown equipment slot is rejected");
            rejected=false;try{SangmonItems.Select(pair,"weapon","missing-sword");}catch(ArgumentException){rejected=true;}Check(rejected,"Unknown selection is rejected");
            var legacy=SangmonItems.Import(pair,null);Check(legacy.WeaponId==pair.WeaponId && legacy.CharmId==pair.CharmId && !ReferenceEquals(pair,legacy),"An older backup without gear preserves current equipment independently");
            var incoming=new SangmonEquipment {WeaponId="none",CharmId="shadow-orb"};var imported=SangmonItems.Import(pair,incoming);
            Check(imported.WeaponId=="none" && imported.CharmId=="shadow-orb" && !ReferenceEquals(imported,incoming),"New backup explicitly restores or clears independent slots");
            var book=new DiaryBook {Equipment=pair,Progress=new SangmonProgress {AwardedDays=new List<string>{"2026-10-10"}}};
            var entry=DiaryTemplates.NewPage();entry.Blocks[0].Text="보존할 일기";book.Days["2026-10-10"]=entry;book.Layouts.Add(SavedLayouts.Capture(entry,"기존 형식",800));
            var store=new DiaryStore(Path.Combine(directory,"items"));store.Save(book);var loaded=store.Load();
            Check(loaded.Version==4 && loaded.Equipment.WeaponId==pair.WeaponId && loaded.Equipment.CharmId==pair.CharmId && loaded.Days["2026-10-10"].Blocks[0].Text==entry.Blocks[0].Text && loaded.Layouts.Count==1 && SangmonGrowth.XP(loaded.Progress)==10,"Equipment round-trips as version four with diary, layouts and growth intact");
            bool allSaved=true;foreach(var item in SangmonItems.All){book.Equipment=SangmonItems.Select(book.Equipment,item.Slot,item.Id);store.Save(book);var current=store.Load().Equipment;allSaved&=(item.Slot=="weapon"?current.WeaponId:current.CharmId)==item.Id;}
            Check(allSaved && SangmonGrowth.XP(store.Load().Progress)==10,"All twelve selections survive reload without granting XP");
            string backup=Path.Combine(directory,"items-export.json");store.Export(backup,book);
            using(var stream=File.OpenRead(backup))Check(DiaryStore.Read(stream).Equipment.WeaponId==book.Equipment.WeaponId,"Equipment is included in JSON backups");
            book.Equipment=null;store.Save(book);Check(book.Version==4 && store.Load().Version==4,"Removing equipment data never downgrades a version-four book");
            book.Equipment=new SangmonEquipment {WeaponId="future-sword",CharmId="frost-sword"};store.Save(book);
            Check(store.Load().Equipment.WeaponId=="none" && store.Load().Equipment.CharmId=="none" && store.Load().Days.Count==1,"Unknown or wrong-slot imported gear falls back while retaining diary content");
            bool oldReadable=true;foreach(int version in new[]{1,2,3})using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"Version\":"+version+",\"Days\":[]}")))oldReadable&=DiaryStore.Read(stream).Equipment==null;
            Check(oldReadable,"Versions one through three remain readable without equipment");
            var future=new DiaryBook {Version=5};rejected=false;try{DiaryStore.Validate(future);}catch(InvalidDataException){rejected=true;}Check(rejected,"Unsupported future version is rejected");
            var fingerprints=new HashSet<string>();bool iconsFit=true;
            foreach(var item in SangmonItems.All)using(var image=new Bitmap(220,260)) {
                using(var g=Graphics.FromImage(image))ItemPainter.Draw(g,new RectangleF(12,12,196,236),item,.2);
                using(var stream=new MemoryStream()){image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);fingerprints.Add(Convert.ToBase64String(stream.ToArray()));}
                iconsFit&=ClearEdges(image);
            }
            Check(fingerprints.Count==12 && iconsFit,"All twelve item icons render distinctly with transparent margins");
            bool fits=true;string failure="";
            foreach(var weapon in SangmonItems.All.Where(i=>i.Slot=="weapon"))foreach(var charm in SangmonItems.All.Where(i=>i.Slot=="charm"))
            foreach(var variant in new[]{MonsterVariant.Original,MonsterVariant.Winged,MonsterVariant.Mini,MonsterVariant.MemoryHero})
            foreach(bool left in new[]{true,false})foreach(var activity in new[]{PetActivity.Hop,PetActivity.Fire,PetActivity.Yawn,PetActivity.Drag})
            using(var image=new Bitmap(240,220)) {
                var equipped=new SangmonEquipment {WeaponId=weapon.Id,CharmId=charm.Id};
                using(var g=Graphics.FromImage(image))MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(.32,activity,.32,0,0),left,variant,equipped);
                bool clearEdges=ClearEdges(image);if(!clearEdges && failure=="")failure=weapon.Id+" "+charm.Id+" "+variant+" "+activity+" "+left;fits&=clearEdges;
            }
            Check(fits,"Every dual-slot combination fits desktop poses in both directions: "+failure);
        }
        private static bool ClearEdges(Bitmap image)
        {
            for(int x=0;x<image.Width;x++)if(image.GetPixel(x,0).A!=0 || image.GetPixel(x,image.Height-1).A!=0)return false;
            for(int y=0;y<image.Height;y++)if(image.GetPixel(0,y).A!=0 || image.GetPixel(image.Width-1,y).A!=0)return false;
            return true;
        }
        public static void ItemPreview(string directory)
        {
            Directory.CreateDirectory(directory);
            using(var font=new Font("맑은 고딕",14,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var small=new Font("맑은 고딕",11,FontStyle.Regular,GraphicsUnit.Pixel))
            for(int frame=0;frame<80;frame++)using(var image=new Bitmap(960,840)) {
                using(var g=Graphics.FromImage(image)) {
                    g.Clear(Color.FromArgb(21,28,45));
                    for(int i=0;i<SangmonItems.All.Length;i++) {
                        var item=SangmonItems.All[i];int x=i%4*240,y=i/4*280;
                        using(var panel=new SolidBrush(Color.FromArgb(35,45,66)))using(var rounded=Design.Rounded(new RectangleF(x+8,y+8,224,264),14))g.FillPath(panel,rounded);
                        using(var pen=new Pen(ItemPainter.Tone(item.Element),2))g.DrawLine(pen,x+24,y+27,x+216,y+27);
                        ItemPainter.Draw(g,new RectangleF(x+20,y+37,200,183),item,frame*.05);
                        TextRenderer.DrawText(g,item.Name,font,new Rectangle(x+12,y+229,216,24),Color.FromArgb(244,239,225),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                        TextRenderer.DrawText(g,item.Slot=="weapon"?"WEAPON":"MAGIC ORB",small,new Rectangle(x+12,y+252,216,18),Color.FromArgb(162,183,206),TextFormatFlags.HorizontalCenter);
                    }
                }
                image.Save(Path.Combine(directory,frame.ToString("D3")+".png"));
            }
            Console.WriteLine("Rendered twelve original items in 80 animated frames.");
        }
    }
}
