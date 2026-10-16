using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Core;

public static class WindowsAndroidInterop
{
    public static int Main(string[] args)
    {
        try {
            DiaryBook book;using(var input=File.OpenRead(args[0])) book=DiaryStore.Read(input);
            var entry=book.Days["2026-10-08"];var text=entry.Blocks.Single(b=>b.Kind=="text");var photo=entry.Blocks.Single(b=>b.Kind=="photo");
            if((book.Version!=4 && book.Version!=5) || entry.Blocks.Count!=6 || text.Id!="windows-text" || text.Text!="안드로이드에서 수정한 글" || text.Title!="제목-text" || text.Prompt!="질문-text" || !text.Wide ||
                entry.Theme!=2 || entry.Mood!="설레요" || entry.PageStyle!="dots" || entry.LayoutMode!="free" ||
                photo.X!=1520 || photo.Y!=1040 || photo.Width!=340 || photo.Height!=250 || string.IsNullOrEmpty(photo.Photo) ||
                entry.Blocks.Single(b=>b.Kind=="todo").Checked || !entry.Blocks.Single(b=>b.Kind=="habit").Checked ||
                book.Progress.AwardedDays.Count!=(book.Version==5?3:2) || book.Equipment.WeaponId!="frost-sword" || book.Equipment.CharmId!="astral-orb" || book.CharacterStyle!="frost" ||
                book.Layouts.Count!=1 || book.Layouts[0].Name!="사진과 한 줄" || !book.Layouts[0].Blocks.Any(b=>b.Kind=="photo-slot")) throw new Exception("Android export lost Windows metadata or content.");
            using(var bitmap=DiaryPhoto.Decode(photo.Photo)) if(bitmap.Width!=16 || bitmap.Height!=8) throw new Exception("Photo damaged.");
            if(book.Version==5 && (SangmonGrowth.XP(book.Progress)!=60 || SangmonGrowth.Flags(book.Progress,new DateTime(2026,10,16))!=31 || SangmonItems.Slots.Any(s=>SangmonItems.Get(book.Equipment,s)=="none")))throw new Exception("V5 mission flags or eight-slot equipment lost.");
            using(var output=File.Create(args[1])) new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(output,book);
            Console.WriteLine("Windows real serializer: Android export, photos, layouts, growth and equipment passed.");return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
