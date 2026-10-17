using System.Drawing;
using System.Drawing.Drawing2D;
using Outline=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // New rewards keep Sangmon's raised eyes, side snout and open leg arch.
    internal static class RewardSkins
    {
        public static bool IsReward(MonsterVariant v) { return v>=MonsterVariant.Apprentice && v<=MonsterVariant.MemoryHero; }
        private static readonly Color Ink=Color.FromArgb(40,53,61);
        private static readonly Color[] Tones={Color.FromArgb(118,179,156),Color.FromArgb(241,186,108),Color.FromArgb(124,167,194),Color.FromArgb(158,137,207),
            Color.FromArgb(99,177,189),Color.FromArgb(167,192,109),Color.FromArgb(205,125,156),Color.FromArgb(232,173,81)};
        private static Color Tone(MonsterVariant v) { return Tones[(int)v-(int)MonsterVariant.Apprentice]; }
        public static bool PaintBody(Graphics g,Outline body,MonsterVariant v)
        {
            if(!IsReward(v)) return false;
            using(var fill=new LinearGradientBrush(new Rectangle(110,430,410,350),Color.FromArgb(251,250,235),Tone(v),80)) g.FillPath(fill,body.Path);
            using(var pen=new Pen(Ink,7) {LineJoin=LineJoin.Round}) g.DrawPath(pen,body.Path);
            return true;
        }
        public static void DrawBehind(Graphics g,MonsterPose pose,MonsterVariant v)
        {
            if(!IsReward(v)) return;
            float sway=pose.Toe*2;
            if(v==MonsterVariant.JournalKnight || v==MonsterVariant.MemoryMage || v==MonsterVariant.PageSovereign || v==MonsterVariant.MemoryHero)
                Fill(g,new Outline().M(360,520).Q(450,531,537,692+sway).L(515,680+sway).L(527,733+sway).Q(457,711,425,652).Close(),Tone(v));
            if(v==MonsterVariant.Bookmark)
                Fill(g,new Outline().M(440,557).L(482,556).L(482,674+sway).L(463,657+sway).L(442,678+sway).Close(),Color.FromArgb(215,117,102));
            if(v==MonsterVariant.StarExplorer || v==MonsterVariant.JournalAlchemist)
                Fill(g,new Outline().M(445,565).Q(482,548,493,579).L(508,643).Q(493,670,464,648).Close(),Tone(v));
        }
        public static void DrawFront(Graphics g,MonsterPose pose,MonsterVariant v)
        {
            if(!IsReward(v)) return;
            Color tone=Tone(v);
            switch(v) {
                case MonsterVariant.Apprentice:
                    Fill(g,new Outline().M(333,507).L(397,529).L(377,553).L(326,536).Close(),tone);
                    Fill(g,new Outline().M(377,543).L(397,576).L(389,591).L(369,559).Close(),tone);
                    Line(g,new Outline().M(409,592).L(438,559),8,Color.FromArgb(197,138,61));
                    Dot(g,407,595,5,Ink); break;
                case MonsterVariant.Bookmark:
                    Fill(g,new Outline().M(364,566).Q(395,558,408,571).Q(425,557,451,565).L(451,620).Q(426,614,408,628).Q(391,615,364,622).Close(),Color.FromArgb(255,249,216));
                    Line(g,new Outline().M(408,571).L(408,625).M(374,583).L(396,585).M(420,582).L(441,581).M(374,597).L(395,599),4,tone); break;
                case MonsterVariant.JournalKnight:
                    Fill(g,new Outline().M(336,521).L(375,530).L(395,552).L(361,571).L(329,549).Close(),Color.FromArgb(207,223,231));
                    Fill(g,new Outline().M(393,572).L(444,572).L(448,606).Q(428,635,418,638).Q(392,614,393,572).Close(),tone);
                    Line(g,new Outline().M(420,581).L(420,618).M(407,596).L(434,596),5,Color.White); break;
                case MonsterVariant.MemoryMage:
                    Fill(g,new Outline().M(333,482).L(376,398).L(404,492).Q(370,510,333,482).Close(),tone);
                    Fill(g,new Outline().M(325,483).Q(367,473,413,493).Q(398,516,329,499).Close(),Color.FromArgb(100,81,147));
                    Star(g,379,449,12,Color.FromArgb(255,223,133));
                    Line(g,new Outline().M(408,624).L(438,554),8,Color.FromArgb(107,91,135)); Star(g,440,548,17,Color.FromArgb(255,223,133)); break;
                case MonsterVariant.StarExplorer:
                    Fill(g,new Outline().M(329,511).L(379,519).L(400,547).L(341,535).Close(),tone);
                    Fill(g,new Outline().M(375,529).L(425,554).L(407,577).L(367,546).Close(),tone);
                    Dot(g,420,604,25,Color.FromArgb(245,239,201));
                    Star(g,420,604,16,Color.FromArgb(69,120,139)); break;
                case MonsterVariant.JournalAlchemist:
                    Fill(g,new Outline().M(332,524).L(398,549).L(452,621).L(423,641).L(358,570).Close(),Color.FromArgb(235,237,215));
                    Fill(g,new Outline().M(402,573).L(419,573).L(419,590).L(439,620).Q(414,643,391,620).L(402,590).Close(),Color.FromArgb(195,227,157));
                    Line(g,new Outline().M(399,614).L(430,614),4,tone); Dot(g,407,604,4,Color.White); break;
                case MonsterVariant.PageSovereign:
                    Fill(g,new Outline().M(335,494).L(326,451).L(354,467).L(371,439).L(386,470).L(415,460).L(406,505).Close(),Color.FromArgb(244,199,107));
                    Dot(g,372,483,7,tone);
                    Fill(g,new Outline().M(361,542).L(425,569).L(449,610).L(421,628).L(388,588).Close(),tone);
                    Star(g,405,580,12,Color.FromArgb(255,225,145)); break;
                case MonsterVariant.MemoryHero:
                    Fill(g,new Outline().M(329,514).L(372,520).L(401,545).L(362,560).L(329,543).Close(),tone);
                    Fill(g,new Outline().M(387,563).L(447,583).L(439,624).L(414,644).L(389,614).Close(),Color.FromArgb(255,226,155));
                    Star(g,416,604,18,Color.FromArgb(170,111,57));
                    Line(g,new Outline().M(436,556).L(486,503),9,Color.FromArgb(173,202,204));
                    Line(g,new Outline().M(431,545).L(452,565),7,tone); break;
            }
        }
        private static void Fill(Graphics g,Outline path,Color color)
        {
            using(path) using(var fill=new SolidBrush(color)) using(var pen=new Pen(Ink,5) {LineJoin=LineJoin.Round}) { g.FillPath(fill,path.Path); g.DrawPath(pen,path.Path); }
        }
        private static void Line(Graphics g,Outline path,float width,Color color)
        {
            using(path) using(var pen=new Pen(color,width) {StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}) g.DrawPath(pen,path.Path);
        }
        private static void Dot(Graphics g,float x,float y,float radius,Color color) { using(var b=new SolidBrush(color)) g.FillEllipse(b,x-radius,y-radius,radius*2,radius*2); }
        private static void Star(Graphics g,float x,float y,float size,Color color)
        {
            Fill(g,new Outline().M(x,y-size).L(x+size*.28f,y-size*.28f).L(x+size,y).L(x+size*.28f,y+size*.28f)
                .L(x,y+size).L(x-size*.28f,y+size*.28f).L(x-size,y).L(x-size*.28f,y-size*.28f).Close(),color);
        }
    }
}
