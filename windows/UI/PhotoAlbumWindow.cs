using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI {
    public sealed class PhotoAlbumWindow:Form {
        public DateTime? SelectedDate;
        private readonly FlowLayoutPanel grid=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,Padding=Design.Pad(14)};
        private readonly DateTimePicker month=new DateTimePicker {Format=DateTimePickerFormat.Custom,CustomFormat="yyyy년 MM월",ShowUpDown=true};
        private readonly DiaryBook book;private int page;
        public PhotoAlbumWindow(DiaryBook book,DateTime date) {
            this.book=book;Text="사진 모아보기";Size=Design.Size(800,650);MinimumSize=Design.Size(500,420);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Sidebar;
            month.Value=date;var toolbar=new FlowLayoutPanel {Dock=DockStyle.Top,Height=Design.P(60),Padding=Design.Pad(12)};toolbar.Controls.Add(month);
            var previous=Design.Button("‹ 이전 사진");var next=Design.Button("다음 사진 ›");previous.Click+=delegate {if(page>0){page--;Render();}};next.Click+=delegate {page++;Render();};toolbar.Controls.Add(previous);toolbar.Controls.Add(next);
            Controls.Add(grid);Controls.Add(toolbar);month.ValueChanged+=delegate {page=0;Render();};Render();
        }
        private void Render() {
            foreach(Control c in grid.Controls.Cast<Control>().ToArray())c.Dispose();
            string prefix=month.Value.ToString("yyyy-MM",CultureInfo.InvariantCulture);
            var photos=book.Days.Where(p=>p.Key.StartsWith(prefix,StringComparison.Ordinal)).OrderByDescending(p=>p.Key).SelectMany(p=>p.Value.Blocks.Where(b=>b.Kind=="photo" && b.Photo!=null).Select(b=>new{Date=p.Key,Block=b})).ToArray();
            page=Math.Max(0,Math.Min(page,Math.Max(0,(photos.Length-1)/24)));
            if(photos.Length==0){grid.Controls.Add(Design.Label("이번 달은 아직 사진이 없어요. 일기에 사진을 추가해 보세요.",11));return;}
            foreach(var item in photos.Skip(page*24).Take(24)) {
                var card=new Panel {Size=Design.Size(215,235),BackColor=Color.White,Margin=Design.Pad(7)};
                var picture=new PictureBox {Location=Design.Point(8,8),Size=Design.Size(199,165),SizeMode=PictureBoxSizeMode.Zoom,Cursor=Cursors.Hand};
                using(var image=DiaryPhoto.Decode(item.Block.Photo)){double scale=Math.Min(400.0/image.Width,300.0/image.Height);picture.Image=new Bitmap(image,new Size(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale))));}
                picture.Disposed+=delegate {if(picture.Image!=null)picture.Image.Dispose();};
                string key=item.Date;var open=Design.Button(key+" 일기로");open.Location=Design.Point(8,197);open.Size=Design.Size(199,30);open.Click+=delegate {SelectedDate=DateTime.ParseExact(key,"yyyy-MM-dd",CultureInfo.InvariantCulture);DialogResult=DialogResult.OK;};picture.Click+=delegate {open.PerformClick();};
                var caption=Design.Label(item.Block.Text,9);caption.AutoSize=false;caption.AutoEllipsis=true;caption.Location=Design.Point(8,177);caption.Size=Design.Size(199,20);card.Controls.AddRange(new Control[]{picture,caption,open});grid.Controls.Add(card);
            }
        }
    }
}
