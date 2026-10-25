using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
public static class WindowsStudioInterop {
 public static int Main(string[] args){try{DiaryBook book;using(var input=File.OpenRead(args[0]))book=DiaryStore.Read(input);if(book.Version!=9||book.Studio.RoomTheme!=1||book.Studio.Furniture.Count!=6||!book.Studio.Pages.Single(p=>p.Date=="2026-10-25").Favourite||!book.Studio.Pages.Single(p=>p.Date=="2026-10-25").Tags.SequenceEqual(new[]{"산책","학교"})||book.Studio.Furniture[5].X!=600)throw new Exception("Studio metadata changed in native round trip");Console.WriteLine("Version-nine native studio compatibility passed.");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
