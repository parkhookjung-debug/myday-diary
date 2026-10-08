using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    public sealed class TemplateGallery : Form
    {
        private readonly FlowLayoutPanel list=new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, BackColor=Color.White, Padding=Design.Pad(12) };
        private readonly FlowLayoutPanel preview=new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, Padding=Design.Pad(24,16,20,12) };
        private readonly Button add=Design.Button("이 형식 추가",true);
        public JournalTemplate SelectedTemplate { get; private set; }
        public TemplateGallery()
        {
            Text="MyDay · 일기 형식"; Font=Design.Font(9); BackColor=Design.Backgrounds[0];
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(780),work.Width-32),Math.Min(Design.P(650),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(700),work.Width-32),Math.Min(Design.P(540),work.Height-32));
            MaximizeBox=false; MinimizeBox=false;
            var header=new Panel { Dock=DockStyle.Top, Height=Design.P(90), Padding=Design.Pad(24), BackColor=Color.White };
            var title=Design.Label("오늘은 어떤 일기를 쓸까요?",18,true); title.Location=Design.Point(24,16); header.Controls.Add(title);
            var intro=Design.Label("원하는 형식으로 시작하고, 블록은 마음대로 바꿔보세요.",9); intro.ForeColor=Design.Muted; intro.Location=Design.Point(25,56); header.Controls.Add(intro);
            var grid=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, Margin=Design.Pad(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,Design.P(204))); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            grid.Controls.Add(list,0,0); grid.Controls.Add(preview,1,0);
            foreach(var template in DiaryTemplates.All) {
                var option=Design.Button(template.Name); option.Tag=template; option.Size=Design.Size(174,46); option.Margin=Design.Pad(0,0,0,8);
                var item=template; option.Click+=delegate { Select(item); }; list.Controls.Add(option);
            }
            var footer=new Panel { Dock=DockStyle.Bottom, Height=Design.P(76), BackColor=Color.White };
            var note=Design.Label("기존 글은 그대로 두고 새 블록을 추가해요.",8); note.ForeColor=Design.Muted; note.Location=Design.Point(24,26); footer.Controls.Add(note);
            var cancel=Design.Button("취소"); cancel.Width=Design.P(70); cancel.DialogResult=DialogResult.Cancel; footer.Controls.Add(cancel);
            add.Width=Design.P(112); add.Click+=delegate { DialogResult=DialogResult.OK; Close(); }; footer.Controls.Add(add);
            footer.Resize+=delegate { add.Location=new Point(footer.Width-Design.P(136),Design.P(20)); cancel.Location=new Point(add.Left-Design.P(82),Design.P(20)); };
            AcceptButton=add; CancelButton=cancel;
            Controls.Add(grid); Controls.Add(header); Controls.Add(footer);
            preview.Resize+=delegate { ResizePreview(); };
            Select(DiaryTemplates.All[0]);
        }
        private void Select(JournalTemplate template)
        {
            SelectedTemplate=template;
            foreach(var button in list.Controls.OfType<Button>()) { bool active=button.Tag==template; button.BackColor=active?Design.Accent:Color.White; button.ForeColor=active?Color.White:Design.Ink; }
            preview.SuspendLayout(); foreach(Control control in preview.Controls.Cast<Control>().ToArray()) control.Dispose();
            var heading=Design.Label(template.Name,20,true); heading.Margin=Design.Pad(0,0,0,8); preview.Controls.Add(heading);
            // A fresh label is used because the old preview is disposed on every selection.
            var explanation=Design.Label(template.Description,9); explanation.ForeColor=Design.Muted; explanation.MaximumSize=Design.Size(440,0); explanation.Margin=Design.Pad(0,0,0,20); preview.Controls.Add(explanation);
            foreach(var section in template.Sections) {
                var card=new CardPanel { Fill=template.Style=="paper"?Design.Paper:Color.White, Pattern=template.Style, Height=Design.P(96), Width=Design.P(440), Margin=Design.Pad(0,0,0,12) };
                var title=Design.Label(section.Title,11,true); title.Location=Design.Point(24,20); card.Controls.Add(title);
                var prompt=Design.Label(section.Prompt,9); prompt.ForeColor=Design.Muted; prompt.Location=Design.Point(24,55); prompt.MaximumSize=Design.Size(380,0); card.Controls.Add(prompt);
                preview.Controls.Add(card);
            }
            preview.AutoScrollPosition=Point.Empty; preview.ResumeLayout(); ResizePreview();
        }
        private void ResizePreview()
        {
            foreach(var card in preview.Controls.OfType<CardPanel>()) {
                card.Width=Math.Max(Design.P(220),preview.ClientSize.Width-preview.Padding.Horizontal-Design.P(22));
                foreach(var label in card.Controls.OfType<Label>()) label.MaximumSize=new Size(Math.Max(Design.P(100),card.Width-Design.P(48)),0);
            }
        }
        internal void VerifyAndRender(string path)
        {
            foreach(var button in list.Controls.OfType<Button>()) {
                button.PerformClick();
                if(SelectedTemplate!=button.Tag || preview.Controls.OfType<CardPanel>().Count()!=SelectedTemplate.Sections.Length)
                    throw new Exception("Template gallery selection did not update preview");
            }
            list.Controls.OfType<Button>().ElementAt(1).PerformClick(); PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(path,System.Drawing.Imaging.ImageFormat.Png); }
            add.PerformClick(); if(DialogResult!=DialogResult.OK) throw new Exception("Template gallery did not confirm selection");
        }
    }
}
