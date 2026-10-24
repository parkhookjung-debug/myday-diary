using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class CalendarDayButton : Button
    {
        public readonly CalendarDay Day;
        private readonly bool selected;
        private bool hover;
        public CalendarDayButton(CalendarDay day,DateTime selected,DateTime min,DateTime max)
        {
            Day=day; this.selected=day.Date==selected.Date;
            Dock=DockStyle.Fill; Margin=Design.Pad(2); FlatStyle=FlatStyle.Flat; Cursor=Cursors.Hand;
            Enabled=day.Date.HasValue && day.Date.Value>=min.Date && day.Date.Value<=max.Date;
            AccessibleName=day.Date.HasValue?day.Date.Value.ToString("yyyy년 M월 d일")+(day.HasEntry?" · 기록 있음":" · 기록 없음")+(day.HasPhoto?" · 사진 있음":""):"날짜 없음";
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g=e.Graphics; g.Clear(Color.White); if(!Day.Date.HasValue) return;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var outline=Design.Rounded(new RectangleF(1,1,Width-3,Height-3),Design.P(7))) {
                if(selected || hover && Enabled) using(var brush=new SolidBrush(selected?Design.Tint:Design.Backgrounds[0])) g.FillPath(brush,outline);
                if(Day.Date.Value==DateTime.Today) using(var pen=new Pen(Design.Accent,Design.P(1))) g.DrawPath(pen,outline);
            }
            Color ink=!Enabled || !Day.InMonth?Design.Muted:selected?Design.Accent:Design.Ink;
            TextRenderer.DrawText(g,Day.Date.Value.Day.ToString(),Design.Font(Height<Design.P(28)?7:9,selected),new Rectangle(0,0,Width,Math.Max(1,Height-Design.P(12))),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            int middle=Width/2,bottom=Height-Design.P(9);
            if(Day.HasEntry) using(var brush=new SolidBrush(Design.Accent)) g.FillEllipse(brush,middle-Design.P(Day.HasPhoto?8:2),bottom,Design.P(4),Design.P(4));
            if(Day.HasPhoto) using(var pen=new Pen(Design.Accent,Design.P(1))) {
                var frame=new Rectangle(middle+Design.P(1),bottom-Design.P(2),Design.P(8),Design.P(6));
                g.DrawRectangle(pen,frame); g.DrawLines(pen,new[] {new Point(frame.Left,frame.Bottom),new Point(frame.Left+Design.P(3),frame.Top+Design.P(2)),new Point(frame.Right,frame.Bottom)});
            }
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-Design.P(3),-Design.P(3)));
        }
    }
    public sealed class MonthCalendar : CardPanel
    {
        private readonly DiaryBook book;
        private readonly DateTime selected,min,max;
        private readonly Action<DateTime> open;
        private readonly Label heading;
        private readonly TableLayoutPanel days;
        internal readonly Button Previous,Next;
        public DateTime Month { get; private set; }
        public event EventHandler MonthChanged;
        public MonthCalendar(DiaryBook book,DateTime selected,DateTime min,DateTime max,Action<DateTime> open)
        {
            this.book=book; this.selected=selected.Date; this.min=min.Date; this.max=max.Date; this.open=open;
            Size=Design.Size(306,360); Padding=Design.Pad(14); Fill=Color.White;
            heading=Design.Label("",12,true); heading.AutoSize=false; heading.TextAlign=ContentAlignment.MiddleCenter; heading.Height=Design.P(30); heading.Top=Design.P(16); Controls.Add(heading);
            Previous=Design.Button("‹"); Previous.Size=Design.Size(30,30); Previous.Location=Design.Point(14,16); Previous.AccessibleName="달력 이전 달";
            Next=Design.Button("›"); Next.Size=Design.Size(30,30); Next.Top=Design.P(16); Next.AccessibleName="달력 다음 달";
            Previous.Click+=delegate { SetMonth(Month.AddMonths(-1)); }; Next.Click+=delegate { SetMonth(Month.AddMonths(1)); }; Controls.Add(Previous); Controls.Add(Next);
            days=new TableLayoutPanel {ColumnCount=7,RowCount=7,Margin=Design.Pad(0),BackColor=Color.White,Location=Design.Point(12,58)};
            for(int i=0;i<7;i++) { days.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/7)); days.RowStyles.Add(new RowStyle(i==0?SizeType.Absolute:SizeType.Percent,i==0?Design.P(28):100f/6)); }
            Controls.Add(days);
            Resize+=delegate { Next.Left=Width-Design.P(44); heading.Left=Design.P(48); heading.Width=Math.Max(Design.P(80),Width-Design.P(96)); days.Size=new Size(Width-Design.P(24),Height-Design.P(76)); };
            SetMonth(selected);
        }
        public void SetMonth(DateTime target)
        {
            if(target<min) target=min; if(target>max) target=max;
            Month=new DateTime(target.Year,target.Month,1); heading.Text=Month.ToString("yyyy년 M월");
            Previous.Enabled=Month>new DateTime(min.Year,min.Month,1); Next.Enabled=Month<new DateTime(max.Year,max.Month,1);
            days.SuspendLayout(); foreach(Control control in days.Controls.Cast<Control>().ToArray()) control.Dispose(); days.Controls.Clear();
            string[] names={"일","월","화","수","목","금","토"};
            for(int i=0;i<7;i++) { var label=Design.Label(names[i],8); label.ForeColor=i==0?Color.FromArgb(173,91,78):Design.Muted; label.Dock=DockStyle.Fill; label.AutoSize=false; label.TextAlign=ContentAlignment.MiddleCenter; days.Controls.Add(label,i,0); }
            var cells=DiaryBrowse.Month(book,Month);
            for(int i=0;i<cells.Count;i++) { var day=cells[i]; var button=new CalendarDayButton(day,selected,min,max);
                button.Click+=delegate { if(day.Date.HasValue) open(day.Date.Value); }; days.Controls.Add(button,i%7,i/7+1); }
            days.ResumeLayout(); if(MonthChanged!=null) MonthChanged(this,EventArgs.Empty);
        }
        internal CalendarDayButton FindDay(DateTime date) { return days.Controls.OfType<CalendarDayButton>().First(b=>b.Day.Date==date.Date); }
    }
}
