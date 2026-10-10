using System;
using System.Linq;

namespace MyDay.Windows.Core
{
    public static partial class TemplateCatalog
    {
        public static readonly string[] Categories={"daily","reflection","gratitude","emotions","learning","planning","wellbeing","relationships","creativity","memories"};
        public static readonly string[] CategoryNames={"일상 기록","회고·성장","감사·행복","마음 정리","학습·독서","목표·계획","생활·루틴","관계·편지","창작·취향","여행·추억"};
        public static string CategoryName(string id) { int i=Array.IndexOf(Categories,id); return i<0?id:CategoryNames[i]; }
        public static readonly string[] ReferenceIds={"dayone","journey","bullet","gibbs","what","cornell","creative","dream"};
        public static string ReferenceUrl(string id)
        {
            switch(id) {
                case "journey":return "https://journey.cloud/types-of-diary";
                case "bullet":return "https://bulletjournal.com/blogs/faq/collections";
                case "gibbs":return "https://reflection.ed.ac.uk/reflectors-toolkit/reflecting-on-experience/gibbs-reflective-cycle";
                case "what":return "https://reflection.ed.ac.uk/reflectors-toolkit/reflecting-on-experience/what-so-what-now-what";
                case "cornell":return "https://lsc.cornell.edu/how-to-study/taking-notes/cornell-note-taking-system/";
                case "creative":return "https://blog.journey.cloud/how-to-start-a-creative-journal/";
                case "dream":return "https://journey.cloud/dream-journal";
                default:return "https://dayoneapp.com/guides/tips-and-tutorials/templates/";
            }
        }
        private static JournalTemplate T(string category,string id,string name,string description,string style,int minutes,string layout,string reference,params JournalSection[] sections)
        { return new JournalTemplate(id,name,description,style,category,minutes,layout,reference,sections); }
        private static JournalSection S(string title,string prompt) { return new JournalSection("text",title,prompt); }
        private static JournalSection E(string title,string prompt) { return new JournalSection("emotion",title,prompt); }
        private static JournalSection D(string title,string prompt) { return new JournalSection("todo",title,prompt); }
        private static JournalSection H(string title,string prompt) { return new JournalSection("habit",title,prompt); }
    }
}
