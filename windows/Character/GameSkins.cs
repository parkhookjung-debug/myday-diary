using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Outline=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // RPG monster materials share Sangmon's original body path, face and gestures.
    internal static class GameSkins
    {
        public static bool IsGame(MonsterVariant variant) { return (int)variant>=(int)MonsterVariant.Ember && (int)variant<=(int)MonsterVariant.Astral; }
        public static Color Light(MonsterVariant variant)
        {
            switch(variant) {
                case MonsterVariant.Ember: return Color.FromArgb(255,218,137);
                case MonsterVariant.Frost: return Color.FromArgb(224,253,255);
                case MonsterVariant.Volt: return Color.FromArgb(255,245,162);
                case MonsterVariant.Venom: return Color.FromArgb(214,248,156);
                case MonsterVariant.Shadow: return Color.FromArgb(202,182,238);
                case MonsterVariant.Golem: return Color.FromArgb(223,220,203);
                case MonsterVariant.Ocean: return Color.FromArgb(190,247,246);
                default: return Color.FromArgb(217,211,255);
            }
        }
        public static Color Tone(MonsterVariant variant)
        {
            switch(variant) {
                case MonsterVariant.Ember: return Color.FromArgb(235,100,70);
                case MonsterVariant.Frost: return Color.FromArgb(88,195,229);
                case MonsterVariant.Volt: return Color.FromArgb(243,198,54);
                case MonsterVariant.Venom: return Color.FromArgb(105,177,73);
                case MonsterVariant.Shadow: return Color.FromArgb(115,77,165);
                case MonsterVariant.Golem: return Color.FromArgb(138,145,136);
                case MonsterVariant.Ocean: return Color.FromArgb(45,163,192);
                default: return Color.FromArgb(108,109,218);
            }
        }
        private static Color Dark(MonsterVariant variant)
        {
            var tone=Tone(variant); return Color.FromArgb(tone.R*55/100,tone.G*55/100,tone.B*65/100);
        }
        private static readonly Color Ink=Color.FromArgb(30,38,55);
        public static bool PaintBody(Graphics g,Outline body,MonsterPose pose,MonsterVariant variant)
        {
            if(!IsGame(variant)) return false;
            var saved=g.Save(); g.SetClip(body.Path,CombineMode.Intersect);
            using(var fill=new LinearGradientBrush(new Rectangle(90,422,470,370),Light(variant),Dark(variant),90)) {
                fill.InterpolationColors=new ColorBlend {
                    Colors=new[] {Light(variant),Tone(variant),Dark(variant),Dark(variant)},
                    Positions=new[] {0f,.49f,.491f,1f}
                };
                g.FillPath(fill,body.Path);
            }
            // A hard lower facet and a short highlight give a readable game silhouette.
            using(var facet=new Outline().M(274,540).L(334,528).L(459,611).L(492,682).L(429,627).L(357,611).Close())
                Fill(g,facet,Color.FromArgb(75,Light(variant)),false);
            Line(g,new Outline().M(353,523).L(398,544).L(411,567),8,Color.FromArgb(170,Light(variant)));
            Color accent=Light(variant);
            switch(variant) {
                case MonsterVariant.Ember:
                    using(var plate=new Outline().M(363,560).L(394,547).L(444,590).L(467,646).L(426,631).L(398,597).Close()) Fill(g,plate,Ink,false);
                    Line(g,new Outline().M(389,556).L(405,581).L(394,600).L(427,616).L(450,640)
                        .M(405,581).L(432,580).M(427,616).L(423,637),6,Color.FromArgb(255,190+(int)(pose.Pulse*40),98));
                    break;
                case MonsterVariant.Frost:
                    using(var facets=new Outline().M(314,521).L(387,570).L(348,641).Close()
                        .M(388,571).L(457,621).L(431,670).Close().M(220,670).L(252,666).L(219,737).Close()) Fill(g,facets,Color.FromArgb(105,accent),false);
                    Line(g,new Outline().M(395,594).L(435,636).M(415,583).L(413,648).M(391,618).L(439,609),5,accent); break;
                case MonsterVariant.Volt:
                    using(var bolt=new Outline().M(399,548).L(372,601).L(408,601).L(387,652)
                        .L(448,581).L(412,584).L(432,550).Close()) Fill(g,bolt,Ink,false);
                    Line(g,new Outline().M(395,563).L(382,594).L(420,594).L(406,619),4,accent); break;
                case MonsterVariant.Venom:
                    using(var ooze=new Outline().M(325,521).Q(388,520,448,569).L(451,615)
                        .Q(438,635,426,608).L(423,589).Q(410,622,397,596).L(390,563)
                        .Q(370,579,358,553).Close()) Fill(g,ooze,Color.FromArgb(128,64,147),false);
                    Dot(g,386,616,9,accent); Dot(g,414,641,6,accent); break;
                case MonsterVariant.Shadow:
                    using(var mist=new Outline().M(323,537).Q(383,524,437,585).L(480,670).L(439,644)
                        .L(458,674).Q(369,632,377,581).L(344,591).Close()) Fill(g,mist,Color.FromArgb(54,42,96),false);
                    Line(g,new Outline().M(387,587).L(421,599).L(394,621).L(418,637),5,Color.FromArgb(208,163,255)); break;
                case MonsterVariant.Golem:
                    using(var plates=new Outline().M(325,531).L(365,530).L(390,562).L(374,593).L(335,585).Close()
                        .M(385,585).L(421,570).L(452,606).L(437,638).L(401,626).Close()) Fill(g,plates,Tone(variant));
                    Line(g,new Outline().M(412,589).L(402,609).L(425,609).M(413,595).L(416,622),5,Color.FromArgb(156,231,198)); break;
                case MonsterVariant.Ocean:
                    Line(g,new Outline().M(307,557).C(337,531,360,584,385,565).C(413,545,430,595,457,579)
                        .M(366,601).C(393,580,420,631,452,611),7,accent);
                    Dot(g,398,639,7,Color.FromArgb(210,accent)); break;
                case MonsterVariant.Astral:
                    Line(g,new Outline().M(399,557).L(439,579).L(437,621).L(400,641).L(364,619).L(365,578).Close()
                        .M(399,557).L(400,641).M(365,578).L(437,621).M(439,579).L(364,619),4,Color.FromArgb(245,213,132));
                    Gem(g,400,603,13,Color.FromArgb(250,225,160)); break;
            }
            g.Restore(saved);
            using(var pen=new Pen(Ink,7.5f) { LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawPath(pen,body.Path);
            return true;
        }
        public static void DrawBehind(Graphics g,MonsterPose pose,MonsterVariant variant)
        {
            if(!IsGame(variant)) return;
            float sway=(float)Math.Sin(pose.Phase*Math.PI*2/4.2)*6;
            Color light=Light(variant),tone=Tone(variant);
            switch(variant) {
                case MonsterVariant.Ember:
                    using(var ridge=new Outline().M(364,529).L(377,487).L(401,550).L(427,520)
                        .L(440,581).L(472,563).L(476,622).Close()) Fill(g,ridge,Ink);
                    Line(g,new Outline().M(383,501).L(392,534).M(432,539).L(441,567).M(470,585).L(468,607),4,light); break;
                case MonsterVariant.Frost:
                    Gem(g,381,511,25,light); Gem(g,432,551,24,tone); Gem(g,471,596,22,light); break;
                case MonsterVariant.Volt:
                    Line(g,new Outline().M(434,509+sway).L(468,496+sway).L(451,524+sway).L(487,534+sway)
                        .M(493,589-sway).L(530,599-sway).L(516,629-sway).L(544,642-sway),6,light); break;
                case MonsterVariant.Venom:
                    Bubble(g,439,515+sway,13,light); Bubble(g,480,550-sway,9,tone);
                    Bubble(g,515,582+sway,6,light); break;
                case MonsterVariant.Shadow:
                    using(var shade=new Outline().M(433,540).Q(485,516+sway,496,493+sway).Q(494,550,477,574)
                        .Q(549,542,550,575).Q(529,601,506,614).Q(552,605,558,638)
                        .L(504,681).Close()) Fill(g,shade,Color.FromArgb(128,100,181)); break;
                case MonsterVariant.Golem:
                    using(var rocks=new Outline().M(359,527).L(353,486).L(385,470).L(409,542)
                        .L(440,527).L(480,565).L(464,609).Close()) Fill(g,rocks,tone);
                    Line(g,new Outline().M(366,488).L(395,533).M(453,544).L(466,567),4,light); break;
                case MonsterVariant.Ocean:
                    using(var crest=new Outline().M(383,540).Q(410,482,441,480).Q(420,524,458,538)
                        .Q(446,567,480,587).L(454,625).Close()) Fill(g,crest,tone);
                    Bubble(g,478,526+sway,9,light); Bubble(g,522,565-sway,6,light); break;
                case MonsterVariant.Astral:
                    Gem(g,440+sway,512-sway,18,Color.FromArgb(245,213,132));
                    Line(g,new Outline().M(439,539).Q(485,559,510,599),4,light);
                    Gem(g,516,598,9,light); break;
            }
        }
        private static void Gem(Graphics g,float x,float y,float size,Color color)
        {
            using(var gem=new Outline().M(x,y-size*1.7f).L(x+size,y).L(x,y+size*.5f).L(x-size,y).Close()) Fill(g,gem,color);
            Line(g,new Outline().M(x,y-size*1.7f).L(x,y+size*.5f).M(x-size,y).L(x+size,y),3,Color.FromArgb(140,Ink));
        }
        private static void Bubble(Graphics g,float x,float y,float size,Color color)
        {
            using(var brush=new SolidBrush(Color.FromArgb(85,color))) g.FillEllipse(brush,x-size,y-size,size*2,size*2);
            using(var pen=new Pen(color,3)) g.DrawEllipse(pen,x-size,y-size,size*2,size*2);
            Dot(g,x-size*.3f,y-size*.35f,2.5f,Light(MonsterVariant.Frost));
        }
        private static void Dot(Graphics g,float x,float y,float size,Color color)
        { using(var brush=new SolidBrush(color)) g.FillEllipse(brush,x-size,y-size,size*2,size*2); }
        private static void Fill(Graphics g,Outline shape,Color color,bool outline=true)
        {
            using(var brush=new SolidBrush(color)) g.FillPath(brush,shape.Path);
            if(outline) using(var pen=new Pen(Ink,5.5f) { LineJoin=LineJoin.Round }) g.DrawPath(pen,shape.Path);
        }
        private static void Line(Graphics g,Outline path,float width,Color color)
        { using(path) using(var pen=new Pen(color,width) { LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawPath(pen,path.Path); }
    }
}
