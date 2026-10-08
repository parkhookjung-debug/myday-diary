using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class BlockCard : CardPanel
    {
        public readonly DiaryBlock Block;
        public readonly TextBox Editor;
        public readonly CheckBox Check;
        public readonly PlacementHandle DragHandle=new PlacementHandle(),ResizeHandle=new PlacementHandle();
        private readonly Label title,hint;
        private readonly string caption;
        private readonly FlowLayoutPanel tools;
        internal Button DeleteButton { get { return (Button)tools.Controls[3]; } }
        public BlockCard(DiaryBlock block, Action changed, Action<int> move, Action remove, Action resize)
        {
            Block = block; Height = Design.P(block.Kind == "emotion" ? 288 : 250);
            Fill = block.Kind == "emotion" ? Color.FromArgb(255,248,239) : Color.White;
            string name = block.Kind == "text" ? "오늘의 일기" : block.Kind == "todo" ? "할 일" : block.Kind == "habit" ? "습관 체크" : "감정처리반";
            caption = block.Kind == "text" ? "기억하고 싶은 오늘의 장면" : block.Kind == "todo" ? "작은 일부터 하나씩" : block.Kind == "habit" ? "나를 위한 작은 약속" : "몬스터에게 마음을 털어놓아요";
            DragHandle.Location=Design.Point(8,14); DragHandle.Height=Design.P(64); DragHandle.BackColor=Fill; Controls.Add(DragHandle);
            title = Design.Label(name, 12, true); title.Location = Design.Point(14,6); DragHandle.Controls.Add(title);
            hint = Design.Label(caption, 9); hint.ForeColor = Design.Muted; hint.Location = Design.Point(14,34); DragHandle.Controls.Add(hint);
            DragHandle.Forward(title); DragHandle.Forward(hint);
            tools = new FlowLayoutPanel { Height=Design.P(30), Width=Design.P(152), Top=Design.P(14), Anchor=AnchorStyles.Top|AnchorStyles.Right, BackColor=Fill,WrapContents=false };
            var up = SmallButton("↑", "앞으로 이동"); up.Click += delegate { move(-1); };
            var down = SmallButton("↓", "뒤로 이동"); down.Click += delegate { move(1); };
            var wide = SmallButton(block.Wide ? "↙" : "↔", "블록 너비 변경"); wide.Click += delegate { resize(); };
            var del = SmallButton("×", "블록 삭제"); del.Click += delegate { remove(); };
            tools.Controls.AddRange(new Control[] { up,down,wide,del }); Controls.Add(tools);
            Editor = new TextBox { Multiline=true, AcceptsReturn=true, ScrollBars=ScrollBars.Vertical,
                Font=Design.Font(11), ForeColor=Design.Ink, BackColor=Fill, BorderStyle=BorderStyle.None,
                MaxLength=100000, Text=block.Text, AccessibleName=name+" 내용", Location=Design.Point(24,84),
                Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right };
            Controls.Add(Editor);
            if (block.Kind == "todo" || block.Kind == "habit")
            {
                Check = new CheckBox { Text=block.Kind=="todo"?"완료했어요":"오늘 실천했어요", Checked=block.Checked,
                    Font=Design.Font(10), ForeColor=Design.Accent, BackColor=Fill, AutoSize=true,
                    Location=new Point(Design.P(24),Height-Design.P(43)), Anchor=AnchorStyles.Left|AnchorStyles.Bottom };
                Check.CheckedChanged += delegate { block.Checked=Check.Checked; changed(); }; Controls.Add(Check);
            }
            else
            {
                var footer = Design.Label(block.Kind=="emotion"?"답변 없이, 내 마음을 남기는 공간이에요.":"완벽한 문장보다 솔직한 한 줄이면 충분해요.",8);
                footer.ForeColor=Design.Muted; footer.Location=new Point(Design.P(24),Height-Design.P(33)); footer.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; Controls.Add(footer);
            }
            Editor.TextChanged += delegate { block.Text=Editor.Text; changed(); };
            ResizeHandle.Size=Design.Size(24,24); ResizeHandle.BackColor=Fill; ResizeHandle.AccessibleName="블록 크기 조절";
            var grip=Design.Label("↘",12,true); grip.ForeColor=Design.Accent; ResizeHandle.Controls.Add(grip); ResizeHandle.Forward(grip); Controls.Add(ResizeHandle);
            Resize += delegate {
                tools.Left=Width-tools.Width-Design.P(16); DragHandle.Width=Math.Max(Design.P(80),Width-Design.P(24));
                Editor.Size=new Size(Math.Max(Design.P(80),Width-Design.P(48)),Math.Max(Design.P(20),Height-Design.P(140)));
                ResizeHandle.Location=new Point(Width-Design.P(34),Height-Design.P(34)); ResizeHandle.BringToFront(); tools.BringToFront();
            };
            SetEditing(false);
        }
        public void SetEditing(bool editing,bool free=false)
        {
            foreach(Control tool in tools.Controls.Cast<Control>().Take(3)) tool.Visible=!free;
            tools.Width=Design.P(free?38:152); tools.Left=Width-tools.Width-Design.P(16);
            tools.PerformLayout();
            DragHandle.Editing=ResizeHandle.Editing=editing; ResizeHandle.Visible=editing;
            DragHandle.Cursor=title.Cursor=hint.Cursor=editing?Cursors.SizeAll:Cursors.Default;
            ResizeHandle.Cursor=Cursors.SizeNWSE; Editor.ReadOnly=editing;
            hint.Text=editing?"제목으로 이동 · ↘로 크기 조절":caption;
        }
        private static Button SmallButton(string label,string accessible)
        {
            var b=Design.Button(label); b.Size=Design.Size(30,26); b.Margin=Design.Pad(0,0,6,0);
            b.FlatAppearance.BorderSize=0; b.AccessibleName=accessible; return b;
        }
    }
}
