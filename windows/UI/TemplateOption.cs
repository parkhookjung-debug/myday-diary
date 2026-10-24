using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class TemplateOption : Button
    {
        public readonly JournalTemplate Template;
        public bool Active;
        public TemplateOption(JournalTemplate template)
        {
            Template=template; Tag=template; Text=template.Name; AccessibleName=template.Name+" 일기 형식";
            Size=Design.Size(224,60); Margin=Design.Pad(0,0,0,6); Cursor=Cursors.Hand;
            FlatStyle=FlatStyle.Flat; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics; g.Clear(Color.White); g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=Design.Rounded(new RectangleF(1,1,Width-3,Height-3),Design.P(10)))
            using(var brush=new SolidBrush(Active?Design.Tint:Color.White)) g.FillPath(brush,path);
            if(Active) using(var pen=new Pen(Design.Accent,Design.P(2))) g.DrawLine(pen,Design.P(8),Design.P(16),Design.P(8),Height-Design.P(16));
            TextRenderer.DrawText(g,Template.Name,Design.Font(10,true),new Rectangle(Design.P(18),Design.P(8),Width-Design.P(28),Design.P(25)),Design.Ink,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g,Template.Minutes+"분 · "+JournalLayouts.Name(Template.Layout),Design.Font(8),new Rectangle(Design.P(18),Design.P(34),Width-Design.P(28),Design.P(20)),Design.Muted,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-Design.P(5),-Design.P(5)));
        }
    }
    public sealed class TemplateDiagram : Control
    {
        public JournalTemplate Template;
        public TemplateDiagram() { DoubleBuffered=true; BackColor=Color.White; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if(Template==null) return;
            var boxes=JournalLayouts.Arrange(Template.Layout,Template.Sections.Length,800);
            int right=0,bottom=0; foreach(var box in boxes) { right=System.Math.Max(right,box.Right); bottom=System.Math.Max(bottom,box.Bottom); }
            float scale=System.Math.Min((Width-Design.P(28))/(float)right,(Height-Design.P(16))/(float)bottom);
            float left=(Width-right*scale)/2,top=(Height-bottom*scale)/2;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            for(int i=0;i<boxes.Count;i++) {
                var b=boxes[i]; var r=new RectangleF(left+b.X*scale,top+b.Y*scale,b.Width*scale,b.Height*scale);
                using(var path=Design.Rounded(r,System.Math.Min(Design.P(6),r.Height/4)))
                using(var brush=new SolidBrush(Template.Sections[i].Kind=="emotion"?Design.Tint:Design.Paper))
                using(var pen=new Pen(Design.Soft)) { e.Graphics.FillPath(brush,path); e.Graphics.DrawPath(pen,path); }
                TextRenderer.DrawText(e.Graphics,(i+1).ToString(),Design.Font(8,true),Rectangle.Round(r),Design.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
        }
    }
}
