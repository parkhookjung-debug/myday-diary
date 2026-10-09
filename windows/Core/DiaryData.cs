using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using MyDay.Windows.Character;

namespace MyDay.Windows.Core
{
    [DataContract]
    public sealed class DiaryBlock
    {
        [DataMember] public string Id;
        [DataMember] public string Kind;
        [DataMember] public string Text;
        [DataMember] public bool Checked;
        [DataMember] public bool Wide;
        [DataMember(EmitDefaultValue=false)] public string Title;
        [DataMember(EmitDefaultValue=false)] public string Prompt;
        [DataMember(EmitDefaultValue=false)] public string Photo;
        internal string ValidatedPhoto;
        [DataMember(EmitDefaultValue=false)] public int X;
        [DataMember(EmitDefaultValue=false)] public int Y;
        [DataMember(EmitDefaultValue=false)] public int Width;
        [DataMember(EmitDefaultValue=false)] public int Height;
        public static DiaryBlock Create(string kind)
        {
            return new DiaryBlock { Id = Guid.NewGuid().ToString("N"), Kind = kind, Text = "" };
        }
    }

    [DataContract]
    public sealed class DiaryEntry
    {
        [DataMember] public int Theme;
        [DataMember] public string Mood;
        [DataMember] public List<DiaryBlock> Blocks;
        [DataMember(EmitDefaultValue=false)] public string LayoutMode="cards";
        [DataMember(EmitDefaultValue=false)] public string PageStyle="plain";
        public static DiaryEntry Empty()
        {
            return new DiaryEntry { Mood = "평온해요", Blocks = new List<DiaryBlock>() };
        }
        public static DiaryEntry FirstPage()
        {
            var entry = Empty();
            entry.Blocks.Add(DiaryBlock.Create("text"));
            entry.Blocks.Add(DiaryBlock.Create("todo"));
            entry.Blocks.Add(DiaryBlock.Create("habit"));
            entry.Blocks.Add(DiaryBlock.Create("emotion"));
            return entry;
        }
    }

    [DataContract]
    public sealed class DiaryBook
    {
        [DataMember] public int Version = 1;
        [DataMember] public Dictionary<string, DiaryEntry> Days = new Dictionary<string, DiaryEntry>();
        [DataMember(EmitDefaultValue=false)] public string CharacterStyle="original";
        [DataMember(EmitDefaultValue=false)] public List<SavedLayout> Layouts=new List<SavedLayout>();
    }

    public sealed class DiaryStore
    {
        public const long MaxFileBytes=128L*1024*1024;
        public readonly string FilePath;
        public DiaryStore(string directory) { FilePath = Path.Combine(directory, "diary.json"); }
        public DiaryBook Load()
        {
            if (!File.Exists(FilePath)) return new DiaryBook();
            using (var stream = File.OpenRead(FilePath)) return Read(stream);
        }
        public static DiaryBook Read(Stream stream)
        {
            if(stream.CanSeek) CheckSize(stream.Length);
            var result = (DiaryBook)new DataContractJsonSerializer(typeof(DiaryBook)).ReadObject(stream);
            Validate(result);
            return result;
        }
        public static void Validate(DiaryBook book)
        {
            if (book == null || book.Version != 1 && book.Version != 2 || book.Days == null)
                throw new InvalidDataException("지원하지 않는 기록 파일입니다.");
            if (book.Days.Count > 50000) throw new InvalidDataException("기록 파일이 너무 큽니다.");
            book.CharacterStyle=MonsterVariants.Id(MonsterVariants.FromId(book.CharacterStyle));
            if(book.Layouts==null) book.Layouts=new List<SavedLayout>();
            SavedLayouts.Validate(book.Layouts);
            long photoCharacters=0;
            foreach (var pair in book.Days)
            {
                DateTime date;
                if (!DateTime.TryParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    throw new InvalidDataException("기록의 날짜 형식이 올바르지 않습니다.");
                var entry = pair.Value;
                if (entry == null || entry.Blocks == null || entry.Blocks.Count > 200 || entry.Theme < 0 || entry.Theme > 2)
                    throw new InvalidDataException("일기 구성이 올바르지 않습니다.");
                if(entry.LayoutMode!="free") entry.LayoutMode="cards";
                if(!new[] {"plain","paper","dots"}.Contains(entry.PageStyle)) entry.PageStyle="plain";
                var ids = new HashSet<string>();
                foreach (var block in entry.Blocks)
                {
                    if (block == null || String.IsNullOrWhiteSpace(block.Id) || !ids.Add(block.Id) ||
                        !new[] { "text", "todo", "habit", "emotion", "photo", "photo-slot" }.Contains(block.Kind) || block.Text == null || block.Text.Length > 100000)
                        throw new InvalidDataException("일기 블록 형식이 올바르지 않습니다.");
                    photoCharacters+=block.Photo==null?0:block.Photo.Length;
                    if(photoCharacters>DiaryPhoto.MaxBookCharacters) throw new InvalidDataException("전체 사진 저장 공간이 가득 찼어요. 백업 후 사용하지 않는 사진 블록을 정리해주세요.");
                    DiaryPhoto.Validate(block);
                    if(block.Title!=null && block.Title.Length>80 || block.Prompt!=null && block.Prompt.Length>300)
                        throw new InvalidDataException("일기 제목이나 질문이 너무 깁니다.");
                    if(block.X<0 || block.Y<0 || block.X>DiaryLayout.MaxPosition || block.Y>DiaryLayout.MaxPosition ||
                        block.Width<0 || block.Width>DiaryLayout.MaxWidth || block.Width>0 && block.Width<DiaryLayout.MinWidth ||
                        block.Height<0 || block.Height>DiaryLayout.MaxHeight || block.Height>0 && block.Height<DiaryLayout.MinHeight)
                        throw new InvalidDataException("일기 블록의 위치 또는 크기가 올바르지 않습니다.");
                }
                if (entry.Mood == null) entry.Mood = "평온해요";
            }
        }
        public void Save(DiaryBook book)
        {
            Validate(book);
            if(book.Layouts.Count>0 || book.Days.Values.Any(e=>e.Blocks.Any(b=>b.Kind=="photo-slot"))) book.Version=2;
            var directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);
            var pending = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(stream, book);
                    CheckSize(stream.Position);
                    stream.Flush(true);
                }
                if (File.Exists(FilePath)) File.Replace(pending, FilePath, FilePath + ".bak", true);
                else File.Move(pending, FilePath);
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }
        public void Export(string destination, DiaryBook book)
        {
            // Serialize before touching the selected destination.
            using (var buffer = new MemoryStream())
            {
                Validate(book);
                if(book.Layouts.Count>0 || book.Days.Values.Any(e=>e.Blocks.Any(b=>b.Kind=="photo-slot"))) book.Version=2;
                new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(buffer, book);
                CheckSize(buffer.Length);
                File.WriteAllBytes(destination, buffer.ToArray());
            }
        }
        public static string Key(DateTime date) { return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        private static void CheckSize(long size)
        {
            if(size>MaxFileBytes) throw new InvalidDataException("기록 파일은 128MB까지 저장·가져올 수 있습니다. 백업 후 오래된 기록을 정리해주세요.");
        }
    }
}
