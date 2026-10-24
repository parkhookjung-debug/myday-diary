using System;
using System.IO;
using System.Linq;

namespace MyDay.Windows.Core
{
    public static partial class SangmonCollection
    {
        public static bool NeedsV8(string kind) {return kind=="draw-tiered" || kind=="exchange-tiered" || kind=="synth" || kind=="craft";}
        public static bool NeedsV8(CollectionBook book) {return book!=null && book.Actions.Any(a=>NeedsV8(a.Kind));}
        public static int Rarity(string itemId)
        {
            var item=SangmonItems.Find(itemId);if(item==null)throw new ArgumentException("알 수 없는 소품이에요.");
            if(Starters.Contains(itemId))return 0;
            for(int i=0;i<4;i++)if(CollectionPolicy.Forms[i].Contains(item.Form))return i;
            throw new InvalidDataException("소품 등급 원본이 필요해요.");
        }
        public static string RarityName(string itemId) {return CollectionPolicy.Names[Rarity(itemId)];}
        public static int ExchangePrice(string itemId) {return CollectionPolicy.ExchangePetals[Rarity(itemId)];}
        public static string OddsText() {return string.Join(" · ",Enumerable.Range(0,4).Select(g=>CollectionPolicy.Names[g]+" "+(CollectionPolicy.Weights[g]/100.0).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"%"));}
        private static void ApplyMilestones(CollectionState state)
        {
            foreach(var m in CollectionPolicy.Milestones)if(state.Owned.Count>=m[0] && state.Milestones.Add(m[0]))state.Materials[m[1]]+=m[2];
        }
        private static void ReplaySynthesis(CollectionState state,string token)
        {
            var parts=(token??"").Split(':');int grade;
            if(!state.Closet || parts.Length!=2 || !int.TryParse(parts[0],out grade) || grade<0 || grade>2 || parts[0]!=grade.ToString(System.Globalization.CultureInfo.InvariantCulture) || (parts[1]!="up" && parts[1]!="stay") || state.Materials[grade]<CollectionPolicy.SynthesisCost)
                throw new InvalidDataException("합성 재료나 결과 기록이 올바르지 않아요.");
            state.Materials[grade]-=CollectionPolicy.SynthesisCost;state.Syntheses++;
            if(parts[1]=="up") {state.Materials[grade+1]++;state.SynthesisSuccesses++;}
            else state.Materials[grade]+=CollectionPolicy.FailureRefund;
        }
        public static CollectionBook Synthesize(CollectionBook current,SangmonProgress progress,int grade,Func<int,int> choose=null)
        {
            if(grade<0 || grade>=3)throw new ArgumentOutOfRangeException("grade");
            current=EnsureCloset(current);var state=State(current,progress);
            if(state.Materials[grade]<CollectionPolicy.SynthesisCost)throw new InvalidOperationException("같은 등급 조각 3개가 필요해요.");
            int roll=(choose??RandomIndex)(100);if(roll<0 || roll>=100)throw new ArgumentOutOfRangeException("choose");
            return Append(current,progress,"synth",grade+":"+(roll<CollectionPolicy.SynthesisPercent[grade]?"up":"stay"));
        }
        public static CollectionBook Craft(CollectionBook current,SangmonProgress progress,string itemId) {return Append(EnsureCloset(current),progress,"craft",itemId);}
    }
}
