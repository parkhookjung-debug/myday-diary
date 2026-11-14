using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.UI;
namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunMiniGamesTests(string directory)
        {
            var snack=new SnackGame(5);snack.Start();for(int i=0;i<5;i++)snack.AddForTest(SnackKind.Cookie,snack.PlayerX,SnackGame.PlayerY);snack.Step(.01,0);
            Check(snack.Score==60&&snack.Combo==5&&snack.MaxCombo==5,"Five snacks give a visible combo bonus without changing base food points");
            snack.AddForTest(SnackKind.Apple,80,629);snack.Step(.02,0);Check(snack.Combo==0&&snack.MaxCombo==5,"Missing food resets the live combo but preserves the best chain");
            int seed=MiniGameRandom.DailySeed(new DateTime(2026,11,13));var a=new SnackGame(4);var b=new SnackGame(99);a.Start(seed);b.Start(seed);for(int i=0;i<60;i++){a.Step(1.0/30,1);b.Step(1.0/30,-1);}
            Check(a.Drops.Count==b.Drops.Count&&a.Drops.Zip(b.Drops,(u,v)=>u.Kind==v.Kind&&u.X==v.X&&u.Y==v.Y).All(x=>x),"Today's challenge uses the same layout even with different movement input");
            a.Start(seed);b.Start(seed);for(int i=0;i<60;i++)a.Step(1.0/30,0);for(int i=0;i<240;i++)b.Step(1.0/120,0);
            Check(a.Drops.Zip(b.Drops,(u,v)=>Math.Abs(u.Y-v.Y)<.001).All(x=>x)&&a.Drops.Count==b.Drops.Count,"Challenge layout reproduces across frame rates and retries");
            Check(MiniGameRandom.DailySeed(new DateTime(2026,11,14))!=seed,"Another calendar day gets a different challenge seed");
            var sling=new SlingshotGame();sling.Start();
            Check(!sling.BeginAim(900,300)&&sling.Shots==0,"A slingshot must be grabbed on the character rather than anywhere");
            sling.BeginAim(SlingshotGame.OriginX+25,SlingshotGame.OriginY+15);sling.Aim(SlingshotGame.OriginX+25,SlingshotGame.OriginY+15);Check(!sling.Launch()&&sling.Shots==0,"Clicking an off-center part of Sangmon does not fire or spend a shot");
            sling.BeginAim(SlingshotGame.OriginX,SlingshotGame.OriginY);sling.Aim(-1000,5000);
            Check(sling.Pull<=MiniGameRules.SlingMaxPull+.001&&sling.Y<=545,"Slingshot pull has bounded strength and stays in the scene");
            sling.CancelAim();Check(sling.X==SlingshotGame.OriginX&&!sling.Flying&&sling.Shots==0,"Cancelling capture returns the character without spending a shot");
            sling.BeginAim(SlingshotGame.OriginX,SlingshotGame.OriginY);sling.Aim(SlingshotGame.OriginX-100,SlingshotGame.OriginY+60);sling.Launch();double ballX=sling.X;sling.Step(.1);
            Check(sling.Flying&&sling.Shots==1&&sling.X>ballX,"A backward pull launches forward with visible travel");
            sling.Pause();ballX=sling.X;double time=sling.Elapsed;sling.Step(.2);Check(sling.X==ballX&&sling.Elapsed==time,"Pause freezes a flying slingshot");sling.Resume();sling.Step(.02);Check(sling.X!=ballX,"A suspended slingshot continues on resume");
            sling.BallForTest(918,300,500,0);sling.Step(.02);Check(sling.X<920&&sling.Impact>0,"Slingshot projectile bounces at the screen edge");
            sling.BallForTest(sling.TargetX,sling.TargetY,0,0);sling.Step(.01);Check(sling.Goals==1&&sling.Score==150&&!sling.Flying,"A centered ring hit awards points once and prepares the next target");
            sling.Step(.1);Check(sling.Score==150,"An already cleared ring cannot award points on subsequent frames");
            for(int i=1;i<5;i++){sling.BallForTest(sling.TargetX,sling.TargetY,0,0);sling.Step(.01);}Check(sling.State==SnackRoundState.Finished&&sling.Goals==5,"Clearing all five rings ends the slingshot round");
            bool allReachable=true;
            for(int goal=0;goal<5;goal++){
                bool found=false;
                for(int dx=-135;dx<=-35&&!found;dx+=5)for(int dy=10;dy<=95&&!found;dy+=5){
                    var trial=new SlingshotGame();trial.Start();for(int j=0;j<goal;j++){trial.BallForTest(trial.TargetX,trial.TargetY,0,0);trial.Step(.01);}
                    trial.BeginAim(SlingshotGame.OriginX,SlingshotGame.OriginY);trial.Aim(SlingshotGame.OriginX+dx,SlingshotGame.OriginY+dy);trial.Launch();for(int t=0;t<480&&trial.Flying;t++)trial.Step(1.0/120);found=trial.Goals>goal;
                }
                allReachable&=found;
            }
            Check(allReachable,"Every ring has a reachable trajectory using the allowed pull strength");
            sling.Start();for(int shot=0;shot<MiniGameRules.SlingShots;shot++){sling.BeginAim(SlingshotGame.OriginX,SlingshotGame.OriginY);sling.Aim(SlingshotGame.OriginX+80,SlingshotGame.OriginY);sling.Launch();for(int t=0;t<500&&sling.Flying;t++)sling.Step(1.0/120);}
            Check(sling.Shots==MiniGameRules.SlingShots&&sling.State==SnackRoundState.Finished&&!sling.BeginAim(SlingshotGame.OriginX,SlingshotGame.OriginY),"The shot limit cannot be bypassed after the last flight");
            var fish=new FishingGame(7);fish.Start();fish.Action();Check(fish.Phase==FishingPhase.Waiting,"A fishing action casts the line");fish.Action();Check(fish.Phase==FishingPhase.Idle&&fish.Score==0,"Pulling before a bite gives no score and permits another cast");
            fish.Action();for(int i=0;i<400&&fish.Phase==FishingPhase.Waiting;i++)fish.Step(.01);Check(fish.Phase==FishingPhase.Bite,"Waiting eventually gives a visible bite");
            fish.Pause();time=fish.Elapsed;fish.Step(.2);Check(fish.Phase==FishingPhase.Bite&&fish.Elapsed==time,"Fishing pause preserves the bite window");fish.Resume();fish.Action();Check(fish.Phase==FishingPhase.Reeling,"Pressing during a bite starts the timing gauge");
            fish.ReelForTest(.5,0);fish.Action();Check(fish.Catches==1&&fish.Perfects==1&&fish.Score==50&&fish.Phase==FishingPhase.Caught,"A centered timing input catches a fish with a perfect bonus");
            int points=fish.Score;fish.Action();Check(fish.Score==points&&fish.Phase==FishingPhase.Waiting,"Tapping a caught fish starts another cast rather than duplicating the reward");
            fish.ReelForTest(.75,0);fish.Action();Check(fish.Phase==FishingPhase.Idle&&fish.Score==points,"A timing miss is recoverable and does not grant fish points");
            fish.Start();for(int i=0;i<7200&&fish.State==SnackRoundState.Playing;i++)fish.Step(1.0/120);Check(fish.State==SnackRoundState.Finished&&Math.Abs(fish.Elapsed-60)<.001,"Fishing ends after its active one-minute round");fish.Start();Check(fish.Score==0&&fish.Catches==0&&fish.Phase==FishingPhase.Idle,"Fishing restart clears the preceding round");
            string path=Path.Combine(directory,"game-records","scores.json");var records=new GameRecords(path);Check(records.Record("sling",150)&&new GameRecords(path).Best("sling")==150,"A game best survives reloading from its own data file");
            var other=new GameRecords(path);records.Record("sling",300);other.Record("fish",50);other.Record("sling",100);var loaded=new GameRecords(path);
            Check(loaded.Best("sling")==300&&loaded.Best("fish")==50&&loaded.Plays("sling")==3,"Concurrent record objects preserve higher scores and other games through locked reloads");
            byte[] before=File.ReadAllBytes(path);using(var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){Check(!loaded.Record("snack",90),"A locked game file reports a save failure");}Check(before.SequenceEqual(File.ReadAllBytes(path)),"A failed game save leaves the original file intact");
            string daily=GameRecords.DailyId(new DateTime(2026,11,13));records.Record(daily,200);Check(new GameRecords(path).Best(daily)==200&&new GameRecords(path).Best("snack")==0,"Daily challenge records are kept separately from ordinary snack runs");
            for(int i=0;i<35;i++)records.Record(GameRecords.DailyId(new DateTime(2026,1,1).AddDays(i)),i);Check(new FileInfo(path).Length<65536,"Old daily score rows are bounded rather than growing indefinitely");
            var journal=new DiaryStore(Path.Combine(directory,"game-isolated-journal"));journal.Save(new DiaryBook());byte[] journalBefore=File.ReadAllBytes(journal.FilePath);records.Record("fish",80);Check(journalBefore.SequenceEqual(File.ReadAllBytes(journal.FilePath)),"Saving game scores does not alter the diary or its experience data");
            string broken=Path.Combine(directory,"broken-game-records.json");File.WriteAllText(broken,"not json");var damaged=new GameRecords(broken);Check(damaged.Problem!=null&&!damaged.Record("snack",100)&&File.ReadAllText(broken)=="not json","Unreadable game records are preserved instead of being reset and overwritten");
            File.WriteAllText(broken,"{\"Version\":2,\"Entries\":[]}");var future=new GameRecords(broken);before=File.ReadAllBytes(broken);Check(!future.Record("fish",30)&&before.SequenceEqual(File.ReadAllBytes(broken)),"A future game record version is preserved without a downgrade");
        }
        public static void MiniGamesPreview(string directory)
        {
            Directory.CreateDirectory(directory);var records=new GameRecords();
            using(var hub=new GameHubWindow(records)){hub.Show();Application.DoEvents();using(var image=new Bitmap(hub.Width,hub.Height)){hub.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-playroom.png"));}hub.Close();}
            using(var snack=new SnackGameWindow(records,true,new DateTime(2026,11,13))){snack.StartRound();for(int i=0;i<5;i++)snack.Game.AddForTest(SnackKind.Cookie,snack.Game.PlayerX,SnackGame.PlayerY);snack.AdvanceGame(.01);snack.Game.AddForTest(SnackKind.Strawberry,180,240);snack.Game.AddForTest(SnackKind.Rock,710,290);snack.SavePreview(Path.Combine(directory,"windows-snack-challenge.png"));}
            using(var sling=new ArcadeGameWindow(ArcadeKind.Slingshot,records)){
                sling.Show();Application.DoEvents();sling.StartRound();var key=sling.NativeKeyDownForTest(Keys.Space);sling.NativeKeyDownForTest(Keys.Space);if(!key.Handled||key.SuppressKeyPress||sling.Sling.State!=SnackRoundState.Paused)throw new Exception("Native slingshot pause repeat/key-up routing failed");sling.NativeKeyUpForTest(Keys.Space);sling.NativeKeyDownForTest(Keys.Space);sling.NativeKeyUpForTest(Keys.Space);
                var tap=new PointF((float)SlingshotGame.OriginX+25,465);sling.BeginPointer(tap);sling.EndPointer(tap);if(sling.Sling.Shots!=0)throw new Exception("Native character tap launches without a drag");
                sling.BeginPointer(new PointF((float)SlingshotGame.OriginX,450));sling.MovePointer(new PointF((float)SlingshotGame.OriginX-105,515));sling.SavePreview(Path.Combine(directory,"windows-slingshot.png"));sling.PauseRound();sling.EndPointer(new PointF(50,540));if(sling.Sling.Shots!=0||sling.Sling.Flying)throw new Exception("Cancelling a slingshot drag launches a phantom shot");sling.TogglePause();
                for(int i=0;i<5;i++){sling.Sling.BallForTest(sling.Sling.TargetX,sling.Sling.TargetY,0,0);sling.Advance(.01);}sling.Advance(.1);if(records.Best("sling")!=750||records.Plays("sling")!=1)throw new Exception("Native slingshot records a result more than once");sling.SavePreview(Path.Combine(directory,"windows-slingshot-result.png"));sling.Close();
            }
            using(var fish=new ArcadeGameWindow(ArcadeKind.Fishing,records)){
                fish.Show();Application.DoEvents();fish.StartRound();fish.NativeKeyDownForTest(Keys.Space);fish.NativeKeyDownForTest(Keys.Space);if(fish.Fish.Phase!=FishingPhase.Waiting)throw new Exception("Holding fishing action casts twice");fish.NativeKeyUpForTest(Keys.Space);for(int i=0;i<400&&fish.Fish.Phase==FishingPhase.Waiting;i++)fish.Advance(.01);fish.SavePreview(Path.Combine(directory,"windows-fishing-bite.png"));fish.BeginPointer(new PointF(550,360));if(fish.Fish.Phase!=FishingPhase.Reeling)throw new Exception("Fishing mouse input does not start the reel gauge");fish.Fish.ReelForTest(.5,0);fish.SavePreview(Path.Combine(directory,"windows-fishing.png"));fish.BeginPointer(new PointF(550,360));if(fish.Fish.Catches!=1)throw new Exception("Fishing mouse timing failed");fish.SavePreview(Path.Combine(directory,"windows-fishing-catch.png"));fish.Close();
            }
            MiniGameArt.WriteResult(Path.Combine(directory,"windows-game-card.png"),new MiniGameResult{Name="오늘의 간식 도전",Score=360,Day=new DateTime(2026,11,13),Details="간식 30개 · 최고 12콤보",Challenge="2026.11.13 · 동일 배치 v1"});
            Console.WriteLine("Native playroom: three games, challenge, pointer/key routing, capture cancellation, timing, single score save and result card passed.");
        }
    }
}
