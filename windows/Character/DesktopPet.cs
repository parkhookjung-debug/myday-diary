using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MyDay.Windows.Character
{
    public sealed class DesktopPet : Form
    {
        private readonly Timer timer = new Timer { Interval = 33 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Action openDiary;
        private bool dragging, moved, roaming = true;
        private Point mouseStart, windowStart;
        private float x, y, vx = -1.1f, vy = .25f;
        private double happyUntil, lastFrame;
        public bool Roaming { get { return roaming; } }
        public event EventHandler PetHidden;
        public DesktopPet(Action openDiary)
        {
            this.openDiary = openDiary;
            Text = "MyDay 불꽃 몬스터"; FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true; Size = new Size(210, 180);
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
            AccessibleName = "바탕화면 불꽃 몬스터. 클릭하면 불을 뿜고 끌어서 이동합니다.";
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 42, area.Bottom - Height - 30);
            x = Left; y = Top;
            var menu = new ContextMenuStrip();
            menu.Items.Add("일기 열기", null, delegate { openDiary(); });
            var roam = new ToolStripMenuItem("자유롭게 움직이기") { Checked = true, CheckOnClick = true };
            roam.CheckedChanged += delegate { roaming = roam.Checked; }; menu.Items.Add(roam);
            var top = new ToolStripMenuItem("다른 창 위에 표시") { Checked = true, CheckOnClick = true };
            top.CheckedChanged += delegate { TopMost = top.Checked; }; menu.Items.Add(top);
            menu.Items.Add("불 뿜기", null, delegate { Fire(); });
            menu.Items.Add("캐릭터 숨기기", null, delegate { Hide(); if (PetHidden != null) PetHidden(this, EventArgs.Empty); });
            ContextMenuStrip = menu;
            timer.Tick += delegate { Advance(); };
            VisibleChanged += delegate { if (Visible) { lastFrame = clock.Elapsed.TotalSeconds; timer.Start(); RenderFrame(); } else timer.Stop(); };
            MouseDown += OnPetDown; MouseMove += OnPetMove; MouseUp += OnPetUp;
            MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) openDiary(); };
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ExStyle |= 0x80000 | 0x80 | 0x8000000; return value; }
        }
        public void Fire() { happyUntil = clock.Elapsed.TotalSeconds + 1.8; if (Visible) RenderFrame(); }
        private void OnPetDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            dragging = true; moved = false; mouseStart = Cursor.Position; windowStart = Location; Capture = true;
        }
        private void OnPetMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point now = Cursor.Position;
            if (Math.Abs(now.X - mouseStart.X) + Math.Abs(now.Y - mouseStart.Y) > 5) moved = true;
            if (!moved) return;
            x = windowStart.X + now.X - mouseStart.X; y = windowStart.Y + now.Y - mouseStart.Y;
            Location = new Point((int)x, (int)y); RenderFrame();
        }
        private void OnPetUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !dragging) return;
            dragging = false; Capture = false; ClampToScreen(); if (!moved) Fire();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture) dragging = false;
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
            float delta = (float)Math.Min(.1, now - lastFrame) * 30; lastFrame = now;
            if (roaming && !dragging && now >= happyUntil && !ContextMenuStrip.Visible)
            {
                var area = Screen.FromPoint(new Point(Left + Width / 2, Top + Height / 2)).WorkingArea;
                x += vx * delta; y += vy * delta;
                if (x <= area.Left || x >= area.Right - Width) vx = -vx;
                if (y <= area.Top || y >= area.Bottom - Height) vy = -vy;
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
                MonsterPainter.Draw(g, new Rectangle(6, 8, Width - 12, Height - 16), MonsterPose.At(seconds, roaming, dragging && moved, happy), vx < 0);
            }
            return bitmap;
        }
        private void RenderFrame()
        {
            if (!IsHandleCreated) return;
            using (var bitmap = MakeFrame(clock.Elapsed.TotalSeconds, clock.Elapsed.TotalSeconds < happyUntil)) LayeredWindow.Update(Handle, bitmap, Location);
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
