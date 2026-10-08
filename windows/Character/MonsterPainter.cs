using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyDay.Windows.Character
{
    public struct MonsterPose
    {
        public float Bob, BodyScale, WidthScale, Toe, Pulse, Tilt, LookX, LookY, EyeScale, FireStrength, Phase, YawnStretch;
        public bool Closed, Happy, Held, Sleeping, Curious;
        public static MonsterPose At(double seconds, bool moving, bool held, bool happy)
        {
            return ForActivity(seconds,held?PetActivity.Drag:happy?PetActivity.Fire:moving?PetActivity.Walk:PetActivity.Rest,seconds,0,0);
        }
        public static MonsterPose ForActivity(double seconds,PetActivity activity,double age,float lookX,float lookY)
        {
            float stride = (float)Math.Sin(seconds * Math.PI * 2 / .8);
            float breath = (float)Math.Sin(seconds * Math.PI * 2 / 2.4);
            bool held=activity==PetActivity.Drag, happy=activity==PetActivity.Fire, moving=activity==PetActivity.Walk;
            bool hop=activity==PetActivity.Hop, sleeping=activity==PetActivity.Sleep, curious=activity==PetActivity.Hover || activity==PetActivity.Look;
            bool yawn=activity==PetActivity.Yawn;
            float leap=(float)Math.Abs(Math.Sin(age*Math.PI/.65));
            float squash=hop?(float)Math.Cos(age*Math.PI*2/.65)*.08f:moving?stride*.045f:breath*.025f;
            float strength=happy?(float)Math.Min(1,Math.Min(age/.08,(PetBehavior.TouchFireSeconds-age)/.18)):0;
            float mouth=yawn && age>=0 && age<PetBehavior.YawnSeconds?(float)Math.Sin(age/PetBehavior.YawnSeconds*Math.PI):0;
            if(yawn && age>.65 && age<1) strength=(float)Math.Sin((age-.65)/.35*Math.PI)*.35f;
            strength=Math.Max(0,strength);
            var result = new MonsterPose {
                Bob = held?0:hop?-leap*11:happy?-Math.Abs(stride)*4:moving?-Math.Abs(stride)*4:breath*1.5f,
                BodyScale = held?1.08f:sleeping?.91f+breath*.018f:1+squash+mouth*.045f,
                WidthScale = held?.94f:sleeping?1.08f:1-squash*.65f,
                Toe = moving?stride*9:hop?leap*4:0,
                Tilt = held?7:hop?(float)Math.Sin(age*Math.PI/.65)*-5:moving?stride*3:curious?(float)Math.Sin(age*2)*3:sleeping?-5:yawn?-mouth*4:breath*1.5f,
                LookX = Math.Max(-1,Math.Min(1,lookX)), LookY=Math.Max(-1,Math.Min(1,lookY)),
                EyeScale=held?1.2f:curious?1.1f:1, FireStrength=strength, Phase=(float)seconds, YawnStretch=mouth,
                Pulse = (float)((Math.Sin(seconds * Math.PI * 2 / .6) + 1) / 2),
                Closed = !held && (sleeping || happy || yawn || seconds%4.2>=3.83 && seconds%4.2<3.92 || seconds%4.2>=4.01 && seconds%4.2<4.09),
                Happy = happy, Held = held, Sleeping=sleeping, Curious=curious
            };
            if(activity==PetActivity.Look) { result.LookX=(float)Math.Sin(age*2.4)*.8f; result.LookY=-.3f; }
            return result;
        }
    }

    // Retains the Android sketch outline; Windows poses add stretch, expressions and reactions.
    public static class MonsterPainter
    {
        public static readonly Color Ink = Color.FromArgb(57, 55, 53);
        public static readonly Color Body = Color.FromArgb(255, 254, 252);
        public static readonly Color Mouth = Color.FromArgb(246, 241, 232);
        public static readonly Color Fire = Color.FromArgb(255, 215, 154);
        public static readonly Color Core = Color.FromArgb(255, 247, 219);
        private sealed class Shape : IDisposable
        {
            public readonly GraphicsPath Path = new GraphicsPath();
            private PointF current;
            public Shape M(float x, float y) { Path.StartFigure(); current = new PointF(x, y); return this; }
            public Shape C(float a, float b, float c, float d, float x, float y)
            { Path.AddBezier(current, new PointF(a, b), new PointF(c, d), new PointF(x, y)); current = new PointF(x, y); return this; }
            public Shape Q(float a, float b, float x, float y)
            { return C(current.X + (a - current.X) * 2 / 3, current.Y + (b - current.Y) * 2 / 3, x + (a - x) * 2 / 3, y + (b - y) * 2 / 3, x, y); }
            public Shape L(float x, float y) { Path.AddLine(current, new PointF(x, y)); current = new PointF(x, y); return this; }
            public Shape Close() { Path.CloseFigure(); return this; }
            public void Dispose() { Path.Dispose(); }
        }
        private static void Fill(Graphics g, Shape shape, Color color, bool outline = true)
        {
            using (var brush = new SolidBrush(color)) g.FillPath(brush, shape.Path);
            if (outline) using (var pen = new Pen(Ink, 5.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) g.DrawPath(pen, shape.Path);
        }
        public static void Draw(Graphics g, Rectangle area, MonsterPose pose, bool facingLeft)
        {
            Draw(g,area,pose,facingLeft,MonsterVariant.Original);
        }
        public static void Draw(Graphics g, Rectangle area, MonsterPose pose, bool facingLeft,MonsterVariant variant)
        {
            var save = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(area.Width / 555f, area.Height / 430f);
            float shadowWidth=area.Width*(.31f+pose.Bob*.005f);
            using(var shadow=new SolidBrush(Color.FromArgb(23,Ink)))
                g.FillEllipse(shadow,area.X+area.Width*.62f-shadowWidth/2,area.Y+area.Height*.91f,shadowWidth,area.Height*.045f);
            g.TranslateTransform(area.X + area.Width / 2f, area.Y + area.Height / 2f + pose.Bob * scale * 5.8f);
            g.ScaleTransform(facingLeft ? scale : -scale, scale);
            g.TranslateTransform(-275, -615);
            if(variant==MonsterVariant.Winged || variant==MonsterVariant.Mini) {
                float shrink=variant==MonsterVariant.Winged?.86f:.82f;
                g.TranslateTransform(275,650); g.ScaleTransform(shrink,shrink); g.TranslateTransform(-275,-650);
            }
            var bodySave = g.Save();
            g.TranslateTransform(275, 730); g.RotateTransform(pose.Tilt+(variant==MonsterVariant.Speedy?-8:0));
            g.ScaleTransform(pose.WidthScale*(variant==MonsterVariant.Speedy?1.05f:1),pose.BodyScale*(variant==MonsterVariant.Speedy?.9f:1)); g.TranslateTransform(-275, -730);
            DrawBehind(g,pose,variant);
            float l = pose.Toe, r = -pose.Toe;
            if(variant==MonsterVariant.Puffy) {
                using(var body=new Shape().M(215,513).C(219,482,231,451,248,439).C(261,433,268,455,275,491)
                    .C(281,466,288,430,307,433).C(326,435,337,469,342,498).C(425,500,509,566,507,647)
                    .C(507,725,441,770,355,776).C(271,788,194,763,165,721).C(142,682,156,654,180,636)
                    .C(150,635,122,613,116,590).C(112,560,158,520,190,515).Q(207,511,215,513).Close()) Fill(g,body,Body);
                using(var fold=new Shape().M(377,641).Q(415,623,429,653).M(468,607).Q(483,623,475,642))
                using(var pen=new Pen(Ink,5.5f) { StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawPath(pen,fold.Path);
            } else if(variant==MonsterVariant.Mini) {
                using(var body=new Shape().M(215,513).C(219,482,231,451,248,439).C(261,433,268,455,275,491)
                    .C(281,466,288,430,307,433).C(326,435,337,469,342,498).C(384,488,427,516,458,553)
                    .C(494,590,504,646,493,701).Q(488,743+r,467,751+r).Q(451,754+r,448,725+r)
                    .Q(436,752+r,420,748+r).C(407,736,413,687,385,685).C(359,679,338,702,318,737)
                    .Q(301,764+l,287,753+l).Q(272,760+l,268,734+l).Q(257,755+l,244,745+l)
                    .C(215,725,217,673,205,632).C(181,637,150,627,133,612).C(112,598,113,582,120,568)
                    .C(137,542,165,519,190,515).Q(207,511,215,513).Close()) Fill(g,body,Body);
            } else {
            using (var body = new Shape().M(215,513).C(219,482,231,451,248,439).C(261,433,268,455,275,491)
                .C(281,466,288,430,307,433).C(326,435,337,469,342,498).C(383,500,405,516,425,535)
                .C(474,576,511,641,518,697).C(523,721,519,736+r,514,744+r).Q(505,746+r,490,731+r)
                .Q(494,745+r,483,752+r).Q(473,758+r,461,747+r).C(438,718,409,679,381,668)
                .C(357,654,334,672,318,684).C(287,708,260,742,238,765+l).C(225,783+l,213,778+l,214,755+l)
                .C(196,767+l,177,761+l,170,749+l).C(154,732,169,702,180,679).L(205,632)
                .C(181,637,150,627,133,612).C(112,598,113,582,120,568).C(137,542,165,519,190,515).Q(207,511,215,513).Close()) Fill(g, body, Body);
            }
            using (var mouth = new Shape().M(120,586).Q(154,567,185,561).C(205,557,222,565,227,579)
                .C(232,593,220,614,209,625).Q(168,643,133,611).Q(123,601,120,586).Close()) Fill(g, mouth, Mouth, false);
            using (var lip = new Shape().M(120,586).Q(154,567,185,561).C(205,557,222,565,227,579)
                .C(232,593,220,614,209,625).M(185,629).Q(148,630,124,602))
            using (var pen = new Pen(Ink, 5.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawPath(pen, lip.Path);
            if(variant==MonsterVariant.Dazed) {
                using(var tongue=new Shape().M(181,623).Q(178,644,190,648).Q(204,648,198,628))
                using(var pen=new Pen(Ink,5.5f) { StartCap=LineCap.Round,EndCap=LineCap.Round }) g.DrawPath(pen,tongue.Path);
            }
            if (pose.Closed)
            {
                using (var eyes = new Shape().M(235,480).Q(245,489,256,478).M(295,478).Q(306,488,316,475))
                using (var pen = new Pen(Ink, 6.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawPath(pen, eyes.Path);
            }
            else foreach (float x in new float[] { 246, 305 })
            {
                var eye = g.Save(); g.TranslateTransform(x+pose.LookX*5,476+pose.LookY*4); g.RotateTransform(18);
                using (var brush = new SolidBrush(Ink)) {
                    if(variant==MonsterVariant.Dazed) g.FillEllipse(brush,-4,-4,8,9);
                    else g.FillEllipse(brush,-9,-17*pose.EyeScale,18,32*pose.EyeScale);
                }
                if(variant!=MonsterVariant.Dazed) using (var shine = new SolidBrush(Color.FromArgb(245,Body))) g.FillEllipse(shine,-5,-11,4,6);
                g.Restore(eye);
            }
            if(pose.Curious || pose.Happy) {
                using(var blush=new SolidBrush(Color.FromArgb(pose.Happy?75:40,238,156,122))) {
                    g.FillEllipse(blush,226,509,22,11); g.FillEllipse(blush,310,509,22,11);
                }
            }
            DrawDetails(g,variant);
            g.Restore(bodySave);
            if (!pose.Held && pose.FireStrength>.01f)
            {
                float size = .42f + pose.FireStrength*(.47f + pose.Pulse*.13f);
                g.TranslateTransform(136,636); g.ScaleTransform(size,size); g.RotateTransform((pose.Pulse-.5f)*7); g.TranslateTransform(-136,-636);
                using (var stream = new Shape().M(121,607).Q(103,611,106,624).L(133,639).Q(129,620,121,607).Close()) Fill(g,stream,Fire,false);
                using (var fire = new Shape().M(130,630).C(115,613,95,609,81,617).C(63,610,42,625,32,638).Q(40,646,56,644)
                    .C(37,658,22,677,30,695).C(33,708,43,708,57,696).C(51,711,52,724,63,725).C(73,726,82,706,83,701)
                    .C(79,718,82,732,91,729).C(102,725,108,708,110,704).C(106,720,111,730,120,721)
                    .C(135,705,150,672,139,650).Q(136,638,130,630).Close()) Fill(g,fire,Fire);
                using (var core = new Shape().M(83,620).Q(68,632,77,640).Q(84,635,88,635).Q(79,650,91,657)
                    .Q(98,650,100,642).Q(95,664,106,665).Q(116,653,116,646).Q(115,669,125,661)
                    .Q(139,640,119,625).Q(100,616,83,620).Close()) Fill(g,core,Core);
                if(pose.Happy && pose.FireStrength>0) {
                    for(int i=0;i<5;i++) {
                        float phase=(pose.Phase*1.7f+i*.19f)%1;
                        float px=70-phase*65,py=641+(float)Math.Sin(phase*6+i)*22;
                        using(var ember=new SolidBrush(Color.FromArgb((int)(170*(1-phase)*pose.FireStrength),i%2==0?Fire:Color.FromArgb(237,160,91))))
                            g.FillEllipse(ember,px,py,5+phase*3,5+phase*3);
                    }
                }
            }
            g.Restore(save);
            if(pose.Sleeping) {
                using(var font=new Font("Segoe UI",Math.Max(8,area.Width*.06f),FontStyle.Bold,GraphicsUnit.Pixel))
                using(var brush=new SolidBrush(Color.FromArgb(125,Ink))) {
                    float rise=(pose.Phase*.25f)%1;
                    g.DrawString("z",font,brush,area.X+area.Width*.72f,area.Y+area.Height*(.12f-rise*.05f));
                    g.DrawString("z",font,brush,area.X+area.Width*.8f,area.Y+area.Height*(.04f-rise*.05f));
                }
            }
        }
        private static void DrawBehind(Graphics g,MonsterPose pose,MonsterVariant variant)
        {
            if(variant==MonsterVariant.Winged) {
                float flap=(float)Math.Sin(pose.Phase*Math.PI*2)*11;
                var saved=g.Save(); g.TranslateTransform(245,575); g.RotateTransform(flap); g.TranslateTransform(-245,-575);
                using(var wing=new Shape().M(250,560).C(193,524,123,453,78,438).Q(108,477,101,528)
                    .Q(139,511,163,553).Q(190,555,204,609).L(265,605).Close()) Fill(g,wing,Body);
                using(var rib=new Shape().M(250,580).Q(169,519,94,468))
                using(var pen=new Pen(Ink,3.5f)) g.DrawPath(pen,rib.Path); g.Restore(saved);
                saved=g.Save(); g.TranslateTransform(340,575); g.RotateTransform(-flap); g.TranslateTransform(-340,-575);
                using(var wing=new Shape().M(329,562).C(378,512,401,425,427,405).Q(449,455,488,482)
                    .Q(451,484,461,524).Q(415,516,398,562).L(355,610).Close()) Fill(g,wing,Body);
                using(var rib=new Shape().M(343,571).Q(412,493,427,429))
                using(var pen=new Pen(Ink,3.5f)) g.DrawPath(pen,rib.Path); g.Restore(saved);
            }
            if(variant==MonsterVariant.Speedy) {
                float shift=(float)Math.Sin(pose.Phase*12)*5;
                using(var pen=new Pen(Ink,4) { StartCap=LineCap.Round,EndCap=LineCap.Round }) {
                    g.DrawLine(pen,465,559,536+shift,568); g.DrawLine(pen,490,600,558+shift,610); g.DrawLine(pen,509,644,565+shift,653);
                }
            }
        }
        private static void DrawDetails(Graphics g,MonsterVariant variant)
        {
            using(var pen=new Pen(Ink,4) { StartCap=LineCap.Round,EndCap=LineCap.Round }) {
                if(variant==MonsterVariant.Spiky) {
                    g.DrawLine(pen,220,483,206,472); g.DrawLine(pen,234,456,222,443); g.DrawLine(pen,312,442,321,428);
                    g.DrawLine(pen,374,510,380,494); g.DrawLine(pen,394,518,403,502); g.DrawLine(pen,456,570,472,562);
                    g.DrawLine(pen,481,604,499,600); g.DrawLine(pen,511,683,527,684); g.DrawLine(pen,507,706,523,711);
                    g.DrawLine(pen,277,533,292,526); g.DrawLine(pen,282,544,297,536);
                    using(var quiver=new Shape().M(130,577).Q(145,587,157,576).Q(169,584,178,574)) g.DrawPath(pen,quiver.Path);
                }
                if(variant==MonsterVariant.Horned) {
                    using(var horn=new Shape().M(345,506).Q(379,471,417,455).Q(416,490,363,519).Close()) Fill(g,horn,Ink);
                    using(var horn=new Shape().M(382,551).Q(403,522,441,515).Q(439,545,398,562).Close()) Fill(g,horn,Ink);
                    g.DrawLine(pen,232,461,257,473); g.DrawLine(pen,295,472,318,460);
                    using(var shine=new SolidBrush(Body)) { g.FillEllipse(shine,391,475,6,7); g.FillEllipse(shine,417,533,6,7); }
                }
            }
        }
    }
}
