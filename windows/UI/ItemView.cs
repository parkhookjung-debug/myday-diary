using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class ItemView : Control
    {
        private readonly Timer timer=new Timer {Interval=50};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        public SangmonItem Item;
        public ItemView()
        {
            DoubleBuffered=true; timer.Tick+=delegate {Invalidate();};
            HandleCreated+=delegate {if(Visible)timer.Start();};
            VisibleChanged+=delegate {if(Visible)timer.Start();else timer.Stop();};
        }
        protected override void OnPaint(PaintEventArgs e) {base.OnPaint(e);ItemPainter.Draw(e.Graphics,ClientRectangle,Item,clock.Elapsed.TotalSeconds);}
        protected override void Dispose(bool disposing) {if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
