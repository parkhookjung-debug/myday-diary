using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Outline=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // These layers add features to the original body; they never replace its outline or face.
    internal static class MonsterAdditions
    {
        private static readonly Color Blue=Color.FromArgb(213,232,243);
        private static readonly Color Green=Color.FromArgb(204,223,186);
        private static readonly Color Pink=Color.FromArgb(245,208,216);
        private static readonly Color Purple=Color.FromArgb(224,214,239);
        private static readonly Color Gold=Color.FromArgb(245,222,156);
        private static readonly Color Ink=MonsterPainter.Ink;
        private static readonly Color White=MonsterPainter.Body;

        public static void DrawBehind(Graphics g,MonsterPose pose,MonsterVariant variant)
        {
            float wave=(float)Math.Sin(pose.Phase*4)*8;
            switch(variant) {
                case MonsterVariant.Fin:
                    using(var fin=new Outline().M(410,556).Q(441,487+wave,480,480).Q(471,532,519,536)
                        .Q(494,555,524,587).Q(477,580,469,631).Close()) Fill(g,fin,Blue);
                    Line(g,new Outline().M(430,554).L(480,500).M(446,573).L(503,541).M(462,596).L(509,579),4);
                    break;
                case MonsterVariant.Tailed:
                    using(var tail=new Outline().M(480,679).C(520,681,563,658+wave,548,624+wave)
                        .C(519,596+wave,531,571+wave,554,580+wave).C(590,596+wave,588,657,561,693)
                        .Q(535,730,497,719).Close()) Fill(g,tail,White);
                    break;
                case MonsterVariant.Furry:
                    using(var fur=new Outline().M(366,506).Q(376,468,397,504).Q(429,483,430,529)
                        .Q(461,523,459,563).Q(494,560,488,601).Q(520,602,511,641)
                        .Q(544,655,524,679).Q(548,708,519,721).Q(527,752,491,733)
                        .L(476,702).L(415,600).Close()) Fill(g,fur,White);
                    break;
                case MonsterVariant.Angel:
                    FeatherWing(g,pose,90,486,245,609,Blue);
                    FeatherWing(g,pose,450,466,529,650,Blue);
                    using(var pen=new Pen(Gold,11)) g.DrawEllipse(pen,222,401,114,24);
                    using(var pen=new Pen(Ink,3)) g.DrawEllipse(pen,222,401,114,24);
                    break;
                case MonsterVariant.Devil:
                    Line(g,new Outline().M(477,695).C(547,742,556,683,540,647).Q(529,625,552,610),6);
                    using(var tip=new Outline().M(536,609).L(572,586).L(560,628).Close()) Fill(g,tip,Pink);
                    break;
                case MonsterVariant.Astronaut:
                    using(var helmet=new Outline().M(192,549).C(166,405,336,367,381,466).Q(407,519,355,559).Close()) Fill(g,helmet,Color.FromArgb(90,Blue));
                    break;
                case MonsterVariant.Headphones:
                    using(var band=new Outline().M(209,504).C(188,401,323,377,357,486)) Line(g,band,12);
                    using(var pad=new Outline().M(197,483).Q(223,479,226,508).L(225,538).Q(202,553,193,530).Close()) Fill(g,pad,Purple);
                    break;
                case MonsterVariant.Scarf:
                    using(var end=new Outline().M(359,553).C(406,568,464,510+wave,510,540+wave)
                        .L(517,574+wave).Q(442,562,398,610).L(350,598).Close()) Fill(g,end,Pink);
                    break;
                case MonsterVariant.Backpack:
                    using(var bag=new Outline().M(450,548).Q(524,539,546,597).L(548,688).Q(521,716,485,697).Close()) Fill(g,bag,Green);
                    Line(g,new Outline().M(506,566).Q(539,565,539,589).M(515,619).L(540,619).L(541,667).L(518,668),4);
                    break;
                case MonsterVariant.Raincoat:
                    using(var hood=new Outline().M(195,541).C(163,440,218,398,281,405).C(360,391,382,478,364,536)
                        .L(327,537).Q(319,441,276,434).Q(220,436,221,533).Close()) Fill(g,hood,Gold);
                    break;
                case MonsterVariant.Moon:
                    using(var cape=new Outline().M(365,539).Q(482,556,548,715).Q(498,754,449,723).L(355,611).Close()) Fill(g,cape,Purple);
                    break;
            }
        }

        public static void DrawFront(Graphics g,MonsterPose pose,MonsterVariant variant)
        {
            float wave=(float)Math.Sin(pose.Phase*4)*7;
            switch(variant) {
                case MonsterVariant.Fin:
                    Line(g,new Outline().M(390,573).Q(405,563,418,573).M(409,594).Q(423,584,435,594),4);
                    break;
                case MonsterVariant.Shell:
                    using(var shell=new Outline().M(364,538).C(415,529,462,573,478,616).Q(488,651,463,669)
                        .Q(416,652,384,605).Q(357,568,364,538).Close()) Fill(g,shell,Green);
                    Line(g,new Outline().M(391,549).Q(424,607,461,653).M(370,568).Q(419,568,461,600)
                        .M(387,609).Q(429,602,476,622),4);
                    break;
                case MonsterVariant.Crystal:
                    Gem(g,369,520,24,78,Purple); Gem(g,405,541,26,63,Blue); Gem(g,441,570,22,51,Pink);
                    break;
                case MonsterVariant.Furry:
                    Line(g,new Outline().M(372,552).L(383,547).L(381,562).M(412,586).L(426,583).L(422,598)
                        .M(454,633).L(467,630).L(463,645),4);
                    break;
                case MonsterVariant.Sprout:
                    Line(g,new Outline().M(363,512).Q(370+wave,477,377+wave,457),5);
                    using(var leaf=new Outline().M(377+wave,468).Q(330,469,332,426).Q(375,425,377+wave,468).Close()) Fill(g,leaf,Green);
                    using(var leaf=new Outline().M(377+wave,468).Q(384,422,426,440).Q(417,481,377+wave,468).Close()) Fill(g,leaf,Green);
                    break;
                case MonsterVariant.Flower:
                    Flower(g,373,503,27); Line(g,new Outline().M(381,528).Q(412,536,413,563),4);
                    break;
                case MonsterVariant.Ribbon:
                    Bow(g,372,509,29); break;
                case MonsterVariant.Angel:
                    Line(g,new Outline().M(447,584).Q(459,574,470,582),4); break;
                case MonsterVariant.Devil:
                    using(var horn=new Outline().M(343,506).Q(332,471,353,444).Q(353,478,373,516).Close()) Fill(g,horn,Pink);
                    using(var horn=new Outline().M(401,536).Q(407,495,433,481).Q(420,517,430,552).Close()) Fill(g,horn,Pink);
                    break;
                case MonsterVariant.Crown:
                    using(var crown=new Outline().M(338,502).L(331,456).L(361,469).L(377,433).L(391,475).L(423,466)
                        .L(407,525).Close()) Fill(g,crown,Gold);
                    Dot(g,376,488,8,Pink); Line(g,new Outline().M(345,493).L(408,515),4); break;
                case MonsterVariant.Wizard:
                    using(var brim=new Outline().M(326,500).Q(372,479,433,531).Q(382,545,326,500).Close()) Fill(g,brim,Purple);
                    using(var hat=new Outline().M(347,491).Q(362,450,351,411).Q(391,422,414,521).Close()) Fill(g,hat,Purple);
                    Star(g,382,472,12,Gold); break;
                case MonsterVariant.Pirate:
                    using(var bandana=new Outline().M(331,498).Q(359,466,403,497).L(422,540).Q(379,514,331,511).Close()) Fill(g,bandana,Pink);
                    using(var end=new Outline().M(405,519).Q(443,510,454,531).L(432,541).L(450,558).Q(420,562,405,519).Close()) Fill(g,end,Pink);
                    Dot(g,374,496,9,White); Line(g,new Outline().M(366,510).L(383,501).M(368,500).L(382,513),3);
                    break;
                case MonsterVariant.Astronaut:
                    Line(g,new Outline().M(199,539).Q(278,570,365,539),8);
                    using(var shine=new Pen(Color.FromArgb(190,Color.White),7) { StartCap=LineCap.Round }) g.DrawArc(shine,202,416,158,128,205,42);
                    using(var patch=new Outline().M(402,580).L(436,586).L(429,616).L(397,608).Close()) Fill(g,patch,Blue);
                    Dot(g,414,599,4,Gold); break;
                case MonsterVariant.Headphones:
                    using(var pad=new Outline().M(341,484).Q(367,480,370,508).L(367,536).Q(342,551,334,528).Close()) Fill(g,pad,Purple);
                    Line(g,new Outline().M(345,499).L(350,532),4); break;
                case MonsterVariant.Glasses:
                    using(var pen=new Pen(Ink,4)) { g.DrawEllipse(pen,224,455,44,45); g.DrawEllipse(pen,282,453,44,45); }
                    Line(g,new Outline().M(268,473).Q(277,467,282,471).M(326,471).L(341,484),4); break;
                case MonsterVariant.Scarf:
                    using(var collar=new Outline().M(216,535).Q(292,558,358,533).L(370,566).Q(290,591,218,563).Close()) Fill(g,collar,Pink);
                    Line(g,new Outline().M(354,577).L(373,593).M(346,580).L(360,600),3); break;
                case MonsterVariant.Backpack:
                    Line(g,new Outline().M(382,530).Q(429,546,437,630),10);
                    Line(g,new Outline().M(382,530).Q(429,546,437,630),5,Green); break;
                case MonsterVariant.SleepCap:
                    using(var cap=new Outline().M(330,499).Q(345,446,382,433).Q(427,425,443,473)
                        .Q(400,452,405,516).Close()) Fill(g,cap,Blue);
                    using(var hem=new Outline().M(327,498).Q(370,494,407,521).L(399,534).Q(360,509,328,515).Close()) Fill(g,hem,White);
                    Dot(g,445,476,13,White); Dot(g,369,480,5,White); Dot(g,385,462,5,White); break;
                case MonsterVariant.Raincoat:
                    using(var collar=new Outline().M(214,533).Q(283,553,346,532).L(369,581).L(290,570).L(220,581).Close()) Fill(g,collar,Gold);
                    Line(g,new Outline().M(279,572).L(285,604).M(303,572).L(309,601),3);
                    Dot(g,285,605,4,Gold); Dot(g,309,602,4,Gold); break;
                case MonsterVariant.Winter:
                    using(var hat=new Outline().M(336,500).Q(329,445,383,448).Q(429,455,421,528).Close()) Fill(g,hat,Green);
                    using(var hem=new Outline().M(331,495).Q(379,493,426,523).L(418,539).Q(372,512,331,512).Close()) Fill(g,hem,White);
                    Dot(g,384,437,14,White); Line(g,new Outline().M(351,471).L(355,490).M(374,466).L(379,494).M(397,474).L(401,504),3);
                    break;
                case MonsterVariant.Star:
                    Line(g,new Outline().M(367,513).Q(382+wave,486,386+wave,461),4);
                    Star(g,389+wave,436,28,Gold); Star(g,425,592,17,Gold); break;
                case MonsterVariant.Moon:
                    using(var moon=new Outline().M(394,449).C(363,443,353,470,365,489).C(380,507,406,498,417,481)
                        .C(388,488,376,467,394,449).Close()) Fill(g,moon,Gold);
                    Line(g,new Outline().M(365,535).Q(354,561,371,583),5); Star(g,423,605,12,Gold); break;
                case MonsterVariant.Heart:
                    Line(g,new Outline().M(370,512).Q(393+wave,490,399+wave,467),4);
                    Heart(g,401+wave,446,27); Heart(g,425,590,19); break;
            }
        }

        private static void FeatherWing(Graphics g,MonsterPose pose,float x,float y,float xx,float yy,Color color)
        {
            float flutter=(float)Math.Sin(pose.Phase*5)*6;
            using(var wing=new Outline().M(xx,yy).Q(x-18,y+45+flutter,x,y+flutter)
                .Q(x+28,y+10+flutter,x+48,y+40).Q(x+62,y+9+flutter,x+77,y+58)
                .Q(x+100,y+25+flutter,x+112,y+78).Q(x+126,y+61,x+139,y+105).Close()) Fill(g,wing,color);
        }
        private static void Gem(Graphics g,float x,float y,float width,float height,Color color)
        {
            using(var gem=new Outline().M(x,y).L(x-width,y-height*.55f).L(x-4,y-height).L(x+width,y-height*.6f).L(x+width*.5f,y+10).Close()) Fill(g,gem,color);
            Line(g,new Outline().M(x,y).L(x-4,y-height).M(x-4,y-height*.5f).L(x+width,y-height*.6f),3);
        }
        private static void Flower(Graphics g,float x,float y,float radius)
        {
            for(int i=0;i<5;i++) {
                double angle=i*Math.PI*2/5;
                float px=x+(float)Math.Cos(angle)*radius*.68f,py=y+(float)Math.Sin(angle)*radius*.68f;
                Dot(g,px,py,radius*.55f,Pink);
            }
            Dot(g,x,y,radius*.4f,Gold);
        }
        private static void Bow(Graphics g,float x,float y,float radius)
        {
            using(var bow=new Outline().M(x,y).Q(x-radius*1.3f,y-radius*1.2f,x-radius*1.5f,y)
                .Q(x-radius*1.3f,y+radius*1.2f,x,y).Q(x+radius*1.3f,y-radius*1.2f,x+radius*1.5f,y)
                .Q(x+radius*1.3f,y+radius*1.2f,x,y).Close()) Fill(g,bow,Pink);
            Dot(g,x,y,7,Pink);
        }
        private static void Heart(Graphics g,float x,float y,float radius)
        {
            using(var heart=new Outline().M(x,y+radius).C(x-radius*2,y,x-radius,y-radius*1.5f,x,y-radius*.5f)
                .C(x+radius,y-radius*1.5f,x+radius*2,y,x,y+radius).Close()) Fill(g,heart,Pink);
        }
        private static void Star(Graphics g,float x,float y,float radius,Color color)
        {
            using(var star=new Outline()) {
                for(int i=0;i<10;i++) {
                    double a=-Math.PI/2+i*Math.PI/5; float r=i%2==0?radius:radius*.45f;
                    float px=x+(float)Math.Cos(a)*r,py=y+(float)Math.Sin(a)*r;
                    if(i==0) star.M(px,py); else star.L(px,py);
                }
                star.Close(); Fill(g,star,color);
            }
        }
        private static void Dot(Graphics g,float x,float y,float radius,Color color)
        {
            using(var brush=new SolidBrush(color)) g.FillEllipse(brush,x-radius,y-radius,radius*2,radius*2);
            using(var pen=new Pen(Ink,3.5f)) g.DrawEllipse(pen,x-radius,y-radius,radius*2,radius*2);
        }
        private static void Fill(Graphics g,Outline shape,Color color) { MonsterPainter.Fill(g,shape,color); }
        private static void Line(Graphics g,Outline shape,float width) { Line(g,shape,width,Ink); }
        private static void Line(Graphics g,Outline shape,float width,Color color)
        {
            using(shape) using(var pen=new Pen(color,width) { StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round }) g.DrawPath(pen,shape.Path);
        }
    }
}
