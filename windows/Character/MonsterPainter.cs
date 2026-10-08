using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MyDay.Windows.Character
{
    public struct MonsterPose
    {
        public float Bob, BodyScale, Toe, Pulse;
        public bool Closed, Happy, Held;
        public static MonsterPose At(double seconds, bool moving, bool held, bool happy)
        {
            float stride = (float)Math.Sin(seconds * Math.PI * 2 / .8);
            float breath = (float)Math.Sin(seconds * Math.PI * 2 / 2.4);
            return new MonsterPose {
                Bob = held ? 0 : happy ? -Math.Abs(stride) * 7 : moving ? -Math.Abs(stride) * 3 : breath,
                BodyScale = held ? 1 : 1 + breath * .025f, Toe = moving && !held && !happy ? stride * 6 : 0,
                Pulse = (float)((Math.Sin(seconds * Math.PI * 2 / .6) + 1) / 2),
                Closed = !held && (happy || seconds % 4.2 >= 4 && seconds % 4.2 < 4.12),
                Happy = happy && !held, Held = held
            };
        }
    }

    // Uses the same sketch coordinates and pose periods as the Android painter.
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
            var save = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(area.Width / 555f, area.Height / 430f);
            g.TranslateTransform(area.X + area.Width / 2f, area.Y + area.Height / 2f + pose.Bob * scale * 5.8f);
            g.ScaleTransform(facingLeft ? scale : -scale, scale);
            g.TranslateTransform(-275, -615);
            var bodySave = g.Save();
            g.TranslateTransform(0, 740); g.ScaleTransform(1, pose.BodyScale); g.TranslateTransform(0, -740);
            float l = pose.Toe, r = -pose.Toe;
            using (var body = new Shape().M(215,513).C(219,482,231,451,248,439).C(261,433,268,455,275,491)
                .C(281,466,288,430,307,433).C(326,435,337,469,342,498).C(383,500,405,516,425,535)
                .C(474,576,511,641,518,697).C(523,721,519,736+r,514,744+r).Q(505,746+r,490,731+r)
                .Q(494,745+r,483,752+r).Q(473,758+r,461,747+r).C(438,718,409,679,381,668)
                .C(357,654,334,672,318,684).C(287,708,260,742,238,765+l).C(225,783+l,213,778+l,214,755+l)
                .C(196,767+l,177,761+l,170,749+l).C(154,732,169,702,180,679).L(205,632)
                .C(181,637,150,627,133,612).C(112,598,113,582,120,568).C(137,542,165,519,190,515).Q(207,511,215,513).Close()) Fill(g, body, Body);
            using (var mouth = new Shape().M(120,586).Q(154,567,185,561).C(205,557,222,565,227,579)
                .C(232,593,220,614,209,625).Q(168,643,133,611).Q(123,601,120,586).Close()) Fill(g, mouth, Mouth, false);
            using (var lip = new Shape().M(120,586).Q(154,567,185,561).C(205,557,222,565,227,579)
                .C(232,593,220,614,209,625).M(185,629).Q(148,630,124,602))
            using (var pen = new Pen(Ink, 5.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawPath(pen, lip.Path);
            if (pose.Closed)
            {
                using (var eyes = new Shape().M(235,480).Q(245,489,256,478).M(295,478).Q(306,488,316,475))
                using (var pen = new Pen(Ink, 6.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawPath(pen, eyes.Path);
            }
            else foreach (float x in new float[] { 246, 305 })
            {
                var eye = g.Save(); g.TranslateTransform(x,476); g.RotateTransform(18);
                using (var brush = new SolidBrush(Ink)) g.FillEllipse(brush,-9,-17,18,32); g.Restore(eye);
            }
            g.Restore(bodySave);
            if (!pose.Held)
            {
                float size = pose.Happy ? .96f + pose.Pulse * .13f : .75f + pose.Pulse * .025f;
                g.TranslateTransform(136,636); g.ScaleTransform(size,size); g.RotateTransform((pose.Pulse-.5f)*7); g.TranslateTransform(-136,-636);
                if (pose.Happy) using (var stream = new Shape().M(121,607).Q(103,611,106,624).L(133,639).Q(129,620,121,607).Close()) Fill(g,stream,Fire,false);
                using (var fire = new Shape().M(130,630).C(115,613,95,609,81,617).C(63,610,42,625,32,638).Q(40,646,56,644)
                    .C(37,658,22,677,30,695).C(33,708,43,708,57,696).C(51,711,52,724,63,725).C(73,726,82,706,83,701)
                    .C(79,718,82,732,91,729).C(102,725,108,708,110,704).C(106,720,111,730,120,721)
                    .C(135,705,150,672,139,650).Q(136,638,130,630).Close()) Fill(g,fire,pose.Happy?Fire:Body);
                using (var core = new Shape().M(83,620).Q(68,632,77,640).Q(84,635,88,635).Q(79,650,91,657)
                    .Q(98,650,100,642).Q(95,664,106,665).Q(116,653,116,646).Q(115,669,125,661)
                    .Q(139,640,119,625).Q(100,616,83,620).Close()) Fill(g,core,pose.Happy?Core:Mouth);
            }
            g.Restore(save);
        }
    }
}
