using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MyDay.Windows.Character
{
    // A rounded relief rendered from our native silhouette, not a keyed concept image.
    // Only Original uses this material. Its small, bounded cache is shared by every view.
    internal static class ClayMaterial
    {
        private const int Width=448, Height=384, Left=96, Top=416;
        private static readonly Dictionary<int,Bitmap> Bodies=new Dictionary<int,Bitmap>();
        private static readonly object Gate=new object();

        public static float Step(float toe) { return Math.Max(-3,Math.Min(3,(int)Math.Round(toe/3)))*3; }

        public static void Body(Graphics g,GraphicsPath outline,float toe)
        {
            int key=(int)Step(toe);
            lock(Gate) {
                Bitmap image;
                if(!Bodies.TryGetValue(key,out image)) { image=Render(outline); Bodies.Add(key,image); }
                var saved=g.Save();
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.DrawImage(image,new Rectangle(Left,Top,Width,Height),0,0,Width,Height,GraphicsUnit.Pixel);
                g.Restore(saved);
            }
        }

        private static Bitmap Render(GraphicsPath outline)
        {
            int length=Width*Height;
            byte[] coverage=new byte[length];
            using(var mask=new Bitmap(Width,Height,PixelFormat.Format32bppArgb)) {
                using(var g=Graphics.FromImage(mask)) {
                    g.SmoothingMode=SmoothingMode.AntiAlias; g.TranslateTransform(-Left,-Top);
                    using(var white=new SolidBrush(Color.White)) g.FillPath(white,outline);
                }
                var data=mask.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                try {
                    byte[] bytes=new byte[data.Stride*Height]; Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
                    for(int y=0;y<Height;y++) for(int x=0;x<Width;x++) coverage[y*Width+x]=bytes[y*data.Stride+x*4+3];
                } finally {mask.UnlockBits(data);}
            }
            // Distance to the silhouette makes the same soft bevel around the snout,
            // eye bumps and stubby feet. No background color enters the alpha channel.
            float[] distance=new float[length];
            for(int i=0;i<length;i++) distance[i]=coverage[i]<128?0:10000;
            for(int y=1;y<Height-1;y++) for(int x=1;x<Width-1;x++) {
                int i=y*Width+x;
                distance[i]=Math.Min(distance[i],Math.Min(Math.Min(distance[i-1]+1,distance[i-Width]+1),Math.Min(distance[i-Width-1]+1.4142f,distance[i-Width+1]+1.4142f)));
            }
            for(int y=Height-2;y>0;y--) for(int x=Width-2;x>0;x--) {
                int i=y*Width+x;
                distance[i]=Math.Min(distance[i],Math.Min(Math.Min(distance[i+1]+1,distance[i+Width]+1),Math.Min(distance[i+Width+1]+1.4142f,distance[i+Width-1]+1.4142f)));
            }
            float[] relief=new float[length];
            for(int i=0;i<length;i++) relief[i]=(float)(52*Math.Sqrt(1-Math.Exp(-distance[i]/42)));
            // Smooth normals at the medial axis instead of drawing sharp extruded ridges.
            relief=Smooth(relief);
            byte[] pixels=new byte[length*4];
            for(int y=1;y<Height-1;y++) for(int x=1;x<Width-1;x++) {
                int i=y*Width+x; if(coverage[i]==0) continue;
                double nx=(relief[i-1]-relief[i+1])*.65, ny=(relief[i-Width]-relief[i+Width])*.65;
                double normal=Math.Sqrt(nx*nx+ny*ny+1);
                double diffuse=Math.Max(0,(-nx*.43-ny*.55+.72)/normal);
                double softHighlight=Math.Pow(Math.Max(0,(-nx*.23-ny*.30+.925)/normal),18)*.055;
                double light=.72+.28*diffuse+softHighlight;
                // Warm clay in shadow; almost white under the upper-left softbox.
                pixels[i*4]=Channel(232*light);
                pixels[i*4+1]=Channel(244*light);
                pixels[i*4+2]=Channel(255*light);
                pixels[i*4+3]=coverage[i];
            }
            var result=new Bitmap(Width,Height,PixelFormat.Format32bppArgb);
            var target=result.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try {Marshal.Copy(pixels,0,target.Scan0,pixels.Length);} finally {result.UnlockBits(target);}
            return result;
        }
        private static byte Channel(double value) {return (byte)Math.Max(0,Math.Min(255,(int)Math.Round(value)));}

        private static float[] Smooth(float[] relief)
        {
            const int radius=22;
            float[] kernel=new float[radius*2+1]; float total=0;
            for(int k=-radius;k<=radius;k++) {kernel[k+radius]=(float)Math.Exp(-k*k/200.0); total+=kernel[k+radius];}
            for(int k=0;k<kernel.Length;k++) kernel[k]/=total;
            float[] across=new float[relief.Length],result=new float[relief.Length];
            for(int y=0;y<Height;y++) for(int x=0;x<Width;x++) {
                float sum=0;
                for(int k=-radius;k<=radius;k++) sum+=relief[y*Width+Math.Max(0,Math.Min(Width-1,x+k))]*kernel[k+radius];
                across[y*Width+x]=sum;
            }
            for(int y=0;y<Height;y++) for(int x=0;x<Width;x++) {
                float sum=0;
                for(int k=-radius;k<=radius;k++) sum+=across[Math.Max(0,Math.Min(Height-1,y+k))*Width+x]*kernel[k+radius];
                result[y*Width+x]=sum;
            }
            return result;
        }

        public static void Face(Graphics g,MonsterPose pose)
        {
            // A single open line, including the small tongue. Never fill an oval snout
            // or create an extra yawn mouth: the reference's side lip stays unchanged.
            using(var lip=new MonsterPainter.Shape().M(134,631)
                .C(177,611,237,595,269,605).C(295,613,302,627,274,635)
                .C(280,654,273,663,263,663).C(254,664,250,652,247,646).L(230,650)) {
                using(var soft=new Pen(Color.FromArgb(24,76,60,48),9) {StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}) g.DrawPath(soft,lip.Path);
                using(var pen=new Pen(Color.FromArgb(215,76,66,54),4.3f) {StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}) g.DrawPath(pen,lip.Path);
            }
            if(pose.Closed) {
                using(var eyes=new MonsterPainter.Shape().M(199,495).Q(208,pose.Happy?484:501,218,495).M(291,481).Q(300,pose.Happy?470:487,310,481))
                using(var pen=new Pen(Color.FromArgb(40,37,32),5) {StartCap=LineCap.Round,EndCap=LineCap.Round}) g.DrawPath(pen,eyes.Path);
            } else {
                using(var fill=new SolidBrush(Color.FromArgb(32,30,26)))
                foreach(var eye in new[] {new PointF(208,495),new PointF(300,481)})
                    g.FillEllipse(fill,eye.X-9.5f+pose.LookX*5,eye.Y-9.5f*pose.EyeScale+pose.LookY*4,19,19*pose.EyeScale);
            }
        }

        public static void Flame(Graphics g)
        {
            // A small round puff with a tapered connection to the existing side lip.
            using(var puff=new MonsterPainter.Shape().M(135,631).C(114,629,108,608,91,603)
                .C(78,594,68,598,65,605).C(38,593,22,611,26,630)
                .C(23,652,48,665,67,654).C(86,656,100,637,115,636).Q(128,635,135,631).Close())
            using(var light=new PathGradientBrush(puff.Path) {CenterPoint=new PointF(59,628),CenterColor=Color.FromArgb(255,254,181),SurroundColors=new[] {Color.FromArgb(255,156,48)}}) g.FillPath(light,puff.Path);
        }
    }
}
