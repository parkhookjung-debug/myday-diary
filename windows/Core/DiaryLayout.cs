using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MyDay.Windows.Core
{
    // Store logical units so placements survive a different Windows DPI.
    public static class DiaryLayout
    {
        public const int MinWidth=300,MaxWidth=1600,MinHeight=180,MaxHeight=1400,MaxPosition=10000;
        public static int DefaultHeight(DiaryBlock block) { return block.Kind=="photo" || block.Kind=="photo-slot"?360:block.Kind=="emotion"?288:250; }
        public static List<Rectangle> AutoArrange(IList<DiaryBlock> blocks,int width)
        {
            int available=Math.Max(MinWidth+12,width-16),x=0,y=4,rowHeight=0;
            bool two=available>=624;
            var result=new List<Rectangle>();
            foreach(var block in blocks) {
                int cardWidth=block.Wide || !two?available-12:(available-24)/2;
                int height=block.Height>0?block.Height:DefaultHeight(block);
                if(x>0 && x+cardWidth>available) { x=0; y+=rowHeight+12; rowHeight=0; }
                result.Add(new Rectangle(x,y,cardWidth,height));
                x+=cardWidth+12; rowHeight=Math.Max(rowHeight,height);
            }
            return result;
        }
        public static void EnableFree(DiaryEntry entry,int width)
        {
            var positions=AutoArrange(entry.Blocks,width);
            for(int i=0;i<entry.Blocks.Count;i++) {
                var block=entry.Blocks[i];
                if(block.Width==0 || block.Height==0) SetBounds(block,positions[i]);
            }
            entry.LayoutMode="free";
        }
        public static void ArrangeFree(DiaryEntry entry,int width)
        {
            var positions=AutoArrange(entry.Blocks,width);
            for(int i=0;i<entry.Blocks.Count;i++) SetBounds(entry.Blocks[i],positions[i]);
        }
        public static void PlaceNew(DiaryEntry entry,DiaryBlock block)
        {
            int bottom=entry.Blocks.Where(b=>b!=block).Select(b=>b.Y+(b.Height>0?b.Height:DefaultHeight(b))).DefaultIfEmpty(0).Max();
            SetBounds(block,new Rectangle(12,Math.Min(MaxPosition,bottom+16),340,DefaultHeight(block)));
        }
        public static Rectangle Bounds(DiaryBlock block)
        { return new Rectangle(block.X,block.Y,block.Width>0?block.Width:340,block.Height>0?block.Height:DefaultHeight(block)); }
        public static Rectangle Drag(Rectangle original,Point delta,bool resize)
        {
            return resize?new Rectangle(original.X,original.Y,Clamp(original.Width+delta.X,MinWidth,MaxWidth),Clamp(original.Height+delta.Y,MinHeight,MaxHeight)):
                new Rectangle(Clamp(original.X+delta.X,0,MaxPosition),Clamp(original.Y+delta.Y,0,MaxPosition),original.Width,original.Height);
        }
        public static void SetBounds(DiaryBlock block,Rectangle bounds)
        {
            block.X=Clamp(bounds.X,0,MaxPosition); block.Y=Clamp(bounds.Y,0,MaxPosition);
            block.Width=Clamp(bounds.Width,MinWidth,MaxWidth); block.Height=Clamp(bounds.Height,MinHeight,MaxHeight);
        }
        private static int Clamp(int value,int min,int max) { return Math.Max(min,Math.Min(max,value)); }
    }
}
