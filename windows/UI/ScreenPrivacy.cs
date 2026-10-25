using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
 public static class ScreenPrivacy {
  private static PinVault vault;private static bool locked,busy;private static DateTime active=DateTime.UtcNow;private static readonly List<Form> hidden=new List<Form>();private static Timer timer;
  private sealed class Activity:IMessageFilter {public bool PreFilterMessage(ref Message m){if((m.Msg>=0x100&&m.Msg<=0x109)||(m.Msg>=0x200&&m.Msg<=0x20e))Touch();return false;}}
  public static bool IsLocked {get{return locked;}}
  public static void Touch(){active=DateTime.UtcNow;}
  public static bool Start(string directory){vault=new PinVault(directory);locked=vault.Enabled;return Reveal();}
  public static void Watch(Action save){Application.AddMessageFilter(new Activity());timer=new Timer{Interval=10000};timer.Tick+=delegate{if(!busy&&!locked&&vault!=null&&vault.Enabled&&(DateTime.UtcNow-active).TotalMinutes>=5){save();Lock();}};timer.Start();}
  public static bool Reveal(){if(!locked)return true;if(busy)return false;busy=true;try{using(var dialog=new PinDialog(vault)){if(dialog.ShowDialog()!=DialogResult.OK)return false;}locked=false;Touch();foreach(var form in hidden.ToArray())if(!form.IsDisposed)form.Show();hidden.Clear();return true;}finally{busy=false;}}
  public static void Lock(){if(vault==null||!vault.Enabled)return;locked=true;foreach(var f in Application.OpenForms.Cast<Form>().ToArray())if(f.Visible){hidden.Add(f);f.Hide();}Reveal();}
  private sealed class PinDialog:Form {
   public PinDialog(PinVault store){Text="MyDay · 일기 잠금";Size=Design.Size(420,240);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;BackColor=Design.Sidebar;Font=Design.Font(10);
    var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=Design.Pad(22),FlowDirection=FlowDirection.TopDown};Controls.Add(stack);stack.Controls.Add(Design.Label("나만의 하루를 열어 볼까요?",14,true));var input=new TextBox{Width=Design.P(330),UseSystemPasswordChar=true,MaxLength=12};stack.Controls.Add(input);var status=Design.Label("숫자 PIN을 입력해 주세요.",9);stack.Controls.Add(status);var open=Design.Button("잠금 해제",true);open.Click+=delegate{try{if(store.Verify(input.Text))DialogResult=DialogResult.OK;else{status.Text="PIN이 맞지 않아요.";input.Clear();}}catch(Exception e){status.Text=e.Message;}};stack.Controls.Add(open);AcceptButton=open;
   }
  }
 }
}
