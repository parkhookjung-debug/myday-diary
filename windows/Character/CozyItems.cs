using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MyDay.Windows.Core;
using Shape=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // Fifty small everyday objects, drawn in a soft stationery / handmade-toy palette.
    // Stable form keys belong to old saves; they no longer describe weapons or magic.
    internal static class CozyItems
    {
        internal static readonly Color[] Palettes={Color.FromArgb(237,218,181),Color.FromArgb(244,187,166),Color.FromArgb(181,214,232),Color.FromArgb(183,216,196),Color.FromArgb(205,191,226),Color.FromArgb(237,188,206)};
        private static readonly Color Ink=Color.FromArgb(143,116,103),Milk=Color.FromArgb(255,251,242),Leaf=Color.FromArgb(155,185,144);
        private static Color Mix(Color a,Color b,float amount) {return Color.FromArgb(a.A,(int)(a.R+(b.R-a.R)*amount),(int)(a.G+(b.G-a.G)*amount),(int)(a.B+(b.B-a.B)*amount));}
        private static void Paint(Graphics g,GraphicsPath path,Color color)
        {
            var bounds=path.GetBounds();if(bounds.Width<.1f || bounds.Height<.1f)return;
            using(var fill=new SolidBrush(Mix(color,Milk,.18f)))g.FillPath(fill,path);
            using(var pen=new Pen(Color.FromArgb(160,Ink),2.7f){LineJoin=LineJoin.Round})g.DrawPath(pen,path);
        }
        private static void Art(Graphics g,Shape path,Color color){using(path)Paint(g,path.Path,color);}
        private static void Ellipse(Graphics g,float x,float y,float w,float h,Color color){using(var path=new GraphicsPath()){path.AddEllipse(x,y,w,h);Paint(g,path,color);}}
        private static void Round(Graphics g,float x,float y,float w,float h,float radius,Color color)
        {
            float d=Math.Min(radius*2,Math.Min(w,h));
            using(var path=new GraphicsPath()){path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();Paint(g,path,color);}
        }
        private static void Line(Graphics g,Shape shape,Color color,float width=3)
        {using(shape)using(var pen=new Pen(color,width){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})g.DrawPath(pen,shape.Path);}
        private static void Dot(Graphics g,float x,float y,float radius,Color color){using(var b=new SolidBrush(color))g.FillEllipse(b,x-radius,y-radius,radius*2,radius*2);}
        private static void Heart(Graphics g,float x,float y,float size,Color color)
        {Art(g,new Shape().M(x,y+size*.8f).C(x-size*1.7f,y-size*.2f,x-size*.9f,y-size*1.25f,x,y-size*.45f).C(x+size*.9f,y-size*1.25f,x+size*1.7f,y-size*.2f,x,y+size*.8f).Close(),color);}
        private static void Star(Graphics g,float x,float y,float radius,Color color)
        {
            var points=new PointF[10];for(int i=0;i<10;i++){double a=-Math.PI/2+i*Math.PI/5;float r=i%2==0?radius:radius*.47f;points[i]=new PointF(x+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r);}
            using(var path=new GraphicsPath()){path.AddPolygon(points);Paint(g,path,color);}
        }
        private static void Flower(Graphics g,float x,float y,float size,Color petals,Color center)
        {for(int i=0;i<5;i++){var saved=g.Save();g.TranslateTransform(x,y);g.RotateTransform(i*72);Ellipse(g,-size*.35f,-size,size*.7f,size,petals);g.Restore(saved);}Ellipse(g,x-size*.27f,y-size*.27f,size*.54f,size*.54f,center);}
        private static void Bow(Graphics g,float x,float y,float size,Color color)
        {
            Art(g,new Shape().M(x-8,y).C(x-size,y-size*.8f,x-size*1.15f,y-size*.6f,x-size*.9f,y+size*.55f).Q(x-size*.8f,y+size*.8f,x-8,y+10).Close(),color);
            Art(g,new Shape().M(x+8,y).C(x+size,y-size*.8f,x+size*1.15f,y-size*.6f,x+size*.9f,y+size*.55f).Q(x+size*.8f,y+size*.8f,x+8,y+10).Close(),color);
            Round(g,x-12,y-13,24,30,9,Mix(color,Milk,.25f));
        }
        private static void Checks(Graphics g,Shape shape,Color color)
        {
            var saved=g.Save();g.SetClip(shape.Path,CombineMode.Intersect);using(var pen=new Pen(Color.FromArgb(90,Milk),8)){for(int x=25;x<190;x+=28)g.DrawLine(pen,x,40,x,211);for(int y=55;y<210;y+=28)g.DrawLine(pen,15,y,185,y);}g.Restore(saved);
        }
        public static void Draw(Graphics g,SangmonItem item)
        {
            Color tone=Palettes[(int)item.Element],pale=Mix(tone,Milk,.55f);
            switch(item.Form) {
                case "sword":
                    Art(g,new Shape().M(80,59).L(120,59).L(120,178).Q(100,185,80,178).Close(),tone);Round(g,80,35,40,29,10,pale);
                    Art(g,new Shape().M(80,178).Q(100,184,120,178).L(100,211).Close(),Milk);Art(g,new Shape().M(94,202).L(106,202).L(100,211).Close(),Ink);Line(g,new Shape().M(89,75).L(89,165),Milk,4);break;
                case "dagger":Round(g,73,68,54,127,13,tone);Art(g,new Shape().M(73,70).Q(100,25,127,70).Close(),tone);Round(g,70,108,60,50,6,Milk);Heart(g,100,133,12,tone);break;
                case "axe":Line(g,new Shape().M(100,204).Q(88,150,100,97),Leaf,7);Ellipse(g,62,140,34,15,Leaf);Ellipse(g,102,122,36,16,Leaf);Flower(g,100,85,49,tone,Milk);break;
                case "hammer":Round(g,25,76,150,108,24,tone);Round(g,48,57,44,28,9,pale);Ellipse(g,69,95,64,64,Milk);Ellipse(g,84,110,34,34,pale);Dot(g,152,94,7,Milk);Line(g,new Shape().M(38,150).L(56,150),Milk,4);break;
                case "staff":Ellipse(g,50,29,100,126,tone);Art(g,new Shape().M(98,153).L(90,165).L(111,165).Close(),tone);Line(g,new Shape().M(100,164).C(122,183,77,197,102,216),Ink,2);Line(g,new Shape().M(71,58).Q(61,72,65,88),Milk,5);break;
                case "spear":Line(g,new Shape().M(100,126).L(100,211),Milk,12);Ellipse(g,51,40,98,98,tone);using(var pen=new Pen(Milk,8)){g.DrawArc(pen,66,55,68,68,15,300);g.DrawArc(pen,82,71,36,36,160,270);}break;
                case "bow":using(var pen=new Pen(Ink,3))g.DrawEllipse(pen,135,92,43,57);using(var pen=new Pen(tone,13))g.DrawArc(pen,133,93,44,55,260,210);Round(g,34,78,110,112,23,tone);Ellipse(g,35,70,108,29,Milk);Heart(g,89,137,18,Milk);break;
                case "wand":Round(g,91,120,18,91,8,tone);using(var pen=new Pen(tone,9))g.DrawEllipse(pen,65,51,70,70);Ellipse(g,43,30,22,22,Color.FromArgb(100,pale));Ellipse(g,141,48,27,27,Color.FromArgb(100,tone));break;
                case "scythe":Art(g,new Shape().M(125,41).C(22,45,20,181,118,192).Q(149,193,172,168).C(91,184,63,78,125,41).Close(),tone);Line(g,new Shape().M(59,128).Q(67,135,74,128).M(83,145).Q(90,152,97,145),Ink,3);break;
                case "book":Round(g,42,44,116,154,15,tone);Round(g,51,44,14,154,5,pale);Round(g,79,71,56,56,10,Milk);Flower(g,107,99,18,pale,tone);Line(g,new Shape().M(80,151).L(133,151).M(80,163).L(118,163),Milk,3);break;
                case "fan":Art(g,new Shape().M(100,171).C(26,155,11,98,50,69).Q(65,46,83,66).Q(101,43,120,65).Q(150,45,164,80).C(197,120,149,165,100,171).Close(),tone);Line(g,new Shape().M(100,164).L(100,207).M(100,159).L(59,90).M(100,159).L(100,77).M(100,159).L(147,88),Milk,4);break;
                case "umbrella":Art(g,new Shape().M(24,112).C(28,34,172,34,176,112).Q(149,94,125,113).Q(100,94,75,113).Q(50,94,24,112).Close(),tone);Line(g,new Shape().M(100,50).L(100,189).Q(100,216,124,203),Ink,6);for(int i=0;i<5;i++)Dot(g,49+i*26,86-(i%2)*17,4,Milk);break;
                case "orb":using(var pen=new Pen(Ink,3))g.DrawEllipse(pen,89,34,22,27);Ellipse(g,43,61,114,114,tone);Heart(g,100,116,27,Milk);Line(g,new Shape().M(79,177).L(70,202).M(121,177).L(131,202),tone,9);break;
                case "crystal":Round(g,23,72,154,108,14,tone);Line(g,new Shape().M(27,81).Q(100,154,173,81).M(29,174).L(76,130).M(171,174).L(124,130),Mix(tone,Ink,.25f),3);Heart(g,100,127,17,Milk);break;
                case "lantern":Round(g,49,71,102,127,19,Color.FromArgb(145,pale));Round(g,45,48,110,29,10,tone);for(int i=0;i<5;i++)Dot(g,73+i%3*27,105+i/3*40,5+i%2,Palettes[(i+(int)item.Element)%6]);Line(g,new Shape().M(66,91).L(66,162),Milk,4);break;
                case "compass":Ellipse(g,30,48,140,140,tone);Ellipse(g,43,61,114,114,Milk);Line(g,new Shape().M(100,83).L(100,120).L(128,136),Ink,6);foreach(int a in new[]{0,90,180,270}){var s=g.Save();g.TranslateTransform(100,118);g.RotateTransform(a);Dot(g,0,-43,4,tone);g.Restore(s);}break;
                case "bell":Art(g,new Shape().M(48,97).L(51,54).Q(72,61,79,77).Q(100,67,121,77).Q(134,59,149,54).L(152,97).C(179,188,21,188,48,97).Close(),tone);Line(g,new Shape().M(100,131).L(100,167),Ink,4);Dot(g,100,134,8,Ink);Ellipse(g,83,31,34,25,pale);break;
                case "bottle":Round(g,61,70,78,131,19,Milk);Round(g,68,37,64,41,9,tone);Round(g,63,111,74,53,7,pale);Heart(g,100,137,16,tone);break;
                case "crown":Line(g,new Shape().M(28,164).C(31,79,169,79,172,164),tone,15);Bow(g,105,99,58,tone);break;
                case "hood":Art(g,new Shape().M(33,164).C(12,30,187,30,167,164).L(138,156).C(153,64,47,64,62,156).Close(),tone);Line(g,new Shape().M(58,155).Q(56,193,82,194).M(142,155).Q(144,193,118,194),tone,7);Bow(g,100,190,23,pale);break;
                case "wizardhat":Art(g,new Shape().M(39,149).C(33,37,167,37,161,149).Close(),tone);for(int i=0;i<5;i++)Line(g,new Shape().M(58+i*20,130).Q(51+i*24,93,72+i*14,72),pale,4);Round(g,34,133,132,31,12,tone);Ellipse(g,83,26,36,36,pale);break;
                case "helmet":Art(g,new Shape().M(54,142).L(64,73).Q(100,56,136,73).L(146,142).Close(),tone);Art(g,new Shape().M(54,133).Q(100,145,146,133).L(175,163).Q(100,187,25,163).Close(),tone);Flower(g,100,105,17,Milk,tone);break;
                case "beret":Art(g,new Shape().M(26,139).C(6,56,188,44,174,127).Q(153,165,40,161).Close(),tone);Round(g,38,145,119,19,8,pale);Line(g,new Shape().M(103,63).L(108,43),tone,10);break;
                case "flower":Flower(g,100,113,67,Milk,tone);Dot(g,93,109,3,Ink);Dot(g,109,109,3,Ink);break;
                case "halo":Line(g,new Shape().M(32,165).C(32,80,168,80,168,165),tone,13);Ellipse(g,62,53,76,76,tone);Ellipse(g,85,76,30,30,Milk);for(int i=0;i<7;i++){double a=i*Math.PI*2/7;Dot(g,100+(float)Math.Cos(a)*28,91+(float)Math.Sin(a)*28,3,Milk);}break;
                case "antlers":Ellipse(g,44,25,41,128,Milk);Ellipse(g,115,25,41,128,Milk);Ellipse(g,55,43,19,89,tone);Ellipse(g,126,43,19,89,tone);Round(g,34,143,132,24,11,tone);break;
                case "glasses":Ellipse(g,28,87,57,48,Color.FromArgb(30,pale));Ellipse(g,115,87,57,48,Color.FromArgb(30,pale));using(var p=new Pen(tone,6)){g.DrawEllipse(p,28,87,57,48);g.DrawEllipse(p,115,87,57,48);}Line(g,new Shape().M(85,109).Q(100,101,115,109),tone,5);break;
                case "monocle":Heart(g,57,112,28,Color.FromArgb(55,tone));Heart(g,141,112,28,Color.FromArgb(55,tone));Line(g,new Shape().M(85,109).Q(99,103,112,109),tone,5);break;
                case "mask":Round(g,22,84,156,63,26,tone);Line(g,new Shape().M(43,115).Q(56,128,71,115).M(128,115).Q(142,128,156,115),Ink,4);Dot(g,36,103,4,Milk);Dot(g,163,103,4,Milk);break;
                case "visor":Round(g,26,87,62,51,17,Mix(tone,Ink,.23f));Round(g,112,87,62,51,17,Mix(tone,Ink,.23f));Line(g,new Shape().M(88,106).Q(100,99,112,106),tone,6);for(int i=0;i<4;i++){Dot(g,38+i*12,94,2.5f,Milk);Dot(g,125+i*12,94,2.5f,Milk);}break;
                case "starpatch":Star(g,145,128,26,tone);Star(g,40,141,12,pale);break;
                case "cheeks":Ellipse(g,25,137,39,20,Color.FromArgb(160,tone));Ellipse(g,137,137,39,20,Color.FromArgb(160,tone));Line(g,new Shape().M(34,142).L(31,148).M(44,142).L(41,148).M(146,142).L(143,148).M(156,142).L(153,148),Milk,2);break;
                case "scarf":Round(g,29,101,142,43,17,tone);Round(g,112,133,34,70,10,tone);for(int i=0;i<5;i++)Line(g,new Shape().M(45+i*25,110).L(45+i*25,135),pale,3);Line(g,new Shape().M(121,147).L(121,188).M(136,147).L(136,188),pale,3);break;
                case "bowtie":Bow(g,100,123,69,tone);break;
                case "tie":Art(g,new Shape().M(25,89).Q(100,125,175,89).Q(137,168,102,192).Q(61,164,25,89).Close(),tone);Line(g,new Shape().M(40,104).Q(100,138,159,104),Milk,4);Heart(g,100,148,14,pale);break;
                case "pendant":Line(g,new Shape().M(28,79).Q(100,211,172,79),tone,6);Heart(g,100,155,27,tone);Dot(g,90,144,4,Milk);break;
                case "collar":for(int i=0;i<7;i++)Ellipse(g,30+i*20,104+(float)Math.Sin(i*Math.PI/6)*31,25,39,Milk);Line(g,new Shape().M(27,102).Q(100,167,174,102),tone,7);Bow(g,100,141,19,tone);break;
                case "headphones":using(var p=new Pen(tone,13))g.DrawArc(p,34,51,132,135,180,180);Ellipse(g,21,99,47,72,pale);Ellipse(g,132,99,47,72,pale);Ellipse(g,29,108,28,52,tone);Ellipse(g,143,108,28,52,tone);break;
                case "wings":Ellipse(g,24,50,71,106,tone);Ellipse(g,105,50,71,106,tone);Ellipse(g,45,127,52,58,pale);Ellipse(g,103,127,52,58,pale);Dot(g,52,86,9,Milk);Dot(g,148,86,9,Milk);break;
                case "cape":using(var blanket=new Shape().M(58,49).Q(100,34,142,49).L(171,190).Q(100,216,29,190).Close()){Paint(g,blanket.Path,tone);Checks(g,blanket,tone);}break;
                case "backpack":Round(g,39,67,122,132,27,tone);Line(g,new Shape().M(73,66).Q(100,24,127,66),pale,9);Round(g,53,127,94,58,15,pale);Round(g,47,70,106,47,15,tone);Heart(g,100,154,14,tone);break;
                case "rocket":Ellipse(g,43,45,38,38,tone);Ellipse(g,119,45,38,38,tone);Ellipse(g,51,110,98,101,tone);Ellipse(g,41,55,118,100,tone);Ellipse(g,75,102,50,34,Milk);Dot(g,74,94,4,Ink);Dot(g,126,94,4,Ink);Dot(g,100,110,5,Ink);Ellipse(g,29,135,38,60,tone);Ellipse(g,133,135,38,60,tone);Ellipse(g,49,186,41,30,pale);Ellipse(g,110,186,41,30,pale);break;
                case "leaf":Art(g,new Shape().M(43,184).C(12,87,109,42,168,44).C(180,148,121,201,43,184).Close(),tone);Line(g,new Shape().M(46,187).L(145,67).M(78,148).L(62,113).M(106,114).L(143,114),Milk,4);Line(g,new Shape().M(65,69).Q(100,27,137,60),pale,7);break;
                case "banner":Line(g,new Shape().M(40,198).Q(27,55,157,48),tone,9);Round(g,52,108,111,83,23,tone);Round(g,59,111,97,35,15,pale);Heart(g,107,145,13,tone);break;
                case "ring":for(int i=0;i<7;i++){double a=i*Math.PI*2/7;Heart(g,100+(float)Math.Cos(a)*72,120+(float)Math.Sin(a)*76,10+i%2*3,tone);}break;
                case "runes":for(int i=0;i<7;i++){double a=i*Math.PI*2/7;var s=g.Save();g.TranslateTransform(100+(float)Math.Cos(a)*72,120+(float)Math.Sin(a)*76);g.RotateTransform(i*51);Ellipse(g,-7,-15,14,30,tone);g.Restore(s);}break;
                case "stars":for(int i=0;i<7;i++){double a=i*Math.PI*2/7;Star(g,100+(float)Math.Cos(a)*72,120+(float)Math.Sin(a)*76,10+i%2*4,tone);}break;
                case "bubbles":for(int i=0;i<7;i++){double a=i*Math.PI*2/7;float x=100+(float)Math.Cos(a)*72,y=120+(float)Math.Sin(a)*76;Ellipse(g,x-11,y-11,22,22,Color.FromArgb(100,tone));Dot(g,x-4,y-5,3,Milk);}break;
                case "cloud":Art(g,new Shape().M(36,170).C(14,153,29,119,54,125).C(56,84,108,82,121,115).C(152,97,179,128,163,149).C(190,166,172,190,144,185).L(56,189).Q(27,194,36,170).Close(),pale);Dot(g,83,158,3,Ink);Dot(g,116,158,3,Ink);Ellipse(g,64,166,19,8,tone);Ellipse(g,117,166,19,8,tone);break;
                case "board":using(var pillow=new Shape().M(29,121).Q(100,101,171,121).Q(183,157,168,190).Q(100,205,32,190).Q(17,158,29,121).Close()){Paint(g,pillow.Path,tone);Checks(g,pillow,tone);}Line(g,new Shape().M(38,130).Q(100,116,162,130).M(40,181).Q(100,194,160,181),Milk,3);break;
            }
        }
    }
}
