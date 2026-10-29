using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace MyDay.Windows.UI {
    // Native adaptation of ceed3927 MoodStickers.swift. Existing mood IDs are unchanged.
    internal sealed class MoodStrip : FlowLayoutPanel {
        private readonly FaceButton[] buttons=new FaceButton[5];
        private readonly string[] moods={"행복해요","설레요","평온해요","피곤해요","속상해요"};
        private string selected="평온해요";
        public event Action<string> Changed;
        public string SelectedMood {get{return selected;}set{selected=value;foreach(var button in buttons)if(button!=null){button.Chosen=button.AccessibleName==value;button.Invalidate();}}}
        public MoodStrip(){
            AutoSize=false;Size=Design.Size(224,38);Margin=Design.Pad(0);WrapContents=false;BackColor=Color.Transparent;
            AccessibleName="오늘의 마음 선택";
            Color[] colors={Color.FromArgb(154,174,105),Color.FromArgb(194,210,151),Color.FromArgb(255,217,139),Color.FromArgb(242,157,107),Color.FromArgb(231,78,76)};
            for(int i=0;i<buttons.Length;i++){int index=i;var b=new FaceButton{Index=i,FaceColor=colors[i],Size=Design.Size(38,36),Margin=Design.Pad(0,0,5,0),AccessibleName=moods[i],AccessibleRole=AccessibleRole.RadioButton,TabStop=true};
                b.Click+=delegate{SelectedMood=moods[index];if(Changed!=null)Changed(selected);};buttons[i]=b;Controls.Add(b);}
            SelectedMood=selected;
        }
        private sealed class FaceButton:Button {
            internal int Index;internal bool Chosen;internal Color FaceColor;
            protected override void OnPaint(PaintEventArgs e){
                var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float d=Math.Min(Width,Height)-6;float x=(Width-d)/2,y=(Height-d)/2;
                using(var fill=new SolidBrush(FaceColor))g.FillEllipse(fill,x,y,d,d);
                using(var pen=new Pen(Color.FromArgb(55,62,91),Math.Max(1.5f,d*.045f)))using(var ink=new SolidBrush(pen.Color)){
                    pen.StartCap=pen.EndCap=LineCap.Round;
                    if(Index==0 || Index>=3){g.DrawArc(pen,x+d*.24f,y+d*.32f,d*.18f,d*.15f,Index==0?190:10,160);g.DrawArc(pen,x+d*.58f,y+d*.32f,d*.18f,d*.15f,Index==0?190:10,160);}
                    else {g.FillEllipse(ink,x+d*.27f,y+d*.35f,d*.1f,d*.1f);g.FillEllipse(ink,x+d*.63f,y+d*.35f,d*.1f,d*.1f);}
                    if(Index==4)g.FillEllipse(ink,x+d*.32f,y+d*.58f,d*.36f,d*.22f);
                    else if(Index==2)g.DrawLine(pen,x+d*.3f,y+d*.64f,x+d*.7f,y+d*.64f);
                    else g.DrawArc(pen,x+d*.28f,y+d*(Index==3?.60f:.45f),d*.44f,d*.3f,Index==3?190:10,160);
                }
                if(Chosen || Focused)using(var pen=new Pen(Design.Accent,Chosen?2:1)){pen.DashStyle=Focused&&!Chosen?DashStyle.Dot:DashStyle.Solid;g.DrawEllipse(pen,x-2,y-2,d+4,d+4);}
            }
        }
    }
}
