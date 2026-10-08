using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Character;

namespace MyDay.Windows.UI
{
    public sealed class MonsterView : Control
    {
        private readonly Timer timer = new Timer { Interval = 50 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double happyUntil;
        public event EventHandler Fired;
        public MonsterView()
        {
            DoubleBuffered = true; BackColor = Color.White; Cursor = Cursors.Hand;
            AccessibleName = "불꽃 몬스터. 클릭하면 불을 뿜습니다.";
            timer.Tick += delegate { Invalidate(); };
            VisibleChanged += delegate { if (Visible) timer.Start(); else timer.Stop(); };
            HandleCreated += delegate { if (Visible) timer.Start(); };
            Click += delegate { Fire(); if (Fired != null) Fired(this, EventArgs.Empty); };
        }
        public void Fire() { happyUntil = clock.Elapsed.TotalSeconds + 1.8; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            MonsterPainter.Draw(e.Graphics, ClientRectangle, MonsterPose.At(clock.Elapsed.TotalSeconds, false, false, clock.Elapsed.TotalSeconds < happyUntil), true);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
