using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class JournalBrowser : Form
    {
        private readonly DiaryBook book;
        private readonly DateTime current,min,max;
        private readonly MonthCalendar calendar;
        private readonly TextBox search;
        private readonly Label placeholder,count,monthSummary;
        private readonly FlowLayoutPanel results;
        private readonly Button clear,previous,next;
        private readonly Label pageCount;
        private readonly Timer searchTimer=new Timer {Interval=250};
        private readonly ToolTip tips=new ToolTip();
        private List<DiaryMatch> matches=new List<DiaryMatch>();
        private int page;
        private bool resizingResults;
        public DateTime? SelectedDate { get; private set; }
        public JournalBrowser(DiaryBook book,DateTime current,DateTime min,DateTime max)
        {
            this.book=book; this.current=current.Date; this.min=min.Date; this.max=max.Date;
            Text="MyDay · 달력과 일기 검색"; Font=Design.Font(10); ForeColor=Design.Ink; BackColor=Design.Backgrounds[0];
            AutoScaleMode=AutoScaleMode.None; ShowInTaskbar=false; StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(980),work.Width-32),Math.Min(Design.P(680),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(740),work.Width-32),Math.Min(Design.P(550),work.Height-32));
            var header=new Panel {Dock=DockStyle.Top,Height=Design.P(92)};
            var eyebrow=Design.Label("MY JOURNAL ARCHIVE",8,true); eyebrow.ForeColor=Design.Accent; eyebrow.Location=Design.Point(24,14); header.Controls.Add(eyebrow);
            var title=Design.Label("나의 기록 찾기",21,true); title.Location=Design.Point(22,36); header.Controls.Add(title);
            var footer=new Panel {Dock=DockStyle.Bottom,Height=Design.P(58)};
            var note=Design.Label("날짜나 검색 결과를 누르면 일기가 열려요.",9); note.ForeColor=Design.Muted; note.Location=Design.Point(24,19); footer.Controls.Add(note);
            var close=Design.Button("닫기"); close.Width=Design.P(74); close.DialogResult=DialogResult.Cancel; footer.Controls.Add(close); CancelButton=close;
            footer.Resize+=delegate { close.Location=new Point(footer.Width-Design.P(98),Design.P(10)); };
            var grid=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=Design.Pad(20,0,20,0),BackColor=BackColor};
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(320))); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            var left=new Panel {Dock=DockStyle.Fill,Margin=Design.Pad(0,0,14,0)};
            calendar=new MonthCalendar(book,current,min,max,OpenDate) {Dock=DockStyle.Top,Height=Design.P(340)}; left.Controls.Add(calendar);
            var leftFooter=new Panel {Dock=DockStyle.Top,Height=Design.P(92)};
            var legend=Design.Label("● 저장한 기록   ▧ 사진 있는 날",8); legend.ForeColor=Design.Muted; legend.Location=Design.Point(8,8); leftFooter.Controls.Add(legend);
            monthSummary=Design.Label("",9,true); monthSummary.ForeColor=Design.Accent; monthSummary.Location=Design.Point(8,32); leftFooter.Controls.Add(monthSummary);
            var today=Design.Button("이번 달"); today.Size=Design.Size(80,28); today.Location=Design.Point(8,57); today.Click+=delegate { calendar.SetMonth(DateTime.Today); }; leftFooter.Controls.Add(today);
            left.Controls.Add(leftFooter); leftFooter.BringToFront();
            left.Resize+=delegate { calendar.Height=Math.Max(Design.P(260),Math.Min(Design.P(340),left.ClientSize.Height-Design.P(92))); };
            var right=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Design.Pad(0)};
            right.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(54))); right.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(40)));
            right.RowStyles.Add(new RowStyle(SizeType.Percent,100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(40)));
            var searchCard=new CardPanel {Dock=DockStyle.Fill,Margin=Design.Pad(0),Padding=Design.Pad(0),Fill=Color.White};
            search=new TextBox {Font=Design.Font(11),ForeColor=Design.Ink,BackColor=Color.White,BorderStyle=BorderStyle.None,MaxLength=200,Location=Design.Point(16,16),AccessibleName="일기 본문과 사진 설명 검색"}; searchCard.Controls.Add(search);
            placeholder=Design.Label("본문·사진 설명에서 찾기",10); placeholder.ForeColor=Design.Muted; placeholder.Location=Design.Point(16,16); placeholder.Click+=delegate { search.Focus(); }; searchCard.Controls.Add(placeholder);
            clear=Design.Button("×"); clear.Size=Design.Size(30,30); clear.Top=Design.P(12); clear.AccessibleName="검색어 지우기"; clear.Click+=delegate { search.Clear(); search.Focus(); }; searchCard.Controls.Add(clear);
            searchCard.Resize+=delegate { clear.Left=searchCard.Width-Design.P(42); search.Width=Math.Max(Design.P(100),clear.Left-Design.P(28)); placeholder.MaximumSize=new Size(search.Width,Design.P(24)); };
            search.GotFocus+=delegate { placeholder.Visible=search.Text.Length==0; }; search.LostFocus+=delegate { placeholder.Visible=search.Text.Length==0; };
            search.TextChanged+=delegate { placeholder.Visible=search.Text.Length==0; clear.Visible=search.Text.Length>0; searchTimer.Stop(); searchTimer.Start(); };
            searchTimer.Tick+=delegate { RunSearch(); }; search.KeyDown+=OnSearchKey;
            tips.SetToolTip(search,"검색어를 입력하면 모든 날짜의 본문과 사진 설명에서 찾아요.");
            count=Design.Label("",9); count.ForeColor=Design.Muted; count.Margin=Design.Pad(4,12,0,0);
            results=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=Design.Pad(0),Margin=Design.Pad(0),BackColor=BackColor};
            results.Resize+=delegate { ResizeResults(); };
            var paging=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false,Padding=Design.Pad(0,6,0,0),Margin=Design.Pad(0)};
            previous=Design.Button("‹"); previous.Size=Design.Size(30,28); previous.AccessibleName="검색 이전 페이지"; previous.Click+=delegate { page--; RenderResults(); };
            next=Design.Button("›"); next.Size=Design.Size(30,28); next.AccessibleName="검색 다음 페이지"; next.Click+=delegate { page++; RenderResults(); };
            pageCount=Design.Label("",8); pageCount.Margin=Design.Pad(8,5,12,0); paging.Controls.AddRange(new Control[] {previous,pageCount,next});
            right.Controls.Add(searchCard,0,0); right.Controls.Add(count,0,1); right.Controls.Add(results,0,2); right.Controls.Add(paging,0,3);
            grid.Controls.Add(left,0,0); grid.Controls.Add(right,1,0); Controls.Add(grid); Controls.Add(header); Controls.Add(footer);
            calendar.MonthChanged+=delegate { RunSearch(); }; clear.Visible=false;
            KeyPreview=true; KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Control && e.KeyCode==Keys.F) { search.Focus(); search.SelectAll(); e.Handled=true; e.SuppressKeyPress=true; } };
            RunSearch();
            Shown+=delegate { search.Focus(); };
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keyData)
        {
            if(keyData==Keys.Escape && search.ContainsFocus && search.Text.Length>0) { search.Clear(); RunSearch(); return true; }
            return base.ProcessCmdKey(ref message,keyData);
        }
        private void OnSearchKey(object sender,KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter) { RunSearch(); e.Handled=true; e.SuppressKeyPress=true; }
            if(e.KeyCode==Keys.Escape && search.Text.Length>0) { search.Clear(); RunSearch(); e.Handled=true; e.SuppressKeyPress=true; }
        }
        private void OpenDate(DateTime target)
        {
            if(target<min || target>max) return;
            SelectedDate=target; DialogResult=DialogResult.OK; Close();
        }
        private void RunSearch()
        {
            searchTimer.Stop(); page=0;
            bool searching=!string.IsNullOrWhiteSpace(search.Text);
            matches=DiaryBrowse.Find(book,search.Text,searching?(DateTime?)null:calendar.Month).Where(m=>m.Date>=min && m.Date<=max).ToList();
            var monthEntries=DiaryBrowse.Find(book,"",calendar.Month);
            monthSummary.Text="이달 기록 "+monthEntries.Count+"일 · 사진 "+monthEntries.Count(m=>m.HasPhoto)+"일";
            count.Text=searching?"전체 기록에서 "+matches.Count+"일 찾았어요":"이달의 기록 · "+matches.Count+"일";
            RenderResults();
        }
        private void RenderResults()
        {
            page=Math.Max(0,Math.Min(page,Math.Max(0,(matches.Count-1)/50)));
            results.SuspendLayout(); foreach(Control control in results.Controls.Cast<Control>().ToArray()) control.Dispose(); results.Controls.Clear();
            foreach(var match in matches.Skip(page*50).Take(50)) {
                var value=match; var row=new HistoryRow(value.Date,(value.Kind=="photo"?"사진 설명 · ":"")+value.Preview,value.Date==current,value.HasPhoto);
                row.Click+=delegate { OpenDate(value.Date); }; results.Controls.Add(row);
            }
            if(matches.Count==0) {
                var empty=new CardPanel {Fill=Color.White,Height=Design.P(144),Width=Design.P(300),Margin=Design.Pad(0)};
                var title=Design.Label(string.IsNullOrWhiteSpace(search.Text)?"이달의 첫 기록을 남겨보세요":"검색 결과가 없어요",12,true); title.AutoSize=false; title.Height=Design.P(28); title.Location=Design.Point(20,24); empty.Controls.Add(title);
                var detail=Design.Label(string.IsNullOrWhiteSpace(search.Text)?"달력의 날짜를 눌러 일기를 시작해요.":"다른 단어로 찾거나 검색어를 지워보세요.",9); detail.ForeColor=Design.Muted; detail.AutoSize=false; detail.Location=Design.Point(20,62); detail.Height=Design.P(64); empty.Controls.Add(detail);
                results.Controls.Add(empty);
            }
            previous.Enabled=page>0; next.Enabled=(page+1)*50<matches.Count;
            pageCount.Text=matches.Count==0?"0 / 0":(page+1)+" / "+((matches.Count+49)/50);
            results.ResumeLayout(); ResizeResults(); results.AutoScrollPosition=Point.Empty;
        }
        private void ResizeResults()
        {
            if(resizingResults) return; resizingResults=true;
            var scroll=results.AutoScrollPosition;
            try {
                results.SuspendLayout(); results.AutoScroll=false;
                int width=Math.Max(Design.P(180),results.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-Design.P(4));
                foreach(Control row in results.Controls) { row.Width=width; foreach(var label in row.Controls.OfType<Label>()) label.Width=Math.Max(Design.P(100),width-Design.P(40)); }
                results.ResumeLayout(true); results.AutoScroll=true; results.PerformLayout();
                results.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);
            } finally { resizingResults=false; }
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing) { searchTimer.Dispose(); tips.Dispose(); } base.Dispose(disposing);
        }
    }
}
