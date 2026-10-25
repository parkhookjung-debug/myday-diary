using System;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
 public sealed class PhotoDecorWindow:Form {
  private Bitmap image;private readonly PictureBox preview=new PictureBox {Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
  public string Result;private int sticker=-1;
  public PhotoDecorWindow(string encoded){image=DiaryPhoto.Decode(encoded);Text="사진 꾸미기 · 원본은 적용 전까지 유지돼요";Size=Design.Size(760,640);MinimumSize=Design.Size(580,480);StartPosition=FormStartPosition.CenterParent;BackColor=Design.Sidebar;Font=Design.Font(10);
   var actions=new FlowLayoutPanel {Dock=DockStyle.Top,Height=Design.P(116),Padding=Design.Pad(10),AutoScroll=true};Controls.Add(preview);Controls.Add(actions);
   Add(actions,"왼쪽 회전",()=>{image.RotateFlip(RotateFlipType.Rotate270FlipNone);RefreshImage();});Add(actions,"오른쪽 회전",()=>{image.RotateFlip(RotateFlipType.Rotate90FlipNone);RefreshImage();});
   Add(actions,"가운데 정사각 자르기",()=>Replace(PhotoDecor.Crop(image,1)));Add(actions,"가운데 4:3 자르기",()=>Replace(PhotoDecor.Crop(image,4.0/3)));
   for(int i=0;i<3;i++){int color=i;Add(actions,new[]{"크림 테두리","라벤더 테두리","숲 테두리"}[i],()=>Replace(PhotoDecor.Frame(image,color)));}
   for(int i=0;i<4;i++){int k=i;Add(actions,PhotoDecor.Stickers[i]+" 붙이기",()=>{sticker=k;Text="사진에서 스티커를 붙일 위치를 눌러요";});}
   var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=Design.P(48)};Add(bottom,"처음으로",()=>Replace(DiaryPhoto.Decode(encoded)));Add(bottom,"이 사진 적용",()=>{Result=PhotoDecor.Encode(image);DialogResult=DialogResult.OK;});Add(bottom,"취소",()=>Close());Controls.Add(bottom);
   preview.MouseClick+=delegate(object sender,MouseEventArgs e){if(sticker<0)return;double scale=Math.Min(preview.Width/(double)image.Width,preview.Height/(double)image.Height);double x=(e.X-(preview.Width-image.Width*scale)/2)/scale,y=(e.Y-(preview.Height-image.Height*scale)/2)/scale;if(x<0||y<0||x>=image.Width||y>=image.Height)return;using(var g=Graphics.FromImage(image)){float side=Math.Max(24,Math.Min(image.Width,image.Height)*0.22f);PhotoDecor.DrawSticker(g,sticker,new RectangleF((float)x-side/2,(float)y-side/2,side,side));}RefreshImage();};RefreshImage();
  }
  private void Replace(Bitmap value){var old=image;image=value;preview.Image=image;old.Dispose();}
  private void RefreshImage(){preview.Image=image;preview.Invalidate();}
  internal static void Add(Control parent,string title,Action action){var b=Design.Button(title);b.AutoSize=true;b.Click+=delegate{try{action();}catch(Exception e){MessageBox.Show(e.Message);}};parent.Controls.Add(b);}
  protected override void Dispose(bool disposing){if(disposing){preview.Image=null;image.Dispose();}base.Dispose(disposing);}
 }
}
