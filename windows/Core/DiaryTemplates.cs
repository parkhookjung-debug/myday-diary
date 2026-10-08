using System;
using System.Collections.Generic;

namespace MyDay.Windows.Core
{
    public sealed class JournalSection
    {
        public readonly string Kind,Title,Prompt;
        public JournalSection(string kind,string title,string prompt) { Kind=kind; Title=title; Prompt=prompt; }
    }
    public sealed class JournalTemplate
    {
        public readonly string Id,Name,Description,Style;
        public readonly JournalSection[] Sections;
        public JournalTemplate(string id,string name,string description,string style,params JournalSection[] sections)
        { Id=id; Name=name; Description=description; Style=style; Sections=sections; }
    }
    // Original Korean prompts inspired by common journaling structures; no downloaded assets.
    public static class DiaryTemplates
    {
        public static readonly JournalTemplate[] All={
            new JournalTemplate("free","자유 일기","형식 없이, 오늘 떠오르는 이야기를 길게 써요.","paper",
                new JournalSection("text","오늘의 이야기","기억하고 싶은 장면부터 천천히 적어보세요.")),
            new JournalTemplate("reflection","하루 회고","장면 · 배움 · 내일, 세 조각으로 하루를 돌아봐요.","paper",
                new JournalSection("text","기억에 남는 장면","오늘 다시 떠올리고 싶은 순간은 무엇인가요?"),
                new JournalSection("text","오늘 알게 된 것","잘된 일이나 아쉬운 일에서 무엇을 배웠나요?"),
                new JournalSection("todo","내일의 작은 약속","내일 나를 위해 할 수 있는 한 가지를 적어요.")),
            new JournalTemplate("gratitude","감사 일기","사소한 기쁨을 놓치지 않고 남겨요.","paper",
                new JournalSection("text","고마웠던 순간","오늘 나를 웃게 한 작은 일은 무엇이었나요?"),
                new JournalSection("text","전하고 싶은 말","고마운 사람에게 마음속 인사를 남겨보세요.")),
            new JournalTemplate("questions","질문 일기","막막할 때, 세 질문을 따라 써보세요.","plain",
                new JournalSection("text","오늘을 한 단어로","그 단어를 고른 이유도 함께 적어보세요."),
                new JournalSection("emotion","내 마음이 머문 곳","오늘 가장 오래 느낀 감정은 어디서 시작됐나요?"),
                new JournalSection("text","나에게 해주고 싶은 말","오늘의 나에게 다정한 한 문장을 남겨요.")),
            new JournalTemplate("bullet","불렛 저널","할 일 · 습관 · 메모를 짧고 간결하게 정리해요.","dots",
                new JournalSection("todo","오늘의 우선순위","가장 중요한 일 하나부터 시작해요."),
                new JournalSection("habit","나를 위한 루틴","오늘 이어가고 싶은 습관을 적어요."),
                new JournalSection("text","짧은 메모","떠오른 생각과 작은 성취를 가볍게 남겨요.")),
            new JournalTemplate("letter","상몬에게 편지","누군가에게 털어놓듯, 내 마음을 자유롭게 써요.","paper",
                new JournalSection("emotion","상몬아, 오늘은 말이야","상몬에게 들려주고 싶은 이야기를 적어보세요."))
        };
        public static List<DiaryBlock> CreateBlocks(JournalTemplate template)
        {
            if(template==null) throw new ArgumentNullException("template");
            var result=new List<DiaryBlock>();
            foreach(var section in template.Sections) {
                var block=DiaryBlock.Create(section.Kind); block.Title=section.Title; block.Prompt=section.Prompt;
                block.Wide=section.Kind=="text" || section.Kind=="emotion"; result.Add(block);
            }
            return result;
        }
        public static bool Append(DiaryEntry entry,JournalTemplate template)
        {
            if(entry.Blocks.Count+template.Sections.Length>200) return false;
            foreach(var block in CreateBlocks(template)) {
                entry.Blocks.Add(block);
                if(entry.LayoutMode=="free") DiaryLayout.PlaceNew(entry,block);
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
