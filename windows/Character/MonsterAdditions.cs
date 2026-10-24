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
                case MonsterVariant.Cat:
                    Ear(g,190,505,185,432,224,477,Pink);
                    Ear(g,337,500,367,435,378,523,Pink); break;
                case MonsterVariant.Rabbit:
                    using(var ear=new Outline().M(195,503).C(174,456,180,398,197,398).C(217,398,226,461,225,488).Close()) Fill(g,ear,White);
                    using(var ear=new Outline().M(337,506).C(330,461,351+wave,397,367+wave,402)
                        .C(391+wave,410,376,482,369,520).Close()) Fill(g,ear,White);
                    Line(g,new Outline().M(197,419).Q(195,461,210,478).M(365+wave,424).Q(352,461,354,490),8,Pink); break;
                case MonsterVariant.Fox:
                    Ear(g,190,505,180,430,226,482,Gold); Ear(g,337,501,375,434,380,523,Gold);
                    using(var tail=new Outline().M(481,681).C(530,693,558,659,565,619+wave).Q(593,658,575,689)
                        .Q(557,729,501,731).Close()) Fill(g,tail,Gold);
                    using(var tip=new Outline().M(557,646+wave).L(565,619+wave).Q(581,639,582,660)
                        .L(570,657).L(568,671).L(558,661).Close()) Fill(g,tip,White); break;
                case MonsterVariant.Puppy:
                    using(var ear=new Outline().M(214,489).Q(164,456,143,493).Q(121,532,144,550)
                        .Q(174,559,227,522).Close()) Fill(g,ear,Gold);
                    using(var ear=new Outline().M(350,495).Q(414,464,449,515).Q(501,572,482,600)
                        .Q(450,611,399,549).Close()) Fill(g,ear,Gold); break;
                case MonsterVariant.Bear:
                    Dot(g,207,477,27,White); Dot(g,356,483,27,White);
                    Dot(g,207,477,14,Pink); Dot(g,356,483,14,Pink); break;
                case MonsterVariant.Ram:
                    Dot(g,195,481,34,Gold); Dot(g,374,484,39,Gold);
                    Line(g,new Outline().M(176,480).C(176,452,216,454,214,481).Q(210,501,194,485)
                        .M(395,484).C(395,452,351,455,354,484).Q(360,505,374,489),4); break;
                case MonsterVariant.Deer:
                    Line(g,new Outline().M(205,498).Q(194,451,183,412).M(195,452).L(161,443).L(153,420)
                        .M(187,430).L(205,415).L(207,398).M(346,497).Q(361,455,375,412)
                        .M(361,453).L(392,448).L(408,423).M(371,429).L(355,414).L(355,400),7,Gold); break;
                case MonsterVariant.Dragon:
                    using(var wing=new Outline().M(407,595).L(443,474+wave).L(512,524+wave)
                        .Q(474,530,484,566).Q(446,560,456,610).Close()) Fill(g,wing,Green);
                    using(var tail=new Outline().M(486,685).Q(537,708,568,674+wave).L(583,642+wave)
                        .Q(594,707,517,728).Close()) Fill(g,tail,Green);
                    Line(g,new Outline().M(424,580).L(449,496+wave).L(492,529+wave),4); break;
                case MonsterVariant.Butterfly:
                    using(var wing=new Outline().M(432,612).C(424,500,489,434,526,481)
                        .C(558,519,520,564,492,581).C(568,557,578,639,536,666).Q(492,693,432,612).Close()) Fill(g,wing,Purple);
                    using(var wing=new Outline().M(213,582).C(146,470,86,468,111,540).Q(129,563,157,575)
                        .Q(100,575,119,621).Q(156,668,213,582).Close()) Fill(g,wing,Pink);
                    Line(g,new Outline().M(459,600).Q(482,535,517,500).M(473,616).Q(518,609,544,634),4); break;
                case MonsterVariant.Shark:
                    using(var fin=new Outline().M(391,548).Q(430,476,478,460).Q(464,505,484,583).Close()) Fill(g,fin,Blue);
                    using(var tail=new Outline().M(488,675).L(542,666).L(566,624+wave).L(565,665)
                        .L(589,684+wave).L(549,695).L(495,709).Close()) Fill(g,tail,Blue); break;
                case MonsterVariant.Peacock:
                    using(var fan=new Outline().M(445,664).C(405,580,402,494,444,480)
                        .Q(475,451,488,499).Q(527,470,536,526).Q(578,518,563,566)
                        .Q(598,597,553,615).Q(563,657,521,654).Close()) Fill(g,fan,Green);
                    Line(g,new Outline().M(466,625).L(447,504).M(475,626).L(486,521).M(490,637).L(530,549).M(503,641).L(550,594),3);
                    Dot(g,448,509,12,Blue); Dot(g,486,524,12,Blue); Dot(g,530,549,12,Blue); Dot(g,548,593,11,Blue); break;
                case MonsterVariant.Turtle:
                    using(var shell=new Outline().M(356,542).C(426,518,496,573,506,639)
                        .Q(516,684,477,695).L(384,630).Close()) Fill(g,shell,Green); break;
                case MonsterVariant.Octopus:
                    using(var tentacles=new Outline().M(477,585).C(501,544,538,528+wave,549,557+wave)
                        .Q(559,586,538,588).Q(527,584,535,568).Q(505,564,501,606)
                        .C(544,594,580,615,571,642).Q(557,670,544,650).Q(542,644,557,637)
                        .Q(533,621,502,637).C(556,667,568,710+wave,539,724+wave)
                        .Q(514,732,516,713).Q(520,705,533,710).Q(535,684,490,674).Close()) Fill(g,tentacles,Pink); break;
                case MonsterVariant.Cactus:
                    using(var stem=new Outline().M(412,579).L(416,498).Q(429,470,443,499).L(442,530)
                        .Q(468,530,466,506).Q(466,489,479,490).Q(504,525,480,548)
                        .L(440,550).L(438,597).Close()) Fill(g,stem,Green);
                    Line(g,new Outline().M(428,503).L(428,569).M(418,513).L(407,507).M(442,521).L(452,512)
                        .M(479,535).L(491,543).M(420,554).L(410,559),3); break;
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
                case MonsterVariant.Cat:
                    Line(g,new Outline().M(212,534).L(190,529).M(215,545).L(193,547)
                        .M(337,537).L(358,530).M(338,547).L(359,548),3); break;
                case MonsterVariant.Fox:
                    Line(g,new Outline().M(385,569).L(398,561).L(398,575),3); break;
                case MonsterVariant.Puppy:
                    Dot(g,421,582,14,Gold); break;
                case MonsterVariant.Ram:
                    Line(g,new Outline().M(359,540).Q(372,526,383,543).Q(398,534,403,553)
                        .M(404,564).Q(417,550,428,568).Q(440,560,446,579),4); break;
                case MonsterVariant.Deer:
                    Dot(g,403,572,5,Gold); Dot(g,424,590,5,Gold); Dot(g,437,613,5,Gold); break;
                case MonsterVariant.Dragon:
                    using(var ridge=new Outline().M(365,515).L(382,484).L(390,531).L(413,512).L(416,553)
                        .L(442,540).L(439,576).Close()) Fill(g,ridge,Gold); break;
                case MonsterVariant.Butterfly:
                    Line(g,new Outline().M(360,512).Q(364,480,380,470).M(371,515).Q(390,492,406,493),3);
                    Dot(g,381,469,4,Purple); Dot(g,407,493,4,Pink); break;
                case MonsterVariant.Axolotl:
                    Gills(g,190,530,-1.3f,wave); Gills(g,365,525,1.3f,wave);
                    Dot(g,225,520,5,Pink); Dot(g,319,520,5,Pink); break;
                case MonsterVariant.Shark:
                    Line(g,new Outline().M(387,565).L(379,585).M(402,571).L(394,590).M(416,579).L(409,597),4); break;
                case MonsterVariant.Peacock:
                    Line(g,new Outline().M(364,512).L(367,470).M(364,500).L(384,478),3);
                    Dot(g,367,469,7,Blue); Dot(g,385,478,7,Green); break;
                case MonsterVariant.Turtle:
                    using(var plate=new Outline().M(387,554).L(416,553).L(439,578).L(429,609).L(397,607).L(381,578).Close()) Fill(g,plate,Green);
                    Line(g,new Outline().M(439,578).L(470,582).L(488,608).L(477,640).L(449,636).L(429,609)
                        .M(449,636).L(443,657).M(397,607).L(393,625),4); break;
                case MonsterVariant.Octopus:
                    Dot(g,518,580,3,White); Dot(g,536,637,3,White); Dot(g,524,690,3,White); break;
                case MonsterVariant.Robot:
                    Line(g,new Outline().M(362,513).L(368,464),5); Dot(g,370,450,13,Blue);
                    using(var plate=new Outline().M(388,557).L(438,570).L(426,616).L(378,599).Close()) Fill(g,plate,Blue);
                    Dot(g,400,580,4,Gold); Dot(g,417,585,4,Pink);
                    Line(g,new Outline().M(392,597).L(419,605),3); break;
                case MonsterVariant.Cactus:
                    Flower(g,435,486,17); break;
            }
        }

        private static void Ear(Graphics g,float x,float y,float tipX,float tipY,float xx,float yy,Color inner)
        {
            using(var ear=new Outline().M(x,y).L(tipX,tipY).L(xx,yy).Close()) Fill(g,ear,White);
            using(var ear=new Outline().M(x+(xx-x)*.2f,y-10).L(tipX+(xx-tipX)*.18f,tipY+21)
                .L(xx-(xx-x)*.2f,yy-10).Close()) Fill(g,ear,inner);
        }
        private static void Gills(Graphics g,float x,float y,float direction,float wave)
        {
            using(var frill=new Outline().M(x,y).Q(x+direction*31,y-45-wave,x+direction*51,y-36-wave)
                .Q(x+direction*52,y-22,x+direction*28,y-13).Q(x+direction*63,y-14,x+direction*64,y+4)
                .Q(x+direction*58,y+25,x+direction*31,y+13).Q(x+direction*49,y+43+wave,x+direction*33,y+51+wave)
                .Q(x+direction*13,y+37,x,y).Close()) Fill(g,frill,Pink);
            Line(g,new Outline().M(x,y).L(x+direction*41,y-28).M(x,y).L(x+direction*50,y+3).M(x,y).L(x+direction*29,y+36),3);
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
