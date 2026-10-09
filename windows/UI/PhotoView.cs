using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    // Fit the entire photo without cropping; each card owns and releases its bitmap.
    public sealed class PhotoView : Control
    {
        private readonly string data;
        private Bitmap photo;
        public PhotoView(string data)
        {
            this.data=data; BackColor=Design.Tint;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            AccessibleName="일기에 첨부한 사진";
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(photo==null) {
                using(var full=DiaryPhoto.Decode(data)) {
                    double thumbnailScale=Math.Min(1,800.0/Math.Max(full.Width,full.Height));
                    photo=new Bitmap(Math.Max(1,(int)(full.Width*thumbnailScale)),Math.Max(1,(int)(full.Height*thumbnailScale)));
                    using(var graphics=Graphics.FromImage(photo)) {
                        graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(full,new Rectangle(0,0,photo.Width,photo.Height));
                    }
                }
            }
            double scale=Math.Min(ClientSize.Width/(double)photo.Width,ClientSize.Height/(double)photo.Height);
            int width=Math.Max(1,(int)(photo.Width*scale)),height=Math.Max(1,(int)(photo.Height*scale));
            e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(photo,new Rectangle((ClientSize.Width-width)/2,(ClientSize.Height-height)/2,width,height));
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing && photo!=null) photo.Dispose(); base.Dispose(disposing);
        }
    }
}
