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
    }

    public sealed class DiaryStore
    {
        public readonly string FilePath;
        public DiaryStore(string directory) { FilePath = Path.Combine(directory, "diary.json"); }
        public DiaryBook Load()
        {
            if (!File.Exists(FilePath)) return new DiaryBook();
            using (var stream = File.OpenRead(FilePath)) return Read(stream);
        }
        public static DiaryBook Read(Stream stream)
        {
            var result = (DiaryBook)new DataContractJsonSerializer(typeof(DiaryBook)).ReadObject(stream);
            Validate(result);
            return result;
        }
        public static void Validate(DiaryBook book)
        {
            if (book == null || book.Version != 1 || book.Days == null)
                throw new InvalidDataException("지원하지 않는 기록 파일입니다.");
            if (book.Days.Count > 50000) throw new InvalidDataException("기록 파일이 너무 큽니다.");
            book.CharacterStyle=MonsterVariants.Id(MonsterVariants.FromId(book.CharacterStyle));
            foreach (var pair in book.Days)
            {
                DateTime date;
                if (!DateTime.TryParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    throw new InvalidDataException("기록의 날짜 형식이 올바르지 않습니다.");
                var entry = pair.Value;
                if (entry == null || entry.Blocks == null || entry.Blocks.Count > 200 || entry.Theme < 0 || entry.Theme > 2)
                    throw new InvalidDataException("일기 구성이 올바르지 않습니다.");
                var ids = new HashSet<string>();
                foreach (var block in entry.Blocks)
                {
                    if (block == null || String.IsNullOrWhiteSpace(block.Id) || !ids.Add(block.Id) ||
                        !new[] { "text", "todo", "habit", "emotion" }.Contains(block.Kind) || block.Text == null || block.Text.Length > 100000)
                        throw new InvalidDataException("일기 블록 형식이 올바르지 않습니다.");
                }
                if (entry.Mood == null) entry.Mood = "평온해요";
            }
        }
        public void Save(DiaryBook book)
        {
            Validate(book);
            var directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);
            var pending = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(stream, book);
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
                new DataContractJsonSerializer(typeof(DiaryBook)).WriteObject(buffer, book);
                File.WriteAllBytes(destination, buffer.ToArray());
            }
        }
        public static string Key(DateTime date) { return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
    }
}
