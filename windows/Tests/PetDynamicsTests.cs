using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Character;
namespace MyDay.Windows {
 internal static partial class Tests {
  private static PetDynamics ThrowPet(RectangleF bounds,PointF start,PointF end){var p=new PetDynamics();p.SetBounds(bounds);p.Place(start);p.BeginDrag(0);p.Drag(end,.05);p.Step(.05);p.Release(.055,true);return p;}
  private static bool Inside(PetDynamics p){var b=p.Bounds;return p.Position.X>=b.Left-.01&&p.Position.X<=b.Right+.01&&p.Position.Y>=b.Top-.01&&p.Position.Y<=b.Bottom+.01;}
  private static void RunPetDynamicsTests(){
   var b=new RectangleF(0,0,700,400);
   var p=new PetDynamics();p.SetBounds(b);p.Place(new PointF(100,100));p.BeginDrag(0);p.Drag(new PointF(110,110),1);p.Step(.1);p.Release(1.01,true);
   Check(!p.Flying&&p.Position==new PointF(110,110),"A slow drag places the pet without an unwanted throw");
   p.Place(new PointF(100,100));p.BeginDrag(0);p.Drag(new PointF(600,100),.04);p.Step(.04);p.Release(.4,true);
   Check(!p.Flying,"Holding still before release discards a stale flick velocity");
   p=ThrowPet(b,new PointF(100,150),new PointF(650,150));
   Check(p.Flying&&p.Velocity.X>0&&Math.Sqrt(p.Velocity.X*p.Velocity.X+p.Velocity.Y*p.Velocity.Y)<=PetDynamics.MaxThrowSpeed+1,"A fast release throws in the pointer direction with bounded speed");
   var range=new RectangleF(-20000,-20000,40000,40000);
   var slow=ThrowPet(range,PointF.Empty,new PointF(30,0));var fast=ThrowPet(range,PointF.Empty,new PointF(60,0));
   Check(Math.Abs(fast.Velocity.X/slow.Velocity.X-2)<.01,"Doubling flick speed doubles launch speed below the protective cap");
   for(int i=0;i<30;i++){slow.Step(.01);fast.Step(.01);}
   Check(fast.Position.X>slow.Position.X*1.8,"A faster throw actually travels farther, rather than changing only its pose");
   bool wall=false,floor=false;float incoming=0,outgoing=0;
   for(int i=0;i<1200&&p.Flying;i++){float before=p.Velocity.X;p.Step(.01);if(p.HitHorizontal&&!wall){wall=true;incoming=Math.Abs(before);outgoing=Math.Abs(p.Velocity.X);Check(p.Velocity.X<0,"Right-edge collision reflects horizontal velocity");}floor|=p.HitVertical;if(!Inside(p))throw new Exception("Thrown pet escaped working area");}
   Check(wall&&floor&&outgoing<incoming&&!p.Flying,"Bounces lose energy and eventually settle on the floor");
   var top=ThrowPet(b,new PointF(300,160),new PointF(300,-20));bool ceiling=false;
   for(int i=0;i<60&&!ceiling;i++){top.Step(.01);if(top.HitVertical&&top.Velocity.Y>0){ceiling=true;Check(top.Velocity.Y>0,"Ceiling collision reflects upward motion downward");}}
   Check(ceiling,"An upward throw can reach and bounce off the top edge");
   var negative=ThrowPet(new RectangleF(-1200,-700,1000,600),new PointF(-700,-400),new PointF(-1500,-400));negative.Step(.1);
   Check(Inside(negative)&&negative.Velocity.X>0,"Negative-coordinate displays reflect left-edge throws without losing the pet");
   var a=ThrowPet(new RectangleF(-20000,-20000,40000,40000),PointF.Empty,new PointF(100,-80));var c=ThrowPet(a.Bounds,PointF.Empty,new PointF(100,-80));
   for(int i=0;i<60;i++)a.Step(1.0/30);for(int i=0;i<240;i++)c.Step(1.0/120);
   Check(Math.Abs(a.Position.X-c.Position.X)<1&&Math.Abs(a.Position.Y-c.Position.Y)<1,"Flight is consistent at 30 and 120 Hz");
   p=ThrowPet(b,new PointF(100,100),new PointF(650,100));p.SetBounds(new RectangleF(50,60,0,0));p.Step(.1);
   Check(Inside(p)&&p.Position==new PointF(50,60),"A changed or zero-sized movement range cannot strand the pet");
   p.BeginDrag(2);p.Drag(new PointF(300,60),2.02);for(int i=0;i<6;i++)p.Step(.01);
   Check(p.ScaleX>1.08&&p.ScaleY<1,"A horizontal pull stretches the body and narrows its other axis");
   Check(p.ScaleX>1.25,"A brisk pull makes a visibly elastic body rather than a tiny wobble");
   for(int i=0;i<120;i++)p.Step(.01);
   Check(Math.Abs(p.ScaleX-1)<.01&&Math.Abs(p.ScaleY-1)<.01,"Keeping the pointer still relaxes a held body back to its original shape");
   p.CancelDrag();p.Step(double.NaN);for(int i=0;i<120;i++)p.Step(.01);
   Check(!p.Flying&&!p.Held&&Math.Abs(p.ScaleX-1)<.01&&Math.Abs(p.ScaleY-1)<.01,"Capture cancellation restores the original body without starting a throw");
   p.Place(new PointF(100,100));p.BeginDrag(4);p.Drag(new PointF(100,400),4.02);for(int i=0;i<6;i++)p.Step(.01);
   Check(p.ScaleY>1.08&&p.ScaleX<1,"A vertical pull stretches height instead of width");
   var edge=ThrowPet(b,new PointF(100,100),new PointF(850,100));bool squash=false;
   for(int i=0;i<80;i++){edge.Step(.01);squash|=edge.HitHorizontal&&edge.ScaleX<1;}
   Check(squash,"A side impact briefly squashes the body at the screen edge");
  }
  private static void VerifyPetPointerRouting(string directory){
   int opened=0;
   using(var pet=new DesktopPet(delegate{opened++;})){
    var start=pet.Location;var pointer=new Point(start.X+180,start.Y+120);
    pet.BeginPointer(pointer,0);pet.MovePointer(new Point(pointer.X+2,pointer.Y),.02);pet.EndPointer(new Point(pointer.X+2,pointer.Y),.04);
    if(opened!=1||pet.Dynamics.Flying)throw new Exception("A click no longer opens the diary exactly once");
    pet.BeginPointer(pointer,1);pet.MovePointer(new Point(pointer.X-160,pointer.Y-60),1.05);for(int i=0;i<5;i++)pet.StepMotion(.01,false);pet.EndPointer(new Point(pointer.X-160,pointer.Y-60),1.055);
    if(opened!=1||!pet.Dynamics.Flying)throw new Exception("Dragging opened the diary or failed to throw");
    var before=pet.Dynamics.Position;pet.StepMotion(.05,true);
    if(before==pet.Dynamics.Position)throw new Exception("Pointer hover swallowed manual throw motion");
    pet.BeginPointer(pointer,2);pet.MovePointer(new Point(pointer.X,pointer.Y+120),2.04);pet.CancelPointer();pet.EndPointer(pointer,2.05);
    if(pet.Dynamics.Held||pet.Dynamics.Flying||opened!=1)throw new Exception("Lost capture caused a phantom throw or opened the diary");
    pet.BeginPointer(pointer,3);pet.MovePointer(new Point(pointer.X+200,pointer.Y),3.04);
    for(int i=0;i<10;i++)pet.StepMotion(.01,false);
    using(var image=pet.MakeFrameCore(3.1,false,pointer)){
     for(int x=0;x<image.Width;x++)if(image.GetPixel(x,0).A>32||image.GetPixel(x,image.Height-1).A>32)throw new Exception("Elastic pet is clipped vertically");
     for(int y=0;y<image.Height;y++)if(image.GetPixel(0,y).A>32||image.GetPixel(image.Width-1,y).A>32)throw new Exception("Elastic pet is clipped horizontally");
     image.Save(Path.Combine(directory,"windows-pet-stretch.png"));
    }
    pet.CancelPointer();
    foreach(var direction in new[]{new Point(1,0),new Point(-1,0),new Point(0,1),new Point(0,-1)}){
     pet.BeginPointer(pointer,4);
     for(int i=1;i<=14;i++){
      pet.MovePointer(new Point(pointer.X+direction.X*i*90,pointer.Y+direction.Y*i*90),4+i*.02);pet.StepMotion(.02,false);
      using(var image=pet.MakeFrameCore(4+i*.02,false,pointer)){
       for(int x=0;x<image.Width;x++)if(image.GetPixel(x,0).A>32||image.GetPixel(x,image.Height-1).A>32)throw new Exception("Maximum elastic drag clips vertically");
       for(int y=0;y<image.Height;y++)if(image.GetPixel(0,y).A>32||image.GetPixel(image.Width-1,y).A>32)throw new Exception("Maximum elastic drag clips horizontally");
      }
     }
     pet.CancelPointer();
    }
   }
   Console.WriteLine("Smoke: native pet click, elastic drag, throw, hover, cancellation and unclipped transparent rendering passed.");
  }
  public static void PetPhysicsPreview(string directory){
   Directory.CreateDirectory(directory);
   using(var pet=new DesktopPet(delegate{}))using(var font=new Font("맑은 고딕",16))using(var small=new Font("맑은 고딕",11))using(var border=new Pen(Color.FromArgb(195,188,175),2)){
    var p=pet.Dynamics;var art=pet.ArtworkBounds;p.SetBounds(new RectangleF(-art.Left,58-art.Top,960-art.Width,480-art.Height));p.Place(new PointF(180,180));
    for(int i=0;i<180;i++){
     double t=i/30.0;string caption=i<20?"상몬을 잡아당겨 보세요":i<45?"쭉 늘어나는 몸":p.Flying?"던지면 화면 가장자리에서 통통!":"다시 편안하게";
     if(i==20)p.BeginDrag(t);
     if(i>=20&&i<45){float f=(i-20)/24f;p.Drag(new PointF(180+450*f,180-100*f),t);}
     if(i==45)p.Release(t,true);
     p.Step(1.0/30);pet.Location=new Point((int)p.Position.X,(int)p.Position.Y);
     using(var canvas=new Bitmap(960,540))using(var g=Graphics.FromImage(canvas)){
      g.Clear(Color.FromArgb(248,246,240));g.DrawRectangle(border,1,58,957,479);
      using(var ink=new SolidBrush(Color.FromArgb(73,76,58))){g.DrawString(caption,font,ink,22,14);g.DrawString("기본 상몬 · 탄성 드래그와 화면 충돌",small,ink,610,22);}
      var pointer=new Point(810,180);
      using(var frame=pet.MakeFrameCore(t,false,pointer))g.DrawImageUnscaled(frame,(int)p.Position.X,(int)p.Position.Y);
      canvas.Save(Path.Combine(directory,"frame-"+i.ToString("D3")+".png"));
     }
    }
   }
  }
 }
}
