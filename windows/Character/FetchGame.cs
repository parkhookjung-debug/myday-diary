using System;
using System.Drawing;
using MyDay.Windows.Core;
namespace MyDay.Windows.Character
{
    public enum FetchPhase { Waiting,Chasing,Returning,Cheering,Stopped }
    // All positions are screen-pixel centers; there is no desktop/file-system input here.
    public sealed class FetchGame
    {
        public readonly PetDynamics Ball=new PetDynamics();
        private RectangleF petRange;
        private PointF grabOffset;
        private double cheer;
        public PointF Home {get;private set;}
        public PointF Movement {get;private set;}
        public FetchPhase Phase {get;private set;}
        public bool FacingLeft {get;private set;}
        public bool Active {get{return Phase!=FetchPhase.Stopped;}}
        public bool Carrying {get{return Phase==FetchPhase.Returning;}}
        public bool Moving {get{return Math.Abs(Movement.X)+Math.Abs(Movement.Y)>.1;}}
        public FetchGame(RectangleF area,RectangleF petRange,PointF home){FacingLeft=true;Home=home;Resize(area,petRange);Phase=FetchPhase.Waiting;Park();}
        private static bool Finite(PointF p){return !float.IsNaN(p.X)&&!float.IsNaN(p.Y)&&!float.IsInfinity(p.X)&&!float.IsInfinity(p.Y);}
        private static PointF Clamp(PointF p,RectangleF r){return new PointF(Math.Max(r.Left,Math.Min(r.Right,p.X)),Math.Max(r.Top,Math.Min(r.Bottom,p.Y)));}
        public void Resize(RectangleF area,RectangleF range){petRange=range;Home=Clamp(Home,range);float radius=(float)MiniGameRules.FetchRadius;Ball.SetBounds(new RectangleF(area.Left+Math.Min(radius,area.Width/2),area.Top+Math.Min(radius,area.Height/2),Math.Max(0,area.Width-radius*2),Math.Max(0,area.Height-radius*2)));}
        private void Park(){var b=Ball.Bounds;Ball.Place(Clamp(new PointF(Home.X+(FacingLeft?-110:110),b.Bottom),b));}
        public void BeginDrag(PointF pointer,double now){if(!Active||!Finite(pointer))return;grabOffset=new PointF(pointer.X-Ball.Position.X,pointer.Y-Ball.Position.Y);Ball.BeginDrag(now);Phase=FetchPhase.Waiting;Movement=PointF.Empty;}
        public void Drag(PointF pointer,double now){if(!Active||!Ball.Held||!Finite(pointer))return;Ball.Drag(new PointF(pointer.X-grabOffset.X,pointer.Y-grabOffset.Y),now);}
        public void Release(PointF pointer,double now){if(!Active||!Ball.Held||!Finite(pointer))return;Drag(pointer,now);if(!Ball.Release(now,true))Ball.StartFlight(PointF.Empty);Phase=FetchPhase.Chasing;}
        public void CancelDrag(){if(!Active||!Ball.Held)return;Ball.CancelDrag();Ball.StartFlight(PointF.Empty);Phase=FetchPhase.Waiting;Movement=PointF.Empty;}
        public void Stop(){Phase=FetchPhase.Stopped;Movement=PointF.Empty;Ball.CancelDrag();}
        public void Step(double seconds,PointF pet){
            Movement=PointF.Empty;if(!Active||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;double dt=Math.Min(.1,seconds);
            if(!Carrying)Ball.Step(dt);
            if(Ball.Held)return;
            if(Phase==FetchPhase.Cheering){cheer-=dt;if(cheer<=0){Phase=FetchPhase.Waiting;Park();}return;}
            if(Phase==FetchPhase.Waiting)return;
            if(Phase==FetchPhase.Chasing){double dx=Ball.Position.X-pet.X,dy=Ball.Position.Y-pet.Y;double speed=Math.Sqrt(Ball.Velocity.X*Ball.Velocity.X+Ball.Velocity.Y*Ball.Velocity.Y);if(dx*dx+dy*dy<=MiniGameRules.FetchCatchRadius*MiniGameRules.FetchCatchRadius&&speed<1400){Phase=FetchPhase.Returning;Ball.Place(Ball.Position);}}
            PointF target=Phase==FetchPhase.Returning?Home:Clamp(Ball.Position,petRange);double mx=target.X-pet.X,my=target.Y-pet.Y,distance=Math.Sqrt(mx*mx+my*my);
            if(Phase==FetchPhase.Returning&&distance<=8){Phase=FetchPhase.Cheering;cheer=.8;Park();return;}
            if(distance>.1){double amount=Math.Min(distance,MiniGameRules.FetchSpeed*dt);Movement=new PointF((float)(mx/distance*amount),(float)(my/distance*amount));if(Math.Abs(Movement.X)>.1)FacingLeft=Movement.X<0;}
            if(Phase==FetchPhase.Returning){var p=new PointF(pet.X+Movement.X+(FacingLeft?-72:72),pet.Y+Movement.Y-3);Ball.Place(Clamp(p,Ball.Bounds));}
        }
    }
}
