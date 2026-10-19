using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    // Native buttons retain Tab/Space access while showing the web design's three swatches.
    public sealed class ThemePicker : UserControl
    {
        private readonly ThemeOption[] options=new ThemeOption[3];
        private int selected=-1;
        public event EventHandler SelectedIndexChanged;
        public int SelectedIndex
        {
            get {return selected;}
            set {
                if(value<0 || value>=options.Length) throw new ArgumentOutOfRangeException("value");
                bool changed=selected!=value; selected=value;
                for(int i=0;i<options.Length;i++) {
                    options[i].Selected=i==value;
                    options[i].AccessibleDescription=i==value?"선택됨":"선택하려면 누르세요";
                    options[i].Invalidate();
                }
                if(changed && SelectedIndexChanged!=null) SelectedIndexChanged(this,EventArgs.Empty);
            }
        }
        public ThemePicker()
        {
            BackColor=Color.Transparent; Size=Design.Size(180,62);
            var names=new[]{"크림","숲","라벤더"};
            for(int i=0;i<options.Length;i++) {
                int index=i;
                options[i]=new ThemeOption {Text=names[i],Swatch=Design.Backgrounds[i],AccessibleName=names[i]+" 배경 선택",TabIndex=i};
                options[i].Click+=delegate {SelectedIndex=index;}; Controls.Add(options[i]);
            }
            Resize+=delegate {Arrange();}; Arrange();
        }
        private void Arrange()
        {
            int gap=Design.P(5),width=Math.Max(1,(ClientSize.Width-gap*2)/3);
            for(int i=0;i<options.Length;i++) if(options[i]!=null) options[i].Bounds=new Rectangle(i*(width+gap),0,width,ClientSize.Height);
        }
        internal void ChooseForTest(int index) {options[index].PerformClick();}
        private sealed class ThemeOption : Button
        {
            public bool Selected;
            public Color Swatch;
            public ThemeOption() {SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;Font=Design.Font(8);}
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(Design.Paper);
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var path=Design.Rounded(new RectangleF(1,1,Width-3,Height-3),Design.P(8)))
                using(var fill=new SolidBrush(Selected?Design.Tint:Design.Paper))
                using(var pen=new Pen(Selected?Design.Olive:Design.Soft)) {e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(pen,path);}
                int diameter=Design.P(18);
                using(var fill=new SolidBrush(Swatch)) using(var pen=new Pen(Design.Soft)) {
                    e.Graphics.FillEllipse(fill,(Width-diameter)/2,Design.P(10),diameter,diameter);
                    e.Graphics.DrawEllipse(pen,(Width-diameter)/2,Design.P(10),diameter,diameter);
                }
                TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(0,Design.P(33),Width,Design.P(22)),Design.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-Design.P(4),-Design.P(4)));
            }
        }
    }
}
