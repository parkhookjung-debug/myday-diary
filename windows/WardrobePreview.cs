using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows
{
    internal static partial class Tests
    {
        public static void WardrobePreview(string directory)
        {
            Directory.CreateDirectory(directory);
            var forms=SangmonItems.Forms.Select((f,i)=>SangmonItems.All.First(item=>item.Form==f.Split('|')[0] && (int)item.Element==i%6)).ToArray();
            using(var image=new Bitmap(1500,1000)) {
                using(var g=Graphics.FromImage(image))using(var font=new Font("맑은 고딕",11,FontStyle.Bold,GraphicsUnit.Pixel)) {
                    g.Clear(Color.FromArgb(21,28,45));
                    for(int i=0;i<forms.Length;i++) {
                        int x=i%10*150,y=i/10*200;var item=forms[i];
                        using(var fill=new SolidBrush(Color.FromArgb(35,45,66)))g.FillRectangle(fill,x+4,y+4,142,192);
                        ItemPainter.Draw(g,new RectangleF(x+12,y+12,126,149),item,.2);
                        using(var fill=new SolidBrush(Color.FromArgb(236,231,217)))g.DrawString(item.Name,font,fill,x+9,y+168);
                        using(var fill=new SolidBrush(Color.FromArgb(168,184,201)))g.DrawString(SangmonItems.SlotName(item.Slot),font,fill,x+9,y+184);
                    }
                }
                image.Save(Path.Combine(directory,"wardrobe-50-forms.png"));
            }
            using(var image=new Bitmap(1200,600)) {
                using(var g=Graphics.FromImage(image))using(var font=new Font("맑은 고딕",13,FontStyle.Bold,GraphicsUnit.Pixel)) {
                    g.Clear(Color.FromArgb(240,236,229));
                    for(int i=0;i<8;i++) {
                        int x=i%4*300,y=i/4*300;var gear=new SangmonEquipment();
                        foreach(string slot in SangmonItems.Slots) {var choices=SangmonItems.All.Where(item=>item.Slot==slot).ToArray();var selected=choices[(i*7+Array.IndexOf(SangmonItems.Slots,slot)*3)%choices.Length];gear=SangmonItems.Select(gear,slot,selected.Id);}
                        using(var fill=new SolidBrush(i%2==0?Color.FromArgb(250,248,243):Color.FromArgb(35,45,66)))g.FillRectangle(fill,x+4,y+4,292,292);
                        MonsterPainter.Draw(g,new Rectangle(x+8,y+28,284,220),MonsterPose.ForActivity(.2,PetActivity.Rest,.2,0,0),true,MonsterVariant.Original,gear);
                        using(var fill=new SolidBrush(i%2==0?Color.FromArgb(84,75,67):Color.White))g.DrawString("LOOK "+(i+1)+" · 8개 슬롯 조합",font,fill,x+16,y+268);
                    }
                }
                image.Save(Path.Combine(directory,"outfits-eight-slots.png"));
            }
        }
    }
}
