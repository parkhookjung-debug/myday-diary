using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    public sealed partial class MyLayoutsWindow
    {
        internal string VerifyAndRender(string directory)
        {
            if(book.Layouts.Count!=0 || SelectedLayout!=null || apply.Enabled) throw new Exception("Empty layout manager retained a selection");
            using(var dialog=new LayoutNameDialog("내 레이아웃 저장","",SaveNamed)) {
                dialog.StartPosition=FormStartPosition.Manual; dialog.Location=new Point(-30000,-30000); dialog.Show(this); Application.DoEvents();
                dialog.NameEditor.Text=" "; dialog.Save.PerformClick();
                if(dialog.IsDisposed || dialog.Error.Text.Length==0 || book.Layouts.Count!=0) throw new Exception("Empty layout name was accepted");
                dialog.NameEditor.Text="사진과 하루"; dialog.Save.PerformClick();
                if(dialog.DialogResult!=DialogResult.OK || book.Layouts.Count!=1 || SelectedLayout.Name!="사진과 하루") throw new Exception("Named layout save failed");
            }
            string id=SelectedLayout.Id;
            RenameSelected("나의 사진 일기"); if(SelectedLayout.Id!=id || SelectedLayout.Name!="나의 사진 일기") throw new Exception("Rename lost layout identity");
            SaveNamed("삭제 확인용"); DeleteSelected();
            if(book.Layouts.Count!=1 || book.Layouts[0].Id!=id) throw new Exception("Delete removed the wrong saved layout");
            SaveNamed("아침의 기록"); SaveNamed("주말의 기록");
            search.Text="사진";
            if(list.Controls.OfType<Button>().Count()!=1 || SelectedLayout.Id!=id) throw new Exception("Saved layout search failed");
            search.Text="no-layout-found";
            if(SelectedLayout!=null || apply.Enabled || rename.Enabled || delete.Enabled) throw new Exception("No-results layout view retained stale actions");
            search.Clear(); Select(book.Layouts.First(l=>l.Id==id));
            var full=Size; Size=MinimumSize; PerformLayout(); Application.DoEvents(); ResizeContent();
            if(list.HorizontalScroll.Visible || preview.HorizontalScroll.Visible || apply.Right>apply.Parent.Width || save.Right>save.Parent.Width) throw new Exception("Compact layout manager clips controls");
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(directory,"windows-my-layouts-compact.png"),System.Drawing.Imaging.ImageFormat.Png); }
            Size=full; PerformLayout(); ResizeContent(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(directory,"windows-my-layouts.png"),System.Drawing.Imaging.ImageFormat.Png); }
            apply.PerformClick(); if(DialogResult!=DialogResult.OK || SelectedLayout.Id!=id) throw new Exception("Manager did not confirm applying the saved layout");
            return id;
        }
        internal void ApplyForTest(string id)
        {
            Select(book.Layouts.First(l=>l.Id==id)); apply.PerformClick();
            if(DialogResult!=DialogResult.OK) throw new Exception("Saved layout selection failed");
        }
    }
}
