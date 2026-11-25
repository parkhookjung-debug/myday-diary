using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.UI;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunTodoTests(string directory)
        {
            var block=DiaryBlock.Create("todo"); block.Text="기존 할 일\n두 번째 할 일"; block.Checked=true;
            var entry=DiaryEntry.Empty(); entry.Blocks.Add(block);
            var book=new DiaryBook(); book.Days["2026-11-25"]=entry;
            var store=new DiaryStore(Path.Combine(directory,"todos")); store.Save(book);
            block=store.Load().Days["2026-11-25"].Blocks[0];
            Check(block.TodoItems.Count==2 && block.TodoItems.All(item=>item.Checked) && block.Text=="기존 할 일\n두 번째 할 일","Legacy todo text and aggregate check migrate without erasing saved content");
            block.TodoItems[0].Importance=1; block.TodoItems[1].Checked=false; block.Checked=false;
            book.Days["2026-11-25"].Blocks[0]=block; store.Save(book);
            string export=Path.Combine(directory,"todo-export.json"); store.Export(export,book);
            using(var input=File.OpenRead(export)) {
                var restored=DiaryStore.Read(input).Days["2026-11-25"].Blocks[0];
                Check(restored.TodoItems[0].Importance==1 && restored.TodoItems[0].Checked && !restored.TodoItems[1].Checked,"Todo priorities and partial checks survive disk and export round trips");
            }
            // Older clients edit the flat projection while retaining optional list metadata.
            block.Text="두 번째 할 일\n기존 할 일\n새 할 일"; DiaryTodos.Normalize(block);
            Check(block.TodoItems[1].Importance==1 && block.TodoItems[1].Checked && !block.TodoItems[0].Checked && !block.TodoItems[2].Checked,"Legacy text edits preserve matched item checks and priorities after reordering");
            block.Checked=true; DiaryTodos.Normalize(block);
            Check(block.TodoItems.All(item=>item.Checked),"Legacy aggregate checkbox applies to every list item");
            store.Save(book); var before=File.ReadAllBytes(store.FilePath);
            block.TodoItems[0].Importance=3;
            bool rejected=false; try {store.Save(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Malformed todo priority cannot overwrite a valid diary"); block.TodoItems[0].Importance=0;
            block.TodoItems=Enumerable.Range(0,101).Select(i=>new DiaryTodoItem {Text=i.ToString()}).ToList();
            rejected=false; try {store.Save(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Oversized todo list is rejected without truncating saved records");
            block.TodoItems=new List<DiaryTodoItem> {new DiaryTodoItem {Text=new string('x',50000)},new DiaryTodoItem {Text=new string('y',50000)}};
            rejected=false; try {store.Save(book);} catch(InvalidDataException) {rejected=true;}
            Check(rejected && before.SequenceEqual(File.ReadAllBytes(store.FilePath)),"Todo aggregate text obeys the existing block text limit");

            var task=DiaryBlock.Create("todo"); task.Text="공부\n산책"; DiaryTodos.Normalize(task);
            entry=DiaryEntry.Empty(); entry.Blocks.Add(task); var baseline=SangmonGrowth.CaptureRecord(entry);
            task.TodoItems[0].Importance=1;
            Check(SangmonGrowth.SameRecord(baseline,entry),"Priority-only changes do not count as new writing activity");
            task.TodoItems[0].Checked=true; var day=new DateTime(2026,11,25);
            var growth=SangmonGrowth.Award(null,entry,day,baseline);
            Check((SangmonGrowth.Flags(growth,day)&SangmonGrowth.Todo)!=0 && !task.Checked && SangmonGrowth.XP(growth)==15,"Completing one list item awards the todo goal before the entire list is checked");
            var again=SangmonGrowth.Award(growth,entry,day,baseline);
            Check(SangmonGrowth.XP(again)==15,"Repeated item checks never multiply the daily todo reward");
        }

        private static IEnumerable<Control> TodoControls(Control root)
        {
            foreach(Control child in root.Controls) {yield return child;foreach(var nested in TodoControls(child)) yield return nested;}
        }
        private static void SmokeTodo(string directory)
        {
            var block=DiaryBlock.Create("todo");int changes=0;
            using(var form=new Form {Size=new Size(1000,620),StartPosition=FormStartPosition.Manual,Location=new Point(-30000,-30000)})
            using(var card=new BlockCard(block,delegate {changes++;},delegate {},delegate {},delegate {},"plain",null,1)) {
                card.Location=new Point(20,20);card.Width=880;form.Controls.Add(card);form.Show();Application.DoEvents();
                var input=TodoControls(card).OfType<TextBox>().Single(c=>c.AccessibleName=="새 할 일 입력");
                var add=TodoControls(card).OfType<Button>().Single(c=>c.AccessibleName=="할 일 추가");
                input.Text="발표 자료 정리";add.PerformClick(); input.Text="산책하기";add.PerformClick();
                var row=TodoControls(card).OfType<TextBox>().First(c=>c.AccessibleName=="할 일 내용");row.Text="발표 자료 완성";
                TodoControls(card).OfType<ComboBox>().First().SelectedIndex=0;
                TodoControls(card).OfType<CheckBox>().First(c=>c.Tag is DiaryTodoItem).Checked=true;
                if(block.TodoItems.Count!=2 || block.TodoItems[0].Text!="발표 자료 완성" || block.TodoItems[0].Importance!=1 || !block.TodoItems[0].Checked || block.Checked) throw new Exception("Native todo item editing failed");
                card.Check.Checked=true;
                if(block.TodoItems.Any(item=>!item.Checked)) throw new Exception("Native todo bulk check failed");
                TodoControls(card).OfType<Button>().Last(c=>c.AccessibleName=="할 일 삭제").PerformClick();
                if(block.TodoItems.Count!=1 || changes<7) throw new Exception("Native todo delete or save callback failed");
                input.Text="사진 정리하기";add.PerformClick(); Application.DoEvents();
                using(var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)) {form.DrawToBitmap(image,form.ClientRectangle);image.Save(Path.Combine(directory,"windows-todo-list.png"));}
                form.Close();
            }
            Console.WriteLine("Smoke: native todo add, edit, priority, individual check, bulk check, delete and rendering passed.");
        }
    }
}
