using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MyDay.Windows.Core
{
    // Embed a bounded, metadata-free JPEG so JSON backups remain self-contained.
    public static class DiaryPhoto
    {
        public const int MaxInputBytes=20*1024*1024,MaxBytes=2*1024*1024,MaxEdge=1600;
        public const int MaxEncodedLength=((MaxBytes+2)/3)*4;
        public const long MaxBookCharacters=64L*1024*1024;
        public static string FromFile(string path)
        {
            using(var stream=File.OpenRead(path)) {
                if(stream.Length==0 || stream.Length>MaxInputBytes) throw new InvalidDataException("사진은 20MB 이하로 선택해주세요.");
                try {
                    using(var source=Image.FromStream(stream,false,true)) {
                        if(source.RawFormat.Guid!=ImageFormat.Jpeg.Guid && source.RawFormat.Guid!=ImageFormat.Png.Guid &&
                            source.RawFormat.Guid!=ImageFormat.Bmp.Guid && source.RawFormat.Guid!=ImageFormat.Gif.Guid)
                            throw new InvalidDataException("JPG, PNG, BMP, GIF 사진을 선택해주세요.");
                        if(source.Width>12000 || source.Height>12000 || (long)source.Width*source.Height>25000000)
                            throw new InvalidDataException("사진 해상도가 너무 큽니다. 2,500만 화소 이하로 선택해주세요.");
                        ApplyOrientation(source);
                        double scale=Math.Min(1,MaxEdge/(double)Math.Max(source.Width,source.Height));
                        using(var bitmap=new Bitmap(Math.Max(1,(int)Math.Round(source.Width*scale)),Math.Max(1,(int)Math.Round(source.Height*scale)),PixelFormat.Format24bppRgb))
                        using(var graphics=Graphics.FromImage(bitmap))
                        using(var output=new MemoryStream())
                        using(var parameters=new EncoderParameters(1)) {
                            graphics.Clear(Color.White); graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                            graphics.DrawImage(source,new Rectangle(0,0,bitmap.Width,bitmap.Height));
                            parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,85L);
                            bitmap.Save(output,ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid),parameters);
                            if(output.Length>MaxBytes) throw new InvalidDataException("사진 용량이 너무 큽니다. 작은 사진을 선택해주세요.");
                            return Convert.ToBase64String(output.ToArray());
                        }
                    }
                } catch(ArgumentException ex) { throw new InvalidDataException("사진을 읽지 못했어요. 이미지 파일을 확인해주세요.",ex); }
                  catch(OutOfMemoryException ex) { throw new InvalidDataException("사진을 읽지 못했어요. 더 작은 이미지 파일을 선택해주세요.",ex); }
            }
        }
        internal static void ApplyOrientation(Image image)
        {
            if(!image.PropertyIdList.Contains(0x112)) return;
            var value=image.GetPropertyItem(0x112).Value;
            if(value.Length<2) return;
            int orientation=BitConverter.ToUInt16(value,0);
            var rotations=new[] {RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.RotateNoneFlipX,RotateFlipType.Rotate180FlipNone,RotateFlipType.Rotate180FlipX,
                RotateFlipType.Rotate90FlipX,RotateFlipType.Rotate90FlipNone,RotateFlipType.Rotate270FlipX,RotateFlipType.Rotate270FlipNone};
            if(orientation>=2 && orientation<=8) image.RotateFlip(rotations[orientation]);
        }
        public static Bitmap Decode(string data)
        {
            if(string.IsNullOrEmpty(data) || data.Length>MaxEncodedLength) throw new InvalidDataException("사진 데이터의 크기가 올바르지 않습니다.");
            try {
                byte[] bytes=Convert.FromBase64String(data);
                if(bytes.Length>MaxBytes || bytes.Length<3 || bytes[0]!=255 || bytes[1]!=216 || bytes[2]!=255)
                    throw new InvalidDataException("저장된 사진 형식이 올바르지 않습니다.");
                using(var stream=new MemoryStream(bytes))
                using(var image=Image.FromStream(stream,false,true)) {
                    if(image.RawFormat.Guid!=ImageFormat.Jpeg.Guid || image.Width>MaxEdge || image.Height>MaxEdge)
                        throw new InvalidDataException("저장된 사진 해상도가 올바르지 않습니다.");
                    return new Bitmap(image);
                }
            } catch(FormatException ex) { throw new InvalidDataException("사진 데이터가 손상되었습니다.",ex); }
              catch(ArgumentException ex) { throw new InvalidDataException("사진 데이터가 손상되었습니다.",ex); }
              catch(OutOfMemoryException ex) { throw new InvalidDataException("사진 데이터가 손상되었습니다.",ex); }
        }
        public static void Validate(DiaryBlock block)
        {
            if(block.Kind!="photo") {
                if(block.Photo!=null) throw new InvalidDataException("사진은 사진 블록에만 저장할 수 있습니다.");
                return;
            }
            if(block.Photo==null) throw new InvalidDataException("사진 블록에 이미지가 없습니다.");
            if(!object.ReferenceEquals(block.Photo,block.ValidatedPhoto)) {
                using(var image=Decode(block.Photo)) { }
                block.ValidatedPhoto=block.Photo;
            }
        }
    }
}
