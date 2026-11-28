using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace MyDay.Windows.Core
{
    public sealed class JumpItem
    {
        public double X {get;internal set;}
        public double Width {get;internal set;}
        public double Height {get;internal set;}
        public bool Food {get;internal set;}
        internal bool Resolved;
    }
    public sealed class JumpGame
    {
        public const double PlayerX=180,Ground=525;
        private readonly MiniGameRandom random;
        private readonly List<JumpItem> items=new List<JumpItem>();
        private readonly ReadOnlyCollection<JumpItem> view;
        private double velocity,remainder,spawnIn;
        public ReadOnlyCollection<JumpItem> Items {get{return view;}}
        public SnackRoundState State {get;private set;}
        public double Height {get;private set;}
        public double Elapsed {get;private set;}
        public double Landing {get;private set;}
        public double Invulnerable {get;private set;}
        public double Bite {get;private set;}
        public int Score {get;private set;}
        public int Lives {get;private set;}
        public int Eaten {get;private set;}
        public int Cleared {get;private set;}
        public bool Grounded {get{return Height<=.001&&velocity<=0;}}
        public double SecondsLeft {get{return Math.Max(0,MiniGameRules.JumpSeconds-Elapsed);}}
        public JumpGame(int seed){random=new MiniGameRandom(seed);view=items.AsReadOnly();Lives=MiniGameRules.JumpLives;}
        public void Start(){items.Clear();Height=velocity=Elapsed=Landing=Invulnerable=Bite=remainder=0;Score=Eaten=Cleared=0;Lives=MiniGameRules.JumpLives;spawnIn=.9;State=SnackRoundState.Playing;}
        public bool Jump(){if(State!=SnackRoundState.Playing||!Grounded)return false;velocity=MiniGameRules.JumpLaunch;return true;}
        public void Pause(){if(State==SnackRoundState.Playing)State=SnackRoundState.Paused;remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        public void Step(double seconds){if(State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;remainder+=Math.Min(.25,seconds);const double h=1.0/120;while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Advance(h);}}
        private void Advance(double h){
            Elapsed=Math.Min(MiniGameRules.JumpSeconds,Elapsed+h);Landing=Math.Max(0,Landing-h);Invulnerable=Math.Max(0,Invulnerable-h);Bite=Math.Max(0,Bite-h);
            if(!Grounded){velocity-=MiniGameRules.JumpGravity*h;Height+=velocity*h;if(Height<=0){Height=velocity=0;Landing=.24;}}
            double speed=MiniGameRules.JumpSpeed+130*Elapsed/MiniGameRules.JumpSeconds;spawnIn-=h;
            if(spawnIn<=0){items.Add(new JumpItem{X=1010,Width=34+random.Next(15),Height=40+random.Next(19)});items.Add(new JumpItem{X=1145,Width=32,Height=105+random.Next(26),Food=true});spawnIn+=1.85+random.NextDouble()*.5;}
            for(int i=items.Count-1;i>=0;i--){
                var item=items[i];item.X-=speed*h;
                if(!item.Resolved){
                    if(item.Food){double px=Math.Max(PlayerX-26,Math.Min(PlayerX+26,item.X));double foodY=Ground-item.Height;double py=Math.Max(Ground-Height-70,Math.Min(Ground-Height,foodY));double dx=item.X-px,dy=foodY-py;if(dx*dx+dy*dy<=18*18){item.Resolved=true;Eaten++;Score+=10;Bite=.4;}}
                    else if(item.X<PlayerX+26&&item.X+item.Width>PlayerX-26&&Height<item.Height){item.Resolved=true;if(Invulnerable<=0){Lives--;Invulnerable=1.2;if(Lives<=0){State=SnackRoundState.Finished;break;}}}
                    else if(item.X+item.Width<PlayerX-26){item.Resolved=true;Cleared++;Score+=20;}
                }
                if(item.X+item.Width<-40||item.Food&&item.Resolved)items.RemoveAt(i);
            }
            if(Elapsed>=MiniGameRules.JumpSeconds-1e-8){Elapsed=MiniGameRules.JumpSeconds;State=SnackRoundState.Finished;}
        }
        internal void AddForTest(double x,double height,bool food=false){items.Add(new JumpItem{X=x,Width=38,Height=height,Food=food});}
        internal void ClearForTest(){items.Clear();}
    }
}
