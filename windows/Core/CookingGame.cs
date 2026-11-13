using System;
namespace MyDay.Windows.Core
{
    public enum CookingPhase { ReadyItem,Heating,Feedback }
    public enum BakeOutcome { None,Under,Good,Perfect,Burnt }
    public sealed class CookingGame
    {
        private static readonly double[] targets={68,74,62,78,66,72,58,76};
        private static readonly string[] recipes={"쿠키","식빵","마시멜로"};
        private double remainder,feedback;
        public SnackRoundState State {get;private set;}
        public CookingPhase Phase {get;private set;}
        public BakeOutcome Outcome {get;private set;}
        public int OrderIndex {get;private set;}
        public int Completed {get;private set;}
        public int Cooked {get;private set;}
        public int Perfects {get;private set;}
        public int Score {get;private set;}
        public int LastGain {get;private set;}
        public double Heat {get;private set;}
        public double Elapsed {get;private set;}
        public string Message {get;private set;}
        public bool Breathing {get{return State==SnackRoundState.Playing&&Phase==CookingPhase.Heating;}}
        public string Recipe {get{return recipes[OrderIndex%recipes.Length];}}
        public int RecipeIndex {get{return OrderIndex%recipes.Length;}}
        public double Target {get{return targets[OrderIndex%targets.Length];}}
        public double HeatRate {get{return MiniGameRules.BakeHeatRate+(OrderIndex*7)%9-4;}}
        public double SecondsLeft {get{return Math.Max(0,MiniGameRules.BakeSeconds-Elapsed);}}
        public CookingGame(){Message="불을 뿜어 포근한 간식을 구워요.";}
        public void Start(){OrderIndex=Completed=Cooked=Perfects=Score=LastGain=0;Heat=Elapsed=remainder=feedback=0;Outcome=BakeOutcome.None;Phase=CookingPhase.ReadyItem;State=SnackRoundState.Playing;Message="누르고 있다가 황금색 구간에서 놓아요.";}
        public bool BeginHeat(){if(State!=SnackRoundState.Playing||Phase!=CookingPhase.ReadyItem)return false;Phase=CookingPhase.Heating;Heat=0;Message="조금만 더… 알맞을 때 놓아요!";return true;}
        public void CancelHeat(){if(Phase!=CookingPhase.Heating)return;Heat=0;Phase=CookingPhase.ReadyItem;Message="불을 껐어요. 다시 천천히 구워봐요.";}
        public bool ReleaseHeat(){if(!Breathing)return false;double difference=Math.Abs(Heat-Target);Finish(Heat>Target+MiniGameRules.BakeTargetWidth/2?BakeOutcome.Burnt:difference<=MiniGameRules.BakePerfectWidth/2?BakeOutcome.Perfect:difference<=MiniGameRules.BakeTargetWidth/2?BakeOutcome.Good:BakeOutcome.Under);return true;}
        private void Finish(BakeOutcome outcome){Outcome=outcome;LastGain=outcome==BakeOutcome.Perfect?MiniGameRules.BakePerfectPoints:outcome==BakeOutcome.Good?MiniGameRules.BakeGoodPoints:0;Score+=LastGain;Completed++;if(LastGain>0)Cooked++;if(outcome==BakeOutcome.Perfect)Perfects++;Phase=CookingPhase.Feedback;feedback=MiniGameRules.BakeFeedbackSeconds;Message=outcome==BakeOutcome.Perfect?"딱 좋아요! 완벽한 "+Recipe+" +"+LastGain:outcome==BakeOutcome.Good?"노릇노릇 맛있어요! +"+LastGain:outcome==BakeOutcome.Under?"조금 덜 익었어요. 다음 간식도 구워봐요!":"앗, 너무 뜨거웠어요! 다음에는 조금 일찍 놓아요.";}
        public void Pause(){if(State==SnackRoundState.Playing){CancelHeat();State=SnackRoundState.Paused;}remainder=0;}
        public void Resume(){if(State==SnackRoundState.Paused)State=SnackRoundState.Playing;}
        public void Step(double seconds){if(State!=SnackRoundState.Playing||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;remainder+=Math.Min(.25,seconds);const double h=1.0/120;while(remainder+1e-10>=h&&State==SnackRoundState.Playing){remainder-=h;Elapsed=Math.Min(MiniGameRules.BakeSeconds,Elapsed+h);if(Phase==CookingPhase.Heating){Heat=Math.Min(100,Heat+HeatRate*h);if(Heat>=100-1e-8){Heat=100;Finish(BakeOutcome.Burnt);}}else if(Phase==CookingPhase.Feedback){feedback-=h;if(feedback<=0){if(Completed>=MiniGameRules.BakeOrders)State=SnackRoundState.Finished;else{OrderIndex=Completed;Heat=0;Outcome=BakeOutcome.None;LastGain=0;Phase=CookingPhase.ReadyItem;Message="다음은 "+Recipe+"! 누르고 있다가 놓아요.";}}}if(Elapsed>=MiniGameRules.BakeSeconds-1e-8){Elapsed=MiniGameRules.BakeSeconds;CancelHeat();State=SnackRoundState.Finished;Message="오늘의 간식 굽기를 마쳤어요.";}}}
    }
}
