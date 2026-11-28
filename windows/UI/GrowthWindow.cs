using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
namespace MyDay.Windows.UI {
    // One companion, one purpose: see progress and check in. No artwork grids or economy.
    internal sealed class GrowthWindow : Form {
        internal readonly Button Attendance=Design.Button("오늘 출석 +5 XP",true);
        internal readonly JournalProgress Meter=new JournalProgress();
        private readonly DiaryBook book;
        private readonly Label title=Design.Label("",20,true),detail=Design.Label("",10),summary=Design.Label("",10);
        internal GrowthWindow(DiaryBook book,Func<bool> checkIn=null) {
            this.book=book;Text="MyDay · 상몬 성장";Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Backgrounds[0];AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.CenterParent;Size=Design.Size(710,640);MinimumSize=Design.Size(580,520);
            var panel=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=Design.Pad(24)};
            panel.Controls.Add(new MonsterView {Size=Design.Size(180,130),BackColor=BackColor});
            foreach(var label in new[]{title,detail,summary}) {label.AutoSize=true;label.Margin=Design.Pad(0,8,0,10);}
            panel.Controls.Add(title);panel.Controls.Add(detail);Meter.Size=Design.Size(480,10);Meter.BackColor=BackColor;panel.Controls.Add(Meter);panel.Controls.Add(summary);
            Attendance.AutoSize=true;Attendance.Margin=Design.Pad(0,10,0,10);panel.Controls.Add(Attendance);
            var rule=Design.Label("출석 +5 XP · 첫 기록 저장 +10 XP\n글 20자 이상 +10 XP · 사진 / 할 일 / 습관 각각 +5 XP\n각 항목 하루 한 번 · 기본 최대 40 XP\n연속 7일 출석마다 +10 XP\n\n만렙 Lv.100 · 누적 3,590 XP\n매일 루틴 약 100일 / 주 5일 약 200일 / 여유롭게 약 300일\n기록을 저장한 뒤 경험치를 지급해요.",10);rule.Margin=Design.Pad(0,12,0,12);panel.Controls.Add(rule);
            var close=Design.Button("닫기");close.DialogResult=DialogResult.Cancel;panel.Controls.Add(close);CancelButton=close;Controls.Add(panel);
            Action refresh=delegate {int level=SangmonGrowth.Level(book.Progress),xp=SangmonGrowth.XP(book.Progress);title.Text="Lv."+level+" · "+SangmonGrowth.Chapter(level);detail.Text="총 "+xp+" XP · 기록한 날 "+(book.Progress==null?0:book.Progress.AwardedDays.Count)+"일";Meter.Value=0;Meter.Maximum=SangmonGrowth.NeededXP(level);Meter.Value=SangmonGrowth.IntoLevel(book.Progress);summary.Text=level>=100?"만렙 달성! 다음 기록도 함께해요.":"다음 레벨까지 "+(Meter.Maximum-Meter.Value)+" XP";bool done=(SangmonGrowth.Flags(book.Progress,DateTime.Today)&SangmonGrowth.Attendance)!=0;Attendance.Text=done?"오늘 출석 완료":"오늘 출석 +5 XP";Attendance.Enabled=checkIn!=null&&!done;};
            Attendance.Click+=delegate {if(checkIn!=null&&checkIn())refresh();};Resize+=delegate {foreach(var l in new[]{title,detail,summary,rule})l.MaximumSize=new Size(Math.Max(Design.P(250),panel.ClientSize.Width-Design.P(64)),0);Meter.Width=Math.Max(Design.P(250),panel.ClientSize.Width-Design.P(64));};refresh();
        }
    }
}
