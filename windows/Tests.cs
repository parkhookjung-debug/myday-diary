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
    internal static partial class Tests
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
                book.CharacterStyle="winged"; store.Save(book);
                Check(store.Load().CharacterStyle=="winged" && store.Load().Days["2026-10-08"].Blocks[0].Text==entry.Blocks[0].Text,"Selected variant survives reload without changing diary text");
                using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"Version\":1,\"Days\":[]}"))) Check(DiaryStore.Read(stream).CharacterStyle=="original","Older diary without variant field loads with original character");
                using(var stream=new MemoryStream(Encoding.UTF8.GetBytes("{\"Version\":1,\"Days\":[{\"Key\":\"2026-10-08\",\"Value\":{\"Theme\":0,\"Mood\":\"평온해요\",\"Blocks\":[{\"Id\":\"old\",\"Kind\":\"text\",\"Text\":\"기존 글\",\"Checked\":false,\"Wide\":false}]}}]}"))) {
                    var legacy=DiaryStore.Read(stream).Days["2026-10-08"];
                    Check(legacy.LayoutMode=="cards" && legacy.PageStyle=="plain" && legacy.Blocks[0].Title==null && legacy.Blocks[0].Width==0 && legacy.Blocks[0].Text=="기존 글","Older diary loads without template or placement fields or losing text");
                }
                RunPhotoTests(directory);
                RunBrowseTests(directory);
                var layout=DiaryEntry.FirstPage(); layout.Blocks[0].Text="이동해도 그대로"; layout.Blocks[1].Checked=true;
                var originalIds=layout.Blocks.Select(b=>b.Id).ToArray(); DiaryLayout.EnableFree(layout,800);
                Check(layout.LayoutMode=="free" && layout.Blocks.Select(b=>b.Id).SequenceEqual(originalIds) && layout.Blocks[0].Text=="이동해도 그대로" && layout.Blocks[1].Checked,"Entering free placement retains block IDs, text and checks");
                DiaryLayout.SetBounds(layout.Blocks[0],new Rectangle(73,145,410,330)); layout.LayoutMode="cards"; DiaryLayout.EnableFree(layout,450);
                Check(DiaryLayout.Bounds(layout.Blocks[0])==new Rectangle(73,145,410,330),"Returning from automatic layout restores prior free placement");
                var newBlock=DiaryBlock.Create("habit"); layout.Blocks.Add(newBlock); DiaryLayout.PlaceNew(layout,newBlock);
                Check(newBlock.Y>=layout.Blocks.Take(4).Max(b=>b.Y+b.Height)+16,"New blocks appear below placed blocks");
                Check(DiaryLayout.Drag(new Rectangle(15,20,400,300),new Point(-1000,-1000),false)==new Rectangle(0,0,400,300) &&
                    DiaryLayout.Drag(new Rectangle(15,20,400,300),new Point(-1000,-1000),true)==new Rectangle(15,20,DiaryLayout.MinWidth,DiaryLayout.MinHeight),"Movement and resizing retain safe positions and usable minimum sizes");
                var layoutBook=new DiaryBook(); layoutBook.Days["2026-10-08"]=layout; layoutBook.Days["2026-10-09"]=DiaryEntry.FirstPage();
                var layoutStore=new DiaryStore(Path.Combine(directory,"layouts")); layoutStore.Save(layoutBook);
                var restored=layoutStore.Load();
                Check(restored.Days["2026-10-08"].LayoutMode=="free" && DiaryLayout.Bounds(restored.Days["2026-10-08"].Blocks[0])==new Rectangle(73,145,410,330) && restored.Days["2026-10-09"].LayoutMode=="cards","Placement and size survive saving independently for each date");
                var safeLayout=File.ReadAllBytes(layoutStore.FilePath); layout.Blocks[0].Width=-1; rejected=false;
                try { layoutStore.Save(layoutBook); } catch(InvalidDataException) { rejected=true; }
                Check(rejected && safeLayout.SequenceEqual(File.ReadAllBytes(layoutStore.FilePath)),"Invalid placement does not overwrite saved diary");
                var journal=DiaryEntry.FirstPage(); journal.Blocks[0].Text="내가 이미 쓴 글"; journal.Blocks[1].Checked=true; journal.Mood="설레요";
                DiaryLayout.EnableFree(journal,800); var retained=journal.Blocks.Select(b=>b.Id).ToArray(); var position=DiaryLayout.Bounds(journal.Blocks[0]);
                DiaryTemplates.Append(journal,DiaryTemplates.All[1]);
                Check(journal.Blocks.Take(4).Select(b=>b.Id).SequenceEqual(retained) && journal.Blocks[0].Text=="내가 이미 쓴 글" && journal.Blocks[1].Checked && journal.Mood=="설레요" && DiaryLayout.Bounds(journal.Blocks[0])==position,"Template append preserves existing content, checks, mood and free placement");
                Check(journal.Blocks.Skip(4).All(b=>b.Y>=position.Bottom+16 && b.Width>=DiaryLayout.MinWidth),"Template sections append below existing free canvas");
                var templateBook=new DiaryBook(); bool templatesSaved=true;
                for(int i=0;i<DiaryTemplates.All.Length;i++) {
                    var templateEntry=DiaryEntry.Empty(); templateEntry.PageStyle=DiaryTemplates.All[i].Style;
                    DiaryTemplates.Append(templateEntry,DiaryTemplates.All[i]); templateBook.Days[DiaryStore.Key(new DateTime(2026,9,1).AddDays(i))]=templateEntry;
                }
                var templateStore=new DiaryStore(Path.Combine(directory,"templates")); templateStore.Save(templateBook); var templateReload=templateStore.Load();
                foreach(var pair in templateBook.Days) templatesSaved&=pair.Value.PageStyle==templateReload.Days[pair.Key].PageStyle && pair.Value.Blocks.Select(b=>b.Title+"|"+b.Prompt+"|"+b.Text).SequenceEqual(templateReload.Days[pair.Key].Blocks.Select(b=>b.Title+"|"+b.Prompt+"|"+b.Text));
                Check(templatesSaved,"All one hundred journal formats preserve titles, prompts and paper styles through storage");
                Check(DiaryTemplates.All.Length==100 && DiaryTemplates.All.Select(t=>t.Id).Distinct().Count()==100 && DiaryTemplates.All.Select(t=>t.Name).Distinct().Count()==100,"Exactly 100 distinct format IDs and names");
                Check(TemplateCatalog.Categories.All(c=>DiaryTemplates.All.Count(t=>t.Category==c)==10),"Each of ten categories has ten formats");
                Check(DiaryTemplates.All.Take(6).Select(t=>t.Id).SequenceEqual(new[] {"free","reflection","gratitude","questions","bullet","letter"}),"Original six formats retain their identity and order");
                Check(DiaryTemplates.All.All(t=>TemplateCatalog.ReferenceIds.Contains(t.Reference) && JournalLayouts.Ids.Contains(t.Layout) && t.Minutes>0 && t.Sections.Length>0 && t.Sections.Length<=6),"Every format has a verified source family, usable layout and bounded section count");
                Check(DiaryTemplates.All.Select(t=>string.Join("|",t.Sections.Select(s=>s.Kind+":"+s.Title+":"+s.Prompt))).Distinct().Count()==100,"All format question and block combinations are distinct");
                Check(DiaryTemplates.Find(null," 코넬 ").Any(t=>t.Id=="cornell-notes") && DiaryTemplates.Find("learning","코넬").All(t=>t.Category=="learning") && DiaryTemplates.Find("daily","코넬").Length==0 && DiaryTemplates.Find(null,"missing-test-format").Length==0,"Search trims queries, combines category and metadata, and handles no results");
                bool layoutsFit=true;
                foreach(var t in DiaryTemplates.All) foreach(int width in new[] {500,900}) {
                    var rectangles=JournalLayouts.Arrange(t.Layout,t.Sections.Length,width);
                    layoutsFit&=rectangles.Count==t.Sections.Length;
                    for(int i=0;i<rectangles.Count;i++) {
                        var r=rectangles[i]; layoutsFit&=r.X>=0 && r.Y>=0 && r.Width>=DiaryLayout.MinWidth && r.Height>=DiaryLayout.MinHeight && r.Height<=DiaryLayout.MaxHeight && r.Right<=width-24;
                        for(int j=0;j<i;j++) layoutsFit&=!r.IntersectsWith(rectangles[j]);
                    }
                }
                Check(layoutsFit,"Every format layout fits wide and narrow canvases without overlapping blocks");
                var catalogPath=Path.Combine(directory,"catalog.md"); TemplateCatalog.WriteCatalog(catalogPath);
                Check(File.ReadAllLines(catalogPath).Count(line=>line.StartsWith("| ") && char.IsDigit(line[2]))==100,"Documentation exports all hundred catalog entries");
                var full=DiaryEntry.Empty(); for(int i=0;i<199;i++) full.Blocks.Add(DiaryBlock.Create("text"));
                Check(!DiaryTemplates.Append(full,DiaryTemplates.All[1]) && full.Blocks.Count==199,"Over-limit template inserts no partial sections");
                var blank=DiaryTemplates.NewPage(); Check(blank.Blocks.Count==1 && blank.Blocks[0].Wide && blank.Blocks[0].Text=="" && blank.PageStyle=="paper","New diary starts with a spacious empty writing page");
                var invalidBook=new DiaryBook(); var invalidEntry=DiaryTemplates.NewPage(); invalidBook.Days["2026-10-08"]=invalidEntry;
                invalidEntry.Blocks[0].Title=new string('x',81); rejected=false; try { DiaryStore.Validate(invalidBook); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"Oversized custom block title is rejected before saving");
                invalidEntry.Blocks[0].Title="제목"; invalidEntry.PageStyle="unknown"; DiaryStore.Validate(invalidBook);
                Check(invalidEntry.PageStyle=="plain" && invalidEntry.Blocks[0].Title=="제목","Unknown paper style falls back without losing custom title");
                book.CharacterStyle="future-skin"; DiaryStore.Validate(book);
                Check(book.CharacterStyle=="original" && book.Days.Count==2,"Unknown imported variant falls back without losing dates");
                Check(MonsterVariants.All.Select(MonsterVariants.Id).Distinct().Count()==56 && MonsterVariants.All.All(v=>MonsterVariants.FromId(MonsterVariants.Id(v))==v),"Fifty-six variant IDs are distinct and round-trip");
                Check(MonsterVariants.All.Take(8).Select(MonsterVariants.Id).SequenceEqual(new[] {"original","puffy","winged","speedy","dazed","spiky","horned","mini"}),"Original eight saved IDs retain their values and order");
                var oldForms=new[] {"droplet","puddle","pill","cube","cloud","twin"};
                var corrected=new[] {"fin","shell","tailed","crystal","furry","flower"};
                bool legacyForms=true;
                for(int i=0;i<oldForms.Length;i++) { book.CharacterStyle=oldForms[i]; DiaryStore.Validate(book); legacyForms&=book.CharacterStyle==corrected[i] && book.Days.Count==2; }
                Check(legacyForms,"Earlier slime choices migrate to corrected appearances without losing diary entries");
                bool allSaved=true;
                foreach(var variant in MonsterVariants.All) { book.CharacterStyle=MonsterVariants.Id(variant); store.Save(book); allSaved&=store.Load().CharacterStyle==MonsterVariants.Id(variant); }
                Check(allSaved,"Every character form survives JSON saving and reloading");
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
                bool variantFits=true; string failingVariant="";
                var fingerprints=new System.Collections.Generic.HashSet<string>();
                foreach(var variant in MonsterVariants.All) {
                    using(var image=new Bitmap(240,220)) {
                        using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(.2,PetActivity.Rest,.2,0,0),true,variant);
                        using(var buffer=new MemoryStream()) { image.Save(buffer,System.Drawing.Imaging.ImageFormat.Png); fingerprints.Add(Convert.ToBase64String(buffer.ToArray())); }
                    }
                    foreach(bool left in new[] { true,false })
                    foreach(var activity in new[] { PetActivity.Walk,PetActivity.Hop,PetActivity.Drag,PetActivity.Fire,PetActivity.Yawn })
                    for(int i=0;i<16;i++) using(var image=new Bitmap(240,220)) {
                        using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(i*.11,activity,i*.11,1,-1),left,variant);
                        bool frameFits=true;
                        for(int x=0;x<image.Width;x++) frameFits&=image.GetPixel(x,0).A==0 && image.GetPixel(x,image.Height-1).A==0;
                        for(int y=0;y<image.Height;y++) frameFits&=image.GetPixel(0,y).A==0 && image.GetPixel(image.Width-1,y).A==0;
                        if(!frameFits && failingVariant=="") failingVariant=variant+" "+activity+" "+i;
                        variantFits&=frameFits;
                    }
                }
                Check(fingerprints.Count==MonsterVariants.All.Length,"All character forms render distinct images");
                Check(variantFits,"Variants fit transparent window when facing either direction: "+failingVariant);
                bool identity=true;
                foreach(var variant in MonsterVariants.All.Skip(8)) using(var image=new Bitmap(240,220)) {
                    using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0),true,variant);
                    float scale=208f/555;
                    Func<float,float,Color> sample=(x,y)=>image.GetPixel((int)Math.Round(120+(x-275)*.86f*scale),(int)Math.Round(110+((y-650)*.86f+35)*scale));
                    identity&=sample(246,455).A>120 && sample(305,455).A>120;
                    identity&=sample(246,476).R<100 && sample(305,476).R<100;
                    identity&=sample(350,730).A==0;
                }
                Check(identity,"All 48 appearances retain raised eyes, original pupils and the open arch between the feet");
                bool materials=true;
                var colors=new System.Collections.Generic.HashSet<int>();
                foreach(var variant in MonsterVariants.All.Where(GameSkins.IsGame)) using(var image=new Bitmap(240,220)) {
                    using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(16,22,208,176),MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0),true,variant);
                    var material=image.GetPixel(145,105);
                    materials&=material.A==255 && material.R+material.G+material.B<680;
                    colors.Add(material.ToArgb());
                }
                Check(materials && colors.Count==8,"Eight RPG styles use distinct body materials rather than accessory-only recoloring");
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
            var store=new DiaryStore(Path.Combine(directory,"isolated-data-"+Guid.NewGuid().ToString("N")));
            var fixture=new DiaryBook(); fixture.Days[DiaryStore.Key(DateTime.Today)]=DiaryEntry.FirstPage();
            using(var form=new DiaryWindow(store,fixture,true))
            {
                Console.WriteLine("Smoke: preparing isolated diary window.");
                form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-30000,-30000);
                form.StartOnDesktop(); if(form.Visible) throw new Exception("Diary opened at desktop startup");
                form.OpenDiary(); Console.WriteLine("Smoke: opened diary."); Application.DoEvents();
                form.SmokeTest(Path.Combine(directory,"windows-example.png")); form.Close();
                Console.WriteLine("Smoke: input, selection, reload and rendering passed.");
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
        public static void VariantPreview(string directory,int start=0,int count=56,bool gameCards=false)
        {
            Directory.CreateDirectory(directory);
            var variants=MonsterVariants.All.Skip(start).Take(count).ToArray();
            using(var font=new Font("맑은 고딕",14,FontStyle.Bold,GraphicsUnit.Pixel))
            for(int frame=0;frame<80;frame++) {
                double time=frame*.05;
                using(var image=new Bitmap(960,((variants.Length+3)/4)*290)) {
                    using(var g=Graphics.FromImage(image)) {
                        g.Clear(gameCards?Color.FromArgb(15,22,37):Color.FromArgb(243,246,238));
                        for(int index=0;index<variants.Length;index++) {
                            var variant=variants[index]; int x=index%4*240,y=index/4*290;
                            using(var panel=new SolidBrush(gameCards?Color.FromArgb(29,40,61):Color.White))
                            using(var rounded=Design.Rounded(new RectangleF(x+8,y+8,224,274),16)) g.FillPath(panel,rounded);
                            if(gameCards) using(var pen=new Pen(GameSkins.Tone(variant),3)) g.DrawLine(pen,x+28,y+35,x+212,y+35);
                            var activity=variant==MonsterVariant.Dazed?PetActivity.Rest:variant==MonsterVariant.Spiky?PetActivity.Look:variant==MonsterVariant.Puffy || variant==MonsterVariant.Mini || variant==MonsterVariant.Angel?PetActivity.Hop:PetActivity.Walk;
                            MonsterPainter.Draw(g,new Rectangle(x+16,y+52,208,176),MonsterPose.ForActivity(time,activity,time,0,0),true,variant);
                            TextRenderer.DrawText(g,MonsterVariants.Name(variant),font,new Rectangle(x+8,y+247,224,28),gameCards?Color.FromArgb(231,238,249):Color.FromArgb(70,85,65),
                                TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
                        }
                    }
                    image.Save(Path.Combine(directory,frame.ToString("D3")+".png"),System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            Console.WriteLine("Rendered "+variants.Length+" variants in 80 animation frames.");
        }
    }
}
