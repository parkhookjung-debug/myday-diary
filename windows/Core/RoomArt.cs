using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace MyDay.Windows.Core {
 // First-party vector source; exported PNGs are shared by Android and macOS.
 public static class RoomArt {
  public static readonly string[] Kinds={"plant","chair","rug","lamp","table","cushion"};
  private static readonly Color Wood=Color.FromArgb(173,128,95),LightWood=Color.FromArgb(215,176,137),Leaf=Color.FromArgb(116,151,111),Pale=Color.FromArgb(247,236,215),Pink=Color.FromArgb(228,180,181),Lavender=Color.FromArgb(190,178,211);
  private static void Shape(Graphics g,Color color,float x,float y,float w,float h,float radius){using(var brush=new SolidBrush(color))using(var path=new GraphicsPath()){float d=radius*2;path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();g.FillPath(brush,path);}}
  private static void Oval(Graphics g,Color c,float x,float y,float w,float h){using(var b=new SolidBrush(c))g.FillEllipse(b,x,y,w,h);}
  public static Bitmap Image(string kind){if(Array.IndexOf(Kinds,kind)<0)throw new ArgumentException("Unknown room item");var image=new Bitmap(240,240);using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(2.4f,2.4f);Oval(g,Color.FromArgb(28,98,74,56),12,81,76,11);
   if(kind=="plant"){using(var p=new Pen(Wood,3))g.DrawLine(p,50,66,50,25);Oval(g,Leaf,25,30,25,15);Oval(g,Color.FromArgb(149,177,134),50,22,25,18);Oval(g,Leaf,34,10,18,26);Oval(g,Color.FromArgb(160,184,145),51,45,22,12);Shape(g,Pink,31,58,38,31,9);Shape(g,Color.FromArgb(237,199,190),27,56,46,9,4);}
   else if(kind=="chair"){Shape(g,Wood,28,15,44,65,12);Shape(g,Pale,33,20,34,41,9);Shape(g,Wood,28,66,7,25,3);Shape(g,Wood,66,66,7,25,3);Shape(g,LightWood,20,57,60,14,5);Shape(g,Color.FromArgb(190,204,165),25,54,50,12,5);}
   else if(kind=="rug"){Oval(g,Lavender,7,31,86,49);Oval(g,Color.FromArgb(216,207,230),14,36,72,38);using(var p=new Pen(Color.FromArgb(240,233,244),1.8f)){for(int i=20;i<=80;i+=10)g.DrawLine(p,i,41,i,68);for(int i=18;i<=82;i+=8){g.DrawLine(p,i,76,i-2,82);g.DrawLine(p,i,31,i-2,26);}}}
   else if(kind=="lamp"){Shape(g,Wood,47,32,6,54,2);Oval(g,LightWood,29,80,42,10);using(var b=new SolidBrush(Pale))g.FillPolygon(b,new[]{new PointF(32,18),new PointF(68,18),new PointF(80,49),new PointF(20,49)});Shape(g,LightWood,20,47,60,6,2);Oval(g,Color.FromArgb(246,220,166),35,13,30,10);}
   else if(kind=="table"){Shape(g,Wood,21,49,7,39,3);Shape(g,Wood,72,49,7,39,3);Shape(g,LightWood,10,43,80,17,7);Shape(g,Wood,11,54,78,6,2);Shape(g,Lavender,29,31,31,7,2);Shape(g,Color.FromArgb(195,209,172),31,24,29,7,2);Shape(g,Pale,66,25,12,18,4);using(var p=new Pen(Pale,3))g.DrawArc(p,74,28,9,11,270,180);}
   else{Shape(g,Pink,19,23,62,60,16);Shape(g,Color.FromArgb(242,211,203),25,29,50,48,12);using(var p=new Pen(Color.FromArgb(222,166,169),1.5f)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,34,38,32,30);}Oval(g,Pink,47,50,6,6);}
  }return image;}
 }
}
