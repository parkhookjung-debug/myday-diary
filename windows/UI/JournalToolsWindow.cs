using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    [DataContract] public sealed class JournalPreferences {
        [DataMember] public bool Enabled;
        [DataMember] public int Minute=1200;
        [DataMember] public string LastDay="";
        public static JournalPreferences Load(string root) {string p=Path.Combine(root,"journal-preferences.json");if(!File.Exists(p))return new JournalPreferences();using(var s=File.OpenRead(p)){var v=(JournalPreferences)new DataContractJsonSerializer(typeof(JournalPreferences)).ReadObject(s);if(v.Minute<0 || v.Minute>=1440)throw new InvalidDataException("알림 시간이 올바르지 않아요.");return v;}}
        public void Save(string root) {Directory.CreateDirectory(root);string p=Path.Combine(root,"journal-preferences.json"),tmp=p+".tmp";using(var s=File.Create(tmp))new DataContractJsonSerializer(typeof(JournalPreferences)).WriteObject(s,this);if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}
    }
    public sealed class JournalToolsWindow : Form
    {
        private readonly TabControl tabs=new TabControl {Dock=DockStyle.Fill};
        private readonly string root;
        private readonly DiaryStore store;
        private readonly DiaryBook book;
        private readonly Action<JournalRevision> restore;
        private readonly JournalHistory archive;
        private readonly ListBox revisions=new ListBox {Dock=DockStyle.Fill,HorizontalScrollbar=true};
        private readonly TextBox preview=new TextBox {Dock=DockStyle.Bottom,Height=100,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
        public JournalToolsWindow(DiaryStore store,DiaryBook book,Action<JournalRevision> restore) {
            this.store=store;this.book=book;this.restore=restore;root=Path.GetDirectoryName(store.FilePath);archive=new JournalHistory(root);
            Text="나의 기록 도구 · "+JournalPolicy.AppVersion;Size=Design.Size(780,640);MinimumSize=Design.Size(600,460);StartPosition=FormStartPosition.CenterParent;Font=Design.Font(10);BackColor=Design.Sidebar;
            Controls.Add(tabs);History();Report();Reminder();Transfer();Updates();
        }
        private FlowLayoutPanel Page(string title) {var page=new TabPage(title);var flow=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=Design.Pad(20)};page.Controls.Add(flow);tabs.TabPages.Add(page);return flow;}
        private static void Note(Control page,string text) {var label=Design.Label(text,10);label.MaximumSize=Design.Size(650,0);label.Margin=Design.Pad(0,0,0,16);page.Controls.Add(label);}
        private static Button Add(Control page,string title,Action action) {var b=Design.Button(title);b.Size=Design.Size(320,36);b.Click+=delegate{try {action();}catch(Exception ex){MessageBox.Show(ex.Message,"MyDay");}};page.Controls.Add(b);return b;}
        private void RefreshRevisions() {revisions.DataSource=archive.List();}
        private void History() {
            var page=new TabPage("1. 휴지통·이력");tabs.TabPages.Add(page);
            var top=new FlowLayoutPanel {Dock=DockStyle.Top,Height=85,AutoScroll=true};
            Add(top,"선택한 버전 복원",delegate{var item=revisions.SelectedItem as JournalRevision;if(item==null)return;if(MessageBox.Show("현재 날짜의 내용을 이 버전으로 바꿀까요? 현재 내용도 이력에 보관해요.","복원",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;restore(item);RefreshRevisions();});
            Add(top,"선택한 이력 영구 삭제",delegate{var item=revisions.SelectedItem as JournalRevision;if(item==null)return;if(MessageBox.Show("이 복구 사본을 영구 삭제할까요? 되돌릴 수 없어요.","이력 삭제",MessageBoxButtons.YesNo)==DialogResult.Yes){archive.Remove(item);RefreshRevisions();}});
            revisions.SelectedIndexChanged+=delegate{var item=revisions.SelectedItem as JournalRevision;preview.Text=item==null?"":item.Date+" · "+item.Entry.Mood+"\r\n"+String.Join("\r\n",item.Entry.Blocks.Select(b=>b.Kind+": "+b.Text+(b.Photo!=null?" [사진 보관됨]":"")));};
            page.Controls.Add(revisions);page.Controls.Add(preview);page.Controls.Add(top);RefreshRevisions();
        }
        private void Report() {
            var page=Page("2. 월간 리포트");var picker=new DateTimePicker {Format=DateTimePickerFormat.Custom,CustomFormat="yyyy년 MM월",ShowUpDown=true};var summary=new TextBox {Multiline=true,ReadOnly=true,Width=540,Height=300,BorderStyle=BorderStyle.None,BackColor=Design.Sidebar,Font=Design.Font(12)};
            picker.ValueChanged+=delegate{summary.Text=JournalReport.ForMonth(book,picker.Value).ToString().Replace("\n","\r\n");};page.Controls.Add(picker);page.Controls.Add(summary);summary.Text=JournalReport.ForMonth(book,picker.Value).ToString().Replace("\n","\r\n");
        }
        private void Reminder() {
            var page=Page("3. 일기 알림");Note(page,"원하는 시간에 하루 한 번 상몬이 알려줘요.\nWindows에서는 앱이 트레이에서 실행 중일 때 알림을 표시해요.\n놓친 날의 알림을 한꺼번에 보내지 않아요.");
            var value=JournalPreferences.Load(root);var enabled=new CheckBox {Text="일기 알림 켜기",AutoSize=true,Checked=value.Enabled};var time=new DateTimePicker {Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm",ShowUpDown=true,Value=DateTime.Today.AddMinutes(value.Minute)};
            page.Controls.Add(enabled);page.Controls.Add(time);Add(page,"알림 설정 저장",delegate{value.Enabled=enabled.Checked;value.Minute=time.Value.Hour*60+time.Value.Minute;value.Save(root);MessageBox.Show("알림 설정을 저장했어요.");});
        }
        private void Transfer() {
            var page=Page("4. Wi-Fi · QR 이동");Note(page,"같은 Wi-Fi의 Windows·Mac·Android 사이에서 백업을 옮겨요.\n1. 아래에서 공통 백업을 저장하세요.\n2. 저장소의 tools/lan-transfer.py를 실행하고 백업을 선택하세요.\n3. QR을 스캔하거나 주소를 열어 받으세요.\n4. 받은 기기의 설정·백업에서 JSON을 가져오세요.\n10분 후 전송이 닫혀요. 전송할 때만 서버를 켜세요.");
            Add(page,"전송할 백업 저장",SaveBackup);
            Add(page,"Wi-Fi 전송 안내 열기",delegate{Process.Start("https://github.com/parkhookjung-debug/myday-diary/blob/codex/windows-app/docs/JOURNAL-TOOLS.md#4-wi-fi--qr-기록-이동");});
        }
        private void SaveBackup() {using(var dialog=new SaveFileDialog {Filter="MyDay 백업|*.json",FileName="myday-"+DateTime.Today.ToString("yyyyMMdd")+".json"})if(dialog.ShowDialog(this)==DialogResult.OK)store.Export(dialog.FileName,book);}
        private void Updates() {
            var page=Page("6. 업데이트");Note(page,"현재 버전 "+JournalPolicy.AppVersion+"\n휴지통·이력, 월간 리포트, 알림, Wi-Fi 전송 도구, 상몬 반응을 추가했어요.\n업데이트 전에는 공통 백업을 저장하세요. 프로그램 파일과 개인 기록 폴더는 분리돼 있어요.");
            var status=Design.Label("확인 버튼을 누를 때만 GitHub에 연결해요.",10);status.MaximumSize=Design.Size(630,0);page.Controls.Add(status);
            var check=Add(page,"최신 정식 버전 확인",delegate{});
            check.Click+=async delegate {check.Enabled=false;try {using(var client=new WebClient()){client.Headers[HttpRequestHeader.UserAgent]="MyDay/"+JournalPolicy.AppVersion;client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";string json=await client.DownloadStringTaskAsync(new Uri("https://api.github.com/repos/parkhookjung-debug/myday-diary/releases/latest"));using(var s=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))){var release=(ReleaseInfo)new DataContractJsonSerializer(typeof(ReleaseInfo)).ReadObject(s);Version latest,current;if(!Version.TryParse((release.Tag??"").TrimStart('v'),out latest)||!Version.TryParse(JournalPolicy.AppVersion,out current))throw new IOException("릴리스 버전을 확인할 수 없어요.");status.Text=latest>current?"새 버전 "+latest+"\n"+(release.Body??""):"현재 버전보다 새로운 정식 릴리스가 없어요.\n개발 브랜치의 빌드는 GitHub Actions에서 확인하세요.";}}}
                catch(WebException ex){status.Text=ex.Response is HttpWebResponse && ((HttpWebResponse)ex.Response).StatusCode==HttpStatusCode.NotFound?"아직 정식 릴리스가 없어요. 개발 빌드는 Actions에 있어요.":"버전 확인 실패 · 연결을 확인하고 다시 시도해 주세요.";}catch(Exception ex){status.Text=ex.Message;}finally{check.Enabled=true;}};
            Add(page,"1. 업데이트 전 백업 저장",SaveBackup);
            Add(page,"2. 다운로드·설치 페이지 열기",delegate{Process.Start("https://github.com/parkhookjung-debug/myday-diary/releases");});
        }
        [DataContract] private sealed class ReleaseInfo {[DataMember(Name="tag_name")]public string Tag="";[DataMember(Name="body")]public string Body="";}
    }
}
