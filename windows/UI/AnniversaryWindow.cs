using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
 public sealed class AnniversaryWindow:Form {
  private readonly DiaryBook book;private readonly Action save;private readonly ListBox list;private readonly TextBox name;private readonly DateTimePicker date;private readonly CheckBox yearly;
  public AnniversaryWindow(DiaryBook book,Action save){this.book=book;this.save=save;book.Studio=book.Studio??new StudioData();Text="MyDay · 생일과 기념일";Size=Design.Size(510,510);MinimumSize=Design.Size(440,440);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Sidebar;
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=Design.Pad(18),ColumnCount=1,RowCount=6};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(48)));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(34)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(38)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(36)));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(44)));Controls.Add(layout);
   var intro=Design.Label("매년 반복하거나 한 번만 표시할 날짜를 등록해요.",9);intro.Dock=DockStyle.Fill;layout.Controls.Add(intro,0,0);
   list=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false};layout.Controls.Add(list,0,1);
   var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,62));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,38));name=new TextBox{Dock=DockStyle.Fill,MaxLength=40};date=new DateTimePicker{Dock=DockStyle.Fill,Format=DateTimePickerFormat.Short};row.Controls.Add(name,0,0);row.Controls.Add(date,1,0);layout.Controls.Add(row,0,2);
   yearly=new CheckBox{Text="매년 반복 (생일·기념일)",Checked=true,Dock=DockStyle.Fill};layout.Controls.Add(yearly,0,3);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight};var add=Design.Button("추가");var remove=Design.Button("선택 삭제");actions.Controls.Add(add);actions.Controls.Add(remove);layout.Controls.Add(actions,0,4);
   var close=Design.Button("닫기");close.Anchor=AnchorStyles.Right;close.DialogResult=DialogResult.OK;layout.Controls.Add(close,0,5);AcceptButton=close;
   add.Click+=delegate{var value=name.Text.Trim();if(value.Length==0){MessageBox.Show(this,"기념일 이름을 입력해 주세요.");return;}if(book.Studio.Anniversaries.Count>=500){MessageBox.Show(this,"기념일은 500개까지 등록할 수 있어요.");return;}book.Studio.Anniversaries.Add(new Anniversary{Name=value,Date=date.Value.ToString("yyyy-MM-dd"),RepeatYearly=yearly.Checked});Persist();name.Clear();Render();};
   remove.Click+=delegate{if(list.SelectedIndex<0)return;book.Studio.Anniversaries.RemoveAt(list.SelectedIndex);Persist();Render();};Render();
  }
  private void Persist(){try{StudioData.Validate(book.Studio);save();}catch(Exception e){MessageBox.Show(this,e.Message,"저장 실패",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  private void Render(){list.Items.Clear();foreach(var item in book.Studio.Anniversaries.OrderBy(a=>a.Date))list.Items.Add(item.Name+" · "+DateTime.ParseExact(item.Date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy년 M월 d일")+(item.RepeatYearly?" · 매년":""));}
 }
}
