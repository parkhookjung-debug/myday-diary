using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class SavedLayoutDiagram : Control
    {
        public SavedLayout Template;
        public SavedLayoutDiagram() { BackColor=Color.White; DoubleBuffered=true; AccessibleName="내 레이아웃 실제 배치 미리보기"; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if(Template==null) return;
            int right=Template.Blocks.Max(b=>b.X+b.Width),bottom=Template.Blocks.Max(b=>b.Y+b.Height);
            float scale=System.Math.Min((Width-Design.P(24))/(float)right,(Height-Design.P(16))/(float)bottom);
            float left=(Width-right*scale)/2,top=(Height-bottom*scale)/2; e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            for(int i=0;i<Template.Blocks.Count;i++) {
                var block=Template.Blocks[i]; var r=new RectangleF(left+block.X*scale,top+block.Y*scale,block.Width*scale,block.Height*scale);
                using(var path=Design.Rounded(r,System.Math.Max(.2f,System.Math.Min(Design.P(6),System.Math.Min(r.Height,r.Width)/4))))
                using(var brush=new SolidBrush(block.Kind=="photo-slot" || block.Kind=="emotion"?Design.Tint:Design.Paper))
                using(var pen=new Pen(Design.Soft)) { e.Graphics.FillPath(brush,path); e.Graphics.DrawPath(pen,path); }
                if(r.Height>=Design.P(15) && r.Width>=Design.P(20)) TextRenderer.DrawText(e.Graphics,(i+1).ToString(),Design.Font(8,true),Rectangle.Round(r),Design.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
        }
    }
}
