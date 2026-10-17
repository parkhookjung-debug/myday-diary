using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;

namespace MyDay.Windows.UI
{
    // Native counterpart of the shared web design. Storage stays in DiaryWindow/Core.
    public sealed partial class DiaryWindow
    {
        private FlowLayoutPanel sidebarFlow;
        private Panel companionRail;
        private MonthCalendar sidebarCalendar;
        private readonly Label todoCount=Design.Label("",10,true),habitCount=Design.Label("",10,true);
        private readonly JournalProgress todoProgress=new JournalProgress(),habitProgress=new JournalProgress();
        private bool arrangingShell;

        private Panel BuildSidebar()
        {
            var side=new Panel { Dock=DockStyle.Left,Width=Design.P(256),BackColor=Design.Sidebar };
            sidebarFlow=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=Design.Pad(16,20,8,12),BackColor=Design.Sidebar };
            var brand=new Panel {Size=Design.Size(216,98),Margin=Design.Pad(0)};
            var name=Design.Label("✳  my day",22,true); name.ForeColor=Design.Olive; name.Location=Design.Point(4,0); brand.Controls.Add(name);
            var tagline=Design.Label("나를 위한 작은 기록",9); tagline.ForeColor=Design.Muted; tagline.Location=Design.Point(8,46); brand.Controls.Add(tagline);
            var space=Design.Label("MY SPACE",8,true); space.ForeColor=Design.Muted; space.Location=Design.Point(8,78); brand.Controls.Add(space); sidebarFlow.Controls.Add(brand);
            var today=Design.Button("오늘로 돌아가기 ↗",true); today.Size=Design.Size(216,34); today.Click+=delegate { ChangeDate(DateTime.Today); }; sidebarFlow.Controls.Add(today);
            sidebarCalendar=new MonthCalendar(book,date,picker.MinDate,picker.MaxDate,ChangeDate) {Size=Design.Size(216,262),Margin=Design.Pad(0,14,0,10)};
            sidebarFlow.Controls.Add(sidebarCalendar);
            browse.Size=Design.Size(216,32); browse.AccessibleName="달력과 일기 검색 열기"; browse.Click+=delegate { BrowseDiary(); }; sidebarFlow.Controls.Add(browse);
            var chat=Design.Button("상몬 라운지 · 채팅");chat.Size=Design.Size(216,32);chat.AccessibleName="서버 채팅 열기";chat.Click+=delegate {OpenChat();};sidebarFlow.Controls.Add(chat);
            var title=Design.Label("차곡차곡 쌓인 기록",10,true); title.Margin=Design.Pad(6,20,0,8); sidebarFlow.Controls.Add(title);
            history.Dock=DockStyle.None; history.Size=Design.Size(216,190); history.Padding=Design.Pad(0); history.BackColor=Design.Sidebar; history.Margin=Design.Pad(0); sidebarFlow.Controls.Add(history);
            var backup=new Panel {Size=Design.Size(216,190),Margin=Design.Pad(0,20,0,0)};
            var records=Design.Label("MY RECORDS",8,true); records.ForeColor=Design.Muted; records.Location=Design.Point(6,0); backup.Controls.Add(records);
            var heading=Design.Label("소중한 하루 보관하기",11,true); heading.Location=Design.Point(6,26); backup.Controls.Add(heading);
            var note=Design.Label("작은 기록도 오래 남을 수 있도록.\n가끔 백업 파일을 보관해 주세요.",9); note.ForeColor=Design.Muted; note.Location=Design.Point(6,56); backup.Controls.Add(note);
            var export=Design.Button("↓ 백업 다운로드"); export.Location=Design.Point(0,104); export.Size=Design.Size(216,32); export.Click+=delegate { ExportBook(); }; backup.Controls.Add(export);
            var import=Design.Button("↥ 백업 복원"); import.Location=Design.Point(0,144); import.Size=Design.Size(216,32); import.Click+=delegate { ImportBook(); }; backup.Controls.Add(import);
            sidebarFlow.Controls.Add(backup); side.Controls.Add(sidebarFlow); return side;
        }

        private Panel BuildCompanionRail()
        {
            companionRail=new Panel {Dock=DockStyle.Right,Width=Design.P(254),BackColor=Design.Sidebar};
            var stack=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=Design.Pad(16,20,8,16),BackColor=Design.Sidebar};
            var friend=new CardPanel {Size=Design.Size(216,396),Fill=Design.Paper,Margin=Design.Pad(0,0,0,16)};
            var eyebrow=Design.Label("YOUR LITTLE FRIEND",8,true); eyebrow.ForeColor=Design.Muted; eyebrow.Location=Design.Point(18,18); friend.Controls.Add(eyebrow);
            avatar.Location=Design.Point(38,48); avatar.Size=Design.Size(140,126); avatar.BackColor=friend.Fill; friend.Controls.Add(avatar);
            var heading=Design.Label("오늘도 네 편이야",14,true); heading.Location=Design.Point(24,186); friend.Controls.Add(heading);
            var hint=Design.Label("많이 해내지 않아도 괜찮아.\n함께하는 작은 친구, 상몬.",9); hint.ForeColor=Design.Muted; hint.Location=Design.Point(24,220); friend.Controls.Add(hint);
            characterChoice.Location=Design.Point(18,270); characterChoice.Size=Design.Size(180,30); characterChoice.AccessibleName="캐릭터 버전";
            characterChoice.Items.AddRange(MonsterVariants.All.Select(v=>(object)MonsterVariants.Name(v)));
            characterChoice.ItemEnabled=i=>SangmonGrowth.CanUse(book.Progress,(MonsterVariant)i);
            characterChoice.ItemLabel=i=>MonsterVariants.Name((MonsterVariant)i)+(characterChoice.ItemEnabled(i)?"":" · 총 "+SangmonGrowth.RequiredXP((MonsterVariant)i)+" XP");
            characterChoice.SelectedIndexChanged+=delegate { if(!binding) SelectVariant((MonsterVariant)characterChoice.SelectedIndex); }; friend.Controls.Add(characterChoice);
            growth.Location=Design.Point(18,310); growth.Size=Design.Size(104,30); growth.Font=Design.Font(8); growth.Click+=delegate { OpenGrowth(); }; friend.Controls.Add(growth);
            items.Location=Design.Point(130,310); items.Size=Design.Size(68,30); items.Font=Design.Font(8); items.Click+=delegate { OpenItems(); }; friend.Controls.Add(items);
            petToggle.Location=Design.Point(18,350); petToggle.Size=Design.Size(104,30); petToggle.Font=Design.Font(8); petToggle.Click+=delegate { TogglePet(); }; friend.Controls.Add(petToggle);
            var hide=Design.Button("창 숨기기"); hide.Location=Design.Point(130,350); hide.Size=Design.Size(68,30); hide.Font=Design.Font(8); hide.Click+=delegate { if(FlushSave()) Hide(); }; friend.Controls.Add(hide); stack.Controls.Add(friend);
            var progress=new CardPanel {Size=Design.Size(216,180),Margin=Design.Pad(0,0,0,16)};
            var progressTitle=Design.Label("차곡차곡, 오늘의 실천",10,true); progressTitle.Location=Design.Point(18,18); progress.Controls.Add(progressTitle);
            todoCount.Location=Design.Point(18,52); habitCount.Location=Design.Point(18,110); progress.Controls.Add(todoCount); progress.Controls.Add(habitCount);
            todoProgress.Location=Design.Point(18,84); habitProgress.Location=Design.Point(18,142);
            todoProgress.AccessibleName="할 일 완료 현황"; habitProgress.AccessibleName="습관 완료 현황";
            foreach(var bar in new[] {todoProgress,habitProgress}) {bar.Size=Design.Size(180,8); bar.Maximum=1; progress.Controls.Add(bar);} stack.Controls.Add(progress);
            var settings=new CardPanel {Size=Design.Size(216,184),Margin=Design.Pad(0,0,0,16)};
            var settingsTitle=Design.Label("오늘의 분위기",11,true); settingsTitle.Location=Design.Point(18,18); settings.Controls.Add(settingsTitle);
            var themeLabel=Design.Label("나와 어울리는 배경",9); themeLabel.ForeColor=Design.Muted; themeLabel.Location=Design.Point(18,52); settings.Controls.Add(themeLabel);
            theme.Items.AddRange(new object[] {"크림","숲","라벤더"}); theme.AccessibleName="배경 테마"; theme.Location=Design.Point(18,82); theme.Size=Design.Size(180,30);
            theme.SelectedIndexChanged+=delegate { if(!binding) {entry.Theme=theme.SelectedIndex; ApplyTheme(); QueueSave();} }; settings.Controls.Add(theme);
            status.MaximumSize=Design.Size(180,0); status.Font=Design.Font(8); status.ForeColor=Design.Muted; status.Location=Design.Point(18,130); settings.Controls.Add(status); stack.Controls.Add(settings);
            companionRail.Controls.Add(stack); return companionRail;
        }

        private void ArrangeShell()
        {
            if(arrangingShell || companionRail==null) return;
            arrangingShell=true;
            try {
                bool narrow=ClientSize.Width<Design.P(1120);
                if(narrow && companionRail.Parent==this) {
                    Controls.Remove(companionRail); companionRail.Dock=DockStyle.None; companionRail.Size=Design.Size(216,796); companionRail.Controls[0].Padding=Design.Pad(0); sidebarFlow.Controls.Add(companionRail);
                } else if(!narrow && companionRail.Parent!=this) {
                    sidebarFlow.Controls.Remove(companionRail); companionRail.Dock=DockStyle.Right; companionRail.Width=Design.P(254); companionRail.Controls[0].Padding=Design.Pad(16,20,8,16); Controls.Add(companionRail); companionRail.SendToBack();
                }
                // WinForms docks in reverse z-order: the fill panel must be laid out last,
                // including a wide initial window that has never moved the companion rail.
                content.BringToFront(); PerformLayout(); ArrangeCards();
            } finally {arrangingShell=false;}
        }

        private void RefreshSidebarCalendar()
        {
            if(sidebarCalendar==null) return;
            var month=sidebarCalendar.Month; var replacement=new MonthCalendar(book,date,picker.MinDate,picker.MaxDate,ChangeDate) {Size=sidebarCalendar.Size,Margin=sidebarCalendar.Margin};
            replacement.SetMonth(month); sidebarFlow.SuspendLayout(); int index=sidebarFlow.Controls.GetChildIndex(sidebarCalendar);
            sidebarFlow.Controls.Remove(sidebarCalendar); sidebarCalendar.Dispose(); sidebarCalendar=replacement; sidebarFlow.Controls.Add(replacement); sidebarFlow.Controls.SetChildIndex(replacement,index); sidebarFlow.ResumeLayout();
        }

        internal void VerifyShellForTest()
        {
            var original=Size; var originalDate=date;
            Size=Design.Size(1360,860); ArrangeShell();
            // A hosted/noninteractive desktop may cap a requested window at its screen width.
            // Validate the layout chosen for the actual client size, not the requested size.
            bool actualWide=ClientSize.Width>=Design.P(1120);
            Console.WriteLine("Smoke: requested wide client="+ClientSize+", three columns="+actualWide);
            if(actualWide) {
                if(companionRail.Parent!=this || content.Right>companionRail.Left || companionRail.Right>ClientSize.Width)
                    throw new Exception("Wide shell overlaps: content="+content.Bounds+", companion="+companionRail.Bounds+", client="+ClientSize);
            } else if(companionRail.Parent!=sidebarFlow) throw new Exception("Screen-capped window lost its compact companion rail");
            Size=MinimumSize; ArrangeShell();
            if(companionRail.Parent!=sidebarFlow || !sidebarFlow.AutoScroll || content.Width<Design.P(400)) throw new Exception("Compact shell lost accessible companion controls");
            Size=original; ArrangeShell();
            sidebarCalendar.SetMonth(originalDate.AddMonths(-1)); var target=sidebarCalendar.Month.AddDays(12);
            sidebarCalendar.FindDay(target).PerformClick();
            if(date!=target) throw new Exception("Sidebar calendar click did not navigate");
            ChangeDate(originalDate);
            foreach(int index in new[] {0,1,2}) {theme.SelectedIndex=index; if(content.BackColor!=Design.Backgrounds[index] || entry.Theme!=index) throw new Exception("Shared theme selection failed");}
            theme.SelectedIndex=0;
            var todo=entry.Blocks.First(b=>b.Kind=="todo"); bool previous=todo.Checked; todo.Checked=true; QueueSave();
            if(todoProgress.Value<1 || !todoCount.Text.Contains("1 /")) throw new Exception("Companion progress did not reflect diary checks");
            todo.Checked=previous; QueueSave(); FlushSave();
        }
    }

    public sealed class JournalProgress : Control
    {
        private int maximum=1,value;
        public int Maximum { get {return maximum;} set {maximum=Math.Max(1,value); Invalidate();} }
        public int Value { get {return value;} set {this.value=Math.Max(0,Math.Min(maximum,value)); Invalidate();} }
        public JournalProgress() {DoubleBuffered=true; BackColor=Color.White;}
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using(var track=new SolidBrush(Design.Soft)) e.Graphics.FillRectangle(track,ClientRectangle);
            using(var fill=new SolidBrush(Design.Accent)) e.Graphics.FillRectangle(fill,0,0,Width*value/maximum,Height);
            base.OnPaint(e);
        }
    }
}
