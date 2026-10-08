using System;
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
    internal static class Tests
    {
        private static int passed;
        private static void Check(bool value,string name)
        {
            if(!value) throw new Exception("FAIL: "+name);
            passed++; Console.WriteLine("PASS: "+name);
        }
        public static void Run()
        {
            string directory=Path.Combine(Path.GetTempPath(),"myday-tests-"+Guid.NewGuid().ToString("N"));
            try
            {
                var store=new DiaryStore(directory); var book=store.Load(); Check(book.Days.Count==0,"Missing file starts an empty book");
                var entry=DiaryEntry.FirstPage(); entry.Blocks[0].Text="한글 일기\n따옴표 \" 와 이모지 🌿";
                entry.Blocks[1].Checked=true; entry.Blocks[2].Wide=true; entry.Theme=2; entry.Mood="속상해요";
                book.Days["2026-10-08"]=entry; store.Save(book);
                var loaded=store.Load().Days["2026-10-08"];
                Check(loaded.Blocks[0].Text==entry.Blocks[0].Text && loaded.Blocks[1].Checked && loaded.Blocks[2].Wide && loaded.Theme==2 && loaded.Mood=="속상해요","Round-trip Korean, text, check, layout and theme");
                book.Days["2026-10-09"]=DiaryEntry.Empty(); store.Save(book);
                Check(store.Load().Days["2026-10-08"].Blocks.Count==4 && store.Load().Days["2026-10-09"].Blocks.Count==0,"Dates remain independent");
                using(var stream=File.OpenRead(store.FilePath+".bak")) Check(DiaryStore.Read(stream).Days.Count==1,"Atomic replacement retains previous valid backup");
                var before=File.ReadAllBytes(store.FilePath); entry.Blocks[0].Kind="invalid";
                bool rejected=false; try { store.Save(book); } catch(InvalidDataException) { rejected=true; }
                Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Invalid save leaves stored diary intact"); entry.Blocks[0].Kind="text";
                entry.Blocks[1].Id=entry.Blocks[0].Id; rejected=false; try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Duplicate block IDs rejected"); entry.Blocks[1].Id=Guid.NewGuid().ToString("N");
                book.Days["bad-date"]=DiaryEntry.Empty(); rejected=false; try { DiaryStore.Validate(book); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Invalid import date rejected"); book.Days.Remove("bad-date");
                var export=Path.Combine(directory,"export.json"); store.Export(export,book);
                using(var stream=File.OpenRead(export)) Check(DiaryStore.Read(stream).Days.Count==2,"Backup export is readable");
                File.WriteAllText(store.FilePath,"broken JSON",Encoding.UTF8); rejected=false; try { store.Load(); } catch(Exception) { rejected=true; }
                Check(rejected && File.ReadAllText(store.FilePath)=="broken JSON","Damaged diary is not silently replaced");
                var held=MonsterPose.At(.2,true,true,true); Check(held.Held && !held.Happy && !held.Closed && held.Toe==0,"Dragging takes priority over fire and walking");
                var pose=MonsterPose.At(.3,true,false,false); var loop=MonsterPose.At(17.1,true,false,false);
                Check(Math.Abs(pose.Pulse-loop.Pulse)<.001 && Math.Abs(pose.Toe-loop.Toe)<.001 && Math.Abs(pose.BodyScale-loop.BodyScale)<.001,"Animation repeats after common 16.8-second period");
                using(var a=new Bitmap(230,190)) using(var b=new Bitmap(230,190))
                {
                    using(var g=Graphics.FromImage(a)) MonsterPainter.Draw(g,new Rectangle(0,0,230,190),MonsterPose.At(.15,false,false,false),true);
                    using(var g=Graphics.FromImage(b)) MonsterPainter.Draw(g,new Rectangle(0,0,230,190),MonsterPose.At(.15,false,false,true),true);
                    int different=0,opaque=0;
                    for(int y=0;y<a.Height;y++) for(int x=0;x<a.Width;x++) { if(a.GetPixel(x,y)!=b.GetPixel(x,y)) different++; if(a.GetPixel(x,y).A>0) opaque++; }
                    Check(a.GetPixel(0,0).A==0 && opaque>1000 && different>500,"Monster keeps transparent margins and distinct fire reaction");
                }
                var gesture=new PetGesture(); gesture.Press(new Point(100,100));
                Check(gesture.Release(new Point(102,100)) && !gesture.Active,"Small pointer movement opens diary as a click");
                gesture.Press(new Point(100,100)); gesture.Move(new Point(125,120)); gesture.Move(new Point(100,100));
                Check(!gesture.Release(new Point(100,100)),"Dragging back to the start does not open diary");
                gesture.Press(new Point(100,100)); gesture.Cancel();
                Check(!gesture.Release(new Point(100,100)),"Cancelled capture does not open diary");
                Console.WriteLine("Passed "+passed+" tests.");
            }
            finally
            {
                string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                string resolved=Path.GetFullPath(directory);
                if(resolved.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved).StartsWith("myday-tests-",StringComparison.Ordinal) && Directory.Exists(resolved)) Directory.Delete(resolved,true);
            }
        }
        public static void Smoke(string directory)
        {
            Directory.CreateDirectory(directory);
            var store=new DiaryStore(Path.Combine(directory,"isolated-data"));
            using(var form=new DiaryWindow(store,new DiaryBook(),true))
            {
                form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-30000,-30000);
                form.StartOnDesktop(); if(form.Visible) throw new Exception("Diary opened at desktop startup");
                form.OpenDiary(); Application.DoEvents();
                form.SmokeTest(Path.Combine(directory,"windows-example.png")); form.Close();
                if(form.Visible || form.IsDisposed) throw new Exception("Close should hide and keep the session alive");
                form.OpenDiary(); if(!form.Visible) throw new Exception("Diary did not reopen");
                form.ExitApp(); if(!form.IsDisposed) throw new Exception("Explicit exit did not dispose diary");
            }
            using(var unopened=new DiaryWindow(store,store.Load(),true))
            {
                bool closed=false; unopened.FormClosed+=delegate { closed=true; };
                unopened.StartOnDesktop(); unopened.ExitApp();
                if(!closed || !unopened.IsDisposed) throw new Exception("Exit before first diary open did not close the session");
            }
            using(var pet=new DesktopPet(delegate {}))
            using(var frame=pet.MakeFrame(.2,true))
            {
                // Exercises the actual layered-window native API without showing a desktop pet.
                LayeredWindow.Update(pet.Handle,frame,new Point(-30000,-30000));
                if(frame.GetPixel(0,0).A!=0) throw new Exception("Pet transparency failed");
                frame.Save(Path.Combine(directory,"windows-pet.png"),System.Drawing.Imaging.ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(directory,"smoke-result.txt"),"PASS: desktop-only startup; open; close-to-hide; reopen; explicit exit before/after first open; input; check; reorder; date navigation; mood and theme; small-window layout; disk reload; layered window; transparent pet",Encoding.UTF8);
            Console.WriteLine("Native Windows smoke test passed.");
        }
    }
}
