using System;
namespace MyDay.Windows.Core
{
    public sealed class SlingshotGame
    {
        public const double OriginX=225,OriginY=450,MinX=85,MaxX=875,MinY=135,MaxY=530;
        private static readonly double[] goalX={610,780,660,805,735},goalY={330,220,420,340,185};
        private double pullX,pullY,grabX,grabY,vx,vy,remainder,flightAge;
        private int bounces;
        public SnackRoundState State {get;private set;}
        public double X {get;private set;}
        public double Y {get;private set;}
        public double PullX {get{return pullX;}}
        public double PullY {get{return pullY;}}
        public double Pull {get{return Math.Sqrt(pullX*pullX+pullY*pullY);}}
        public bool Aiming {get;private set;}
        public bool Flying {get;private set;}
        public int Shots {get;private set;}
        public int Goals {get;private set;}
        public int Score {get;private set;}
        public double Elapsed {get;private set;}
        public double Impact {get;private set;}
        public double Success {get;private set;}
        public double TargetX {get{return goalX[Math.Min(Goals,4)];}}
        public double TargetY {get{return goalY[Math.Min(Goals,4)];}}
        public double SecondsLeft {get{return Math.Max(0,MiniGameRules.SlingSeconds-Elapsed);}}
        public SlingshotGame(){ResetBall();}
        private void ResetBall(){X=OriginX;Y=OriginY;vx=vy=pullX=pullY=grabX=grabY=0;Flying=Aiming=false;flightAge=0;bounces=0;}
        public void Start(){Shots=Goals=Score=0;Elapsed=Impact=Success=remainder=0;ResetBall();State=SnackRoundState.Playing;}
        public bool BeginAim(double x,double y){if(State!=SnackRoundState.Playing||Shots>=MiniGameRules.SlingShots||Flying||!Finite(x)||!Finite(y)||(x-X)*(x-X)+(y-Y)*(y-Y)>80*80)return false;grabX=x-OriginX;grabY=y-OriginY;pullX=pullY=0;Aiming=true;return true;}
        public void Aim(double x,double y){if(!Aiming||!Finite(x)||!Finite(y))return;pullX=x-OriginX-grabX;pullY=Math.Min(95,y-OriginY-grabY);double d=Pull;if(d>MiniGameRules.SlingMaxPull){pullX*=MiniGameRules.SlingMaxPull/d;pullY*=MiniGameRules.SlingMaxPull/d;}X=OriginX+pullX;Y=OriginY+pullY;}
        public bool Launch(){if(!Aiming||State!=SnackRoundState.Playing)return false;Aiming=false;if(Pull<12){ResetBall();return false;}vx=-pullX*MiniGameRules.SlingStrength;vy=-pullY*MiniGameRules.SlingStrength;Flying=true;Shots++;flightAge=0;return true;}
        public void CancelAim(){if(Aiming)ResetBall();}
        public void Pause(){if(State==SnackRoundState.Playing){CancelAim();State=SnackRoundState.Paused;}remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        private static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
        public void Step(double seconds){if(State!=SnackRoundState.Playing||!Finite(seconds)||seconds<=0)return;remainder+=Math.Min(.25,seconds);const double h=1.0/120;while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Advance(h);}}
        private void Advance(double h){
            Elapsed=Math.Min(MiniGameRules.SlingSeconds,Elapsed+h);Impact=Math.Max(0,Impact-h);Success=Math.Max(0,Success-h);
            if(Flying){flightAge+=h;vy+=MiniGameRules.SlingGravity*h;X+=vx*h;Y+=vy*h;
                double dx=X-TargetX,dy=Y-TargetY;
                if(dx*dx+dy*dy<=44*44){Score+=dx*dx+dy*dy<18*18?150:100;Goals++;Success=.8;ResetBall();if(Goals==5||Shots>=MiniGameRules.SlingShots)State=SnackRoundState.Finished;}
                else {
                    if(X<MinX&&vx<0||X>MaxX&&vx>0){X=Math.Max(MinX,Math.Min(MaxX,X));vx=-vx*.65;Impact=.25;bounces++;}
                    if(Y<MinY&&vy<0){Y=MinY;vy=-vy*.65;Impact=.25;bounces++;}
                    if(Y>MaxY&&vy>0){Y=MaxY;vy=-vy*.6;vx*=.8;Impact=.25;bounces++;}
                    if(flightAge>3.8||bounces>=4){ResetBall();if(Shots>=MiniGameRules.SlingShots)State=SnackRoundState.Finished;}
                }
            }
            if(Elapsed>=MiniGameRules.SlingSeconds-1e-8){Elapsed=MiniGameRules.SlingSeconds;State=SnackRoundState.Finished;CancelAim();}
        }
        internal void BallForTest(double x,double y,double horizontal,double vertical){X=x;Y=y;vx=horizontal;vy=vertical;Flying=true;}
    }
}
