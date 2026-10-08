using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    // A local diary entry, styled like a conversation. No remote messaging state.
    public sealed class HistoryRow : Button
    {
        private readonly DateTime date;
        private readonly string preview;
        private readonly bool selected;
        private bool hover;
        public HistoryRow(DateTime date,string preview,bool selected)
        {
            this.date=date; this.preview=preview; this.selected=selected;
            AccessibleName=date.ToString("yyyy년 M월 d일")+" 일기";
            Size=Design.Size(232,78); Margin=Design.Pad(0,0,0,4);
            Cursor=Cursors.Hand; TabStop=true; FlatStyle=FlatStyle.Flat;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics; g.Clear(Color.White); g.SmoothingMode=SmoothingMode.AntiAlias;
            if(selected || hover) using(var path=Design.Rounded(new RectangleF(1,1,Width-3,Height-3),Design.P(14)))
                using(var brush=new SolidBrush(selected?Design.Tint:Design.Backgrounds[0])) g.FillPath(brush,path);
            var circle=new Rectangle(Design.P(12),Design.P(18),Design.P(40),Design.P(40));
            using(var brush=new SolidBrush(selected?Design.Accent:Design.Soft)) g.FillEllipse(brush,circle);
            TextRenderer.DrawText(g,date.Day.ToString(),Design.Font(11,true),circle,selected?Color.White:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            string title=date==DateTime.Today?"오늘의 기록":date.ToString("M월 d일, dddd",new System.Globalization.CultureInfo("ko-KR"));
            TextRenderer.DrawText(g,title,Design.Font(10,true),new Rectangle(Design.P(64),Design.P(17),Width-Design.P(76),Design.P(24)),Design.Ink,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g,preview,Design.Font(9),new Rectangle(Design.P(64),Design.P(43),Width-Design.P(76),Design.P(23)),Design.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-Design.P(5),-Design.P(5)));
        }
    }
}
