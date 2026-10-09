using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class GrowthWindow : Form
    {
        private readonly DiaryBook book;
        private readonly Func<MonsterVariant,bool> equip;
        private readonly FlowLayoutPanel rewards=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,Padding=Design.Pad(16,8,16,8)};
        internal readonly Button[] EquipButtons=new Button[8];
        internal GrowthWindow(DiaryBook book,Func<MonsterVariant,bool> equip)
        {
            this.book=book; this.equip=equip;
            Text="MyDay · 상몬 성장"; Font=Design.Font(10); ForeColor=Design.Ink; BackColor=Design.Backgrounds[0];
            AutoScaleMode=AutoScaleMode.None; StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;
            ClientSize=new Size(Math.Min(Design.P(960),work.Width-64),Math.Min(Design.P(720),work.Height-80));
            MinimumSize=Design.Size(700,520);
            var hero=new CardPanel {Dock=DockStyle.Top,Height=Design.P(184),Fill=Design.Paper};
            var avatar=new MonsterView {Location=Design.Point(24,20),Size=Design.Size(160,140),Variant=MonsterVariants.FromId(book.CharacterStyle),BackColor=hero.Fill}; hero.Controls.Add(avatar);
            var eyebrow=Design.Label("SANGMON · GROWTH JOURNAL",8,true); eyebrow.ForeColor=Design.Accent; eyebrow.Location=Design.Point(204,19); hero.Controls.Add(eyebrow);
            var title=Design.Label("Lv."+SangmonGrowth.Level(book.Progress)+"  함께 쌓아가는 하루",20,true); title.Location=Design.Point(202,42); hero.Controls.Add(title);
            int xp=SangmonGrowth.XP(book.Progress), days=xp/SangmonGrowth.DailyXP;
            var meter=new ProgressBar {Minimum=0,Maximum=50,Value=xp%50,Location=Design.Point(204,91),Size=Design.Size(390,9),Style=ProgressBarStyle.Continuous}; hero.Controls.Add(meter);
            var detail=Design.Label((xp%50)+" / 50 XP  ·  기록한 날 "+days+"일  ·  총 "+xp+" XP",9); detail.Location=Design.Point(204,110); detail.ForeColor=Design.Muted; hero.Controls.Add(detail);
            var next=SangmonGrowth.Rewards.FirstOrDefault(v=>!SangmonGrowth.CanUse(book.Progress,v));
            var hint=Design.Label(next==MonsterVariant.Original?"모든 성장 외형을 모았어요. 다음 기록도 함께해요!":"다음 보상  ·  Lv."+SangmonGrowth.RequiredLevel(next)+" "+MonsterVariants.Name(next),9,true); hint.ForeColor=Design.Accent; hint.Location=Design.Point(204,140); hero.Controls.Add(hint);
            hero.Resize+=delegate { meter.Width=Math.Max(Design.P(100),Math.Min(Design.P(520),hero.ClientSize.Width-Design.P(238))); title.MaximumSize=new Size(hero.ClientSize.Width-Design.P(220),Design.P(40)); hint.MaximumSize=new Size(hero.ClientSize.Width-Design.P(220),Design.P(32)); };
            var footer=new Panel {Dock=DockStyle.Bottom,Height=Design.P(60),Padding=Design.Pad(24,10,20,0)};
            bool today=book.Progress!=null && book.Progress.AwardedDays.Contains(DiaryStore.Key(DateTime.Today));
            var rule=Design.Label((today?"오늘의 +10 XP 획득 완료":"내용을 기록하고 저장하면 오늘의 +10 XP")+"\n하루 한 번 · 50 XP마다 레벨 업 · 쉬는 날에도 성장 유지",9); rule.ForeColor=Design.Muted; footer.Controls.Add(rule);
            var close=Design.Button("닫기"); close.Dock=DockStyle.Right; close.Width=Design.P(80); close.DialogResult=DialogResult.Cancel; footer.Controls.Add(close); CancelButton=close;
            Controls.Add(rewards); Controls.Add(hero); Controls.Add(footer);
            for(int i=0;i<SangmonGrowth.Rewards.Length;i++) {
                var v=SangmonGrowth.Rewards[i]; bool unlocked=SangmonGrowth.CanUse(book.Progress,v);
                var card=new CardPanel {Size=Design.Size(212,218),Margin=Design.Pad(6),Fill=unlocked?Color.White:Color.FromArgb(241,242,237)};
                var badge=Design.Label("Lv."+SangmonGrowth.RequiredLevel(v)+"  ·  "+(unlocked?"해금 완료":"성장 보상"),8,true); badge.ForeColor=unlocked?Design.Accent:Design.Muted; badge.Location=Design.Point(16,12); card.Controls.Add(badge);
                var preview=new MonsterView {Variant=v,Location=Design.Point(16,32),Size=Design.Size(176,116),BackColor=card.Fill}; card.Controls.Add(preview);
                var name=Design.Label(MonsterVariants.Name(v),10,true); name.Location=Design.Point(16,149); card.Controls.Add(name);
                var button=Design.Button("",unlocked); button.Location=Design.Point(16,180); button.Size=Design.Size(176,28); button.Font=Design.Font(8); button.Enabled=unlocked;
                button.AccessibleName=MonsterVariants.Name(v)+" 장착"; EquipButtons[i]=button;
                button.Click+=delegate { if(equip(v)) { avatar.Variant=v; RefreshButtons(); } }; card.Controls.Add(button);
                card.Resize+=delegate { preview.Width=card.ClientSize.Width-Design.P(32); button.Width=card.ClientSize.Width-Design.P(32); };
                rewards.Controls.Add(card);
            }
            rewards.Resize+=delegate { LayoutCards(); }; LayoutCards(); RefreshButtons();
        }
        private void LayoutCards()
        {
            int width=rewards.ClientSize.Width-Design.P(32)-SystemInformation.VerticalScrollBarWidth;
            int columns=width>=Design.P(840)?4:3;
            foreach(Control card in rewards.Controls) card.Width=Math.Max(Design.P(180),width/columns-Design.P(12));
        }
        private void RefreshButtons()
        {
            for(int i=0;i<EquipButtons.Length;i++) {
                var v=SangmonGrowth.Rewards[i]; bool unlocked=SangmonGrowth.CanUse(book.Progress,v);
                EquipButtons[i].Text=!unlocked?"Lv."+SangmonGrowth.RequiredLevel(v)+"에 열려요":book.CharacterStyle==MonsterVariants.Id(v)?"장착 중":"이 모습으로 함께하기";
                EquipButtons[i].Enabled=unlocked && book.CharacterStyle!=MonsterVariants.Id(v);
                EquipButtons[i].BackColor=unlocked?(EquipButtons[i].Enabled?Design.Accent:Design.Tint):Color.White;
            }
        }
    }
}
