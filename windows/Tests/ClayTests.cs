using System.Drawing;
using MyDay.Windows.Character;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        private static void RunClayTests()
        {
            using(var idle=new Bitmap(448,384))
            using(var yawn=new Bitmap(448,384)) {
                using(var g=Graphics.FromImage(idle)) {g.TranslateTransform(-96,-416); ClayMaterial.Face(g,MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0));}
                using(var g=Graphics.FromImage(yawn)) {g.TranslateTransform(-96,-416); ClayMaterial.Face(g,MonsterPose.ForActivity(.8,PetActivity.Yawn,.8,0,0));}
                bool sameLip=true,eyesChanged=false;
                for(int y=0;y<384;y++) for(int x=0;x<448;x++) {
                    if(y>=160) sameLip&=idle.GetPixel(x,y)==yawn.GetPixel(x,y);
                    else eyesChanged|=idle.GetPixel(x,y)!=yawn.GetPixel(x,y);
                }
                Check(sameLip && eyesChanged,"Clay yawn closes the eyes while retaining the exact side lip with no new mouth");
            }
            using(var face=new Bitmap(448,384)) {
                using(var g=Graphics.FromImage(face)) {g.TranslateTransform(-96,-416);ClayMaterial.Face(g,MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0));}
                Check(face.GetPixel(208-96,495-416).A==255 && face.GetPixel(208-96,482-416).A==0 && face.GetPixel(195-96,495-416).A==0,"Redesigned eyes are solid round dots rather than tall glossy ovals");
                Check(face.GetPixel(205-96,625-416).A==0 && face.GetPixel(263-96,663-416).A>100,"Reference mouth remains an open curve with a small tongue and no filled snout");
            }
            using(var image=new Bitmap(320,280)) {
                using(var g=Graphics.FromImage(image)) MonsterPainter.DrawLegacy(g,new Rectangle(12,18,296,226),MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0),true);
                int min=255,max=0,body=0;
                for(int y=0;y<280;y++) for(int x=0;x<320;x++) {
                    var pixel=image.GetPixel(x,y);
                    if(pixel.A==255 && pixel.R>180 && pixel.G>160 && pixel.B>150) {min=System.Math.Min(min,pixel.R);max=System.Math.Max(max,pixel.R);body++;}
                }
                Check(body>8000 && max-min>35 && image.GetPixel(0,0).A==0 && image.GetPixel(200,192).A==255 && image.GetPixel(200,214).A<32,"Clay body has rounded light and shadow, transparent margins and the redesigned low flat belly");
            }
        }
    }
}
