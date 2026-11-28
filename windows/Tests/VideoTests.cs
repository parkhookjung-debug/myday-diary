using System;
using System.IO;
using System.Linq;
using MyDay.Windows.Core;
namespace MyDay.Windows {
    internal static partial class Tests {
        private static void RunVideoTests(string directory){
            var store=new DiaryStore(Path.Combine(directory,"video-common"));
            foreach(string path in new[]{"C:\\Videos\\산책.mp4","/Users/test/산책.mov","content://media/external/video/media/5"}){
                var book=new DiaryBook();var entry=DiaryEntry.Empty();var video=DiaryBlock.Create("video");video.VideoPath=path;video.Text="영상 설명";entry.Blocks.Add(video);book.Days["2026-11-28"]=entry;store.Save(book);var restored=store.Load();
                Check(restored.Version==10&&restored.Days["2026-11-28"].Blocks[0].VideoPath==path&&restored.Days["2026-11-28"].Blocks[0].Text=="영상 설명","Version-ten video keeps local references for every platform");
                var baseline=SangmonGrowth.CaptureRecord(DiaryEntry.Empty());Check(!SangmonGrowth.SameRecord(baseline,entry),"A video attachment is meaningful diary activity");
            }
            var old=DiaryBlock.Create("text");old.MacVideoPath="/tmp/old.mp4";old.MacVideoText="이전 설명";DiaryVideo.Validate(old);Check(old.Kind=="video"&&old.VideoPath=="/tmp/old.mp4"&&old.Text=="이전 설명"&&old.MacVideoPath==null,"Legacy Mac video fields migrate without dropping original references");
            foreach(string path in new[]{"https://example.com/a.mp4","//server/a.mp4","\\\\server\\a.mp4","relative.mp4","/tmp/a\0.mp4"})Check(!DiaryVideo.ValidReference(path),"Network, relative or control-character video references are rejected");
            Check(!DiaryVideo.LocalPlayable("/tmp/old.mp4")&&!DiaryVideo.LocalPlayable("content://media/video/5"),"Foreign device references require reselection before Windows playback");
        }
    }
}
