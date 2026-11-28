using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.Character
{
    public sealed partial class DesktopPet : Form
    {
        private readonly Timer timer = new Timer { Interval = 33 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Action openDiary;
        private bool roaming = true;
        private readonly PetGesture gesture = new PetGesture();
        private readonly PetBehavior behavior = new PetBehavior(Environment.TickCount);
        private Point windowStart;
        private readonly PetDynamics dynamics=new PetDynamics();
        private readonly RectangleF artworkBounds;
        internal PetDynamics Dynamics {get{return dynamics;}}
        internal RectangleF ArtworkBounds {get{return artworkBounds;}}
        private static Rectangle RenderArea(Size size){return new Rectangle((size.Width-208)/2,(size.Height-176)/2,208,176);}
        private static RectangleF MeasureArtwork(Size size){
            using(var image=new Bitmap(size.Width,size.Height)){
                using(var g=Graphics.FromImage(image))MonsterPainter.Draw(g,RenderArea(size),MonsterPose.ForActivity(0,PetActivity.Rest,0,0,0),true);
                int left=size.Width,top=size.Height,right=0,bottom=0;
                for(int y=0;y<size.Height;y++)for(int x=0;x<size.Width;x++)if(image.GetPixel(x,y).A>32){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x+1);bottom=Math.Max(bottom,y+1);}
                return RectangleF.FromLTRB(left,top,right,bottom);
            }
        }
        private RectangleF OriginBounds(Rectangle area){return new RectangleF(area.Left-artworkBounds.Left,area.Top-artworkBounds.Top,Math.Max(0,area.Width-artworkBounds.Width),Math.Max(0,area.Height-artworkBounds.Height));}
        private void SyncPosition(){Location=new Point((int)Math.Round(dynamics.Position.X),(int)Math.Round(dynamics.Position.Y));}
        private double lastFrame;
        private MonsterVariant variant;
        private SangmonEquipment equipment;
        public SangmonEquipment Equipment {get{return SangmonItems.Copy(equipment);}set{equipment=SangmonItems.Copy(value);if(Visible)RenderFrame();}}
        public MonsterVariant Variant {get{return MonsterVariant.Original;}set{variant=MonsterVariant.Original;if(Visible)RenderFrame();}}
        public bool Roaming { get { return roaming; } }
        public event EventHandler PetHidden;
        public DesktopPet(Action openDiary,Action openGrowth=null,Action openGame=null)
        {
            this.openDiary = openDiary;
            Text = "MyDay 상몬"; FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true; Size = new Size(420, 440);
            artworkBounds=MeasureArtwork(Size);
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            AccessibleName = "바탕화면 상몬. 클릭하면 일기를 열고, 끌면 늘어나며 빠르게 놓으면 화면 가장자리에서 튕깁니다.";
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - (int)artworkBounds.Right - 42, area.Bottom - (int)artworkBounds.Bottom - 30);
            dynamics.SetBounds(OriginBounds(area));dynamics.Place(Location);dynamics.Clamp();SyncPosition();
            var menu = new ContextMenuStrip();
            menu.Items.Add("일기 열기", null, delegate { openDiary(); });
            if(openGrowth!=null) menu.Items.Add("상몬 성장",null,delegate { openGrowth(); });
            if(openGame!=null)menu.Items.Add("상몬 게임 · 플레이룸",null,delegate {openGame();});
            var roam = new ToolStripMenuItem("자유롭게 움직이기") { Checked = true, CheckOnClick = true };
            roam.CheckedChanged += delegate { roaming = roam.Checked; }; menu.Items.Add(roam);
            var top = new ToolStripMenuItem("다른 창 위에 표시") { Checked = true, CheckOnClick = true };
            top.CheckedChanged += delegate { TopMost = top.Checked; if(toy!=null)toy.TopMost=TopMost; }; menu.Items.Add(top);
            menu.Items.Add("불 뿜기", null, delegate { Fire(); });
            menu.Items.Add("통통 뛰기", null, delegate { behavior.Hop(); });
            menu.Items.Add("공놀이 시작 / 공 다시 꺼내기",null,delegate{StartFetch();});
            menu.Items.Add("공놀이 끝내기",null,delegate{StopFetch();});
            menu.Items.Add("캐릭터 숨기기", null, delegate { Hide(); if (PetHidden != null) PetHidden(this, EventArgs.Empty); });
            ContextMenuStrip = menu;
            timer.Tick += delegate { Advance(); };
            VisibleChanged += delegate { if (Visible) { lastFrame = clock.Elapsed.TotalSeconds; timer.Start(); RenderFrame(); } else {timer.Stop();StopFetch();} };
            MouseDown += OnPetDown; MouseMove += OnPetMove; MouseUp += OnPetUp;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ExStyle |= 0x80000 | 0x80 | 0x8000000; return value; }
        }
        public void Fire() { behavior.Fire(); behavior.Step(0,roaming,false,false); if (Visible) RenderFrame(); }
        public void Celebrate() { behavior.Hop(); if(Visible) RenderFrame(); }
        private void OnPetDown(object sender, MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;
            BeginPointer(Cursor.Position,clock.Elapsed.TotalSeconds);Capture=true;
        }
        private void OnPetMove(object sender, MouseEventArgs e)
        {
            if(gesture.Active)MovePointer(Cursor.Position,clock.Elapsed.TotalSeconds);
        }
        private void OnPetUp(object sender, MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left||!gesture.Active)return;
            EndPointer(Cursor.Position,clock.Elapsed.TotalSeconds);Capture=false;
        }
        internal void BeginPointer(Point point,double time)
        {
            StopFetch();gesture.Press(point);windowStart=Location;dynamics.Place(Location);dynamics.BeginDrag(time);
            behavior.Step(0,roaming,true,false);RenderFrame();
        }
        internal void MovePointer(Point point,double time)
        {
            if(!gesture.Active)return;gesture.Move(point);
            if(gesture.Moved)dynamics.Drag(new PointF(windowStart.X+gesture.Offset.X,windowStart.Y+gesture.Offset.Y),time);
        }
        internal void EndPointer(Point point,double time)
        {
            if(!gesture.Active)return;
            bool click=gesture.Release(point);
            if(click)dynamics.CancelDrag();
            else{
                dynamics.Drag(new PointF(windowStart.X+gesture.Offset.X,windowStart.Y+gesture.Offset.Y),time);
                dynamics.SetBounds(OriginBounds(Screen.FromPoint(point).WorkingArea));
                if(!dynamics.Release(time,true))behavior.Land();
            }
            SyncPosition();RenderFrame();
            if(click){Fire();openDiary();}
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            // A normal mouse-up has already released the gesture. Capture loss never throws.
            if(!Capture&&gesture.Active)CancelPointer();
        }
        internal void CancelPointer(){gesture.Cancel();dynamics.CancelDrag();SyncPosition();RenderFrame();}
        internal void StepMotion(double delta,bool hover)
        {
            var bodyPoint=new Point((int)(dynamics.Position.X+artworkBounds.Left+artworkBounds.Width/2),(int)(dynamics.Position.Y+artworkBounds.Top+artworkBounds.Height/2));
            var activeArea=Screen.FromPoint(bodyPoint).WorkingArea;
            dynamics.SetBounds(OriginBounds(activeArea));
            bool fetching=FetchPlaying;var fetchMotion=StepFetch(delta,activeArea);
            bool wasFlying=dynamics.Flying;
            var motion=behavior.Step(delta,roaming&&!wasFlying&&!fetching,gesture.Active,hover&&!wasFlying&&!fetching);
            if(!gesture.Active&&!wasFlying){
                if(fetching)motion=fetchMotion;
                var p=dynamics.Position;var bounds=dynamics.Bounds;
                behavior.Bounce(p.X+motion.X<=bounds.Left&&motion.X<0||p.X+motion.X>=bounds.Right&&motion.X>0,p.Y+motion.Y<=bounds.Top&&motion.Y<0||p.Y+motion.Y>=bounds.Bottom&&motion.Y>0);
                dynamics.Translate(motion);
            }
            dynamics.Step(delta);
            if(wasFlying&&!dynamics.Flying)behavior.Land();
            SyncPosition();if(toy!=null)toy.RefreshFrame();
        }
        private void Advance()
        {
            double now=clock.Elapsed.TotalSeconds,delta=Math.Min(.1,now-lastFrame);lastFrame=now;
            var proximity=Rectangle.Ceiling(artworkBounds);proximity.Offset(Location);proximity.Inflate(16,16);
            StepMotion(delta,proximity.Contains(Cursor.Position)||ContextMenuStrip.Visible);RenderFrame();
        }
        public Bitmap MakeFrame(double seconds,bool happy){return MakeFrameCore(seconds,happy,Cursor.Position);}
        internal Bitmap MakeFrameCore(double seconds,bool happy,Point pointer)
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                float lookX=(pointer.X-(Left+Width/2f))/Width*(behavior.FacingLeft?1:-1);
                float lookY=(pointer.Y-(Top+Height/2f))/Height;
                PetActivity activity=gesture.Active && gesture.Moved?PetActivity.Drag:happy?PetActivity.Fire:behavior.Activity;
                double age=activity==behavior.Activity?behavior.Age:Math.Min(seconds,.9);
                if(FetchPlaying&&fetch.Moving&&activity!=PetActivity.Fire)activity=PetActivity.Walk;
                var pose=MonsterPose.ForActivity(seconds,activity,age,lookX,lookY);
                if(dynamics.Flying){pose.Closed=false;pose.Sleeping=false;pose.Bob=0;pose.EyeScale=1.12f;}
                pose.WidthScale*=dynamics.ScaleX;pose.BodyScale*=dynamics.ScaleY;pose.Tilt+=dynamics.Tilt;
                bool facing=FetchPlaying?fetch.FacingLeft:dynamics.Flying&&Math.Abs(dynamics.Velocity.X)>30?dynamics.Velocity.X<0:behavior.FacingLeft;
                MonsterPainter.Draw(g,RenderArea(Size),pose,facing,variant,equipment);
            }
            return bitmap;
        }
        private void RenderFrame()
        {
            if (!IsHandleCreated || !Visible) return;
            using (var bitmap = MakeFrame(clock.Elapsed.TotalSeconds, behavior.Activity==PetActivity.Fire)) LayeredWindow.Update(Handle, bitmap, Location);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { StopFetch();timer.Dispose(); if (ContextMenuStrip != null) ContextMenuStrip.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal static class LayeredWindow
    {
        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; public Point(int x,int y) { X=x; Y=y; } }
        [StructLayout(LayoutKind.Sequential)] private struct Size { public int X, Y; public Size(int x,int y) { X=x; Y=y; } }
        [StructLayout(LayoutKind.Sequential, Pack=1)] private struct Blend { public byte Operation, Flags, Alpha, Format; }
        [DllImport("user32.dll", SetLastError=true)] private static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dst, ref Point p, ref Size size, IntPtr src, ref Point source, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        public static void Update(IntPtr handle, Bitmap bitmap, System.Drawing.Point location)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, hbitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen); hbitmap = bitmap.GetHbitmap(Color.FromArgb(0)); previous = SelectObject(memory, hbitmap);
                var point = new Point(location.X, location.Y); var size = new Size(bitmap.Width, bitmap.Height); var source = new Point(0,0);
                var blend = new Blend { Alpha=255, Format=1 };
                if (!UpdateLayeredWindow(handle, screen, ref point, ref size, memory, ref source, 0, ref blend, 2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (hbitmap != IntPtr.Zero) DeleteObject(hbitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
