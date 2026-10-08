using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MyDay.Windows.UI
{
    // Team design lives here; diary state and storage do not depend on these values.
    public static class Design
    {
        private static readonly float Dpi = GetScale();
        private static float GetScale() { using (var g = Graphics.FromHwnd(System.IntPtr.Zero)) return g.DpiX / 96f; }
        public static int P(int value) { return (int)System.Math.Round(value * Dpi); }
        public static int U(int pixels) { return (int)System.Math.Round(pixels / Dpi); }
        public static Point Point(int x, int y) { return new Point(P(x), P(y)); }
        public static Size Size(int width, int height) { return new Size(P(width), P(height)); }
        public static Padding Pad(int all) { return new Padding(P(all)); }
        public static Padding Pad(int l, int t, int r, int b) { return new Padding(P(l), P(t), P(r), P(b)); }
        public static readonly Color Ink = Color.FromArgb(35, 35, 48);
        public static readonly Color Muted = Color.FromArgb(115, 115, 132);
        public static readonly Color Accent = Color.FromArgb(112, 86, 232);
        public static readonly Color Soft = Color.FromArgb(235, 235, 243);
        public static readonly Color Tint = Color.FromArgb(242, 238, 255);
        public static readonly Color[] Backgrounds = { Color.FromArgb(249, 249, 252), Color.FromArgb(246, 249, 255), Color.FromArgb(249, 246, 255) };
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        public static Font Font(float size, bool bold = false)
        {
            string key = size.ToString(System.Globalization.CultureInfo.InvariantCulture) + (bold ? "b" : "r");
            Font font;
            if (!Fonts.TryGetValue(key, out font)) { font = new Font("맑은 고딕", size, bold ? FontStyle.Bold : FontStyle.Regular); Fonts.Add(key, font); }
            return font;
        }
        public static ChoiceButton Choice(int width) { return new ChoiceButton { Width = P(width) }; }
        public static Label Label(string text, float size, bool bold = false)
        {
            return new Label { Text = text, Font = Font(size, bold), ForeColor = Ink, AutoSize = true, BackColor = Color.Transparent };
        }
        public static Button Button(string text, bool primary = false)
        {
            var b = new RoundedButton { Text = text, AutoSize = false, Height = 36, Width = 106,
                Font = Font(9), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = primary ? Accent : Color.White, ForeColor = primary ? Color.White : Ink,
                Margin = new Padding(0, 0, 8, 0), UseVisualStyleBackColor = false };
            b.Height = P(36); b.Width = P(106); b.Margin = Pad(0, 0, 8, 0);
            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = Soft;
            return b;
        }
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            var d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }
    }

    public class RoundedButton : Button
    {
        private bool hover;
        public RoundedButton() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnMouseEnter(System.EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(System.EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var surface=Parent;
            while(surface!=null && surface.BackColor.A<255) surface=surface.Parent;
            e.Graphics.Clear(surface==null?Color.White:surface.BackColor);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=hover && Enabled ? (BackColor==Design.Accent ? Color.FromArgb(96,70,213) : Design.Tint) : BackColor;
            using(var path=Design.Rounded(new RectangleF(1,1,Width-3,Height-3),Design.P(10)))
            using(var brush=new SolidBrush(fill))
            using(var pen=new Pen(FlatAppearance.BorderColor)) {
                e.Graphics.FillPath(brush,path);
                if(FlatAppearance.BorderSize>0) e.Graphics.DrawPath(pen,path);
            }
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?ForeColor:Design.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-Design.P(5),-Design.P(5)));
        }
    }

    public sealed class ChoiceButton : RoundedButton
    {
        public readonly List<object> Items = new List<object>();
        private int selected = -1;
        public event System.EventHandler SelectedIndexChanged;
        public object SelectedItem { get { return selected < 0 ? null : Items[selected]; } }
        public int SelectedIndex
        {
            get { return selected; }
            set {
                if (value < 0 || value >= Items.Count) throw new System.ArgumentOutOfRangeException("value");
                bool changed = selected != value; selected = value; Text = Items[value] + "  ▾";
                if (changed && SelectedIndexChanged != null) SelectedIndexChanged(this, System.EventArgs.Empty);
            }
        }
        public ChoiceButton()
        {
            Height = Design.P(30); Font = Design.Font(9); FlatStyle = FlatStyle.Flat;
            BackColor = Color.White; ForeColor = Design.Ink; Cursor = Cursors.Hand;
            FlatAppearance.BorderColor = Design.Soft;
            Click += delegate {
                var menu = new ContextMenuStrip();
                for (int i = 0; i < Items.Count; i++) {
                    int index = i;
                    var item = new ToolStripMenuItem(Items[i].ToString()) { Checked = selected == i };
                    item.Click += delegate { SelectedIndex = index; }; menu.Items.Add(item);
                }
                menu.Closed += delegate { menu.Dispose(); }; menu.Show(this, new Point(0, Height));
            };
        }
    }

    public class CardPanel : Panel
    {
        public Color Fill = Color.White;
        public CardPanel() { DoubleBuffered = true; Padding = Design.Pad(22); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Design.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), Design.P(20)))
            using (var brush = new SolidBrush(Fill))
            using (var pen = new Pen(Design.Soft))
            { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path); }
            base.OnPaint(e);
        }
    }
}
