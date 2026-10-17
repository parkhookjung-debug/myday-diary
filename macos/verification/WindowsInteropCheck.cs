// Compile with the unmodified v0.3.9 Windows source and /main:WindowsInteropCheck.
// Exercises its real DataContractJsonSerializer and DiaryStore.Validate, not a duplicate schema.
using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;

public static class WindowsInteropCheck
{
    public static int Main(string[] args)
    {
        try {
            if(args.Length != 2) throw new Exception("Expected Mac export and Windows re-export paths");
            DiaryBook book;
            using(var stream = File.OpenRead(args[0])) book = DiaryStore.Read(stream);
            var entry = book.Days["2024-02-29"];
            if(book.Version != 4 || entry.Blocks.Count != 6 || entry.Mood != "설레요" || entry.LayoutMode != "free" || entry.PageStyle != "dots")
                throw new Exception("Entry fields were not preserved");
            if(entry.Blocks[0].Text != "맥에서 수정한 글" || entry.Blocks[0].Id != "windows-id-0" || entry.Blocks[0].Title != "제목 0" || entry.Blocks[0].Prompt != "질문 0?" || !entry.Blocks[0].Wide)
                throw new Exception("Text or template metadata was not preserved");
            if(!entry.Blocks[1].Checked || !entry.Blocks[2].Checked || entry.Blocks[4].Text != "산책 사진 설명" || String.IsNullOrEmpty(entry.Blocks[4].Photo) || entry.Blocks[5].Kind != "photo-slot")
                throw new Exception("Checks or photos were not preserved");
            if(entry.Blocks[4].X != 1520 || entry.Blocks[4].Y != 1040 || entry.Blocks[4].Width != 340 || entry.Blocks[4].Height != 250)
                throw new Exception("Logical geometry was not preserved");
            if(book.Layouts.Count != 1 || book.Layouts[0].Name != "사진과 한 줄" || book.Layouts[0].Blocks[0].Kind != "photo-slot" || book.Progress.AwardedDays.Count != 2 || book.Equipment.WeaponId != "frost-sword" || book.Equipment.CharmId != "astral-orb")
                throw new Exception("Layouts, growth or equipment were not preserved");
            new DiaryStore(Path.GetDirectoryName(Path.GetFullPath(args[1]))).Export(args[1], book);
            Console.WriteLine("Windows v0.3.9 real serializer / validator: passed");
            return 0;
        } catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
