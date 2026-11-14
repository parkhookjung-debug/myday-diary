using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI
{
    public sealed class AnniversaryWindow:Form
    {
        private readonly DiaryBook book;
        private readonly Action save;
        private readonly ListBox list=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,AccessibleName="등록된 생일과 기념일"};
        private readonly TextBox name=new TextBox{Dock=DockStyle.Fill,MaxLength=StudioCatalog.AnniversaryNameLength,AccessibleName="기념일 이름"};
        private readonly DateTimePicker date=new DateTimePicker{Dock=DockStyle.Fill,Format=DateTimePickerFormat.Short,AccessibleName="기념일 날짜"};
        private readonly CheckBox yearly=new CheckBox{Text="매년 반복 (생일·기념일)",Checked=true,Dock=DockStyle.Fill};
        private readonly Button add=Design.Button("추가",true),remove=Design.Button("선택 삭제"),close=Design.Button("닫기");
        private Anniversary[] rows=new Anniversary[0];
        public AnniversaryWindow(DiaryBook book,Action save)
        {
            this.book=book;this.save=save;Text="MyDay · 생일과 기념일";ClientSize=Design.Size(560,520);MinimumSize=Design.Size(460,450);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Paper;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=Design.Pad(18),ColumnCount=1,RowCount=6};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(54)));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(40)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(40)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(48)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(44)));Controls.Add(layout);
            var intro=Design.Label("생일처럼 매년 반복하거나, 한 번만 표시할 날짜를 등록해요.",10);intro.Dock=DockStyle.Fill;layout.Controls.Add(intro,0,0);layout.Controls.Add(list,0,1);
            var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));fields.Controls.Add(name,0,0);fields.Controls.Add(date,1,0);layout.Controls.Add(fields,0,2);layout.Controls.Add(yearly,0,3);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};add.Size=remove.Size=Design.Size(130,36);actions.Controls.AddRange(new Control[]{add,remove});layout.Controls.Add(actions,0,4);close.Size=Design.Size(100,34);close.Anchor=AnchorStyles.Right;close.DialogResult=DialogResult.Cancel;layout.Controls.Add(close,0,5);CancelButton=close;AcceptButton=add;add.Enabled=remove.Enabled=save!=null;
            add.Click+=delegate{if(Add(name.Text,date.Value,yearly.Checked,true))name.Clear();};remove.Click+=delegate{RemoveSelected(true,true);};Render();
        }
        private Anniversary[] Entries {get{return book.Studio==null?new Anniversary[0]:book.Studio.Anniversaries.ToArray();}}
        private bool Persist(Anniversary[] values,bool showErrors){try{Anniversaries.Commit(book,values,save);Render();return true;}catch(Exception e){if(showErrors)MessageBox.Show(this,e.Message,"저장하지 못했어요",MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}}
        private bool Add(string value,DateTime when,bool repeat,bool showErrors){var item=new Anniversary{Name=value.Trim(),Date=when.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),RepeatYearly=repeat};return Persist(Entries.Concat(new[]{item}).ToArray(),showErrors);}
        private bool RemoveSelected(bool confirm,bool showErrors){if(list.SelectedIndex<0)return false;string id=rows[list.SelectedIndex].Id;if(confirm&&MessageBox.Show(this,"선택한 기념일을 삭제할까요?","기념일 삭제",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return false;return Persist(Entries.Where(a=>a.Id!=id).ToArray(),showErrors);}
        private void Render(){rows=Entries.OrderBy(a=>a.Date,StringComparer.Ordinal).ThenBy(a=>a.Name,StringComparer.Ordinal).ToArray();list.Items.Clear();foreach(var item in rows)list.Items.Add(item.Name+" · "+DateTime.ParseExact(item.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture).ToString("yyyy년 M월 d일")+(item.RepeatYearly?" · 매년":" · 한 번"));}
        internal bool AddForTest(string value,DateTime when,bool repeat){return Add(value,when,repeat,false);}
        internal bool RemoveForTest(string id){list.SelectedIndex=Array.FindIndex(rows,a=>a.Id==id);return RemoveSelected(false,false);}
        internal void VerifyLayoutForTest(){foreach(var b in new[]{add,remove,close})if(!b.Parent.ClientRectangle.Contains(b.Bounds))throw new InvalidOperationException("Anniversary action clipped");}
    }
}
