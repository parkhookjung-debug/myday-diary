using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyDay.Windows.UI
{
    public sealed class LayoutNameDialog : Form
    {
        internal readonly TextBox NameEditor;
        internal readonly Button Save;
        internal readonly Label Error;
        public LayoutNameDialog(string title,string initial,Action<string> submit)
        {
            Text=title; Font=Design.Font(10); BackColor=Design.Backgrounds[0]; AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.CenterParent; FormBorderStyle=FormBorderStyle.FixedDialog; MinimizeBox=MaximizeBox=false; ShowInTaskbar=false;
            ClientSize=Design.Size(430,240);
            var heading=Design.Label(title,17,true); heading.Location=Design.Point(24,20); Controls.Add(heading);
            var note=Design.Label("나중에 쉽게 찾을 수 있는 이름을 붙여주세요. (최대 40자)",9); note.ForeColor=Design.Muted; note.Location=Design.Point(25,62); Controls.Add(note);
            var card=new CardPanel {Fill=Color.White,Location=Design.Point(24,90),Size=Design.Size(382,42)}; Controls.Add(card);
            NameEditor=new TextBox {BorderStyle=BorderStyle.None,Font=Design.Font(11),Text=initial,MaxLength=40,AccessibleName="레이아웃 이름",Location=Design.Point(14,10),Width=Design.P(354)}; card.Controls.Add(NameEditor);
            Error=Design.Label("",8); Error.AutoSize=false; Error.Size=Design.Size(382,40); Error.ForeColor=Color.Firebrick; Error.Location=Design.Point(24,140); Controls.Add(Error);
            var cancel=Design.Button("취소"); cancel.Size=Design.Size(70,32); cancel.Location=Design.Point(234,192); cancel.DialogResult=DialogResult.Cancel; Controls.Add(cancel);
            Save=Design.Button("저장",true); Save.Size=Design.Size(92,32); Save.Location=Design.Point(314,192); Controls.Add(Save);
            Save.Click+=delegate { try { submit(NameEditor.Text.Trim()); DialogResult=DialogResult.OK; Close(); } catch(Exception ex) { Error.Text=ex.Message; NameEditor.Focus(); } };
            AcceptButton=Save; CancelButton=cancel; Shown+=delegate { NameEditor.Focus(); NameEditor.SelectAll(); };
        }
    }
}
