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
        private readonly Label placeholder;
        private readonly Action changed;
        private Panel todoList;
        private Panel todoHeader;
        private TextBox todoInput;
        private Label todoHint;
        private Button todoAdd;
        private bool syncingTodo;
        private readonly Color todoHeaderFill;
        internal readonly PhotoView Photo;
        internal readonly Button ReplacePhoto;
        internal Button DeleteButton { get { return (Button)tools.Controls[3]; } }
        public BlockCard(DiaryBlock block, Action changed, Action<int> move, Action remove, Action resize,string pageStyle="plain",Action replacePhoto=null,int theme=0)
        {
            Block = block; this.changed=changed; Height = Design.P(DiaryLayout.DefaultHeight(block));
            Pattern=pageStyle;
            todoHeaderFill=TodoHeaderColor(theme);
            Fill = block.Kind == "emotion" ? Design.Tint : pageStyle=="paper"?Design.Paper:Color.White;
            bool isPhoto=block.Kind=="photo" || block.Kind=="photo-slot";
            string name = isPhoto ? "사진 기록" : block.Kind == "text" ? "오늘의 일기" : block.Kind == "todo" ? "To Do List" : block.Kind == "habit" ? "습관 체크" : "감정처리반";
            if(!string.IsNullOrWhiteSpace(block.Title)) name=block.Title;
            caption = isPhoto ? "PHOTO MOMENT" : block.Kind == "text" ? "DAILY JOURNAL" : block.Kind == "todo" ? "CHECK LIST · PRIORITY" : block.Kind == "habit" ? "HABIT TRACKER" : "DEAR SANGMON";
            DragHandle.Location=Design.Point(8,12); DragHandle.Height=Design.P(56); DragHandle.BackColor=Fill; Controls.Add(DragHandle);
            title = Design.Label(name, 11, true); title.Location = Design.Point(14,4); title.AutoSize=false; title.Height=Design.P(24); title.AutoEllipsis=true; DragHandle.Controls.Add(title);
            hint = Design.Label(caption, 8); hint.ForeColor = Design.Muted; hint.Location = Design.Point(14,29); DragHandle.Controls.Add(hint);
            DragHandle.Forward(title); DragHandle.Forward(hint);
            tools = new FlowLayoutPanel { Height=Design.P(30), Width=Design.P(152), Top=Design.P(14), Anchor=AnchorStyles.Top|AnchorStyles.Right, BackColor=Fill,WrapContents=false };
            var up = SmallButton("↑", "앞으로 이동"); up.Click += delegate { move(-1); };
            var down = SmallButton("↓", "뒤로 이동"); down.Click += delegate { move(1); };
            var wide = SmallButton(block.Wide ? "↙" : "↔", "블록 너비 변경"); wide.Click += delegate { resize(); };
            var del = SmallButton("×", "블록 삭제"); del.Click += delegate { remove(); };
            tools.Controls.AddRange(new Control[] { up,down,wide,del }); Controls.Add(tools);
            Editor = new TextBox { Multiline=true, AcceptsReturn=true, ScrollBars=ScrollBars.Vertical,
                Font=Design.Font(11), ForeColor=Design.Ink, BackColor=Fill, BorderStyle=BorderStyle.None,
                MaxLength=DesktopContract.MaxTextUtf16, Text=block.Text, AccessibleName=name+" 내용", Location=Design.Point(24,78),
                Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right };
            Controls.Add(Editor);
            if(block.Kind=="todo") {
                if(block.TodoItems==null) block.TodoItems=(block.Text??"").Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries).Select(line=>new DiaryTodoItem {Text=line,Checked=block.Checked}).ToList();
                todoInput=new TextBox { Font=Design.Font(10),ForeColor=Design.Ink,BackColor=Fill,BorderStyle=BorderStyle.FixedSingle,MaxLength=DesktopContract.MaxTextUtf16,AccessibleName="새 할 일 입력",AccessibleDescription="할 일을 입력하고 Enter 또는 추가 버튼을 누르세요.",Location=Design.Point(24,78),Height=Design.P(26),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right };
                todoHint=Design.Label("할 일을 입력하세요",9); todoHint.ForeColor=Design.Muted; todoHint.BackColor=Fill; todoHint.Location=Design.Point(31,82); todoHint.Click+=delegate { todoInput.Focus(); };
                todoInput.GotFocus+=delegate { todoHint.Visible=false; };
                todoInput.LostFocus+=delegate { todoHint.Visible=todoInput.Text.Length==0; };
                todoInput.TextChanged+=delegate { todoHint.Visible=todoInput.Text.Length==0 && !todoInput.Focused; };
                todoAdd=Design.Button("추가",true); todoAdd.Size=Design.Size(64,28); todoAdd.AccessibleName="할 일 추가";
                todoAdd.Click+=delegate { AddTodoItem(); };
                todoInput.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.KeyCode==Keys.Enter) { e.SuppressKeyPress=true; AddTodoItem(); } };
                todoHeader=new Panel { BackColor=todoHeaderFill,Location=Design.Point(24,112),Height=Design.P(24),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right };
                var listHeading=Design.Label("Check List",9,true); listHeading.AutoSize=false; listHeading.TextAlign=ContentAlignment.MiddleCenter; listHeading.Location=Design.Point(28,0); listHeading.Height=Design.P(24); todoHeader.Controls.Add(listHeading);
                var importanceHeading=Design.Label("중요도",9,true); importanceHeading.AutoSize=false; importanceHeading.TextAlign=ContentAlignment.MiddleCenter; importanceHeading.Location=Design.Point(0,0); importanceHeading.Height=Design.P(24); todoHeader.Controls.Add(importanceHeading);
                todoList=new Panel { AutoScroll=true,BackColor=Fill,Location=Design.Point(24,142),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right };
                Controls.Add(todoInput); Controls.Add(todoHint); Controls.Add(todoAdd); Controls.Add(todoHeader); Controls.Add(todoList); Editor.Visible=false;
                if(block.TodoItems.Count>0) block.Checked=block.TodoItems.All(item=>item.Checked);
                RefreshTodoRows();
            }
            if(isPhoto) {
                Photo=new PhotoView(block.Photo); Controls.Add(Photo);
                ReplacePhoto=Design.Button(block.Kind=="photo-slot"?"사진 선택":"사진 바꾸기"); ReplacePhoto.Size=Design.Size(90,25);
                ReplacePhoto.AccessibleName=block.Kind=="photo-slot"?"빈 사진 자리에 사진 선택":"첨부 사진 바꾸기"; ReplacePhoto.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
                ReplacePhoto.Location=new Point(Design.P(24),Height-Design.P(38));
                ReplacePhoto.Click+=delegate { if(replacePhoto!=null) replacePhoto(); }; Controls.Add(ReplacePhoto);
                Editor.ScrollBars=ScrollBars.None;
            }
            placeholder=Design.Label(isPhoto?"이 순간을 한 줄로 남겨보세요.":string.IsNullOrWhiteSpace(block.Prompt)?"오늘의 이야기를 여기에 남겨보세요.":block.Prompt,10);
            placeholder.AutoSize=false; placeholder.ForeColor=Design.Muted; placeholder.BackColor=Fill;
            placeholder.Visible=block.Kind!="todo" && string.IsNullOrEmpty(Editor.Text); placeholder.Click+=delegate { Editor.Focus(); }; Controls.Add(placeholder);
            Editor.GotFocus+=delegate { placeholder.Visible=false; };
            Editor.LostFocus+=delegate { placeholder.Visible=Editor.Text.Length==0 && !Editor.ReadOnly; };
            if (block.Kind == "todo" || block.Kind == "habit")
            {
                Check = new CheckBox { Text=block.Kind=="todo"?"목록 모두 완료":"오늘 실천했어요", Checked=block.Checked,
                    Font=Design.Font(10), ForeColor=Design.Accent, BackColor=Fill, AutoSize=true,
                    Location=new Point(Design.P(24),Height-Design.P(43)), Anchor=AnchorStyles.Left|AnchorStyles.Bottom };
                Check.CheckedChanged += delegate {
                    if(block.Kind=="todo" && !syncingTodo) {
                        syncingTodo=true;
                        try {
                            if(block.TodoItems.Count==0) block.Checked=Check.Checked;
                            else {
                                foreach(var item in block.TodoItems) item.Checked=Check.Checked;
                                foreach(Control row in todoList.Controls) { var box=row.Controls.OfType<CheckBox>().FirstOrDefault(); if(box!=null) box.Checked=Check.Checked; }
                                UpdateTodoLegacy();
                            }
                        }
                        finally { syncingTodo=false; }
                    } else block.Checked=Check.Checked;
                    changed();
                }; Controls.Add(Check);
                if(block.Kind=="todo") Check.Visible=block.TodoItems.Count>0;
            }
            else if(!isPhoto)
            {
                var footer = Design.Label(block.Kind=="emotion"?"나만 볼 수 있는 감정 기록":"나에게 · 자동 저장",8);
                footer.ForeColor=Design.Muted; footer.Location=new Point(Design.P(24),Height-Design.P(33)); footer.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; Controls.Add(footer);
            }
            Editor.TextChanged += delegate {
                placeholder.Visible=block.Kind!="todo" && Editor.Text.Length==0 && !Editor.Focused && !Editor.ReadOnly;
                if(block.Kind=="todo") { if(!syncingTodo) ReplaceTodoItemsFromEditor(); }
                else { block.Text=Editor.Text; changed(); }
            };
            ResizeHandle.Size=Design.Size(24,24); ResizeHandle.BackColor=Fill; ResizeHandle.AccessibleName="블록 크기 조절";
            var grip=Design.Label("↘",12,true); grip.ForeColor=Design.Accent; ResizeHandle.Controls.Add(grip); ResizeHandle.Forward(grip); Controls.Add(ResizeHandle);
            Resize += delegate {
                tools.Left=Width-tools.Width-Design.P(16); DragHandle.Width=Math.Max(Design.P(80),Width-tools.Width-Design.P(36));
                hint.AutoSize=false; hint.Width=Math.Max(Design.P(40),DragHandle.Width-Design.P(20)); hint.Height=Design.P(22); hint.AutoEllipsis=true;
                title.Width=hint.Width;
                Editor.Size=new Size(Math.Max(Design.P(80),Width-Design.P(48)),Math.Max(Design.P(20),Height-Design.P(140)));
                if(Photo!=null) {
                    int photoHeight=Math.Max(Design.P(35),Height-Design.P(185));
                    Photo.Bounds=new Rectangle(Design.P(24),Design.P(73),Math.Max(Design.P(80),Width-Design.P(48)),photoHeight);
                    Editor.Bounds=new Rectangle(Design.P(24),Photo.Bottom+Design.P(8),Photo.Width,Math.Max(Design.P(25),Height-Photo.Bottom-Design.P(56)));
                }
                if(todoList!=null) {
                    todoInput.Bounds=new Rectangle(Design.P(24),Design.P(78),Math.Max(Design.P(70),Width-Design.P(104)),Design.P(26));
                    todoHint.Location=new Point(Design.P(31),Design.P(82));
                    todoAdd.Location=new Point(Width-Design.P(88),Design.P(77));
                    todoHeader.Bounds=new Rectangle(Design.P(24),Design.P(112),Math.Max(Design.P(80),Width-Design.P(48)),Design.P(24));
                    var listHeading=todoHeader.Controls[0]; listHeading.Width=Math.Max(Design.P(40),todoHeader.Width-Design.P(100));
                    var importanceHeading=todoHeader.Controls[1]; importanceHeading.Location=new Point(todoHeader.Width-Design.P(84),0); importanceHeading.Width=Design.P(72);
                    todoList.Bounds=new Rectangle(Design.P(24),Design.P(142),Math.Max(Design.P(80),Width-Design.P(48)),Math.Max(Design.P(28),Height-Design.P(196)));
                    RefreshTodoRows();
                }
                placeholder.Bounds=new Rectangle(Editor.Location,Editor.Size);
                ResizeHandle.Location=new Point(Width-Design.P(34),Height-Design.P(34)); ResizeHandle.BringToFront(); tools.BringToFront();
            };
            SetEditing(false);
        }
        public void SetEditing(bool editing,bool free=false)
        {
            foreach(Control tool in tools.Controls.Cast<Control>().Take(3)) tool.Visible=!free;
            tools.Width=Design.P(free?38:152); tools.Left=Width-tools.Width-Design.P(16);
            DragHandle.Width=Math.Max(Design.P(80),Width-tools.Width-Design.P(36));
            hint.Width=Math.Max(Design.P(40),DragHandle.Width-Design.P(20));
            title.Width=hint.Width;
            tools.PerformLayout();
            DragHandle.Editing=ResizeHandle.Editing=editing; ResizeHandle.Visible=editing;
            DragHandle.Cursor=title.Cursor=hint.Cursor=editing?Cursors.SizeAll:Cursors.Default;
            ResizeHandle.Cursor=Cursors.SizeNWSE; Editor.ReadOnly=editing;
            if(ReplacePhoto!=null) ReplacePhoto.Enabled=!editing;
            if(todoInput!=null) { todoInput.Enabled=!editing; todoAdd.Enabled=!editing; }
            if(todoList!=null) todoList.Enabled=!editing;
            if(todoHint!=null) todoHint.Visible=!editing && todoInput.Text.Length==0 && !todoInput.Focused;
            placeholder.Visible=Block.Kind!="todo" && Editor.Text.Length==0 && !editing && !Editor.Focused;
            hint.Text=editing?"제목으로 이동 · ↘로 크기 조절":caption;
        }
        private static Button SmallButton(string label,string accessible)
        {
            var b=Design.Button(label); b.Size=Design.Size(30,26); b.Margin=Design.Pad(0,0,6,0);
            b.FlatAppearance.BorderSize=0; b.AccessibleName=accessible; return b;
        }
        private void AddTodoItem()
        {
            string value=todoInput.Text.Trim();
            if(value.Length==0) { todoInput.Focus(); return; }
            if(Block.TodoItems.Count>=100) { todoInput.Enabled=false; todoAdd.Enabled=false; return; }
            Block.TodoItems.Add(new DiaryTodoItem {Text=value,Importance=0}); todoInput.Clear(); UpdateTodoLegacy(); RefreshTodoRows();
            todoList.AutoScrollPosition=new Point(0,todoList.VerticalScroll.Maximum); changed(); todoInput.Focus();
        }
        private void ReplaceTodoItemsFromEditor()
        {
            var old=Block.TodoItems??new System.Collections.Generic.List<DiaryTodoItem>();
            var lines=Editor.Text.Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries);
            Block.TodoItems=lines.Take(100).Select((line,index)=>new DiaryTodoItem {Text=line.TrimEnd('\r'),Checked=index<old.Count && old[index].Checked,Importance=index<old.Count?old[index].Importance:0}).ToList();
            UpdateTodoLegacy(); RefreshTodoRows(); changed();
        }
        private void UpdateTodoLegacy()
        {
            Block.Text=String.Join("\n",Block.TodoItems.Select(item=>item.Text));
            Block.Checked=Block.TodoItems.Count>0 && Block.TodoItems.All(item=>item.Checked);
            syncingTodo=true;
            try { if(Editor.Text!=Block.Text) Editor.Text=Block.Text; if(Check!=null) Check.Checked=Block.Checked; }
            finally { syncingTodo=false; }
        }
        private void RefreshTodoRows()
        {
            if(todoList==null) return;
            foreach(Control control in todoList.Controls.Cast<Control>().ToArray()) { todoList.Controls.Remove(control); control.Dispose(); }
            int rowHeight=Design.P(34), rowWidth=Math.Max(Design.P(120),todoList.ClientSize.Width-Design.P(8));
            for(int i=0;i<Block.TodoItems.Count;i++) {
                var item=Block.TodoItems[i];
                var row=new Panel {Location=new Point(0,i*rowHeight),Size=new Size(rowWidth,rowHeight),BackColor=Fill};
                var check=new CheckBox {Checked=item.Checked,AutoSize=false,Size=Design.Size(22,26),Location=Point.Empty,Tag=item,BackColor=Fill,ForeColor=Design.Ink,AccessibleName=item.Text+" 완료"};
                int textWidth=Math.Max(Design.P(56),rowWidth-Design.P(120));
                var text=new TextBox {Text=item.Text,Font=Design.Font(10),ForeColor=Design.Ink,BackColor=Fill,BorderStyle=BorderStyle.None,Location=new Point(Design.P(30),Design.P(3)),Size=new Size(textWidth,Design.P(25)),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,MaxLength=DesktopContract.MaxTextUtf16,AccessibleName="할 일 내용"};
                var importance=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Font=Design.Font(9),ForeColor=ImportanceColor(item.Importance),BackColor=todoHeaderFill,Location=new Point(rowWidth-Design.P(88),Design.P(2)),Size=Design.Size(62,25),Anchor=AnchorStyles.Top|AnchorStyles.Right,AccessibleName="할 일 중요도"};
                importance.Items.AddRange(new object[]{"상","중","하"}); importance.SelectedIndex=item.Importance==1?0:item.Importance==2?2:1;
                var remove=Design.Button("×"); remove.BackColor=Fill; remove.ForeColor=Design.Muted; remove.Size=Design.Size(24,24); remove.Location=new Point(rowWidth-Design.P(25),Design.P(1)); remove.Anchor=AnchorStyles.Top|AnchorStyles.Right; remove.AccessibleName="할 일 삭제";
                var textRule=new Panel {BackColor=Design.Soft,Location=new Point(Design.P(29),Design.P(30)),Size=new Size(textWidth,Design.P(1)),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
                var importanceRule=new Panel {BackColor=Design.Soft,Location=new Point(rowWidth-Design.P(88),Design.P(30)),Size=Design.Size(62,1),Anchor=AnchorStyles.Top|AnchorStyles.Right};
                check.CheckedChanged+=delegate { if(syncingTodo) return; item.Checked=check.Checked; UpdateTodoLegacy(); changed(); };
                text.TextChanged+=delegate { if(syncingTodo) return; item.Text=text.Text; UpdateTodoLegacy(); changed(); };
                importance.SelectedIndexChanged+=delegate { if(syncingTodo || importance.SelectedIndex<0) return; item.Importance=importance.SelectedIndex==0?1:importance.SelectedIndex==2?2:0; importance.ForeColor=ImportanceColor(item.Importance); changed(); };
                remove.Click+=delegate { Block.TodoItems.Remove(item); if(Block.TodoItems.Count<100) { todoInput.Enabled=true; todoAdd.Enabled=true; } UpdateTodoLegacy(); RefreshTodoRows(); changed(); };
                row.Controls.AddRange(new Control[]{check,text,importance,remove,textRule,importanceRule}); todoList.Controls.Add(row);
            }
            todoList.AutoScrollMinSize=new Size(0,Block.TodoItems.Count*rowHeight);
            if(Check!=null) Check.Visible=Block.TodoItems.Count>0;
            if(Check!=null && Block.TodoItems.Count>0 && Check.Checked!=Block.TodoItems.All(item=>item.Checked)) Check.Checked=Block.TodoItems.All(item=>item.Checked);
        }
        public void FocusInput()
        {
            if(Block.Kind=="todo" && todoInput!=null) todoInput.Focus(); else Editor.Focus();
        }
        private static Color ImportanceColor(int importance) { return importance==1?Design.Accent:importance==2?Design.Muted:Design.Olive; }
        private static Color TodoHeaderColor(int theme) { return theme==1?Color.FromArgb(224,233,218):theme==2?Color.FromArgb(235,226,243):Color.FromArgb(244,231,218); }
    }
}
