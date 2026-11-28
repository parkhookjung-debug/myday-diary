using System;
using System.IO;
using System.Linq;
namespace MyDay.Windows.Core {
    public static class DiaryVideo {
        public static bool ValidReference(string path){
            if(string.IsNullOrEmpty(path)||path.Length>4096||path.Any(char.IsControl))return false;
            if(path.StartsWith("content://",StringComparison.Ordinal))return Uri.IsWellFormedUriString(path,UriKind.Absolute);
            return path.StartsWith("/",StringComparison.Ordinal)&&!path.StartsWith("//",StringComparison.Ordinal) || path.Length>=3&&char.IsLetter(path[0])&&path[1]==':'&&(path[2]=='\\'||path[2]=='/');
        }
        public static void Validate(DiaryBlock block){
            if(block.MacVideoPath!=null){if(block.Kind!="text"||!ValidReference(block.MacVideoPath))throw new InvalidDataException("이전 영상 경로가 올바르지 않아요.");block.Kind="video";block.VideoPath=block.MacVideoPath;if(block.MacVideoText!=null)block.Text=block.MacVideoText;block.MacVideoPath=block.MacVideoText=null;}
            if(block.VideoPath!=null&&(block.Kind!="video"||!ValidReference(block.VideoPath)))throw new InvalidDataException("영상 파일 경로가 올바르지 않아요.");
        }
        public static bool LocalPlayable(string path){return ValidReference(path)&&path.Length>=3&&char.IsLetter(path[0])&&path[1]==':'&&new[]{".mp4",".mov",".m4v",".webm",".avi",".mkv"}.Contains(Path.GetExtension(path).ToLowerInvariant())&&File.Exists(path);}
    }
}
