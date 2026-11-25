using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyDay.Windows.Core
{
    // Keep the legacy Text/Checked projection usable by clients with a single checkbox.
    public static class DiaryTodos
    {
        public const int MaxItems = 100;
        public static void Normalize(DiaryBlock block)
        {
            if(block.Kind!="todo") return;
            if(block.TodoLayoutVersion<1) { block.Wide=true; block.TodoLayoutVersion=1; }
            if(block.TodoItems==null)
                block.TodoItems=Lines(block.Text).Select(text=>new DiaryTodoItem {Text=text,Checked=block.Checked}).ToList();
            Validate(block.TodoItems);
            var previous=block.TodoItems;
            bool all=previous.Count>0 && previous.All(item=>item.Checked);
            // A Mac/Android client may have edited only the legacy aggregate checkbox.
            if(previous.Count>0 && block.Checked!=all)
                foreach(var item in previous) item.Checked=block.Checked;
            if(block.Text!=String.Join("\n",previous.Select(item=>item.Text))) {
                var remaining=new List<DiaryTodoItem>(previous);
                block.TodoItems=Lines(block.Text).Select(text=> {
                    var match=remaining.FirstOrDefault(item=>item.Text==text);
                    if(match!=null) remaining.Remove(match);
                    return new DiaryTodoItem {Text=text,Checked=match!=null?match.Checked:previous.Count==0 && block.Checked,Importance=match==null?0:match.Importance};
                }).ToList();
            }
            Validate(block.TodoItems);
            block.Text=String.Join("\n",block.TodoItems.Select(item=>item.Text));
            if(block.TodoItems.Count>0) block.Checked=block.TodoItems.All(item=>item.Checked);
        }
        private static string[] Lines(string text) {return (text??"").Split(new[]{"\r\n","\n"},StringSplitOptions.RemoveEmptyEntries);}
        private static void Validate(List<DiaryTodoItem> items)
        {
            if(items.Count>MaxItems || items.Any(item=>item==null || item.Text==null || item.Text.Length>DesktopContract.MaxTextUtf16) ||
                items.Sum(item=>(long)item.Text.Length)+Math.Max(0,items.Count-1)>DesktopContract.MaxTextUtf16)
                throw new InvalidDataException("할 일 목록은 항목 100개, 전체 100,000자까지 저장할 수 있습니다.");
            if(items.Any(item=>item.Importance<0 || item.Importance>2)) throw new InvalidDataException("할 일 중요도 값이 올바르지 않습니다.");
        }
    }
}
