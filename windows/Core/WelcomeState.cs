using System.IO;
using System.Text;
namespace MyDay.Windows.Core {
 public static class WelcomeState {
    private static string PathFor(string directory){return Path.Combine(directory,"welcome-complete.txt");}
    public static bool ShouldShow(string directory,bool hasRecords){return !hasRecords && !File.Exists(PathFor(directory));}
    public static void Complete(string directory){RecoveryStore.AtomicWrite(PathFor(directory),Encoding.UTF8.GetBytes("1"));}
 }
 public static class RewardOverview {
    public static string Text(DiaryBook book){var state=SangmonCollection.State(book.Collection,book.Progress);int xp=SangmonGrowth.XP(book.Progress);
        return "Lv."+SangmonGrowth.Level(book.Progress)+" · "+xp+" XP · 만렙까지 "+System.Math.Max(0,SangmonGrowth.MaxXP-xp)+" XP\n"+
            "클로버 "+state.Coins+"개 · "+state.Coins/SangmonCollection.DrawCost+"회 뽑기 가능 · 다음 1회까지 "+System.Math.Max(0,SangmonCollection.DrawCost-state.Coins)+"개\n"+
            (state.Owned.Count==300?"소품 300종 수집 완료":(10-state.Draws%10)+"회 후 새 소품 보장")+" · 보유 "+state.Owned.Count+" / 300종";
    }
 }
}
