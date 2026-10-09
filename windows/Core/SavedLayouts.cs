using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace MyDay.Windows.Core
{
    [DataContract]
    public sealed class LayoutBlock
    {
        [DataMember] public string Kind;
        [DataMember(EmitDefaultValue=false)] public string Title;
        [DataMember(EmitDefaultValue=false)] public string Prompt;
        [DataMember] public bool Wide;
        [DataMember] public int X,Y,Width,Height;
        public Rectangle Bounds { get { return new Rectangle(X,Y,Width,Height); } }
    }
    [DataContract]
    public sealed class SavedLayout
    {
        [DataMember] public string Id,Name,Mode,Style;
        [DataMember] public int Theme;
        [DataMember] public List<LayoutBlock> Blocks;
    }
    public static class SavedLayouts
    {
        public const int MaxLayouts=50;
        private const int MaxAutoPosition=200*(DiaryLayout.MaxHeight+12);
        public static void Validate(IEnumerable<SavedLayout> layouts)
        {
            if(layouts==null || layouts.Count()>MaxLayouts) throw new InvalidDataException("내 레이아웃은 최대 50개까지 저장할 수 있어요.");
            var ids=new HashSet<string>(); var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var layout in layouts) {
                if(layout==null || string.IsNullOrWhiteSpace(layout.Id) || layout.Id.Length>64 || !ids.Add(layout.Id)) throw new InvalidDataException("레이아웃 ID가 올바르지 않습니다.");
                ValidateName(layout.Name);
                if(!names.Add(layout.Name)) throw new InvalidDataException("같은 이름의 레이아웃이 있어요. 다른 이름을 적어주세요.");
                if(layout.Mode!="free" && layout.Mode!="cards" || !new[] {"paper","plain","dots"}.Contains(layout.Style) || layout.Theme<0 || layout.Theme>2 ||
                    layout.Blocks==null || layout.Blocks.Count==0 || layout.Blocks.Count>200) throw new InvalidDataException("레이아웃 구성이 올바르지 않습니다.");
                foreach(var block in layout.Blocks) {
                    if(block==null || !new[] {"text","todo","habit","emotion","photo-slot"}.Contains(block.Kind) ||
                        block.Title!=null && block.Title.Length>80 || block.Prompt!=null && block.Prompt.Length>300 ||
                        block.X<0 || block.X>DiaryLayout.MaxPosition || block.Y<0 || block.Y>(layout.Mode=="cards"?MaxAutoPosition:DiaryLayout.MaxPosition) ||
                        block.Width<DiaryLayout.MinWidth || block.Width>DiaryLayout.MaxWidth || block.Height<DiaryLayout.MinHeight || block.Height>DiaryLayout.MaxHeight)
                        throw new InvalidDataException("레이아웃 블록이나 배치가 올바르지 않습니다.");
                }
            }
        }
        public static void ValidateName(string name)
        {
            if(string.IsNullOrWhiteSpace(name) || name!=name.Trim() || name.Length>40 || name.Any(char.IsControl)) throw new InvalidDataException("이름은 공백만 넣지 말고 1~40자로 적어주세요.");
        }
        public static SavedLayout Capture(DiaryEntry entry,string name,int canvasWidth)
        {
            var layout=new SavedLayout {Id=Guid.NewGuid().ToString("N"),Name=(name??"").Trim(),Mode=entry.LayoutMode,Style=entry.PageStyle,Theme=entry.Theme,Blocks=new List<LayoutBlock>()};
            var positions=entry.LayoutMode=="free"?entry.Blocks.Select(DiaryLayout.Bounds).ToList():DiaryLayout.AutoArrange(entry.Blocks,Math.Min(canvasWidth,DiaryLayout.MaxWidth+28));
            for(int i=0;i<entry.Blocks.Count;i++) {
                var block=entry.Blocks[i]; var r=positions[i];
                layout.Blocks.Add(new LayoutBlock {Kind=block.Kind=="photo"?"photo-slot":block.Kind,Title=block.Title,Prompt=block.Prompt,Wide=block.Wide,X=r.X,Y=r.Y,Width=r.Width,Height=r.Height});
            }
            Validate(new[] {layout}); return layout;
        }
        public static List<SavedLayout> Add(IList<SavedLayout> current,SavedLayout layout)
        {
            var result=current.Concat(new[] {layout}).ToList(); Validate(result); return result;
        }
        public static SavedLayout Copy(SavedLayout source,string name=null)
        {
            return new SavedLayout {Id=source.Id,Name=name??source.Name,Mode=source.Mode,Style=source.Style,Theme=source.Theme,
                Blocks=source.Blocks.Select(b=>new LayoutBlock {Kind=b.Kind,Title=b.Title,Prompt=b.Prompt,Wide=b.Wide,X=b.X,Y=b.Y,Width=b.Width,Height=b.Height}).ToList()};
        }
        public static List<SavedLayout> Rename(IList<SavedLayout> current,string id,string name)
        {
            if(!current.Any(l=>l.Id==id)) throw new InvalidDataException("저장한 레이아웃을 찾지 못했어요.");
            var result=current.Select(l=>l.Id==id?Copy(l,(name??"").Trim()):l).ToList(); Validate(result); return result;
        }
        public static List<SavedLayout> Merge(IList<SavedLayout> current,IList<SavedLayout> incoming)
        {
            Validate(current); Validate(incoming); var result=current.Select(l=>Copy(l)).ToList();
            foreach(var item in incoming) {
                result.RemoveAll(l=>l.Id==item.Id); string name=item.Name; int suffix=2;
                while(result.Any(l=>string.Equals(l.Name,name,StringComparison.OrdinalIgnoreCase))) {
                    string tail=" ("+(suffix++)+")"; int length=Math.Min(item.Name.Length,40-tail.Length);
                    if(length>0 && char.IsHighSurrogate(item.Name[length-1])) length--;
                    name=item.Name.Substring(0,length)+tail;
                }
                result.Add(Copy(item,name));
            }
            Validate(result); return result;
        }
        public static void Apply(DiaryEntry entry,SavedLayout layout)
        {
            Validate(new[] {layout});
            bool fresh=entry.Blocks.Count==0 || entry.Blocks.Count==1 && entry.LayoutMode=="cards" && entry.Blocks[0].Kind=="text" &&
                entry.Blocks[0].Text.Length==0 && entry.Blocks[0].Title==DiaryTemplates.All[0].Sections[0].Title && entry.Blocks[0].Width==0;
            int count=fresh?0:entry.Blocks.Count;
            if(count+layout.Blocks.Count>200) throw new InvalidDataException("한 날짜에는 최대 200개 블록을 넣을 수 있어요.");
            string mode=fresh?layout.Mode:entry.LayoutMode;
            var existing=mode=="free" && !fresh?entry.Blocks.Select(DiaryLayout.Bounds).ToList():new List<Rectangle>();
            int offset=existing.Count==0?0:existing.Max(r=>r.Bottom)+16;
            var added=new List<DiaryBlock>();
            foreach(var saved in layout.Blocks) {
                if(mode=="free" && saved.Y+offset>DiaryLayout.MaxPosition) throw new InvalidDataException("아래에 배치를 추가할 공간이 부족해요. 새 날짜에서 사용하거나 기존 블록을 위로 옮겨주세요.");
                var block=DiaryBlock.Create(saved.Kind); block.Title=saved.Title; block.Prompt=saved.Prompt; block.Wide=saved.Wide;
                if(mode=="free") { var rectangle=saved.Bounds; rectangle.Offset(0,offset); DiaryLayout.SetBounds(block,rectangle); }
                else block.Height=saved.Height;
                added.Add(block);
            }
            if(fresh) { entry.Blocks.Clear(); entry.LayoutMode=layout.Mode; entry.PageStyle=layout.Style; entry.Theme=layout.Theme; }
            entry.Blocks.AddRange(added);
        }
    }
}
