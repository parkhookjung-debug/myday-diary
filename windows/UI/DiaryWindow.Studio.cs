using System;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
 public sealed partial class DiaryWindow {
  internal void VerifyStudioForTest(string directory){book.Studio=MyDay.Windows.Tests.StudioFixture().Studio;int[] x={90,380,280,770,580,470},y={420,530,720,330,560,730};for(int i=0;i<6;i++){book.Studio.Furniture[i].X=x[i];book.Studio.Furniture[i].Y=y[i];}using(var window=new StudioWindow(book,entry,date,Path.GetDirectoryName(store.FilePath),_=>true,_=>{})){window.StartPosition=FormStartPosition.Manual;window.Location=new System.Drawing.Point(-30000,-30000);window.Show();Application.DoEvents();for(int i=0;i<6;i++){window.Tabs.SelectedIndex=i;Application.DoEvents();using(var image=new System.Drawing.Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new System.Drawing.Rectangle(System.Drawing.Point.Empty,window.Size));image.Save(Path.Combine(directory,"windows-studio-"+i+".png"));}}window.Close();}}
  private bool SaveStudioChange(bool record){if(record){QueueSave();}else QueueBookSave();bool ok=FlushSave();if(!ok)MessageBox.Show(this,"저장하지 못했어요. 창을 닫기 전에 다시 저장해 주세요.");return ok;}
  private void OpenStudio(){if(!ScreenPrivacy.Reveal()||!FlushSave())return;using(var studio=new StudioWindow(book,entry,date,Path.GetDirectoryName(store.FilePath),SaveStudioChange,ChangeDate))studio.ShowDialog(this);RenderCards();RefreshHistory();}
  private void DecoratePhoto(DiaryBlock block){if(!FlushSave()||block.Photo==null)return;using(var editor=new PhotoDecorWindow(block.Photo))if(editor.ShowDialog(this)==DialogResult.OK){var old=block.Photo;block.Photo=editor.Result;block.ValidatedPhoto=null;try{DiaryStore.Validate(book);SaveStudioChange(true);}catch(Exception ex){block.Photo=old;block.ValidatedPhoto=null;MessageBox.Show(ex.Message);}RenderCards();}}
 }
}
