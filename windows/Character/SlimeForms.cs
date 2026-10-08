using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Outline=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // Original silhouettes inspired by broad slime shapes. No downloaded game assets are used.
    internal static class SlimeForms
    {
        public static void PlaceFace(Graphics g,MonsterVariant variant)
        {
            float x=0,y=0,sx=1,sy=1;
            switch(variant) {
                case MonsterVariant.Droplet: y=50; break;
                case MonsterVariant.Puddle: x=-10; y=125; sy=.8f; break;
                case MonsterVariant.Pill: x=40; break;
                case MonsterVariant.Cube: x=25; y=65; break;
                case MonsterVariant.Cloud: y=60; break;
                case MonsterVariant.Sprout: y=55; break;
                case MonsterVariant.Twin: x=-50; y=72; sx=sy=.7f; break;
                case MonsterVariant.Ribbon: x=-10; y=40; sx=sy=.9f; break;
            }
            g.TranslateTransform(x+275,y+570); g.ScaleTransform(sx,sy); g.TranslateTransform(-275,-570);
        }
        public static bool DrawBody(Graphics g,MonsterPose pose,MonsterVariant variant)
        {
            float wave=(float)Math.Sin(pose.Phase*Math.PI*2/1.6)*7;
            Color ink=MonsterPainter.Ink;
            switch(variant) {
                case MonsterVariant.Droplet:
                    using(var body=new Outline().M(321+wave,424).C(321,461,246,493,197,535)
                        .C(145,579,109,626,122,679).C(134,740,197,770,294,774)
                        .C(393,781,472,758,489,701).C(509,639,477,589,420,551)
                        .C(376,521,335,479,321+wave,424).Close()) Fill(g,body,Color.FromArgb(232,242,250));
                    Highlight(g,371,529,407,561); break;
                case MonsterVariant.Puddle:
                    using(var body=new Outline().M(157,630).C(214,589,313,580+wave,401,606)
                        .C(442,616,464,645,493,655).C(541,672,522,699,497,706)
                        .Q(529,741,487,749).Q(469,783,432,765).Q(394,789,356,766)
                        .Q(304,798,267,774).Q(210,790,189,765).Q(140,782,125,754)
                        .Q(81,756,91,727).Q(57,706,98,687).C(93,659,125,643,157,630).Close()) Fill(g,body,Color.FromArgb(236,247,234));
                    Highlight(g,339,623,402,639); break;
                case MonsterVariant.Pill:
                    using(var body=new Outline().M(282,437).C(349,415,390,444,397,501)
                        .C(401+wave*.3f,555,386,590,397,633).C(419,687,404,743,366,763)
                        .C(312,785,242,767,219,719).C(205,687,226,654,202,626)
                        .Q(153,631,145,599).Q(137,574,172,552).C(197,537,188,469,229,446)
                        .Q(252,434,282,437).Close()) Fill(g,body,Color.FromArgb(249,242,230));
                    Highlight(g,361,484,365,529); break;
                case MonsterVariant.Cube:
                    using(var body=new Outline().M(166,495).L(442,495+wave*.2f).Q(490,493,492,536)
                        .L(492,706).Q(492,750,449,752).L(166,752).Q(128,752,130,709)
                        .L(130,536).Q(128,496,166,495).Close()) Fill(g,body,Color.FromArgb(242,237,249));
                    using(var line=new Outline().M(426,507).L(426,698).Q(426,725,449,740))
                    using(var pen=new Pen(Color.FromArgb(65,ink),4)) g.DrawPath(pen,line.Path);
                    Highlight(g,173,518,382,518); break;
                case MonsterVariant.Cloud:
                    using(var body=new Outline().M(149,575).C(146,527,204,510,237,525)
                        .C(246,476,308,463,344,501).C(382,469,439,498,437,540)
                        .C(490,535,524,579,501,615).C(542,645,523,698,483,704)
                        .C(464,757,408,763,374,738).C(345,785,284,792,253,754)
                        .C(204,786,156,763,151,723).C(101,718,90,679,112,652)
                        .C(78,618,110,577,149,575).Close()) Fill(g,body,Color.FromArgb(255,252,250));
                    Highlight(g,380,522,404,534); break;
                case MonsterVariant.Sprout:
                    using(var stem=new Outline().M(296,510).Q(309+wave*.5f,472,326,447))
                    using(var pen=new Pen(ink,6) { StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawPath(pen,stem.Path);
                    using(var leaf=new Outline().M(326,457).C(292,458,272,433,276,407)
                        .C(308,405,335,427,326,457).Close()) Fill(g,leaf,Color.FromArgb(170,199,167));
                    using(var leaf=new Outline().M(321,450).C(335,420,372,412,393,429)
                        .C(377,460,345,471,321,450).Close()) Fill(g,leaf,Color.FromArgb(199,218,174));
                    using(var body=new Outline().M(193,553).C(224,495,295,481,351,504)
                        .C(400,524,427,570,456,609).C(498,665,475,740,413,762)
                        .C(340,791,258,766,216,743).C(172,721,132,705,130,668)
                        .C(99,647,99,617,122,598).Q(153,573,193,553).Close()) Fill(g,body,Color.FromArgb(242,248,232));
                    Highlight(g,366,543,393,572); break;
                case MonsterVariant.Twin:
                    using(var body=new Outline().M(152,579).C(155,531,225,513,264,554)
                        .C(286,505,348,489,386,514).C(439,492,489,548,478,590)
                        .C(517,630,502,699,460,722).C(421,748,371,739,337,715)
                        .C(309,763,250,776,212,745).C(159,756,124,720,128,680)
                        .C(94,661,97,626,113,609).Q(127,586,152,579).Close()) Fill(g,body,Color.FromArgb(255,241,230));
                    using(var brush=new SolidBrush(ink)) {
                        if(!pose.Closed) { g.FillEllipse(brush,367,556,13,24); g.FillEllipse(brush,411,552,13,24); }
                    }
                    using(var face=new Outline().M(415,622).Q(451,606,459,623).M(337,663).Q(356,688,340,708))
                    using(var pen=new Pen(ink,5) { StartCap=LineCap.Round,EndCap=LineCap.Round }) {
                        g.DrawPath(pen,face.Path);
                        if(pose.Closed) { g.DrawLine(pen,366,569,383,572); g.DrawLine(pen,410,566,427,569); }
                    }
                    Highlight(g,443,551,462,578); break;
                case MonsterVariant.Ribbon:
                    using(var body=new Outline().M(150,550).C(185,500,230,475,280,500)
                        .C(330,520,360,610,405,620).C(451,630,450,576+wave,425,585)
                        .C(410,585,415,563,440,560).C(490,544,521,604,508,646)
                        .C(491,711,435,755,377,714).C(338,685,315,611,278,627)
                        .C(244,638,245,720,204,733).C(169,750,147,720,157,681)
                        .C(177,652,164,627,133,612).C(112,595,115,568,150,550).Close()) Fill(g,body,Color.FromArgb(252,244,226));
                    Highlight(g,368,648,401,678); break;
                default: return false;
            }
            return true;
        }
        private static void Fill(Graphics g,Outline body,Color color) { MonsterPainter.Fill(g,body,color); }
        private static void Highlight(Graphics g,float x,float y,float xx,float yy)
        {
            using(var pen=new Pen(Color.FromArgb(210,Color.White),9) { StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawLine(pen,x,y,xx,yy);
        }
    }
}
