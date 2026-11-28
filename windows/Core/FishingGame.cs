using System;
namespace MyDay.Windows.Core
{
    public enum FishingPhase { Idle,Waiting,Bite,Reeling,Caught }
    public sealed class FishingGame
    {
        private readonly MiniGameRandom random;
        private double phaseTime,wait,remainder;
        public SnackRoundState State {get;private set;}
        public FishingPhase Phase {get;private set;}
        public double Elapsed {get;private set;}
        public int Score {get;private set;}
        public int Catches {get;private set;}
        public int Perfects {get;private set;}
        public string Message {get;private set;}
        public string FishName {get;private set;}
        public double Zone {get;private set;}
        public double Indicator {get{return .5+Math.Sin(phaseTime*3.7)*.47;}}
        public double SecondsLeft {get{return Math.Max(0,MiniGameRules.FishSeconds-Elapsed);}}
        public FishingGame(int seed){random=new MiniGameRandom(seed);Message="Space 또는 물가를 눌러 낚싯줄을 던져요.";}
        public void Start(){Elapsed=phaseTime=remainder=0;Score=Catches=Perfects=0;Phase=FishingPhase.Idle;State=SnackRoundState.Playing;Message="낚싯줄을 던져볼까요?";FishName="";}
        public void Action(){
            if(State!=SnackRoundState.Playing)return;
            if(Phase==FishingPhase.Idle||Phase==FishingPhase.Caught){Phase=FishingPhase.Waiting;phaseTime=0;wait=MiniGameRules.FishWaitMin+random.NextDouble()*(MiniGameRules.FishWaitMax-MiniGameRules.FishWaitMin);Message="찌를 보며 조금만 기다려요…";}
            else if(Phase==FishingPhase.Waiting){Phase=FishingPhase.Idle;phaseTime=0;Message="조금 빨랐어요. 다시 던져보세요!";}
            else if(Phase==FishingPhase.Bite){Phase=FishingPhase.Reeling;phaseTime=0;Zone=.25+random.NextDouble()*.5;Message="움직이는 표시가 초록 칸에 오면 한 번 더!";}
            else if(Phase==FishingPhase.Reeling){
                double accuracy=Math.Abs(Indicator-Zone);
                if(accuracy<=MiniGameRules.FishZoneWidth/2){bool perfect=accuracy<=.035;int points=perfect?50:30;Score+=points;Catches++;if(perfect)Perfects++;FishName=new[]{"통통 붕어","반짝 송사리","작은 잉어"}[random.Next(3)];Message=FishName+"! +"+points+(perfect?" · 완벽해요!":"");Phase=FishingPhase.Caught;}
                else{Phase=FishingPhase.Idle;Message="아깝다! 다시 도전해요.";}
                phaseTime=0;
            }
        }
        public void Pause(){if(State==SnackRoundState.Playing)State=SnackRoundState.Paused;remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        public void Step(double seconds){if(State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;remainder+=Math.Min(.25,seconds);const double h=1.0/120;while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Elapsed=Math.Min(MiniGameRules.FishSeconds,Elapsed+h);phaseTime+=h;
                if(Phase==FishingPhase.Waiting&&phaseTime>=wait){Phase=FishingPhase.Bite;phaseTime=0;Message="입질! 지금 눌러요!";}
                else if(Phase==FishingPhase.Bite&&phaseTime>=MiniGameRules.FishBiteWindow){Phase=FishingPhase.Idle;phaseTime=0;Message="물고기가 지나갔어요. 다시 던져요.";}
                else if(Phase==FishingPhase.Reeling&&phaseTime>=4){Phase=FishingPhase.Idle;phaseTime=0;Message="다시 차분히 도전해요.";}
                else if(Phase==FishingPhase.Caught&&phaseTime>=1.4){Phase=FishingPhase.Idle;phaseTime=0;}
                if(Elapsed>=MiniGameRules.FishSeconds-1e-8){Elapsed=MiniGameRules.FishSeconds;State=SnackRoundState.Finished;Message="오늘의 낚시를 마쳤어요.";}
            }}
        internal void ReelForTest(double zone,double age){Phase=FishingPhase.Reeling;Zone=zone;phaseTime=age;}
    }
}
