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
        private static void RunSnackGameTests()
        {
            var game=new SnackGame(42);game.Step(.1,1);
            Check(game.State==SnackRoundState.Ready&&game.Elapsed==0,"The snack game waits for explicit start");
            game.Start();Check(game.Lives==3&&game.Score==0&&game.SecondsLeft==60,"A snack round begins with three lives and sixty seconds");
            foreach(var kind in new[]{SnackKind.Apple,SnackKind.Strawberry,SnackKind.Cookie})game.AddForTest(kind,game.PlayerX,SnackGame.PlayerY);
            game.Step(.01,0);Check(game.Score==30&&game.Eaten==3&&game.Lives==3&&game.Drops.Count==0,"All three foods award ten points and are consumed exactly once");
            game.Step(.01,0);Check(game.Score==30,"Consumed food cannot award points again on a later frame");
            game.AddForTest(SnackKind.Rock,game.PlayerX,SnackGame.PlayerY);game.AddForTest(SnackKind.Thorn,game.PlayerX,SnackGame.PlayerY);game.Step(.01,0);
            Check(game.Lives==2&&game.Invulnerable>0&&game.Score==30,"Simultaneous hazards cost one life and start a brief protection period");
            game.AddForTest(SnackKind.Apple,game.PlayerX,SnackGame.PlayerY);game.Step(.01,0);Check(game.Score==40&&game.Lives==2,"Food remains collectible during temporary protection");
            double elapsed=game.Elapsed,x=game.PlayerX;int count=game.Drops.Count;game.Pause();game.Step(.2,1);
            Check(game.State==SnackRoundState.Paused&&game.Elapsed==elapsed&&game.PlayerX==x&&game.Drops.Count==count,"Pause freezes time, movement and falling objects");
            game.Resume();game.Step(.01,1);Check(game.Elapsed>elapsed&&game.PlayerX>x,"Resume restores keyboard movement");
            game.Start();game.Aim(-1000);for(int i=0;i<120;i++)game.Step(.01,0);Check(game.PlayerX==SnackGame.Left,"Mouse movement is clamped to the left play boundary");
            game.Aim(10000);for(int i=0;i<150;i++)game.Step(.01,0);Check(game.PlayerX==SnackGame.Right,"Mouse movement is clamped to the right play boundary");
            game.Step(double.NaN,double.NaN);game.Aim(double.NaN);game.Step(.01,double.PositiveInfinity);Check(!double.IsNaN(game.PlayerX)&&!double.IsInfinity(game.PlayerX),"Invalid input cannot corrupt the game state");
            game.Start();game.AddForTest(SnackKind.Cookie,80,590);game.Step(.25,0);Check(game.Lives==3&&game.Score==0,"Missing food does not punish the player");
            var a=new SnackGame(4);var b=new SnackGame(4);a.Start();b.Start();
            for(int i=0;i<60;i++)a.Step(1.0/30,1);for(int i=0;i<240;i++)b.Step(1.0/120,1);
            Check(Math.Abs(a.PlayerX-b.PlayerX)<.01&&Math.Abs(a.Elapsed-b.Elapsed)<.00001&&a.Drops.Count==b.Drops.Count&&a.Drops.Zip(b.Drops,(u,v)=>u.Kind==v.Kind&&Math.Abs(u.Y-v.Y)<.01).All(v=>v),"Fixed-step simulation matches at thirty and one hundred twenty FPS");
            game.Start();
            for(int hit=0;hit<3;hit++){
                if(hit>0)for(int j=0;j<150;j++)game.Step(.01,0);
                game.AddForTest(SnackKind.Rock,game.PlayerX,SnackGame.PlayerY);game.Step(.01,0);
            }
            Check(game.Lives==0&&game.State==SnackRoundState.Finished,"Losing the third life ends the round");elapsed=game.Elapsed;game.Step(.2,1);Check(game.Elapsed==elapsed,"A finished round cannot continue in the background");
            game.Start();Check(game.Lives==3&&game.Score==0&&game.Drops.Count==0&&game.Elapsed==0&&game.Invulnerable==0,"Restart clears score, hazards, protection and elapsed time");
            for(int i=0;i<7200&&game.State==SnackRoundState.Playing;i++){
                // Remove each approaching hazard at an edge via test-only setup to isolate the time limit.
                foreach(var drop in game.Drops)drop.X=SnackGame.Left;
                game.Step(1.0/120,0);
                if(game.Drops.Count>30)throw new Exception("Snack objects grew without a bound");
            }
            Check(game.State==SnackRoundState.Finished&&Math.Abs(game.Elapsed-60)<.001&&game.Lives>0,"Surviving for sixty seconds completes the game with bounded objects");
        }
        public static void SnackGamePreview(string directory)
        {
            Directory.CreateDirectory(directory);
            using(var window=new SnackGameWindow()){
                window.Show();Application.DoEvents();
                using(var frame=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(directory,"windows-game-ready.png"));}
                window.StartRound();var game=window.Game;
                var key=window.NativeKeyDownForTest(Keys.Right);
                if(!key.Handled||key.SuppressKeyPress)throw new Exception("Game key handling suppresses native key release");
                window.AdvanceGame(.05);double movedX=game.PlayerX;window.NativeKeyUpForTest(Keys.Right);window.AdvanceGame(.1);
                if(movedX<=480||Math.Abs(game.PlayerX-movedX)>.01)throw new Exception("Releasing an arrow does not stop movement");
                window.StartRound();
                game.AddForTest(SnackKind.Apple,150,210);game.AddForTest(SnackKind.Cookie,325,340);game.AddForTest(SnackKind.Strawberry,600,260);game.AddForTest(SnackKind.Rock,430,185);game.AddForTest(SnackKind.Thorn,780,380);
                game.AddForTest(SnackKind.Apple,game.PlayerX,SnackGame.PlayerY);game.Step(.01,0);
                window.SavePreview(Path.Combine(directory,"windows-game-playing.png"));
                window.NativeKeyDownForTest(Keys.Space);window.NativeKeyDownForTest(Keys.Space);
                if(game.State!=SnackRoundState.Paused)throw new Exception("Holding Space repeatedly toggles pause");
                window.NativeKeyUpForTest(Keys.Space);window.NativeKeyDownForTest(Keys.Space);window.NativeKeyUpForTest(Keys.Space);
                if(game.State!=SnackRoundState.Playing)throw new Exception("Space does not resume");
                window.PauseRound();double time=game.Elapsed;game.Step(.1,1);if(game.State!=SnackRoundState.Paused||game.Elapsed!=time)throw new Exception("Native game pause failed");
                window.SavePreview(Path.Combine(directory,"windows-game-paused.png"));
                window.TogglePause();if(game.State!=SnackRoundState.Playing)throw new Exception("Native game resume failed");
                window.StartRound();if(game.Score!=0||game.Lives!=3)throw new Exception("Native game restart failed");
                window.ClientSize=Design.Size(680,480);Application.DoEvents();
                using(var frame=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));frame.Save(Path.Combine(directory,"windows-game-compact.png"));}
                window.Close();
            }
            Console.WriteLine("Native snack game: ready, start, foods, pause, resume, restart, compact layout and rendering passed.");
        }
    }
}
