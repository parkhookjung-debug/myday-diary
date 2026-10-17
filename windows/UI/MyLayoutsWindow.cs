using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed partial class MyLayoutsWindow : Form
    {
        private readonly DiaryBook book;
        private readonly Func<string,SavedLayout> saveCurrent;
        private readonly Action<SavedLayout,string> renameLayout;
        private readonly Action<SavedLayout> deleteLayout;
        private readonly FlowLayoutPanel list,preview;
        private readonly TextBox search;
        private readonly Label count;
        private readonly Button apply,rename,delete,save;
        private bool resizing;
        public SavedLayout SelectedLayout { get; private set; }
        public MyLayoutsWindow(DiaryBook book,Func<string,SavedLayout> saveCurrent,Action<SavedLayout,string> renameLayout,Action<SavedLayout> deleteLayout)
        {
            this.book=book; this.saveCurrent=saveCurrent; this.renameLayout=renameLayout; this.deleteLayout=deleteLayout;
            Text="MyDay · 내 레이아웃"; Font=Design.Font(10); BackColor=Design.Backgrounds[0]; AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.CenterParent; ShowInTaskbar=false;
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(960),work.Width-32),Math.Min(Design.P(700),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(740),work.Width-32),Math.Min(Design.P(560),work.Height-32));
            var header=new Panel {Dock=DockStyle.Top,Height=Design.P(148),BackColor=Color.White};
            var title=Design.Label("내가 꾸민 페이지, 다시 쓰기",19,true); title.Location=Design.Point(24,18); header.Controls.Add(title);
            var note=Design.Label("블록·배치·종이·배경을 저장해요. 본문·사진·체크 상태는 새로 시작해요.",9); note.ForeColor=Design.Muted; note.Location=Design.Point(25,58); header.Controls.Add(note);
            var searchCard=new CardPanel {Fill=Design.Backgrounds[1],Location=Design.Point(24,98),Size=Design.Size(250,34)}; header.Controls.Add(searchCard);
            var icon=Design.Label("검색",8,true); icon.ForeColor=Design.Muted; icon.Location=Design.Point(12,8); searchCard.Controls.Add(icon);
            search=new TextBox {BorderStyle=BorderStyle.None,Font=Design.Font(10),BackColor=searchCard.Fill,Location=Design.Point(50,7),Width=Design.P(180),MaxLength=40,AccessibleName="내 레이아웃 이름 검색"}; searchCard.Controls.Add(search);
            save=Design.Button("현재 배치 저장",true); save.Size=Design.Size(120,34); save.Top=Design.P(98); save.AccessibleName="현재 일기 배치를 새 레이아웃으로 저장"; header.Controls.Add(save);
            header.Resize+=delegate { save.Left=header.Width-Design.P(144); note.MaximumSize=new Size(header.Width-Design.P(48),Design.P(36)); };
            count=Design.Label("",8); count.ForeColor=Design.Muted; count.Location=Design.Point(290,106); header.Controls.Add(count);
            var grid=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(256))); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            list=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.White,Padding=Design.Pad(12)};
            preview=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=Design.Pad(22,16,20,12)};
            grid.Controls.Add(list,0,0); grid.Controls.Add(preview,1,0);
            var footer=new Panel {Dock=DockStyle.Bottom,Height=Design.P(76),BackColor=Color.White};
            rename=Design.Button("이름 변경"); rename.Size=Design.Size(80,32); rename.Top=Design.P(22); footer.Controls.Add(rename);
            delete=Design.Button("삭제"); delete.Size=Design.Size(54,32); delete.Top=Design.P(22); footer.Controls.Add(delete);
            var close=Design.Button("닫기"); close.Size=Design.Size(66,32); close.Top=Design.P(22); close.DialogResult=DialogResult.Cancel; footer.Controls.Add(close); CancelButton=close;
            apply=Design.Button("이 배치 불러오기",true); apply.Size=Design.Size(130,32); apply.Top=Design.P(22); footer.Controls.Add(apply);
            footer.Resize+=delegate { rename.Left=Design.P(24); delete.Left=Design.P(112); apply.Left=footer.Width-Design.P(154); close.Left=apply.Left-Design.P(78); };
            Controls.Add(grid); Controls.Add(header); Controls.Add(footer);
            search.TextChanged+=delegate { RefreshList(); }; search.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.KeyCode==Keys.Enter) { e.Handled=true; e.SuppressKeyPress=true; } };
            save.Click+=delegate { OpenSaveName(); }; rename.Click+=delegate { OpenRename(); };
            delete.Click+=delegate { if(SelectedLayout!=null && MessageBox.Show(this,"저장한 레이아웃을 삭제할까요? 작성한 일기는 유지돼요.","레이아웃 삭제",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes) DeleteSelected(); };
            apply.Click+=delegate { if(SelectedLayout!=null) { DialogResult=DialogResult.OK; Close(); } };
            list.Resize+=delegate { ResizeContent(); }; preview.Resize+=delegate { ResizeContent(); };
            RefreshList();
        }
        private void OpenSaveName()
        {
            int number=1; while(book.Layouts.Any(l=>l.Name=="나의 페이지 "+number)) number++;
            using(var dialog=new LayoutNameDialog("내 레이아웃 저장","나의 페이지 "+number,delegate(string name) { SaveNamed(name); })) dialog.ShowDialog(this);
        }
        private void SaveNamed(string name)
        {
            var saved=saveCurrent(name); search.Clear(); RefreshList(saved.Id);
        }
        private void OpenRename()
        {
            if(SelectedLayout==null) return;
            using(var dialog=new LayoutNameDialog("레이아웃 이름 변경",SelectedLayout.Name,delegate(string name) { RenameSelected(name); })) dialog.ShowDialog(this);
        }
        private void RenameSelected(string name) { var selected=SelectedLayout; renameLayout(selected,name); RefreshList(selected.Id); }
        private void DeleteSelected()
        {
            try { deleteLayout(SelectedLayout); RefreshList(); } catch(Exception ex) { MessageBox.Show(this,"삭제하지 못했어요.\n"+ex.Message); }
        }
        private void RefreshList(string preferredId=null)
        {
            preferredId=preferredId??(SelectedLayout==null?null:SelectedLayout.Id);
            var matches=book.Layouts.Where(l=>l.Name.IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            list.SuspendLayout(); foreach(Control control in list.Controls.Cast<Control>().ToArray()) control.Dispose(); list.Controls.Clear();
            foreach(var layout in matches) {
                var value=layout; var button=Design.Button(value.Name); button.Height=Design.P(60); button.Width=Design.P(220); button.Margin=Design.Pad(0,0,0,8); button.TextAlign=ContentAlignment.MiddleLeft; button.Tag=value;
                button.AccessibleName=value.Name+" · "+value.Blocks.Count+"개 블록"; button.Click+=delegate { Select(value); }; list.Controls.Add(button);
            }
            count.Text=matches.Length+" / "+book.Layouts.Count+"개"; save.Enabled=book.Layouts.Count<SavedLayouts.MaxLayouts;
            Select(matches.FirstOrDefault(l=>l.Id==preferredId)??matches.FirstOrDefault());
            list.ResumeLayout(); ResizeContent(); list.AutoScrollPosition=Point.Empty;
        }
        private void Select(SavedLayout layout)
        {
            SelectedLayout=layout; apply.Enabled=rename.Enabled=delete.Enabled=layout!=null;
            foreach(var button in list.Controls.OfType<Button>()) { button.BackColor=button.Tag==layout?Design.Tint:Color.White; button.ForeColor=button.Tag==layout?Design.Accent:Design.Ink; button.Invalidate(); }
            preview.SuspendLayout(); foreach(Control control in preview.Controls.Cast<Control>().ToArray()) control.Dispose(); preview.Controls.Clear();
            if(layout==null) {
                var heading=Design.Label(book.Layouts.Count==0?"내 첫 레이아웃을 저장해보세요":"검색 결과가 없어요",16,true); preview.Controls.Add(heading);
                var note=Design.Label(book.Layouts.Count==0?"일기에서 블록을 꾸민 다음\n현재 배치 저장을 눌러 이름을 붙여요.":"다른 이름으로 찾아보세요.",10); note.ForeColor=Design.Muted; note.Margin=Design.Pad(0,14,0,0); preview.Controls.Add(note);
            } else {
                var heading=Design.Label(layout.Name,19,true); preview.Controls.Add(heading);
                var meta=Design.Label(layout.Blocks.Count+"개 블록 · "+(layout.Mode=="free"?"자유 배치":"자동 정렬")+" · "+(layout.Style=="paper"?"노트":layout.Style=="dots"?"도트":"카드"),9); meta.ForeColor=Design.Accent; meta.Margin=Design.Pad(0,8,0,14); preview.Controls.Add(meta);
                preview.Controls.Add(new SavedLayoutDiagram {Template=layout,Size=Design.Size(460,210),Margin=Design.Pad(0,0,0,14)});
                var note=Design.Label("새 날짜에는 저장한 배치를 그대로 사용해요.\n작성 중인 일기에는 기존 블록 뒤에 추가해요.",9); note.ForeColor=Design.Muted; note.Margin=Design.Pad(0,0,0,14); preview.Controls.Add(note);
                for(int i=0;i<layout.Blocks.Count;i++) { var block=layout.Blocks[i]; string kind=block.Kind=="photo-slot"?"사진 자리":block.Kind=="todo"?"할 일":block.Kind=="habit"?"습관":block.Kind=="emotion"?"감정":"글 일기";
                    var label=Design.Label((i+1)+". "+(string.IsNullOrWhiteSpace(block.Title)?kind:block.Title+"  ·  "+kind),9); label.Margin=Design.Pad(0,0,0,8); preview.Controls.Add(label); }
            }
            preview.ResumeLayout(); ResizeContent(); preview.AutoScrollPosition=Point.Empty;
        }
        private void ResizeContent()
        {
            if(resizing) return; resizing=true;
            try {
                list.SuspendLayout(); preview.SuspendLayout(); list.AutoScroll=preview.AutoScroll=false;
                int width=System.Math.Max(Design.P(160),list.ClientSize.Width-list.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-Design.P(4));
                foreach(Control control in list.Controls) control.Width=width;
                int previewWidth=System.Math.Max(Design.P(180),preview.ClientSize.Width-preview.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-Design.P(4));
                foreach(Control control in preview.Controls) { var label=control as Label; if(label!=null) label.MaximumSize=new Size(previewWidth,0); else control.Width=previewWidth; }
                list.ResumeLayout(true); preview.ResumeLayout(true); list.AutoScroll=preview.AutoScroll=true; list.PerformLayout(); preview.PerformLayout();
            } finally { resizing=false; }
        }
    }
}
