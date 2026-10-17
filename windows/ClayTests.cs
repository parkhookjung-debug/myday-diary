using System.Drawing;
using MyDay.Windows.Character;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunClayTests()
        {
            using(var idle=new Bitmap(260,240))
            using(var yawn=new Bitmap(260,240)) {
                using(var g=Graphics.FromImage(idle)) {g.TranslateTransform(0,-420); ClayMaterial.Face(g,MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0));}
                using(var g=Graphics.FromImage(yawn)) {g.TranslateTransform(0,-420); ClayMaterial.Face(g,MonsterPose.ForActivity(.8,PetActivity.Yawn,.8,0,0));}
                bool sameLip=true,eyesChanged=false;
                for(int y=0;y<240;y++) for(int x=0;x<260;x++) {
                    if(y>=125) sameLip&=idle.GetPixel(x,y)==yawn.GetPixel(x,y);
                    else eyesChanged|=idle.GetPixel(x,y)!=yawn.GetPixel(x,y);
                }
                Check(sameLip && eyesChanged,"Clay yawn closes the eyes while retaining the exact side lip with no new mouth");
            }
            using(var image=new Bitmap(320,280)) {
                using(var g=Graphics.FromImage(image)) MonsterPainter.Draw(g,new Rectangle(12,18,296,226),MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0),true);
                int min=255,max=0,body=0;
                for(int y=0;y<280;y++) for(int x=0;x<320;x++) {
                    var pixel=image.GetPixel(x,y);
                    if(pixel.A==255 && pixel.R>180 && pixel.G>160 && pixel.B>150) {min=System.Math.Min(min,pixel.R);max=System.Math.Max(max,pixel.R);body++;}
                }
                Check(body>8000 && max-min>35 && image.GetPixel(0,0).A==0 && image.GetPixel(200,192).A==0,"Clay body has rounded light and shadow, transparent margins and an open lower arch");
            }
        }
    }
}
