using System;
using System.Drawing;

namespace MyDay.Windows.Character
{
    public enum PetActivity { Rest, Walk, Hop, Look, Sleep, Hover, Fire, Drag, Yawn }

    // Behavior and motion are independent of window handles and can be replayed in tests.
    public sealed class PetBehavior
    {
        public const double TouchFireSeconds=.7;
        public const double YawnSeconds=1.4;
        public const double YawnCooldownSeconds=30;
        private readonly Random random;
        private PetActivity scheduled = PetActivity.Hop;
        private double remaining = 2.1, fireRemaining, landingRemaining, yawnCooldown=20;
        private float targetX = -34, targetY = 8;
        public PetActivity Activity { get; private set; }
        public double Age { get; private set; }
        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }
        public bool FacingLeft { get; private set; }
        public PetBehavior(int seed) { random=new Random(seed); FacingLeft=true; Activity=PetActivity.Hop; }
        public void Fire() { fireRemaining=TouchFireSeconds; if(Activity==PetActivity.Fire) Age=0; }
        public void Land() { landingRemaining=.7; if(Activity==PetActivity.Hop) Age=0; }
        public void Hop() { landingRemaining=1.9; if(Activity==PetActivity.Hop) Age=0; }
        public PointF Step(double delta, bool roaming, bool held, bool hover)
        {
            delta=Math.Max(0,Math.Min(.1,delta));
            yawnCooldown=Math.Max(0,yawnCooldown-delta);
            if(scheduled==PetActivity.Yawn && (held || hover || fireRemaining>0 || !roaming)) {
                scheduled=PetActivity.Rest; remaining=1.5;
            }
            PetActivity next;
            if(held) { fireRemaining=0; landingRemaining=0; next=PetActivity.Drag; }
            else if(fireRemaining>0) next=PetActivity.Fire;
            else if(landingRemaining>0) next=PetActivity.Hop;
            else if(hover) next=PetActivity.Hover;
            else if(!roaming) next=PetActivity.Rest;
            else {
                remaining-=delta;
                if(remaining<=0) ChooseNext();
                next=scheduled;
            }
            if(Activity!=next) { Activity=next; Age=0; } else Age+=delta;
            if(!held) { fireRemaining=Math.Max(0,fireRemaining-delta); if(next==PetActivity.Hop) landingRemaining=Math.Max(0,landingRemaining-delta); }
            bool travel=roaming && !held && !hover && fireRemaining<=0 && landingRemaining<=0 && (Activity==PetActivity.Walk || Activity==PetActivity.Hop);
            float speed=Activity==PetActivity.Hop?1.35f:1;
            float blend=(float)(1-Math.Exp(-delta*9));
            VelocityX+=((travel?targetX*speed:0)-VelocityX)*blend;
            VelocityY+=((travel?targetY*speed:0)-VelocityY)*blend;
            if(travel && Math.Abs(VelocityX)>6) FacingLeft=VelocityX<0;
            if(held || hover || Activity==PetActivity.Fire || Activity==PetActivity.Yawn || !roaming) return PointF.Empty;
            return new PointF(VelocityX*(float)delta,VelocityY*(float)delta);
        }
        public void Bounce(bool horizontal,bool vertical)
        {
            if(horizontal) { targetX=-targetX; VelocityX=-VelocityX; FacingLeft=targetX<0; }
            if(vertical) { targetY=-targetY; VelocityY=-VelocityY; }
        }
        private void ChooseNext()
        {
            int choice=random.Next(100);
            scheduled=choice<48?PetActivity.Walk:choice<68?PetActivity.Hop:choice<84?PetActivity.Look:choice<94?PetActivity.Rest:PetActivity.Sleep;
            if(choice>=94 && yawnCooldown<=0) { scheduled=PetActivity.Yawn; yawnCooldown=YawnCooldownSeconds; }
            remaining=scheduled==PetActivity.Walk?3+random.NextDouble()*3:scheduled==PetActivity.Sleep?2.4:scheduled==PetActivity.Yawn?YawnSeconds:1.5+random.NextDouble();
            double angle=random.NextDouble()*Math.PI*2;
            targetX=(float)Math.Cos(angle)*38; targetY=(float)Math.Sin(angle)*18;
        }
    }
}
