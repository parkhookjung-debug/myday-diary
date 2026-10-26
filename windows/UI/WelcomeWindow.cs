using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI {
 public sealed class WelcomeWindow:Form {
    private readonly FlowLayoutPanel content=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    private readonly Button[] tabs=new Button[4];
    private readonly DiaryBook book;private readonly Action<int> action;
    public int SelectedSection {get;private set;}
    public WelcomeWindow(DiaryBook book,Action<int> action=null,int section=0) {
        this.book=book;this.action=action;Text=ExperienceCatalog.Title;Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Sidebar;
        AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(810,680);MinimumSize=Design.Size(620,520);
        var top=new Panel{Dock=DockStyle.Top,Height=Design.P(100),Padding=Design.Pad(24)};
        var title=Design.Label("작은 기록을 시작하는 방법",20,true);title.Location=Design.Point(24,20);top.Controls.Add(title);
        var subtitle=Design.Label("기록하고 · 꾸미고 · 함께 자라고 · 안전하게 보관해요",10);subtitle.ForeColor=Design.Muted;subtitle.Location=Design.Point(26,60);top.Controls.Add(subtitle);
        var nav=new FlowLayoutPanel{Dock=DockStyle.Left,Width=Design.P(170),Padding=Design.Pad(14,10,0,0),FlowDirection=FlowDirection.TopDown,WrapContents=false};
        for(int i=0;i<4;i++){int n=i;tabs[i]=Design.Button(ExperienceCatalog.Titles[i]);tabs[i].Size=Design.Size(146,42);tabs[i].Margin=Design.Pad(0,0,0,8);tabs[i].Click+=delegate{SelectSection(n);};nav.Controls.Add(tabs[i]);}
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=Design.P(64),Padding=Design.Pad(24,12,0,0)};
        var close=Design.Button("시작하기",true);close.Size=Design.Size(130,38);close.Click+=delegate{Close();};footer.Controls.Add(close);CancelButton=close;
        content.Padding=Design.Pad(20,12,24,24);content.Resize+=delegate{FitText();};
        Controls.Add(content);Controls.Add(nav);Controls.Add(footer);Controls.Add(top);SelectSection(section);
    }
    public void SelectSection(int index){
        SelectedSection=index;content.SuspendLayout();foreach(Control c in System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Cast<Control>(content.Controls)))c.Dispose();
        for(int i=0;i<4;i++)tabs[i].BackColor=i==index?Design.Selection:Design.Paper;
        var heading=Design.Label(ExperienceCatalog.Titles[index],17,true);heading.Margin=Design.Pad(0,0,0,18);content.Controls.Add(heading);
        if(index==2){var status=Design.Label(RewardOverview.Text(book),11,true);status.ForeColor=Design.Olive;status.Margin=Design.Pad(0,0,0,18);content.Controls.Add(status);}
        var body=Design.Label(ExperienceCatalog.Bodies[index],10);body.Margin=Design.Pad(0,0,0,18);content.Controls.Add(body);
        if(index==0 && action!=null){string[] names={"글 일기 쓰기","사진 추가","하루 꾸미기"};for(int i=0;i<3;i++){int n=i;var go=Design.Button(names[i]);go.Size=Design.Size(210,38);go.Margin=Design.Pad(0,0,0,8);go.Click+=delegate{Close();action(n);};content.Controls.Add(go);}}
        content.ResumeLayout();content.AutoScrollPosition=Point.Empty;FitText();
    }
    private void FitText(){foreach(Control c in content.Controls)if(c is Label)c.MaximumSize=new Size(Math.Max(Design.P(180),content.ClientSize.Width-Design.P(56)),0);}
 }
 public sealed partial class DiaryWindow {
    private void OpenWelcome(){if(!ScreenPrivacy.Reveal() || !FlushSave())return;using(var guide=new WelcomeWindow(book,i=>{if(i==0)AddBlock("text");else if(i==1)SelectPhoto(null);else OpenStudio();}))guide.ShowDialog(this);try{WelcomeState.Complete(Path.GetDirectoryName(store.FilePath));}catch(Exception ex){MessageBox.Show(this,"안내 완료 상태를 저장하지 못했어요.\n"+ex.Message);}}
    internal void VerifyWelcomeForTest(string directory){using(var guide=new WelcomeWindow(book,i=>{})){guide.StartPosition=FormStartPosition.Manual;guide.Location=new Point(-30000,-30000);guide.Show(this);Application.DoEvents();for(int i=0;i<4;i++){guide.SelectSection(i);Application.DoEvents();if(guide.SelectedSection!=i)throw new Exception("Guide navigation failed");using(var image=new Bitmap(guide.Width,guide.Height)){guide.DrawToBitmap(image,new Rectangle(Point.Empty,guide.Size));image.Save(Path.Combine(directory,"windows-welcome-"+i+".png"));}}guide.Close();}}
 }
}
