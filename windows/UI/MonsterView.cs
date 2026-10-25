using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class MonsterView : Control
    {
        private readonly Timer timer = new Timer { Interval = 50 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double happyUntil, happyStarted;
        private double celebrateUntil,celebrateStarted;
        public event EventHandler Fired;
        public bool ShowScene;
        public bool Sleepy;
        private MonsterVariant variant;
        private SangmonEquipment equipment;
        public SangmonEquipment Equipment {get{return SangmonItems.Copy(equipment);}set{equipment=SangmonItems.Copy(value);Invalidate();}}
        public MonsterVariant Variant { get { return variant; } set { variant=value; Invalidate(); } }
        public MonsterView()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor,true);
            DoubleBuffered = true; BackColor = Color.White; Cursor = Cursors.Hand;
            AccessibleName = "불꽃 몬스터. 클릭하면 불을 뿜습니다.";
            timer.Tick += delegate { Invalidate(); };
            VisibleChanged += delegate { if (Visible) timer.Start(); else timer.Stop(); };
            HandleCreated += delegate { if (Visible) timer.Start(); };
            Click += delegate { Fire(); if (Fired != null) Fired(this, EventArgs.Empty); };
        }
        public void Fire() { happyStarted=clock.Elapsed.TotalSeconds; happyUntil=happyStarted + PetBehavior.TouchFireSeconds; Invalidate(); }
        public void Celebrate() {celebrateStarted=clock.Elapsed.TotalSeconds;celebrateUntil=celebrateStarted+1.6;Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(ShowScene) {
                e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int diameter=Math.Min(Width-Design.P(30),Height-Design.P(12));
                using(var halo=new SolidBrush(Design.FriendHalo)) e.Graphics.FillEllipse(halo,(Width-diameter)/2,Design.P(2),diameter,diameter);
                using(var shadow=new SolidBrush(Color.FromArgb(26,166,159,117))) e.Graphics.FillEllipse(shadow,Width/2-Design.P(31),Height-Design.P(16),Design.P(62),Design.P(9));
                TextRenderer.DrawText(e.Graphics,"✦",Design.Font(15),Design.Point(5,20),Design.Progress);
                TextRenderer.DrawText(e.Graphics,"✧",Design.Font(15),new Point(Width-Design.P(22),Height-Design.P(38)),Design.Progress);
            }
            double now=clock.Elapsed.TotalSeconds;
            var activity=now<happyUntil?PetActivity.Fire:Sleepy?PetActivity.Sleep:now<celebrateUntil?PetActivity.Hop:PetActivity.Rest;
            MonsterPainter.Draw(e.Graphics, ClientRectangle, MonsterPose.ForActivity(now,activity,now<happyUntil?now-happyStarted:now<celebrateUntil?now-celebrateStarted:now,0,0), true,variant,equipment);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }
}
