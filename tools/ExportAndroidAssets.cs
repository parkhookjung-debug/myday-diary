using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Script.Serialization;
using MyDay.Windows.Core;
using MyDay.Windows.Character;

// Compile with the Windows renderer sources; this keeps both platforms' artwork identical.
public static class AndroidAssetExporter
{
    public static int Main(string[] args)
    {
        try{return Export(args);}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
    private static int Export(string[] args)
    {
        if(args.Length<1 || args.Length>3 || (args.Length>=2 && args[1]!="--items-only")) return 1;
        bool itemsOnly=args.Length>=2;
        string itemSlot=args.Length==3?args[2]:null;
        if(itemSlot!=null && !SangmonItems.Slots.Contains(itemSlot))return 1;
        var root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
        var characters=Path.Combine(root,"sangmon");Directory.CreateDirectory(characters);
        var icons=Path.Combine(root,"items");Directory.CreateDirectory(icons);
        var portraits=Path.Combine(root,"portraits");Directory.CreateDirectory(portraits);
        var overlays=Path.Combine(root,"equipment");Directory.CreateDirectory(overlays);
        var json=new JavaScriptSerializer();
        var identityPath=Path.Combine(root,"..","java","com","myday","diary","data","SangmonIdentity.kt");
        File.WriteAllText(identityPath,"package com.myday.diary.data\n\n// Generated from the Windows catalog by tools/export-android-assets.ps1.\nobject SangmonIdentity {\n    val levels=mapOf(\n        "+string.Join(",\n        ",MonsterVariants.All.Select(v=>json.Serialize(MonsterVariants.Id(v))+" to "+SangmonGrowth.RequiredLevel(v)))+"\n    )\n}\n",new System.Text.UTF8Encoding(false));
        File.AppendAllText(identityPath,"\nobject SangmonUnlockXP {\n    val values=mapOf("+string.Join(",",MonsterVariants.All.Select(v=>json.Serialize(MonsterVariants.Id(v))+" to "+SangmonGrowth.RequiredXP(v)))+")\n}\n",new System.Text.UTF8Encoding(false));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(identityPath),"EquipmentCatalog.kt"),"package com.myday.diary.data\n\n// Generated from windows/Core/SangmonItems.cs. Do not edit.\nobject EquipmentCatalog {\n    val slots=listOf("+string.Join(",",SangmonItems.Slots.Select(json.Serialize))+")\n    val names=listOf("+string.Join(",",SangmonItems.SlotNames.Select(json.Serialize))+")\n    val items=mapOf(\n        "+string.Join(",\n        ",SangmonItems.All.Select(i=>json.Serialize(i.Id)+" to "+json.Serialize(i.Slot)))+"\n    )\n}\n",new System.Text.UTF8Encoding(false));
        File.WriteAllText(Path.Combine(root,"catalog.json"),json.Serialize(new {
            characters=MonsterVariants.All.Select(v=>new {id=MonsterVariants.Id(v),name=MonsterVariants.Name(v),level=SangmonGrowth.RequiredLevel(v),xp=SangmonGrowth.RequiredXP(v)}).ToArray(),
            items=SangmonItems.All.Select(i=>new {id=i.Id,name=i.Name,slot=i.Slot,description=i.Description,element=i.Element.ToString()}).ToArray(),
            templates=DiaryTemplates.All.Select(t=>new {id=t.Id,name=t.Name,description=t.Description,style=t.Style,category=t.Category,minutes=t.Minutes,layout=t.Layout,
                sections=t.Sections.Select((s,index)=>new {kind=s.Kind,title=s.Title,prompt=s.Prompt,
                    x=JournalLayouts.Arrange(t.Layout,t.Sections.Length,900)[index].X,y=JournalLayouts.Arrange(t.Layout,t.Sections.Length,900)[index].Y,
                    width=JournalLayouts.Arrange(t.Layout,t.Sections.Length,900)[index].Width,height=JournalLayouts.Arrange(t.Layout,t.Sections.Length,900)[index].Height}).ToArray()}).ToArray()
        }),new System.Text.UTF8Encoding(false));
        var activities=new[] {PetActivity.Rest,PetActivity.Walk,PetActivity.Fire,PetActivity.Yawn,PetActivity.Hop,PetActivity.Look,PetActivity.Sleep,PetActivity.Drag};
        foreach(var variant in itemsOnly?new MonsterVariant[0]:MonsterVariants.All) using(var atlas=new Bitmap(192*8,192*activities.Length)) {
            using(var g=Graphics.FromImage(atlas)) for(int row=0;row<activities.Length;row++) for(int frame=0;frame<8;frame++) {
                var activity=activities[row];
                double age=row==0?new[] {0,.3,.8,1.2,2.4,3.2,3.86,4.05}[frame]:row==1 || row==2?frame*.1:row==4?frame*.65/8:row==3?frame*.2:frame*.3;
                var saved=g.Save();g.TranslateTransform(frame*192,row*192);
                MonsterPainter.Draw(g,new Rectangle(10,12,172,158),MonsterPose.ForActivity(age,activity,age,0,0),true,variant);g.Restore(saved);
            }
            atlas.Save(Path.Combine(characters,MonsterVariants.Id(variant)+".png"),ImageFormat.Png);
            using(var portrait=atlas.Clone(new Rectangle(0,0,192,192),PixelFormat.Format32bppArgb)) portrait.Save(Path.Combine(portraits,MonsterVariants.Id(variant)+".png"),ImageFormat.Png);
        }
        var items=SangmonItems.All.Where(i=>itemSlot==null || i.Slot==itemSlot).ToArray();
        foreach(var item in items) using(var image=new Bitmap(192,240)) {
            using(var g=Graphics.FromImage(image)) ItemPainter.Draw(g,new RectangleF(8,8,176,224),item,.2);
            image.Save(Path.Combine(icons,item.Id+".png"),ImageFormat.Png);
            using(var overlay=new Bitmap(192,192)) {
                using(var g=Graphics.FromImage(overlay))ItemPainter.DrawOverlay(g,new Rectangle(10,12,172,158),item);
                for(int edge=0;edge<192;edge++)if(overlay.GetPixel(edge,0).A!=0 || overlay.GetPixel(edge,191).A!=0 || overlay.GetPixel(0,edge).A!=0 || overlay.GetPixel(191,edge).A!=0)throw new InvalidOperationException("Equipment overlay clipped: "+item.Id);
                overlay.Save(Path.Combine(overlays,item.Id+".png"),ImageFormat.Png);
            }
            Console.WriteLine("Exported item "+item.Id);
        }
        var fixture=new DiaryBook {Version=4,CharacterStyle="frost",Progress=new SangmonProgress(),Equipment=new SangmonEquipment {WeaponId="frost-sword",CharmId="astral-orb"}};
        fixture.Progress.AwardedDays.AddRange(new[] {"2026-10-07","2026-10-08"});
        var entry=DiaryEntry.Empty();entry.Theme=2;entry.Mood="설레요";entry.LayoutMode="free";entry.PageStyle="dots";
        foreach(var kind in new[] {"text","todo","habit","emotion","photo","photo-slot"}) {
            var block=DiaryBlock.Create(kind);block.Id="windows-"+kind;block.Title="제목-"+kind;block.Prompt="질문-"+kind;block.Text="윈도우 원본 "+kind;
            block.Checked=kind=="todo" || kind=="habit";block.Wide=kind=="text";block.X=kind=="photo"?1520:12;block.Y=kind=="photo"?1040:12;
            block.Width=340;block.Height=250;
            if(kind=="photo") using(var bitmap=new Bitmap(16,8)) using(var buffer=new MemoryStream()) {using(var g=Graphics.FromImage(bitmap)) g.Clear(Color.Coral);bitmap.Save(buffer,ImageFormat.Jpeg);block.Photo=Convert.ToBase64String(buffer.ToArray());}
            entry.Blocks.Add(block);
        }
        fixture.Days["2026-10-08"]=entry;fixture.Layouts.Add(SavedLayouts.Capture(entry,"사진과 한 줄",900));fixture.Layouts[0].Id="windows-fixture-layout";
        DiaryStore.Validate(fixture);
        var resourcePath=Path.Combine(root,"..","..","test","resources");Directory.CreateDirectory(resourcePath);
        using(var output=File.Create(Path.Combine(resourcePath,"windows-v4.json"))) new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(output,fixture);
        var deviceResources=Path.Combine(root,"..","..","androidTest","assets");Directory.CreateDirectory(deviceResources);
        File.Copy(Path.Combine(resourcePath,"windows-v4.json"),Path.Combine(deviceResources,"windows-v4.json"),true);
        fixture.Version=5;fixture.Progress=SangmonGrowth.Award(SangmonGrowth.CheckIn(fixture.Progress,new DateTime(2026,10,16)),entry,new DateTime(2026,10,16));
        foreach(string slot in SangmonItems.Slots.Skip(2))fixture.Equipment=SangmonItems.Select(fixture.Equipment,slot,SangmonItems.All.First(i=>i.Slot==slot).Id);
        DiaryStore.Validate(fixture);
        using(var output=File.Create(Path.Combine(resourcePath,"windows-v5.json")))new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(output,fixture);
        File.Copy(Path.Combine(resourcePath,"windows-v5.json"),Path.Combine(deviceResources,"windows-v5.json"),true);
        fixture.Version=6;fixture.Collection=SangmonCollection.Draw(null,fixture.Progress,n=>0);fixture.Collection=SangmonCollection.Draw(fixture.Collection,fixture.Progress,n=>0);
        using(var output=File.Create(Path.Combine(resourcePath,"windows-v6.json")))new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(output,fixture);
        File.Copy(Path.Combine(resourcePath,"windows-v6.json"),Path.Combine(deviceResources,"windows-v6.json"),true);
        Console.WriteLine("Exported "+(itemsOnly?"existing Sangmon atlases unchanged, ":"64 Sangmon atlases, ")+items.Length+" items/overlays and 100 templates.");return 0;
    }
}
