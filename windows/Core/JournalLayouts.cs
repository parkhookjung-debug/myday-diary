using System;
using System.Collections.Generic;
using System.Drawing;

namespace MyDay.Windows.Core
{
    public static class JournalLayouts
    {
        public static readonly string[] Ids={"page","cards","split","timeline","letter","dashboard","cornell","compare"};
        public static string Name(string id)
        {
            switch(id) {
                case "page":return "긴 글"; case "cards":return "질문 카드"; case "split":return "본문 + 메모";
                case "timeline":return "타임라인"; case "letter":return "편지"; case "dashboard":return "플래너";
                case "cornell":return "코넬 노트"; case "compare":return "나란히 비교"; default:return "긴 글";
            }
        }
        public static List<Rectangle> Arrange(string layout,int count,int width)
        {
            var result=new List<Rectangle>();
            int available=Math.Max(DiaryLayout.MinWidth,Math.Min(DiaryLayout.MaxWidth,width-36));
            int half=(available-16)/2; bool two=half>=DiaryLayout.MinWidth;
            if(layout=="split" && count>1 && two) {
                int side=Math.Max(DiaryLayout.MinWidth,available*2/5),main=available-side-16;
                int height=(count-1)*216-16;
                result.Add(new Rectangle(12,12,main,Math.Max(300,height)));
                for(int i=1;i<count;i++) result.Add(new Rectangle(12+main+16,12+(i-1)*216,side,200));
            } else if(layout=="cornell" && count==3 && two) {
                int cue=DiaryLayout.MinWidth;
                result.Add(new Rectangle(12,12,cue,250));
                result.Add(new Rectangle(12+cue+16,12,available-cue-16,250));
                result.Add(new Rectangle(12,278,available,200));
            } else if(layout=="compare" && count>=2 && two) {
                result.Add(new Rectangle(12,12,half,280)); result.Add(new Rectangle(28+half,12,available-half-16,280));
                for(int i=2;i<count;i++) result.Add(new Rectangle(12,308+(i-2)*216,available,200));
            } else if(layout=="dashboard" && count>1 && two) {
                result.Add(new Rectangle(12,12,available,180));
                for(int i=1;i<count;i++) result.Add(new Rectangle(12+((i-1)%2)*(half+16),208+((i-1)/2)*216,half,200));
            } else if(layout=="cards" && two) {
                for(int i=0;i<count;i++) result.Add(new Rectangle(12+(i%2)*(half+16),12+(i/2)*236,half,220));
            } else {
                int y=12;
                for(int i=0;i<count;i++) { int height=layout=="letter" && i==0?340:layout=="page"?280:200;
                    result.Add(new Rectangle(12,y,available,height)); y+=height+16; }
            }
            return result;
        }
    }
}
