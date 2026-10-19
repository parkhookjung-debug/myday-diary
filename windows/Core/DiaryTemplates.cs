using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDay.Windows.Core
{
    public sealed class JournalSection
    {
        public readonly string Kind,Title,Prompt;
        public JournalSection(string kind,string title,string prompt) { Kind=kind; Title=title; Prompt=prompt; }
    }
    public sealed class JournalTemplate
    {
        public readonly string Id,Name,Description,Style,Category,Layout,Reference;
        public readonly int Minutes;
        public readonly JournalSection[] Sections;
        public JournalTemplate(string id,string name,string description,string style,params JournalSection[] sections)
            :this(id,name,description,style,"daily",5,"page","dayone",sections) { }
        public JournalTemplate(string id,string name,string description,string style,string category,int minutes,string layout,string reference,params JournalSection[] sections)
        { Id=id; Name=name; Description=description; Style=style; Category=category; Minutes=minutes; Layout=layout; Reference=reference; Sections=sections; }
    }
    // Original Korean prompts inspired by common journaling structures; no downloaded assets.
    public static class DiaryTemplates
    {
        public static readonly JournalTemplate[] All=SharedTemplateCatalog.All;
        public static JournalTemplate[] Find(string category,string query)
        {
            query=(query??"").Trim();
            return All.Where(t=>(string.IsNullOrEmpty(category) || t.Category==category) &&
                (query.Length==0 || (t.Name+" "+t.Description+" "+TemplateCatalog.CategoryName(t.Category)+" "+JournalLayouts.Name(t.Layout)+" "+
                string.Join(" ",t.Sections.Select(s=>s.Title+" "+s.Prompt))).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
        }
        public static List<DiaryBlock> CreateBlocks(JournalTemplate template)
        {
            if(template==null) throw new ArgumentNullException("template");
            var result=new List<DiaryBlock>();
            foreach(var section in template.Sections) {
                var block=DiaryBlock.Create(section.Kind); block.Title=section.Title; block.Prompt=section.Prompt;
                block.Wide=template.Layout=="page" || template.Layout=="letter" || template.Layout=="timeline"; result.Add(block);
            }
            return result;
        }
        public static bool Append(DiaryEntry entry,JournalTemplate template,int canvasWidth=800)
        {
            if(entry.Blocks.Count+template.Sections.Length>DesktopContract.MaxBlocks) return false;
            var positions=JournalLayouts.Arrange(template.Layout,template.Sections.Length,canvasWidth);
            int bottom=entry.Blocks.Count==0?0:entry.Blocks.Max(b=>DiaryLayout.Bounds(b).Bottom)+16;
            int index=0;
            foreach(var block in CreateBlocks(template)) {
                entry.Blocks.Add(block);
                if(entry.LayoutMode=="free") { var r=positions[index]; r.Offset(0,bottom); DiaryLayout.SetBounds(block,r); }
                index++;
            }
            return true;
        }
        public static DiaryEntry NewPage()
        {
            var entry=DiaryEntry.Empty(); entry.PageStyle="paper";
            entry.Blocks=CreateBlocks(All[0]); return entry;
        }
    }
}
