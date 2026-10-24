using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Chat;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    // Draw plain text directly: pasted markup never becomes HTML or executable links.
    internal sealed class ChatTranscript : ScrollableControl
    {
        private sealed class Row {public ChatMessage Message;public bool Mine;public Rectangle Bounds;public string Header;}
        private readonly List<Row> rows=new List<Row>();
        private const TextFormatFlags Wrap=TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPrefix;
        public ChatTranscript() {DoubleBuffered=true;AutoScroll=true;BackColor=ColorTranslator.FromHtml(DesktopContract.ChatBackground);AccessibleName="채팅 메시지";}
        public void Clear() {rows.Clear();Text="";AutoScrollMinSize=Size.Empty;Invalidate();}
        public void Append(ChatMessage message,bool mine)
        {
            DateTimeOffset time;string label=DateTimeOffset.TryParse(message.sentAt,out time)?time.ToLocalTime().ToString("HH:mm"):"";
            rows.Add(new Row {Message=message,Mine=mine,Header=message.nickname+" · "+label});
            if(rows.Count>DesktopContract.ChatVisibleHistory)rows.RemoveAt(0);
            Text=string.Join(Environment.NewLine,rows.Select(r=>r.Header+Environment.NewLine+r.Message.text));
            Reflow();AutoScrollPosition=new Point(0,AutoScrollMinSize.Height);Invalidate();
        }
        private void Reflow()
        {
            if(rows==null)return;
            int available=Math.Max(Design.P(140),ClientSize.Width-SystemInformation.VerticalScrollBarWidth-Design.P(24));
            int limit=Math.Max(Design.P(110),available*82/100),y=Design.P(12);
            foreach(var row in rows) {
                var body=TextRenderer.MeasureText(row.Message.text,Font,new Size(limit-Design.P(28),int.MaxValue),Wrap);
                var header=TextRenderer.MeasureText(row.Header,Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
                int width=Math.Min(limit,Math.Max(body.Width,header.Width)+Design.P(28));
                row.Bounds=new Rectangle(row.Mine?available-width:Design.P(4),y,width,body.Height+Design.P(48));y+=row.Bounds.Height+Design.P(14);
            }
            AutoScrollMinSize=new Size(0,y+Design.P(8));
        }
        protected override void OnResize(EventArgs e) {base.OnResize(e);Reflow();Invalidate();}
        protected override void OnFontChanged(EventArgs e) {base.OnFontChanged(e);Reflow();Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(rows.Count==0) {
                TextRenderer.DrawText(e.Graphics,"입장해서 첫 이야기를 남겨보세요.",Font,new Rectangle(Design.P(18),Design.P(24),Math.Max(100,ClientSize.Width-Design.P(36)),Design.P(48)),Design.Muted,Wrap);return;
            }
            foreach(var row in rows) {
                var box=row.Bounds;box.Offset(AutoScrollPosition);
                if(box.Bottom<0 || box.Top>ClientSize.Height)continue;
                int diameter=Design.P(20);
                using(var path=new GraphicsPath()) {
                    path.AddArc(box.X,box.Y,diameter,diameter,180,90);path.AddArc(box.Right-diameter,box.Y,diameter,diameter,270,90);
                    path.AddArc(box.Right-diameter,box.Bottom-diameter,diameter,diameter,0,90);path.AddArc(box.X,box.Bottom-diameter,diameter,diameter,90,90);path.CloseFigure();
                    using(var fill=new SolidBrush(ColorTranslator.FromHtml(row.Mine?DesktopContract.ChatMine:DesktopContract.ChatOther)))e.Graphics.FillPath(fill,path);
                }
                TextRenderer.DrawText(e.Graphics,row.Header,Font,new Rectangle(box.X+Design.P(14),box.Y+Design.P(10),box.Width-Design.P(28),Design.P(22)),row.Mine?Design.Accent:Design.Muted,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(e.Graphics,row.Message.text,Font,new Rectangle(box.X+Design.P(14),box.Y+Design.P(34),box.Width-Design.P(28),box.Height-Design.P(42)),Design.Ink,Wrap);
            }
        }
    }
}
