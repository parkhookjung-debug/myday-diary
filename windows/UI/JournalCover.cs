using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    public sealed class JournalCover : Panel
    {
        public JournalCover() { DoubleBuffered=true; BackColor=Design.Backgrounds[0]; }
        protected override void OnPaint(PaintEventArgs e)
        {
            if(Width<4 || Height<4) return;
            var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            base.OnPaint(e);
        }
    }
}
