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
                    g.Clear(Color.FromArgb(245,238,228));
                    for(int i=0;i<forms.Length;i++) {
                        int x=i%10*150,y=i/10*200;var item=forms[i];
                        using(var fill=new SolidBrush(Color.FromArgb(255,250,243)))g.FillRectangle(fill,x+4,y+4,142,192);
                        ItemPainter.Draw(g,new RectangleF(x+12,y+12,126,149),item,.2);
                        using(var fill=new SolidBrush(Color.FromArgb(111,90,77)))g.DrawString(item.Name,font,fill,x+9,y+168);
                        using(var fill=new SolidBrush(Color.FromArgb(157,133,115)))g.DrawString(SangmonItems.SlotName(item.Slot),font,fill,x+9,y+184);
                    }
                }
                image.Save(Path.Combine(directory,"wardrobe-50-forms.png"));
            }
            using(var image=new Bitmap(1200,600)) {
                using(var g=Graphics.FromImage(image))using(var font=new Font("맑은 고딕",13,FontStyle.Bold,GraphicsUnit.Pixel)) {
                    g.Clear(Color.FromArgb(240,236,229));
                    for(int i=0;i<8;i++) {
                        int x=i%4*300,y=i/4*300;var gear=new SangmonEquipment();
                        string[][] looks={
                            new[]{"frost-sword","solar-wizardhat","shadow-glasses","ember-scarf"},
                            new[]{"ember-axe","storm-helmet","frost-backpack","astral-runes"},
                            new[]{"solar-scythe","ember-hood","frost-mask","shadow-board"},
                            new[]{"astral-staff","frost-crown","solar-bowtie","ember-ring"},
                            new[]{"storm-book","shadow-beret","solar-pendant","frost-banner"},
                            new[]{"frost-bow","astral-flower","ember-collar","solar-cloud"},
                            new[]{"ember-umbrella","frost-hood","storm-scarf","shadow-rocket"},
                            new[]{"solar-spear","shadow-antlers","astral-cheeks","storm-wings"}
                        };
                        foreach(string id in looks[i]){var chosen=SangmonItems.Find(id);gear=SangmonItems.Select(gear,chosen.Slot,chosen.Id);}
                        using(var fill=new SolidBrush(i%2==0?Color.FromArgb(250,248,243):Color.FromArgb(255,250,243)))g.FillRectangle(fill,x+4,y+4,292,292);
                        MonsterPainter.Draw(g,new Rectangle(x+8,y+28,284,220),MonsterPose.ForActivity(.2,PetActivity.Rest,.2,0,0),true,MonsterVariant.Original,gear);
                        using(var fill=new SolidBrush(i%2==0?Color.FromArgb(84,75,67):Color.FromArgb(111,90,77)))g.DrawString(new[]{"책상 위 작은 친구","꽃길 산책","포근한 낮잠","기분 좋은 날","나만의 기록 시간","따뜻한 한 잔","비 오는 오후","토끼와 사탕"}[i],font,fill,x+16,y+268);
                    }
                }
                image.Save(Path.Combine(directory,"outfits-eight-slots.png"));
            }
        }
    }
}
