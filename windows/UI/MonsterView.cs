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
        private double happyUntil, happyStarted;
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
        public void Fire() { happyStarted=clock.Elapsed.TotalSeconds; happyUntil=happyStarted + PetBehavior.TouchFireSeconds; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            double now=clock.Elapsed.TotalSeconds;
            var activity=now<happyUntil?PetActivity.Fire:PetActivity.Rest;
            MonsterPainter.Draw(e.Graphics, ClientRectangle, MonsterPose.ForActivity(now,activity,now<happyUntil?now-happyStarted:now,0,0), true);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
