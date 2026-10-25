using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
namespace MyDay.Windows.Core {
 public static class PhotoDecor {
  public static readonly string[] Stickers={"꽃","하트","별","테이프"};
  public static readonly Color[] Colors={Color.FromArgb(240,224,213),Color.FromArgb(219,210,236),Color.FromArgb(220,231,211)};
  public static string Encode(Bitmap source) {double scale=Math.Min(1,1600.0/Math.Max(source.Width,source.Height));using(var b=new Bitmap(Math.Max(1,(int)(source.Width*scale)),Math.Max(1,(int)(source.Height*scale))))using(var g=Graphics.FromImage(b))using(var output=new MemoryStream())using(var p=new EncoderParameters(1)){g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,new Rectangle(0,0,b.Width,b.Height));p.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,88L);b.Save(output,ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid),p);if(output.Length>DiaryPhoto.MaxBytes)throw new IOException("사진은 편집 후 2MB 이하여야 해요.");return Convert.ToBase64String(output.ToArray());}}
  public static Bitmap Crop(Bitmap image,double ratio) {int w=image.Width,h=image.Height;if(w/(double)h>ratio)w=Math.Max(1,(int)(h*ratio));else h=Math.Max(1,(int)(w/ratio));return image.Clone(new Rectangle((image.Width-w)/2,(image.Height-h)/2,w,h),PixelFormat.Format32bppArgb);}
  public static Bitmap Frame(Bitmap image,int color) {if(color<0||color>=Colors.Length)throw new ArgumentException();int edge=Math.Max(6,Math.Min(image.Width,image.Height)/16);var b=new Bitmap(image.Width+2*edge,image.Height+2*edge);using(var g=Graphics.FromImage(b)){g.Clear(Colors[color]);g.DrawImage(image,edge,edge,image.Width,image.Height);}if(b.Width>1600||b.Height>1600){string data=Encode(b);b.Dispose();return DiaryPhoto.Decode(data);}return b;}
  public static void DrawSticker(Graphics g,int kind,RectangleF rect) {var state=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/100,rect.Height/100);g.SmoothingMode=SmoothingMode.AntiAlias;using(var pink=new SolidBrush(Color.FromArgb(227,156,168)))using(var olive=new SolidBrush(Color.FromArgb(122,148,110)))using(var yellow=new SolidBrush(Color.FromArgb(239,205,125))){if(kind==0){for(int i=0;i<6;i++){double a=i*Math.PI/3;g.FillEllipse(pink,32+(float)Math.Cos(a)*23,32+(float)Math.Sin(a)*23,36,36);}g.FillEllipse(yellow,35,35,30,30);}else if(kind==1){using(var p=new GraphicsPath()){p.AddBezier(50,85,0,50,2,10,28,14);p.AddBezier(28,14,42,14,49,24,50,29);p.AddBezier(50,29,55,4,87,6,92,30);p.AddBezier(92,30,100,50,70,73,50,85);g.FillPath(pink,p);}}else if(kind==2){var pts=Enumerable.Range(0,10).Select(i=>{double a=i*Math.PI/5-Math.PI/2;float r=i%2==0?44:20;return new PointF(50+(float)Math.Cos(a)*r,50+(float)Math.Sin(a)*r);}).ToArray();g.FillPolygon(yellow,pts);}else{g.RotateTransform(-8);g.FillRectangle(olive,8,35,80,30);using(var pen=new Pen(Color.FromArgb(221,231,207),3)){for(int i=15;i<85;i+=15)g.DrawLine(pen,i,36,i+9,64);}}}g.Restore(state);}
  public static Bitmap Sticker(int kind) {if(kind<0||kind>=4)throw new ArgumentException();var b=new Bitmap(320,240);using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(250,248,243));DrawSticker(g,kind,new RectangleF(60,20,200,200));}return b;}
 }
}
