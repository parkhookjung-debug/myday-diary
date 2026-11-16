using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace MyDay.Windows.UI {
 public sealed partial class DiaryWindow {
  private void OpenCompanion(){OpenGrowth();}
  internal void VerifyCommonScreensForTest(string directory){
   using(var hub=new GrowthWindow(book,ClaimAttendance)){hub.Show(this);Application.DoEvents();using(var image=new Bitmap(hub.Width,hub.Height)){hub.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-companion-hub.png"));}hub.Close();}
   using(var settings=new PreferencesWindow(entry.Theme,SetThemeFromSettings,delegate{},delegate{},delegate{})){settings.Show(this);Application.DoEvents();using(var image=new Bitmap(settings.Width,settings.Height)){settings.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-common-settings.png"));}settings.Close();}
  }
 }
}
