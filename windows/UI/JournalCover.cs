using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    public sealed class JournalCover : Panel
    {
        public JournalCover() { DoubleBuffered=true; BackColor=Color.FromArgb(234,241,232); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Width<4 || Height<4) return;
            var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var gradient=new LinearGradientBrush(ClientRectangle,BackColor,Color.FromArgb(248,242,224),0f)) g.FillRectangle(gradient,ClientRectangle);
            using(var pen=new Pen(Color.FromArgb(25,38,99,84),Design.P(1)))
                for(int i=0;i<7;i++) g.DrawArc(pen,Width-Design.P(235)+Design.P(i*22),-Design.P(95),Design.P(360),Design.P(250),50,190);
            base.OnPaint(e);
        }
    }
}
