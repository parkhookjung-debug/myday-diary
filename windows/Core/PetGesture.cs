using System;
using System.Drawing;

namespace MyDay.Windows.Core
{
    // A drag remains a drag even if it returns to its starting point.
    public sealed class PetGesture
    {
        private Point start;
        public bool Active { get; private set; }
        public bool Moved { get; private set; }
        public Point Offset { get; private set; }
        public void Press(Point point) { start=point; Active=true; Moved=false; Offset=Point.Empty; }
        public void Move(Point point)
        {
            if(!Active) return;
            Offset=new Point(point.X-start.X,point.Y-start.Y);
            if(Math.Abs(Offset.X)+Math.Abs(Offset.Y)>5) Moved=true;
        }
        public bool Release(Point point)
        {
            if(!Active) return false;
            Move(point); Active=false; return !Moved;
        }
        public void Cancel() { Active=false; }
    }
}
