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
    public sealed class DesktopPet : Form
    {
        private readonly Timer timer = new Timer { Interval = 33 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Action openDiary;
        private bool roaming = true;
        private readonly PetGesture gesture = new PetGesture();
        private readonly PetBehavior behavior = new PetBehavior(Environment.TickCount);
        private Point windowStart;
        private float x, y;
        private double lastFrame;
        private MonsterVariant variant;
        private readonly ToolStripMenuItem[] variantItems=new ToolStripMenuItem[MonsterVariants.All.Length];
        public event EventHandler VariantChanged;
        public MonsterVariant Variant {
            get { return variant; }
            set {
                if(variant==value) return;
                variant=value;
                foreach(var item in MonsterVariants.All) variantItems[(int)item].Checked=item==value;
                if(Visible) RenderFrame();
                if(VariantChanged!=null) VariantChanged(this,EventArgs.Empty);
            }
        }
        public bool Roaming { get { return roaming; } }
        public event EventHandler PetHidden;
        public DesktopPet(Action openDiary)
        {
            this.openDiary = openDiary;
            Text = "MyDay 상몬"; FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true; Size = new Size(240, 220);
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            AccessibleName = "바탕화면 상몬. 한 번 클릭하면 일기를 열고 끌어서 이동합니다.";
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 42, area.Bottom - Height - 30);
            x = Left; y = Top;
            var menu = new ContextMenuStrip();
            menu.Items.Add("일기 열기", null, delegate { openDiary(); });
            var versions=new ToolStripMenuItem("캐릭터 버전");
            foreach(var item in MonsterVariants.All) {
                var choice=item; var option=new ToolStripMenuItem(MonsterVariants.Name(item)) { Checked=item==variant };
                option.Click+=delegate { Variant=choice; }; variantItems[(int)item]=option; versions.DropDownItems.Add(option);
            }
            menu.Items.Add(versions);
            var games=new ToolStripMenuItem("게임 스타일");
            foreach(var item in MonsterVariants.All) if(GameSkins.IsGame(item)) {
                var choice=item; var option=new ToolStripMenuItem(MonsterVariants.Name(item)) { Checked=item==variant };
                option.Click+=delegate { Variant=choice; }; games.DropDownItems.Add(option);
                VariantChanged+=delegate { option.Checked=variant==choice; };
            }
            menu.Items.Add(games);
            var roam = new ToolStripMenuItem("자유롭게 움직이기") { Checked = true, CheckOnClick = true };
            roam.CheckedChanged += delegate { roaming = roam.Checked; }; menu.Items.Add(roam);
            var top = new ToolStripMenuItem("다른 창 위에 표시") { Checked = true, CheckOnClick = true };
            top.CheckedChanged += delegate { TopMost = top.Checked; }; menu.Items.Add(top);
            menu.Items.Add("불 뿜기", null, delegate { Fire(); });
            menu.Items.Add("통통 뛰기", null, delegate { behavior.Hop(); });
            menu.Items.Add("캐릭터 숨기기", null, delegate { Hide(); if (PetHidden != null) PetHidden(this, EventArgs.Empty); });
            ContextMenuStrip = menu;
            timer.Tick += delegate { Advance(); };
            VisibleChanged += delegate { if (Visible) { lastFrame = clock.Elapsed.TotalSeconds; timer.Start(); RenderFrame(); } else timer.Stop(); };
            MouseDown += OnPetDown; MouseMove += OnPetMove; MouseUp += OnPetUp;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ExStyle |= 0x80000 | 0x80 | 0x8000000; return value; }
        }
        public void Fire() { behavior.Fire(); behavior.Step(0,roaming,false,false); if (Visible) RenderFrame(); }
        private void OnPetDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            gesture.Press(Cursor.Position); windowStart = Location; Capture = true;
        }
        private void OnPetMove(object sender, MouseEventArgs e)
        {
            if (!gesture.Active) return;
            gesture.Move(Cursor.Position);
            if (!gesture.Moved) return;
            x = windowStart.X + gesture.Offset.X; y = windowStart.Y + gesture.Offset.Y;
            Location = new Point((int)x, (int)y); RenderFrame();
        }
        private void OnPetUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !gesture.Active) return;
            bool click = gesture.Release(Cursor.Position);
            Capture = false; ClampToScreen();
            if (click) { Fire(); openDiary(); }
            else { behavior.Land(); Fire(); }
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) gesture.Cancel();
        }
        private void ClampToScreen()
        {
            Rectangle area = Screen.FromPoint(new Point((int)x + Width / 2, (int)y + Height / 2)).WorkingArea;
            x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));
            Location = new Point((int)x, (int)y);
        }
        private void Advance()
        {
            double now = clock.Elapsed.TotalSeconds;
            double delta = Math.Min(.1, now - lastFrame); lastFrame = now;
            var proximity = new Rectangle(Left-16,Top-16,Width+32,Height+32);
            bool hover=proximity.Contains(Cursor.Position) || ContextMenuStrip.Visible;
            var motion=behavior.Step(delta,roaming,gesture.Active,hover);
            float speed=MonsterVariants.SpeedFactor(variant);
            motion=new PointF(motion.X*speed,motion.Y*speed);
            if (!gesture.Active)
            {
                var area = Screen.FromPoint(new Point(Left + Width / 2, Top + Height / 2)).WorkingArea;
                x += motion.X; y += motion.Y;
                behavior.Bounce(x<=area.Left && motion.X<0 || x>=area.Right-Width && motion.X>0,
                    y<=area.Top && motion.Y<0 || y>=area.Bottom-Height && motion.Y>0);
                ClampToScreen();
            }
            RenderFrame();
        }
        public Bitmap MakeFrame(double seconds, bool happy)
        {
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                Point pointer=Cursor.Position;
                float lookX=(pointer.X-(Left+Width/2f))/Width*(behavior.FacingLeft?1:-1);
                float lookY=(pointer.Y-(Top+Height/2f))/Height;
                PetActivity activity=gesture.Active && gesture.Moved?PetActivity.Drag:happy?PetActivity.Fire:behavior.Activity;
                double age=activity==behavior.Activity?behavior.Age:Math.Min(seconds,.9);
                MonsterPainter.Draw(g, new Rectangle(16, 22, Width - 32, Height - 44), MonsterPose.ForActivity(seconds,activity,age,lookX,lookY), behavior.FacingLeft,variant);
            }
            return bitmap;
        }
        private void RenderFrame()
        {
            if (!IsHandleCreated) return;
            using (var bitmap = MakeFrame(clock.Elapsed.TotalSeconds, behavior.Activity==PetActivity.Fire)) LayeredWindow.Update(Handle, bitmap, Location);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); if (ContextMenuStrip != null) ContextMenuStrip.Dispose(); }
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
