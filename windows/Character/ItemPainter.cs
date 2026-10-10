using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using MyDay.Windows.Core;
using Shape=MyDay.Windows.Character.MonsterPainter.Shape;

namespace MyDay.Windows.Character
{
    // Original vector equipment: metallic facets, gemstone cores and bounded magical halos.
    public static class ItemPainter
    {
        private static readonly Color Ink=Color.FromArgb(24,29,52),Gold=Color.FromArgb(241,199,110);
        public static Color Tone(ItemElement e)
        {
            switch(e) {
                case ItemElement.Solar:return Color.FromArgb(255,211,93);
                case ItemElement.Ember:return Color.FromArgb(244,83,41);
                case ItemElement.Frost:return Color.FromArgb(102,226,255);
                case ItemElement.Storm:return Color.FromArgb(116,167,255);
                case ItemElement.Shadow:return Color.FromArgb(171,99,244);
                default:return Color.FromArgb(145,132,255);
            }
        }
        public static void Draw(Graphics g,RectangleF area,SangmonItem item,double seconds)
        {
            if(item==null || area.Width<=0 || area.Height<=0) return;
            var saved=g.Save(); g.SmoothingMode=SmoothingMode.AntiAlias;
            float scale=Math.Min(area.Width/200,area.Height/240);
            g.TranslateTransform(area.X+area.Width/2,area.Y+area.Height/2); g.ScaleTransform(scale,scale); g.TranslateTransform(-100,-120);
            if(item.Slot=="weapon") Sword(g,item.Element,seconds); else Orb(g,item.Element,seconds);
            g.Restore(saved);
        }
        private static void Halo(Graphics g,float x,float y,float width,float height,Color tone,float pulse)
        {
            using(var path=new GraphicsPath()) {
                path.AddEllipse(x-width/2,y-height/2,width,height);
                using(var glow=new PathGradientBrush(path) {CenterColor=Color.FromArgb((int)(80+pulse*35),tone),SurroundColors=new[] {Color.FromArgb(0,tone)}}) g.FillPath(glow,path);
            }
        }
        private static void Sword(Graphics g,ItemElement e,double time)
        {
            Color tone=Tone(e); float pulse=(float)(.5+.5*Math.Sin(time*3));
            Halo(g,100,148,90,164,tone,pulse);
            using(var blade=new Shape()) {
                switch(e) {
                    case ItemElement.Ember: blade.M(82,82).L(75,107).L(87,119).L(77,139).L(92,216).L(100,232).L(115,172).L(111,122).L(124,105).L(117,82).Close(); break;
                    case ItemElement.Frost: blade.M(83,83).L(77,107).L(87,177).L(100,235).L(115,184).L(125,110).L(118,83).Close(); break;
                    case ItemElement.Storm: blade.M(82,84).L(79,145).L(90,128).L(86,177).L(100,234).L(117,178).L(114,125).L(124,139).L(120,84).Close(); break;
                    case ItemElement.Shadow: blade.M(89,81).C(68,122,113,130,85,155).L(98,163).C(76,187,83,215,117,234).C(99,200,125,186,113,167).L(127,157).C(106,136,127,106,110,83).Close(); break;
                    case ItemElement.Astral: blade.M(83,83).L(71,105).L(87,127).L(78,158).L(93,185).L(100,234).L(117,204).L(127,167).L(117,139).L(129,111).L(114,84).Close(); break;
                    default: blade.M(85,82).L(82,153).L(93,211).L(100,234).L(109,211).L(119,154).L(115,82).Close(); break;
                }
                Color dark=e==ItemElement.Ember?Color.FromArgb(103,19,30):e==ItemElement.Shadow?Color.FromArgb(53,24,102):Color.FromArgb(42,82,137);
                Color highlight=e==ItemElement.Solar?Color.FromArgb(255,254,234):e==ItemElement.Ember?Color.FromArgb(255,153,53):e==ItemElement.Shadow?Color.FromArgb(204,156,255):Color.FromArgb(223,249,255);
                Paint(g,blade,highlight,e==ItemElement.Solar?Color.FromArgb(127,147,175):dark,tone);
                var clip=g.Save();g.SetClip(blade.Path,CombineMode.Intersect);
                using(var facet=new Shape().M(101,82).L(96,167).L(101,235).L(120,165).L(118,85).Close()) Flat(g,facet,Color.FromArgb(180,tone),false);
                Line(g,new Shape().M(101,94).L(99,199).L(100,224),2.2f,Color.FromArgb(230,244,255));
                if(e==ItemElement.Ember) Line(g,new Shape().M(94,108).L(107,121).L(94,139).L(106,151).L(98,170).L(104,190),3,Color.FromArgb(255,190,95));
                if(e==ItemElement.Astral || e==ItemElement.Storm) for(int i=0;i<4;i++) Line(g,new Shape().M(90,110+i*18).L(106,116+i*18).L(96,122+i*18),1.7f,tone);
                g.Restore(clip);
            }
            using(var handle=new Shape().M(94,26).L(106,26).L(109,79).L(91,79).Close()) Paint(g,handle,tone,Color.FromArgb(35,27,72),Gold);
            for(int y=34;y<76;y+=9) Line(g,new Shape().M(94,y).L(106,y+5),2,Color.FromArgb(191,194,223));
            using(var guard=new Shape()) {
                if(e==ItemElement.Ember) guard.M(99,75).L(79,56).L(50,52).L(62,68).L(46,87).L(77,76).L(90,90).L(110,90).L(124,76).L(154,87).L(137,67).L(150,52).L(121,56).Close();
                else if(e==ItemElement.Frost || e==ItemElement.Storm) guard.M(100,75).L(66,61).L(44,71).L(59,77).L(52,102).L(76,82).L(92,95).L(108,95).L(124,82).L(147,102).L(142,77).L(157,71).L(134,61).Close();
                else if(e==ItemElement.Shadow) guard.M(96,76).L(64,62).L(47,74).L(68,80).L(58,103).L(83,87).L(93,101).L(111,89).L(144,77).L(132,56).L(118,75).Close();
                else guard.M(99,75).Q(71,77,46,64).L(49,83).L(72,81).L(86,93).L(114,93).L(128,81).L(151,83).L(154,64).Q(128,77,99,75).Close();
                Color guardLight=e==ItemElement.Ember?Color.FromArgb(164,55,49):e==ItemElement.Shadow?Color.FromArgb(119,71,174):e==ItemElement.Frost?Color.FromArgb(219,241,255):Gold;
                Color guardDark=e==ItemElement.Ember?Color.FromArgb(45,24,33):e==ItemElement.Shadow?Color.FromArgb(43,24,72):Color.FromArgb(97,76,110);
                Paint(g,guard,guardLight,guardDark,Gold);
            }
            Line(g,new Shape().M(53,70).Q(75,81,92,83).M(108,83).Q(128,76,147,70),1.6f,Gold);
            if(e==ItemElement.Ember || e==ItemElement.Shadow) {
                using(var fang=new Shape().M(80,72).L(78,92).L(87,83).L(93,94).L(97,79).Close())Paint(g,fang,tone,Ink,Gold);
                using(var fang=new Shape().M(120,72).L(123,92).L(113,83).L(107,94).L(103,79).Close())Paint(g,fang,tone,Ink,Gold);
            } else {
                using(var pen=new Pen(Gold,2))g.DrawEllipse(pen,88,74,24,24);
                Line(g,new Shape().M(85,88).Q(73,101,68,91).M(115,88).Q(127,101,132,91),2,Gold);
            }
            Gem(g,100,86,10,tone); Gem(g,100,20,9,tone);
            if(e==ItemElement.Shadow || e==ItemElement.Ember) for(int i=0;i<3;i++) {
                float y=114+i*31,offset=(float)Math.Sin(time*2+i)*5;
                Line(g,new Shape().M(129+offset,y).Q(142,y+10,132+offset,y+20),2.4f,Color.FromArgb(70+(int)(pulse*50),tone));
            }
            Spark(g,73,53,4,Color.FromArgb(230,Gold)); Spark(g,116,150,3,Color.FromArgb((int)(120+pulse*100),tone));
        }
        private static void Orb(Graphics g,ItemElement e,double time)
        {
            Color tone=Tone(e); float pulse=(float)(.5+.5*Math.Sin(time*2.7));
            Halo(g,100,119,174,174,tone,pulse);
            if(e==ItemElement.Frost) {
                for(int i=0;i<6;i++) {
                    var s=g.Save();g.TranslateTransform(100,120);g.RotateTransform(i*60);
                    using(var crystal=new Shape().M(-12,-12).L(-16,-46).L(0,-83).L(15,-40).L(8,-9).Close()) Paint(g,crystal,Color.FromArgb(243,255,255),Color.FromArgb(51,102,181),tone);
                    Line(g,new Shape().M(0,-12).L(0,-75).M(0,-39).L(-15,-46),1.6f,Color.White);g.Restore(s);
                }
                Gem(g,100,120,26,tone);return;
            }
            if(e==ItemElement.Ember) {
                using(var flame=new Shape().M(98,52).C(61,66,91,94,60,105).C(50,92,61,76,67,69).C(34,93,57,126,43,146)
                    .C(39,183,89,197,120,180).C(154,169,171,140,148,105).Q(151,135,133,141).C(140,105,111,97,124,65).Q(98,78,98,52).Close())
                    Paint(g,flame,Color.FromArgb(255,220,130),Color.FromArgb(211,39,25),tone);
                using(var core=new Shape().M(98,103).Q(73,123,80,152).Q(97,172,116,150).Q(93,154,105,128).Q(118,142,121,126).Q(113,109,98,103).Close()) Flat(g,core,Color.FromArgb(126,29,39));
                Line(g,new Shape().M(97,57).C(74,78,100,100,70,114).C(57,142,57,166,84,178),3,Color.FromArgb(255,239,164));
                Line(g,new Shape().M(99,116).Q(85,132,97,148).Q(108,152,109,140),2.2f,Color.FromArgb(255,159,53));
                Halo(g,108,106,82,76,Color.FromArgb(255,185,43),pulse);
                return;
            }
            using(var circle=new GraphicsPath()) {
                circle.AddEllipse(51,70,98,98);
                using(var fill=new PathGradientBrush(circle) {CenterPoint=new PointF(83,93),CenterColor=Color.FromArgb(201,207,255),SurroundColors=new[] {Color.FromArgb(37,30,93)}})g.FillPath(fill,circle);
                using(var pen=new Pen(tone,3))g.DrawPath(pen,circle);
            }
            using(var shine=new SolidBrush(Color.FromArgb(145,Color.White)))g.FillEllipse(shine,70,82,15,11);
            for(int i=0;i<9;i++) { float a=(float)(i*2.399+time*.12);float radius=14+i*3;Spark(g,100+(float)Math.Cos(a)*radius,120+(float)Math.Sin(a)*radius,2+i%2,tone); }
            var orbit=g.Save();g.TranslateTransform(100,120);g.RotateTransform((float)Math.Sin(time*.4)*5);
            if(e==ItemElement.Shadow) {
                using(var pen=new Pen(Color.FromArgb(86,52,147),9))g.DrawEllipse(pen,-59,-59,118,118);
                for(int i=0;i<12;i++) { var s=g.Save();g.RotateTransform(i*30);using(var spike=new Shape().M(-7,-55).L(0,-80).L(9,-60).Close()) Paint(g,spike,tone,Color.FromArgb(31,22,63),tone);g.Restore(s); }
            } else if(e==ItemElement.Solar) {
                using(var pen=new Pen(Gold,5))g.DrawEllipse(pen,-62,-62,124,124);
                using(var pen=new Pen(Color.FromArgb(255,243,193),2))g.DrawEllipse(pen,-71,-71,142,142);
                for(int i=0;i<8;i++) {var s=g.Save();g.RotateTransform(i*45);Spark(g,0,-65,16,Gold);g.Restore(s);}
                Gem(g,0,0,28,Color.FromArgb(116,179,255));
            } else if(e==ItemElement.Storm) {
                for(int i=0;i<6;i++) {var s=g.Save();g.RotateTransform(i*60);Line(g,new Shape().M(0,-72).L(-10,-49).L(6,-49).L(-4,-23),5,tone);g.Restore(s);}
                Gem(g,0,0,25,Color.FromArgb(237,243,255));
            } else {
                using(var pen=new Pen(Gold,3))g.DrawEllipse(pen,-70,-39,140,78);
                Line(g,new Shape().M(0,-62).L(20,-30).L(0,-11).L(-20,-30).Close(),2.3f,Gold);
                Spark(g,0,0,29,Color.FromArgb(235,222,255));Gem(g,0,-67,10,tone);Gem(g,0,65,10,tone);
            }
            g.Restore(orbit);
        }
        internal static void DrawEquipped(Graphics g,MonsterPose pose,SangmonEquipment equipment)
        {
            if(equipment==null)return;
            // Items sit beside the body, preserving eyes and the open leg arch in either facing direction.
            var weapon=SangmonItems.Find(equipment.WeaponId);var charm=SangmonItems.Find(equipment.CharmId);
            if(weapon!=null) {
                var save=g.Save();g.TranslateTransform(480,625);g.RotateTransform(-15+(float)Math.Sin(pose.Phase*2)*3);g.TranslateTransform(-480,-625);
                Draw(g,new RectangleF(415,450,190,300),weapon,pose.Phase);g.Restore(save);
            }
            if(charm!=null) Draw(g,new RectangleF(340,410+(float)Math.Sin(pose.Phase*2)*8,118,126),charm,pose.Phase);
        }
        private static void Paint(Graphics g,Shape path,Color light,Color dark,Color edge)
        {
            var bounds=path.Path.GetBounds();
            using(var fill=new LinearGradientBrush(bounds,light,dark,30)) {
                fill.InterpolationColors=new ColorBlend {Colors=new[]{dark,light,light,dark},Positions=new[]{0f,.32f,.45f,1f}};
                g.FillPath(fill,path.Path);
            }
            using(var pen=new Pen(Ink,2.7f){LineJoin=LineJoin.Round})g.DrawPath(pen,path.Path);
            using(var pen=new Pen(Color.FromArgb(145,edge),1)){g.DrawPath(pen,path.Path);}
        }
        private static void Flat(Graphics g,Shape path,Color color,bool outline=true)
        {using(var fill=new SolidBrush(color))g.FillPath(fill,path.Path);if(outline)using(var pen=new Pen(Ink,2))g.DrawPath(pen,path.Path);}
        private static void Line(Graphics g,Shape path,float width,Color color)
        {using(path)using(var pen=new Pen(color,width){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawPath(pen,path.Path);}
        private static void Gem(Graphics g,float x,float y,float radius,Color tone)
        {
            using(var gem=new Shape().M(x,y-radius).L(x+radius*.7f,y).L(x,y+radius).L(x-radius*.7f,y).Close())Paint(g,gem,Color.FromArgb(231,249,255),tone,tone);
            Line(g,new Shape().M(x,y-radius+2).L(x,y+radius-2).M(x-radius*.7f,y).L(x+radius*.7f,y),1,Color.FromArgb(215,Color.White));
        }
        private static void Spark(Graphics g,float x,float y,float size,Color color)
        {using(var star=new Shape().M(x,y-size).L(x+size*.22f,y-size*.22f).L(x+size,y).L(x+size*.22f,y+size*.22f).L(x,y+size).L(x-size*.22f,y+size*.22f).L(x-size,y).L(x-size*.22f,y-size*.22f).Close())Flat(g,star,color,false);}
    }
}
