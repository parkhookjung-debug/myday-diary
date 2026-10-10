using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        internal static void WritePhotoFixture(string path)
        {
            using(var bitmap=new Bitmap(1800,1200))
            using(var g=Graphics.FromImage(bitmap)) {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                using(var sky=new LinearGradientBrush(new Rectangle(0,0,1800,1200),Color.FromArgb(194,218,214),Color.FromArgb(249,236,210),90f)) g.FillRectangle(sky,0,0,1800,1200);
                using(var sun=new SolidBrush(Color.FromArgb(251,225,158))) g.FillEllipse(sun,1180,150,180,180);
                using(var hills=new SolidBrush(Color.FromArgb(143,173,154))) g.FillPolygon(hills,new[] {new Point(0,700),new Point(420,410),new Point(920,770),new Point(1400,430),new Point(1800,660),new Point(1800,1200),new Point(0,1200)});
                using(var green=new SolidBrush(Color.FromArgb(73,117,97))) g.FillEllipse(green,-350,750,2500,900);
                using(var pathBrush=new SolidBrush(Color.FromArgb(231,210,173))) g.FillPolygon(pathBrush,new[] {new Point(870,800),new Point(950,800),new Point(1300,1200),new Point(650,1200)});
                bitmap.Save(path,ImageFormat.Png);
            }
        }
        private static void RunPhotoTests(string directory)
        {
            string path=Path.Combine(directory,"sample.png"); WritePhotoFixture(path);
            string data=DiaryPhoto.FromFile(path);
            using(var image=DiaryPhoto.Decode(data)) Check(image.Width==1600 && image.Height==1067 && image.PropertyIdList.Length==0,"Photo normalization bounds resolution, preserves ratio and strips metadata");
            var block=DiaryBlock.Create("photo"); block.Photo=data; block.Text="산책의 기억";
            DiaryLayout.SetBounds(block,new Rectangle(15,20,450,360));
            var book=new DiaryBook(); var entry=DiaryEntry.Empty(); entry.LayoutMode="free"; entry.Blocks.Add(block); book.Days["2026-10-09"]=entry;
            var store=new DiaryStore(Path.Combine(directory,"photos")); store.Save(book);
            var export=Path.Combine(directory,"photo-backup.json"); store.Export(export,book); File.Delete(path);
            DiaryBook imported; using(var stream=File.OpenRead(export)) imported=DiaryStore.Read(stream);
            var restored=imported.Days["2026-10-09"].Blocks[0];
            using(var image=DiaryPhoto.Decode(restored.Photo)) Check(restored.Photo==data && restored.Text==block.Text && DiaryLayout.Bounds(restored)==DiaryLayout.Bounds(block) && image.Width==1600,"Photo and caption survive backup import after original file deletion");
            var before=File.ReadAllBytes(store.FilePath); block.Photo="broken-base64"; bool rejected=false;
            try { store.Save(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Replacing a validated photo with corrupt data cannot overwrite saved diary");
            using(var stream=new MemoryStream()) {
                new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(stream,book); stream.Position=0; rejected=false;
                try { DiaryStore.Read(stream); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Corrupt photos are rejected during backup import before applying records");
            }
            block.Photo=null; rejected=false; try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Photo blocks without an image are rejected");
            block.Photo=data; block.Kind="text"; rejected=false; try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Non-photo blocks cannot hide attachment data"); block.Kind="photo";
            string junk=Path.Combine(directory,"invalid.png"); File.WriteAllText(junk,"this is not an image"); rejected=false;
            try { DiaryPhoto.FromFile(junk); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Invalid image files fail without creating a photo");
            string big=Path.Combine(directory,"large.jpg"); using(var stream=File.Create(big)) stream.SetLength(DiaryPhoto.MaxInputBytes+1L);
            rejected=false; try { DiaryPhoto.FromFile(big); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Oversized input is rejected before decoding");
            using(var stream=File.Create(big)) stream.SetLength(DiaryStore.MaxFileBytes+1);
            rejected=false; using(var stream=File.OpenRead(big)) try { DiaryStore.Read(stream); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Oversized backup is rejected before deserializing its contents");
            string transparent=Path.Combine(directory,"transparent.png");
            using(var image=new Bitmap(50,50)) image.Save(transparent,ImageFormat.Png);
            using(var image=DiaryPhoto.Decode(DiaryPhoto.FromFile(transparent))) Check(image.GetPixel(25,25).R>245 && image.Width==50,"Transparent input gets a white background without upscaling");
            string oriented=Path.Combine(directory,"oriented.jpg");
            using(var image=new Bitmap(80,40)) image.Save(oriented,ImageFormat.Jpeg);
            byte[] jpeg=File.ReadAllBytes(oriented);
            // EXIF APP1: little-endian TIFF, orientation=6 (90 degrees clockwise).
            byte[] exif={255,225,0,34,69,120,105,102,0,0,73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,6,0,0,0,0,0,0,0};
            File.WriteAllBytes(oriented,jpeg.Take(2).Concat(exif).Concat(jpeg.Skip(2)).ToArray());
            using(var image=DiaryPhoto.Decode(DiaryPhoto.FromFile(oriented))) Check(image.Width==40 && image.Height==80,"Phone EXIF orientation is applied before metadata removal");
            block.Photo=new string('A',DiaryPhoto.MaxEncodedLength+4); rejected=false;
            try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Oversized embedded data is rejected"); block.Photo=data;
            long sum=0; int index=0; while(sum<=DiaryPhoto.MaxBookCharacters) {
                var copy=DiaryBlock.Create("photo"); copy.Photo=data; copy.ValidatedPhoto=data; entry.Blocks.Add(copy); sum+=data.Length; index++;
                if(index==199) { var next=DiaryEntry.Empty(); book.Days[DiaryStore.Key(new DateTime(2026,10,9).AddDays(book.Days.Count))]=next; entry=next; index=0; }
            }
            rejected=false; try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Aggregate attachment limit prevents unbounded photo backups");
        }
    }
}
