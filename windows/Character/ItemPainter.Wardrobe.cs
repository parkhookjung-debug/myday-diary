using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MyDay.Windows.Core;
using Shape=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    public static partial class ItemPainter
    {
        private static void Wardrobe(Graphics g,SangmonItem item,double seconds)
        {
            Color tone=Tone(item.Element),light=Color.FromArgb(239,250,255),dark=Color.FromArgb(tone.R/3,tone.G/3,tone.B/3);
            Action<Shape> metal=shape=>{using(shape)Paint(g,shape,light,dark,tone);};
            Action<Shape> cloth=shape=>{using(shape)Paint(g,shape,Color.FromArgb((tone.R+255)/2,(tone.G+255)/2,(tone.B+255)/2),tone,Gold);};
            switch(item.Form) {
                case "dagger":metal(new Shape().M(100,32).L(121,116).L(106,151).L(106,210).L(94,210).L(94,151).L(79,116).Close());Line(g,new Shape().M(71,147).L(129,147),7,Gold);break;
                case "axe":Line(g,new Shape().M(100,44).L(100,215),12,Gold);metal(new Shape().M(96,63).Q(61,55,33,87).L(45,139).Q(69,112,97,113).Close());metal(new Shape().M(104,63).Q(139,55,167,87).L(155,139).Q(131,112,103,113).Close());break;
                case "hammer":Line(g,new Shape().M(100,95).L(100,215),13,Gold);metal(new Shape().M(38,49).L(162,49).L(174,67).L(174,117).L(26,117).L(26,67).Close());break;
                case "staff":Line(g,new Shape().M(100,69).L(100,215),9,Gold);using(var p=new Pen(tone,9))g.DrawArc(p,64,26,72,88,40,290);Gem(g,104,65,19,tone);break;
                case "spear":metal(new Shape().M(100,18).L(127,88).L(108,116).L(106,218).L(94,218).L(92,116).L(73,88).Close());break;
                case "bow":Line(g,new Shape().M(65,30).Q(160,120,65,210),12,tone);Line(g,new Shape().M(65,30).L(92,120).L(65,210),2,Gold);metal(new Shape().M(33,116).L(143,116).L(143,106).L(176,120).L(143,134).L(143,124).L(33,124).Close());break;
                case "wand":Line(g,new Shape().M(80,203).L(118,74),10,Gold);Spark(g,120,65,34,tone);Gem(g,120,65,12,tone);break;
                case "scythe":Line(g,new Shape().M(89,59).L(100,218),10,Gold);metal(new Shape().M(44,67).C(90,15,155,18,177,107).Q(133,51,94,68).L(68,90).Close());break;
                case "book":cloth(new Shape().M(27,57).Q(65,43,100,66).Q(138,43,173,57).L(173,182).Q(135,169,100,194).Q(60,169,27,182).Close());Line(g,new Shape().M(100,66).L(100,194).M(40,81).L(80,87).M(120,87).L(160,81),3,Gold);break;
                case "fan":cloth(new Shape().M(100,194).L(24,95).Q(100,7,176,95).Close());for(int i=0;i<7;i++)Line(g,new Shape().M(100,194).L(32+i*23,92-(float)Math.Sin(i*Math.PI/6)*29),2,Gold);break;
                case "umbrella":cloth(new Shape().M(22,109).Q(100,5,178,109).Q(150,90,126,110).Q(100,90,74,110).Q(45,90,22,109).Close());Line(g,new Shape().M(100,47).L(100,190).Q(100,219,125,204),5,Gold);break;
                case "crystal":metal(new Shape().M(100,30).L(151,83).L(139,173).L(100,209).L(61,173).L(49,83).Close());Line(g,new Shape().M(100,30).L(81,110).L(100,209).M(81,110).L(151,83),3,Gold);break;
                case "lantern":Line(g,new Shape().M(72,55).Q(100,9,128,55),5,Gold);metal(new Shape().M(59,59).L(141,59).L(156,181).L(44,181).Close());Spark(g,100,125,33,tone);Line(g,new Shape().M(45,188).L(155,188),10,Gold);break;
                case "compass":using(var b=new SolidBrush(dark))g.FillEllipse(b,30,50,140,140);using(var p=new Pen(Gold,7))g.DrawEllipse(p,30,50,140,140);metal(new Shape().M(100,57).L(122,120).L(100,183).L(78,120).Close());Spark(g,100,120,17,tone);break;
                case "bell":cloth(new Shape().M(44,168).Q(66,145,65,100).C(65,45,135,45,135,100).Q(134,145,156,168).Close());using(var b=new SolidBrush(Gold))g.FillEllipse(b,89,171,22,22);break;
                case "bottle":metal(new Shape().M(78,40).L(122,40).L(122,81).Q(159,108,150,180).Q(100,211,50,180).Q(41,108,78,81).Close());cloth(new Shape().M(58,142).Q(100,119,142,142).L(141,176).Q(100,197,59,176).Close());Line(g,new Shape().M(77,45).L(123,45),11,Gold);break;
                case "crown":cloth(new Shape().M(32,158).L(24,68).L(65,102).L(100,48).L(135,102).L(176,68).L(168,158).Close());Gem(g,100,126,16,tone);break;
                case "hood":cloth(new Shape().M(28,174).C(14,38,174,16,172,174).L(139,154).Q(143,87,100,77).Q(57,87,61,154).Close());break;
                case "wizardhat":cloth(new Shape().M(50,153).L(99,28).Q(117,73,145,153).Close());cloth(new Shape().M(23,158).Q(100,127,177,158).Q(164,181,100,176).Q(36,181,23,158).Close());Spark(g,100,102,15,Gold);break;
                case "helmet":metal(new Shape().M(30,160).L(34,100).C(40,37,160,37,166,100).L(170,160).L(133,153).L(133,109).L(67,109).L(67,153).Close());Line(g,new Shape().M(100,53).L(100,102),7,Gold);break;
                case "beret":cloth(new Shape().M(24,137).C(5,63,182,39,177,120).Q(161,164,40,158).Close());Line(g,new Shape().M(50,158).L(151,150).M(111,70).L(116,50),7,Gold);break;
                case "flower":for(int i=0;i<6;i++){var s=g.Save();g.TranslateTransform(100,117);g.RotateTransform(i*60);using(var b=new SolidBrush(tone))g.FillEllipse(b,-20,-69,40,66);g.Restore(s);}Gem(g,100,117,22,Gold);break;
                case "halo":using(var p=new Pen(Color.FromArgb(80,tone),25))g.DrawEllipse(p,29,80,142,64);using(var p=new Pen(tone,8))g.DrawEllipse(p,29,80,142,64);Spark(g,150,89,12,Gold);break;
                case "antlers":Line(g,new Shape().M(68,175).Q(27,107,43,48).M(45,108).L(20,85).M(43,83).L(66,60).M(132,175).Q(173,107,157,48).M(155,108).L(180,85).M(157,83).L(134,60),10,tone);break;
                case "glasses":using(var p=new Pen(tone,6)){g.DrawEllipse(p,28,88,57,46);g.DrawEllipse(p,115,88,57,46);g.DrawLine(p,85,108,115,108);}break;
                case "monocle":using(var p=new Pen(Gold,7))g.DrawEllipse(p,115,84,58,58);Line(g,new Shape().M(158,137).Q(185,176,157,193),3,tone);break;
                case "mask":cloth(new Shape().M(20,88).Q(60,65,100,94).Q(140,65,180,88).L(167,142).L(100,130).L(33,142).Close());using(var b=new SolidBrush(Color.FromArgb(28,31,43))){g.FillEllipse(b,40,98,32,18);g.FillEllipse(b,128,98,32,18);}break;
                case "visor":metal(new Shape().M(22,84).Q(100,68,178,84).L(168,141).Q(100,157,32,141).Close());Line(g,new Shape().M(40,102).L(152,102),8,tone);break;
                case "starpatch":Spark(g,140,111,37,tone);Line(g,new Shape().M(20,96).L(117,111).M(164,111).L(182,95),3,Gold);break;
                case "cheeks":Spark(g,45,144,17,tone);Spark(g,155,144,17,tone);break;
                case "scarf":cloth(new Shape().M(30,100).Q(100,137,170,100).L(165,143).Q(100,171,35,143).Close());cloth(new Shape().M(115,145).L(151,140).L(157,207).L(130,194).L(120,207).Close());break;
                case "bowtie":cloth(new Shape().M(92,122).L(29,84).L(29,172).L(92,135).L(108,135).L(171,172).L(171,84).L(108,122).Close());Gem(g,100,128,16,Gold);break;
                case "tie":cloth(new Shape().M(82,73).L(118,73).L(110,106).L(129,178).L(100,207).L(71,178).L(90,106).Close());break;
                case "pendant":Line(g,new Shape().M(27,76).Q(100,211,173,76),5,Gold);Gem(g,100,157,30,tone);break;
                case "collar":cloth(new Shape().M(24,98).L(82,115).L(100,170).L(118,115).L(176,98).L(149,167).L(100,139).L(51,167).Close());break;
                case "headphones":using(var p=new Pen(Gold,13))g.DrawArc(p,34,48,132,135,180,180);metal(new Shape().M(24,105).L(62,105).L(62,169).L(24,169).Close());metal(new Shape().M(138,105).L(176,105).L(176,169).L(138,169).Close());break;
                case "wings":cloth(new Shape().M(100,166).C(54,151,13,112,21,44).Q(80,65,100,120).Q(120,65,179,44).C(187,112,146,151,100,166).Close());Line(g,new Shape().M(100,166).L(37,74).M(100,166).L(163,74),3,Gold);break;
                case "cape":cloth(new Shape().M(65,45).L(135,45).L(177,205).L(135,185).L(100,210).L(65,185).L(23,205).Close());Line(g,new Shape().M(67,49).Q(100,79,133,49),7,Gold);break;
                case "backpack":cloth(new Shape().M(45,72).Q(100,28,155,72).L(165,192).Q(100,211,35,192).Close());metal(new Shape().M(56,129).L(144,129).L(144,180).L(56,180).Close());Line(g,new Shape().M(77,59).Q(100,16,123,59),7,Gold);break;
                case "rocket":metal(new Shape().M(100,25).C(167,102,134,175,126,181).L(74,181).C(66,175,33,102,100,25).Close());Gem(g,100,102,23,tone);cloth(new Shape().M(72,144).L(34,194).L(76,179).M(128,144).L(166,194).L(124,179).Close());break;
                case "leaf":cloth(new Shape().M(48,193).C(20,88,103,40,174,33).C(182,132,127,196,48,193).Close());Line(g,new Shape().M(36,209).L(152,61).M(78,155).L(64,111).M(105,123).L(144,123),4,Gold);break;
                case "banner":Line(g,new Shape().M(50,31).L(50,215),8,Gold);cloth(new Shape().M(53,41).L(170,53).L(141,100).L(171,144).L(53,132).Close());Spark(g,104,88,24,Gold);break;
                case "ring":using(var p=new Pen(tone,5))g.DrawEllipse(p,22,46,156,156);using(var p=new Pen(Gold,2))g.DrawEllipse(p,35,59,130,130);for(int i=0;i<8;i++){var s=g.Save();g.TranslateTransform(100,124);g.RotateTransform(i*45+(float)seconds*6);Spark(g,0,-78,10,tone);g.Restore(s);}break;
                case "runes":for(int i=0;i<8;i++){var s=g.Save();g.TranslateTransform(100,120);g.RotateTransform(i*45);Line(g,new Shape().M(-10,-60).L(0,-85).L(10,-60).M(-6,-70).L(6,-70),4,tone);g.Restore(s);}break;
                case "stars":for(int i=0;i<7;i++){double a=i*Math.PI*2/7;Spark(g,100+(float)Math.Cos(a)*70,120+(float)Math.Sin(a)*76,11+i%3*3,tone);}break;
                case "bubbles":for(int i=0;i<8;i++){double a=i*Math.PI/4;using(var p=new Pen(tone,3))g.DrawEllipse(p,91+(float)Math.Cos(a)*70,111+(float)Math.Sin(a)*76,18+i%2*6,18+i%2*6);}break;
                case "cloud":cloth(new Shape().M(36,164).C(12,142,28,110,56,116).C(57,75,109,77,118,107).C(154,86,179,123,163,143).C(190,160,172,186,145,181).L(57,185).Q(25,192,36,164).Close());break;
                case "board":metal(new Shape().M(22,139).Q(100,114,178,139).L(163,166).Q(100,190,37,166).Close());Line(g,new Shape().M(45,185).L(156,185),5,tone);break;
            }
            // An element sigil distinguishes material families even at small sizes.
            if(item.Slot=="weapon" || item.Slot=="charm")Gem(g,100,119,8,tone);
        }

        private static RectangleF Placement(string slot)
        {
            switch(slot) {
                case "head":return new RectangleF(184,350,190,180);
                case "face":return new RectangleF(174,380,200,210);
                case "neck":return new RectangleF(175,470,210,180);
                case "back":return new RectangleF(335,440,220,260);
                case "aura":return new RectangleF(120,425,420,365);
                case "ground":return new RectangleF(145,690,350,135);
                case "weapon":return new RectangleF(415,450,190,300);
                default:return new RectangleF(340,410,118,126);
            }
        }
        private static RectangleF WeaponPlacement(SangmonItem item) {return item.Form=="sword"?new RectangleF(415,450,190,300):new RectangleF(370,460,190,260);}
        internal static void DrawWearables(Graphics g,MonsterPose pose,SangmonEquipment equipment,bool behind)
        {
            foreach(string slot in behind?new[] {"ground","aura","back"}:new[] {"neck","head","face"}) {
                var item=SangmonItems.Find(SangmonItems.Get(equipment,slot));
                if(item!=null)Draw(g,Placement(slot),item,pose.Phase);
            }
        }
        public static void DrawOverlay(Graphics g,Rectangle area,SangmonItem item)
        {
            var save=g.Save();float scale=Math.Min(area.Width/555f,area.Height/430f);
            g.TranslateTransform(area.X+area.Width/2f,area.Y+area.Height/2f);g.ScaleTransform(scale,scale);g.TranslateTransform(-275,-615);
            if(item.Slot=="weapon"){g.TranslateTransform(480,625);g.RotateTransform(-15);g.TranslateTransform(-480,-625);}
            Draw(g,item.Slot=="weapon"?WeaponPlacement(item):Placement(item.Slot),item,0);g.Restore(save);
        }
    }
}
