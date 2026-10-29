using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        // Rendered by the shipped painter, so the preview is an implementation proof.
        public static void ClayPreview(string directory)
        {
            Directory.CreateDirectory(directory);
            var activities=new[] {PetActivity.Rest,PetActivity.Walk,PetActivity.Hop,PetActivity.Look,PetActivity.Fire,PetActivity.Yawn,PetActivity.Sleep,PetActivity.Drag,PetActivity.Rest};
            var labels=new[] {"숨 쉬기","작은 걸음","점프","두리번","터치 불꽃","하품 · 옆입 유지","졸기","드래그","장비 착용"};
            var ages=new[] {0.0,.2,.325,.4,.25,.4,1.0,.2,.2};
            using(var board=new Bitmap(960,900)) {
                using(var g=Graphics.FromImage(board))
                using(var font=new Font("맑은 고딕",14,FontStyle.Bold,GraphicsUnit.Pixel)) {
                    g.Clear(Color.FromArgb(237,234,228));
                    for(int i=0;i<activities.Length;i++) {
                        int x=i%3*320,y=i/3*300; bool dark=i==6 || i==8;
                        using(var brush=new SolidBrush(dark?Color.FromArgb(34,39,48):Color.FromArgb(250,248,243))) g.FillRectangle(brush,x+6,y+6,308,288);
                        var pose=MonsterPose.ForActivity(ages[i],activities[i],ages[i],0,0);
                        var equipment=i==8?new SangmonEquipment {WeaponId="frost-sword",CharmId="astral-orb"}:null;
                        MonsterPainter.Draw(g,new Rectangle(x+12,y+25,296,226),pose,true,MonsterVariant.Original,equipment);
                        using(var brush=new SolidBrush(dark?Color.FromArgb(234,231,225):Color.FromArgb(84,75,67))) g.DrawString(labels[i],font,brush,x+20,y+266);
                        using(var sprite=new Bitmap(320,280)) {
                            using(var sg=Graphics.FromImage(sprite)) MonsterPainter.Draw(sg,new Rectangle(12,18,296,226),pose,true,MonsterVariant.Original,equipment);
                            sprite.Save(Path.Combine(directory,"pose-"+i+".png"),ImageFormat.Png);
                        }
                    }
                }
                board.Save(Path.Combine(directory,"sangmon-clay-applied-v1.png"),ImageFormat.Png);
            }
        }
    }
}
