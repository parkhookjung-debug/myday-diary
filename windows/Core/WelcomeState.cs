using System.IO;
using System.Text;
namespace MyDay.Windows.Core {
 public static class WelcomeState {
    private static string PathFor(string directory){return Path.Combine(directory,"welcome-complete.txt");}
    public static bool ShouldShow(string directory,bool hasRecords){return !hasRecords && !File.Exists(PathFor(directory));}
    public static void Complete(string directory){RecoveryStore.AtomicWrite(PathFor(directory),Encoding.UTF8.GetBytes("1"));}
 }
 public static class RewardOverview {
    public static string Text(DiaryBook book){int xp=SangmonGrowth.XP(book.Progress);return "Lv."+SangmonGrowth.Level(book.Progress)+" · "+xp+" XP · 만렙까지 "+System.Math.Max(0,SangmonGrowth.MaxXP-xp)+" XP\n출석과 오늘의 기록으로 함께 자라요.";}
 }
}
