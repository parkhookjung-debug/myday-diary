using System;
using System.Drawing;

namespace MyDay.Windows.Character
{
    // Screen-pixel motion, independent of WinForms and wall-clock timers.
    // Bounds describe allowed WINDOW origins after the transparent artwork margins are removed.
    public sealed class PetDynamics
    {
        public const float MinThrowSpeed=140, MaxThrowSpeed=5200, Gravity=1150;
        public const float ThrowGain=1.80f;
        private const float Restitution=.72f;
        private PointF target,lastSample;
        private double sampleTime,lastMovement,dragPoseAge;
        private float dragX,dragY,impactX,impactY;
        private double impactAgeX=10,impactAgeY=10;
        public PointF Position {get;private set;}
        public PointF Velocity {get;private set;}
        public RectangleF Bounds {get;private set;}
        public bool Held {get;private set;}
        public bool Flying {get;private set;}
        public bool HitHorizontal {get;private set;}
        public bool HitVertical {get;private set;}
        public float ScaleX {get;private set;}
        public float ScaleY {get;private set;}
        public float Tilt {get;private set;}
        public PetDynamics(){ScaleX=ScaleY=1;Bounds=new RectangleF(-100000,-100000,200000,200000);}
        private static float Limit(float x,float low,float high){return Math.Max(low,Math.Min(high,x));}
        private static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
        public void SetBounds(RectangleF value)
        {
            if(!Finite(value.X)||!Finite(value.Y)||!Finite(value.Width)||!Finite(value.Height))throw new ArgumentException("Finite screen bounds required");
            Bounds=new RectangleF(value.X,value.Y,Math.Max(0,value.Width),Math.Max(0,value.Height));
            if(!Held)Clamp();
        }
        public void Place(PointF point)
        {
            if(!Finite(point.X)||!Finite(point.Y))throw new ArgumentException("Finite position required");
            Position=point;target=point;Velocity=PointF.Empty;Flying=false;
        }
        // A released toy falls even when the pointer was moving too slowly to fling it.
        public void StartFlight(PointF velocity){if(!Finite(velocity.X)||!Finite(velocity.Y))throw new ArgumentException("Finite velocity required");Held=false;Flying=true;float speed=(float)Math.Sqrt(velocity.X*velocity.X+velocity.Y*velocity.Y),factor=Math.Min(1,MaxThrowSpeed/Math.Max(1,speed));Velocity=new PointF(velocity.X*factor,velocity.Y*factor);Clamp();}
        public void Translate(PointF delta){Position=new PointF(Position.X+delta.X,Position.Y+delta.Y);Clamp();}
        public void Clamp(){Position=new PointF(Limit(Position.X,Bounds.Left,Bounds.Right),Limit(Position.Y,Bounds.Top,Bounds.Bottom));}
        public void BeginDrag(double now)
        {
            Held=true;Flying=false;Velocity=PointF.Empty;dragX=dragY=0;target=lastSample=Position;sampleTime=lastMovement=now;dragPoseAge=0;
            impactX=impactY=0;
        }
        public void Drag(PointF point,double now)
        {
            if(!Held)return;
            if(!Finite(point.X)||!Finite(point.Y))return;
            target=point;double dt=now-sampleTime;
            // Coalesce high-frequency pointer messages rather than dividing by a tiny interval.
            if(dt<.001)return;
            float dx=point.X-lastSample.X,dy=point.Y-lastSample.Y;
            float blend=(float)(1-Math.Exp(-dt*24));
            dragX+=(Limit(dx/(float)dt,-12000,12000)-dragX)*blend;
            dragY+=(Limit(dy/(float)dt,-12000,12000)-dragY)*blend;
            if(Math.Abs(dx)+Math.Abs(dy)>.1f){lastMovement=now;dragPoseAge=0;}
            lastSample=point;sampleTime=now;
        }
        public bool Release(double now,bool allowThrow)
        {
            if(!Held)return false;
            Held=false;
            float decay=(float)Math.Exp(-Math.Max(0,now-sampleTime)*24);
            float vx=dragX*decay*ThrowGain,vy=dragY*decay*ThrowGain,speed=(float)Math.Sqrt(vx*vx+vy*vy);
            Flying=allowThrow&&now-lastMovement<.12&&speed>=MinThrowSpeed;
            if(Flying){float scale=Math.Min(1,MaxThrowSpeed/Math.Max(1,speed));Velocity=new PointF(vx*scale,vy*scale);}
            else {Velocity=PointF.Empty;Position=target;Impact(false,.12f);}
            Clamp();return Flying;
        }
        public void CancelDrag(){Held=false;Flying=false;Velocity=PointF.Empty;dragX=dragY=0;Clamp();}
        private void Impact(bool horizontal,float amount)
        {
            if(horizontal){impactX=Math.Max(.10f,Math.Min(1,amount));impactAgeX=0;}
            else{impactY=Math.Max(.10f,Math.Min(1,amount));impactAgeY=0;}
        }
        public void Step(double seconds)
        {
            if(double.IsNaN(seconds))return;
            double dt=Math.Max(0,Math.Min(.1,seconds));HitHorizontal=HitVertical=false;
            impactAgeX+=dt;impactAgeY+=dt;dragPoseAge+=dt;
            if(Held){float follow=(float)(1-Math.Exp(-dt*16));Position=new PointF(Position.X+(target.X-Position.X)*follow,Position.Y+(target.Y-Position.Y)*follow);}
            else if(Flying){
                double left=dt;
                while(left>0.000001){float h=(float)Math.Min(1.0/120,left);left-=h;
                    float vx=Velocity.X*(float)Math.Exp(-h*.42),vy=Velocity.Y+Gravity*h;
                    float x=Position.X+vx*h,y=Position.Y+vy*h;
                    if((x<Bounds.Left&&vx<0)||(x>Bounds.Right&&vx>0)){
                        HitHorizontal=true;Impact(true,Math.Abs(vx)/1500);vx=-vx*Restitution;
                        x=Limit(x,Bounds.Left,Bounds.Right);
                    }
                    if(y<Bounds.Top&&vy<0){HitVertical=true;Impact(false,Math.Abs(vy)/1500);y=Bounds.Top;vy=-vy*Restitution;}
                    if(y>=Bounds.Bottom&&vy>=0){
                        HitVertical=true;y=Bounds.Bottom;
                        if(vy>150){Impact(false,vy/1500);vy=-vy*Restitution;}
                        else {vy=0;vx*=(float)Math.Exp(-h*8);}
                    }
                    Position=new PointF(Limit(x,Bounds.Left,Bounds.Right),Limit(y,Bounds.Top,Bounds.Bottom));Velocity=new PointF(vx,vy);
                    if(vy==0&&Math.Abs(vx)<24&&y>=Bounds.Bottom){Flying=false;Velocity=PointF.Empty;break;}
                }
            }
            float dragRelax=(float)Math.Exp(-dragPoseAge*10);
            float sx=1,sy=1,vxPose=Held?dragX*dragRelax:Velocity.X,vyPose=Held?dragY*dragRelax:Velocity.Y;
            if(Held||Flying){
                float lagX=target.X-Position.X,lagY=target.Y-Position.Y;
                float lag=Held?(float)Math.Sqrt(lagX*lagX+lagY*lagY)/180:0;
                float amount=Math.Min(Held?.75f:.18f,(float)Math.Sqrt(vxPose*vxPose+vyPose*vyPose)/(Held?1800:5000)+lag);
                if(Math.Abs(vxPose)>Math.Abs(vyPose)){sx+=amount;sy-=amount*.4f;}
                else {sy+=amount;sx-=amount*.4f;}
            }
            float ix=impactX*(float)(Math.Exp(-impactAgeX*10)*Math.Cos(impactAgeX*25));
            float iy=impactY*(float)(Math.Exp(-impactAgeY*10)*Math.Cos(impactAgeY*25));
            sx+=iy*.32f-ix*.32f;sy+=ix*.24f-iy*.32f;
            // Show the compression immediately; relax smoothly after the impact.
            float blendPose=HitHorizontal||HitVertical?.75f:(float)(1-Math.Exp(-dt*28));
            ScaleX+=(Limit(sx,.65f,1.80f)-ScaleX)*blendPose;ScaleY+=(Limit(sy,.65f,1.80f)-ScaleY)*blendPose;
            Tilt+=(Limit(vxPose*.006f,-13,13)-Tilt)*blendPose;
        }
    }
}
