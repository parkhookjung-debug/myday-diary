using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyDay.Windows.Core
{
    public enum SnackRoundState { Ready, Playing, Paused, Finished }
    public enum SnackKind { Apple, Strawberry, Cookie, Rock, Thorn }
    public sealed class SnackDrop
    {
        public SnackKind Kind {get;internal set;}
        public double X {get;internal set;}
        public double Y {get;internal set;}
        public double Speed {get;internal set;}
        public bool Food {get{return Kind<=SnackKind.Cookie;}}
    }
    // Fixed-step, screen-independent rules. No diary, experience, files or networking.
    public sealed class SnackGame
    {
        public const int Width=960, Height=600, PlayerY=490;
        public const double Left=60,Right=900;
        private MiniGameRandom random;
        private readonly List<SnackDrop> drops=new List<SnackDrop>();
        private readonly ReadOnlyCollection<SnackDrop> view;
        private double target=Width/2,spawnIn=.4,remainder;
        public ReadOnlyCollection<SnackDrop> Drops {get{return view;}}
        public SnackRoundState State {get;private set;}
        public int Score {get;private set;}
        public int Eaten {get;private set;}
        public int Lives {get;private set;}
        public double Elapsed {get;private set;}
        public double PlayerX {get;private set;}
        public double Invulnerable {get;private set;}
        public double Bite {get;private set;}
        public double Hit {get;private set;}
        public double SecondsLeft {get{return Math.Max(0,SnackRules.RoundSeconds-Elapsed);}}
        public SnackGame(int seed){random=new MiniGameRandom(seed);view=drops.AsReadOnly();PlayerX=target=Width/2;Lives=SnackRules.StartingLives;}
        public int LastGain {get;private set;}
        public int Combo {get;private set;}
        public int MaxCombo {get;private set;}
        public void Start(int? seed=null){if(seed.HasValue)random=new MiniGameRandom(seed.Value);drops.Clear();Score=Eaten=Combo=MaxCombo=LastGain=0;Lives=SnackRules.StartingLives;Elapsed=Invulnerable=Bite=Hit=remainder=0;spawnIn=.4;PlayerX=target=Width/2;State=SnackRoundState.Playing;}
        public void Pause(){if(State==SnackRoundState.Playing)State=SnackRoundState.Paused;remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        public void Aim(double x){if(!double.IsNaN(x)&&!double.IsInfinity(x))target=Math.Max(Left,Math.Min(Right,x));}
        public void Step(double seconds,double axis)
        {
            if(State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            if(double.IsNaN(axis)||double.IsInfinity(axis))axis=0;
            axis=Math.Max(-1,Math.Min(1,axis));remainder+=Math.Min(.25,seconds);
            const double h=1.0/120;
            while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Advance(h,axis);}
        }
        private void Advance(double dt,double axis)
        {
            Elapsed=Math.Min(SnackRules.RoundSeconds,Elapsed+dt);
            Invulnerable=Math.Max(0,Invulnerable-dt);Bite=Math.Max(0,Bite-dt);Hit=Math.Max(0,Hit-dt);
            if(axis!=0){PlayerX=Math.Max(Left,Math.Min(Right,PlayerX+axis*SnackRules.PlayerSpeed*dt));target=PlayerX;}
            else {double move=target-PlayerX;PlayerX+=Math.Sign(move)*Math.Min(Math.Abs(move),SnackRules.PlayerSpeed*dt);}
            double difficulty=Elapsed/SnackRules.RoundSeconds;
            spawnIn-=dt;
            if(spawnIn<=0){
                var kind=random.NextDouble()<SnackRules.HazardProbability?(random.Next(2)==0?SnackKind.Rock:SnackKind.Thorn):(SnackKind)random.Next(3);
                drops.Add(new SnackDrop {Kind=kind,X=Left+random.NextDouble()*(Right-Left),Y=96,Speed=160+difficulty*135+random.NextDouble()*50});
                spawnIn+=.68-difficulty*.29;
            }
            for(int i=drops.Count-1;i>=0;i--){
                var drop=drops[i];drop.Y+=drop.Speed*dt;
                double dx=drop.X-PlayerX,dy=drop.Y-PlayerY;
                // A forgiving inner-body hit circle; the snout/eye tips are not hurt boxes.
                if(dx*dx+dy*dy<=46*46){
                    drops.RemoveAt(i);
                    if(drop.Food){Combo++;MaxCombo=Math.Max(MaxCombo,Combo);LastGain=SnackRules.FoodPoints+(Combo%SnackRules.ComboLength==0?SnackRules.ComboBonus:0);Score+=LastGain;Eaten++;Bite=.45;}
                    else if(Invulnerable<=0){Lives--;Combo=0;Invulnerable=SnackRules.InvulnerableSeconds;Hit=.7;if(Lives<=0){State=SnackRoundState.Finished;break;}}
                } else if(drop.Y>Height+30){if(drop.Food)Combo=0;drops.RemoveAt(i);}
            }
            if(Elapsed>=SnackRules.RoundSeconds-1e-8){Elapsed=SnackRules.RoundSeconds;State=SnackRoundState.Finished;}
        }
        internal void AddForTest(SnackKind kind,double x,double y){drops.Add(new SnackDrop{Kind=kind,X=x,Y=y,Speed=180});}
    }
}
