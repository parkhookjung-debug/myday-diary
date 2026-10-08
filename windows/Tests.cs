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
                var life=new PetBehavior(17); var motion=life.Step(.05,true,false,true);
                Check(life.Activity==PetActivity.Hover && motion==PointF.Empty,"Mouse proximity stops travel for a reliable click");
                life.Fire(); life.Step(.05,true,false,true);
                Check(life.Activity==PetActivity.Fire,"Explicit fire remains visible while hovered");
                life.Step(.05,true,true,true);
                Check(life.Activity==PetActivity.Drag && life.Step(.05,true,true,false)==PointF.Empty,"Dragging overrides roaming and fire");
                life.Step(.05,false,false,false);
                Check(life.Activity==PetActivity.Rest && life.Step(.05,false,false,false)==PointF.Empty,"Movement switch pauses travel");
                life.Land(); life.Step(.05,false,false,false);
                Check(life.Activity==PetActivity.Hop,"Drop triggers a landing bounce even when roaming is paused");
                var replay=new PetBehavior(22); var replay2=new PetBehavior(22); bool bounded=true, same=true;
                var activities=new System.Collections.Generic.HashSet<PetActivity>();
                for(int i=0;i<3600;i++) {
                    var a=replay.Step(.033,true,false,false); var b=replay2.Step(.033,true,false,false);
                    activities.Add(replay.Activity); same&=a==b;
                    bounded&=Math.Abs(replay.VelocityX)<=52 && Math.Abs(replay.VelocityY)<=25;
                }
                Check(bounded && same && activities.Contains(PetActivity.Look) && (activities.Contains(PetActivity.Sleep) || activities.Contains(PetActivity.Yawn)) && activities.Contains(PetActivity.Walk),"Seeded behavior varies activity and keeps velocity bounded");
                var bounce=new PetBehavior(1); bounce.Step(.1,true,false,false); float speed=bounce.VelocityX; bounce.Bounce(true,false);
                Check(bounce.VelocityX==-speed && !bounce.FacingLeft,"Wall reaction reverses motion and facing");
                var jump=MonsterPose.ForActivity(.325,PetActivity.Hop,.325,0,0);
                var ground=MonsterPose.ForActivity(0,PetActivity.Hop,0,0,0);
                Check(jump.Bob<ground.Bob-8 && jump.WidthScale>ground.WidthScale && !jump.Closed,"Jump has lift and stretch distinct from landing");
                Check(MonsterPose.ForActivity(4,PetActivity.Sleep,1,0,0).Closed && MonsterPose.ForActivity(4,PetActivity.Drag,1,0,0).EyeScale>1,"Sleeping and held expressions stay distinct");
                bool fits=true;
                foreach(var activity in new[] { PetActivity.Walk,PetActivity.Hop,PetActivity.Look,PetActivity.Fire,PetActivity.Drag,PetActivity.Sleep,PetActivity.Yawn })
                for(int i=0;i<24;i++) using(var image=new Bitmap(240,220)) {
                    using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(i*.075,activity,i*.075,1,-1),true);
                    for(int x=0;x<image.Width;x++) fits&=image.GetPixel(x,0).A==0 && image.GetPixel(x,image.Height-1).A==0;
                    for(int y=0;y<image.Height;y++) fits&=image.GetPixel(0,y).A==0 && image.GetPixel(image.Width-1,y).A==0;
                }
                Check(fits,"Animated poses remain inside the transparent desktop window");
                bool quiet=true;
                foreach(var activity in new[] { PetActivity.Rest,PetActivity.Walk,PetActivity.Hop,PetActivity.Look,PetActivity.Sleep,PetActivity.Hover,PetActivity.Drag })
                    for(int i=0;i<30;i++) quiet&=MonsterPose.ForActivity(i*.1,activity,i*.1,0,0).FireStrength==0;
                Check(quiet,"Ordinary movement, hover and rest never emit fire");
                Check(MonsterPose.ForActivity(.2,PetActivity.Fire,.2,0,0).FireStrength>0 && MonsterPose.ForActivity(.71,PetActivity.Fire,.71,0,0).FireStrength==0,"Touch fire ends within 0.7 seconds");
                var yawn=MonsterPose.ForActivity(.8,PetActivity.Yawn,.8,0,0);
                Check(yawn.Closed && yawn.YawnStretch>.8 && yawn.FireStrength>0 && yawn.FireStrength<=.35 && MonsterPose.ForActivity(1.1,PetActivity.Yawn,1.1,0,0).FireStrength==0,"Yawn closes eyes, stretches and emits only a small brief puff");
                var calm=new PetBehavior(22); double previousYawn=-100,time=0; bool rare=true, everYawned=false;
                for(int i=0;i<20000;i++) {
                    var previous=calm.Activity; calm.Step(.05,true,false,false); time+=.05;
                    rare&=calm.Activity!=PetActivity.Fire;
                    if(calm.Activity==PetActivity.Yawn && previous!=PetActivity.Yawn) { rare&=time-previousYawn>=29.9; previousYawn=time; everYawned=true; }
                }
                Check(rare && everYawned,"Autonomous yawns have cooldown and never trigger touch fire");
                using(var image=new Bitmap(240,220)) {
                    using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(.2,PetActivity.Rest,.2,0,0),true);
                    int warm=0;
                    for(int y=0;y<image.Height;y++) for(int x=0;x<image.Width;x++) { var p=image.GetPixel(x,y); if(p.A>0 && p.R>240 && p.G>170 && p.G<235 && p.B<180) warm++; }
                    Check(warm==0,"Idle renderer has no flame-colored pixels");
                }
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
        public static void Preview(string directory)
        {
            Directory.CreateDirectory(directory);
            var activities=new[] { PetActivity.Rest,PetActivity.Fire,PetActivity.Yawn,PetActivity.Walk };
            var labels=new[] { "평소에는 불꽃 없이","건드리면 잠깐만","가끔 하품할 때 조금","다시 걸어다니기" };
            using(var font=new Font("맑은 고딕",15,FontStyle.Bold,GraphicsUnit.Pixel))
            using(var text=new SolidBrush(Color.FromArgb(70,85,65)))
            for(int frame=0;frame<80;frame++) {
                double time=frame*.05;
                using(var image=new Bitmap(720,580)) {
                    using(var g=Graphics.FromImage(image)) {
                        g.Clear(Color.FromArgb(243,246,238));
                        for(int i=0;i<activities.Length;i++) {
                            int x=i%2*360,y=i/2*290;
                            using(var panel=new SolidBrush(Color.White))
                            using(var rounded=Design.Rounded(new RectangleF(x+10,y+10,340,270),18)) g.FillPath(panel,rounded);
                            double age=activities[i]==PetActivity.Yawn?time-1:time;
                            var activity=activities[i];
                            if(activity==PetActivity.Fire && age>=PetBehavior.TouchFireSeconds || activity==PetActivity.Yawn && (age<0 || age>=PetBehavior.YawnSeconds)) { activity=PetActivity.Rest; age=time; }
                            MonsterPainter.Draw(g,new Rectangle(x+50,y+62,260,180),MonsterPose.ForActivity(time,activity,age,0,0),true);
                            g.DrawString(labels[i],font,text,x+98,y+255);
                        }
                    }
                    image.Save(Path.Combine(directory,frame.ToString("D3")+".png"),System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            Console.WriteLine("Rendered 80 native animation frames.");
        }
    }
}
