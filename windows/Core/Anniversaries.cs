using System;
using System.Collections.Generic;
using System.Linq;
namespace MyDay.Windows.Core
{
    public static class Anniversaries
    {
        public static void Commit(DiaryBook book,IEnumerable<Anniversary> entries,Action save)
        {
            if(save==null)throw new InvalidOperationException("저장할 수 없는 미리보기입니다.");
            var previous=book.Studio;var source=previous??new StudioData();
            var candidate=new StudioData{RoomTheme=source.RoomTheme,Furniture=source.Furniture,Pages=source.Pages,Anniversaries=entries.ToList()};
            StudioData.Validate(candidate);book.Studio=candidate;
            try{save();}catch{book.Studio=previous;throw;}
        }
    }
}
