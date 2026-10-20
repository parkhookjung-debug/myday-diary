using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class AtelierWindow : Form
    {
        private readonly DiaryBook book;
        private readonly Label balance=Design.Label("",10,true),outcome=Design.Label("소품은 남겨두고, 조각으로 새로운 모습을 만나요.",10);
        private readonly Label[] recipeCounts=new Label[3];
        internal readonly Button[] SynthesisButtons=new Button[3];
        internal readonly Button CraftButton=Design.Button("같은 등급 조각 5개로 제작",true);
        private readonly ChoiceButton grade=Design.Choice(100),item=Design.Choice(340);
        private readonly Label achievements=Design.Label("",9);
        private SangmonItem[] targets=new SangmonItem[0];
        internal Control AchievementSection {get{return achievements;}}
        internal AtelierWindow(DiaryBook book,Func<int,bool> synthesize,Func<string,bool> craft)
        {
            this.book=book;Text="MyDay · 작은 조각 공방";Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Backgrounds[0];AutoScaleMode=AutoScaleMode.None;
            ClientSize=Design.Size(820,760);MinimumSize=Design.Size(700,580);StartPosition=FormStartPosition.CenterParent;
            var top=new Panel {Dock=DockStyle.Top,Height=Design.P(140),BackColor=Design.Paper};
            var title=Design.Label("작은 조각으로, 조금 더 특별하게",19,true);title.Location=Design.Point(24,18);top.Controls.Add(title);
            balance.Location=Design.Point(24,58);top.Controls.Add(balance);outcome.Location=Design.Point(24,94);outcome.AutoSize=false;outcome.Height=Design.P(40);top.Controls.Add(outcome);
            top.Resize+=delegate {outcome.Width=Math.Max(Design.P(100),top.Width-Design.P(48));};
            var stack=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=Design.Pad(20,12,20,12)};
            var explanation=Design.Label("합성은 같은 등급 조각 3개를 사용해요. 성공하면 상위 조각 1개, 실패하면 원래 조각 1개를 돌려줘요.\n소품·착용 장비는 소모하지 않아요. 조각 5개로 같은 등급의 미수집 소품을 직접 고를 수도 있어요.",9);explanation.AutoSize=false;explanation.Height=Design.P(58);stack.Controls.Add(explanation);
            for(int i=0;i<3;i++) {
                int g=i;var card=new CardPanel {Height=Design.P(104),Margin=Design.Pad(0,0,0,10),Fill=Design.Paper};
                var name=Design.Label(CollectionPolicy.Names[g]+" 조각 → "+CollectionPolicy.Names[g+1]+" 조각",12,true);name.Location=Design.Point(18,12);name.ForeColor=ColorTranslator.FromHtml(CollectionPolicy.Colors[g+1]);card.Controls.Add(name);
                recipeCounts[g]=Design.Label("",9);recipeCounts[g].Location=Design.Point(18,44);card.Controls.Add(recipeCounts[g]);
                var button=Design.Button("3개 합성 · 성공 "+CollectionPolicy.SynthesisPercent[g]+"%",true);button.Size=Design.Size(230,30);button.Location=Design.Point(18,68);SynthesisButtons[g]=button;card.Controls.Add(button);
                button.Click+=delegate {if(!synthesize(g))return;bool up=book.Collection.Actions.Last().ItemId.EndsWith(":up",StringComparison.Ordinal);outcome.Text=up?CollectionPolicy.Names[g+1]+" 조각을 만들었어요!": "이번에는 조각이 남았어요 · "+CollectionPolicy.Names[g]+" 조각 1개 반환";outcome.ForeColor=up?Design.Olive:Design.Accent;RefreshState();};stack.Controls.Add(card);
            }
            var creation=new CardPanel {Height=Design.P(152),Fill=Design.Paper,Margin=Design.Pad(0,0,0,12)};
            var createTitle=Design.Label("갖고 싶던 소품, 직접 고르기",12,true);createTitle.Location=Design.Point(18,12);creation.Controls.Add(createTitle);
            grade.Location=Design.Point(18,46);grade.Items.AddRange(CollectionPolicy.Names.Cast<object>());grade.SelectedIndex=0;creation.Controls.Add(grade);
            item.Location=Design.Point(128,46);creation.Controls.Add(item);CraftButton.Location=Design.Point(18,94);CraftButton.Size=Design.Size(300,32);creation.Controls.Add(CraftButton);
            grade.SelectedIndexChanged+=delegate {RefreshTargets();};item.SelectedIndexChanged+=delegate {RefreshCraft();};
            CraftButton.Click+=delegate {if(item.SelectedIndex<0 || item.SelectedIndex>=targets.Length)return;var selected=targets[item.SelectedIndex];if(craft(selected.Id)){outcome.Text="["+SangmonCollection.RarityName(selected.Id)+"] "+selected.Name+" 제작 완료!";outcome.ForeColor=Design.Olive;RefreshState();}};stack.Controls.Add(creation);
            var achievementTitle=Design.Label("차곡차곡, 수집 성취",12,true);achievementTitle.Margin=Design.Pad(2,8,0,12);stack.Controls.Add(achievementTitle);achievements.AutoSize=false;achievements.Height=Design.P(164);stack.Controls.Add(achievements);
            var close=Design.Button("닫기");close.Dock=DockStyle.Bottom;close.Height=Design.P(40);close.DialogResult=DialogResult.Cancel;CancelButton=close;
            Controls.Add(stack);Controls.Add(top);Controls.Add(close);
            Action arrange=delegate {int width=Math.Max(Design.P(500),stack.ClientSize.Width-Design.P(48)-SystemInformation.VerticalScrollBarWidth);foreach(Control c in stack.Controls)c.Width=width;};stack.Resize+=delegate {arrange();};arrange();RefreshState();
        }
        private void RefreshState()
        {
            var state=SangmonCollection.State(book.Collection,book.Progress);
            balance.Text=string.Join(" · ",Enumerable.Range(0,4).Select(g=>CollectionPolicy.Names[g]+" 조각 "+state.Materials[g]));
            for(int g=0;g<3;g++) {recipeCounts[g].Text="보유 "+state.Materials[g]+"개 · 성공 시 상위 1개 / 실패 시 원래 1개 반환";SynthesisButtons[g].Enabled=state.Materials[g]>=3 && book.Collection.Actions.Count<SangmonCollection.MaxActions;}
            achievements.Text=string.Join("\n",CollectionPolicy.Milestones.Select(m=>(state.Milestones.Contains(m[0])?"✓ 달성":"○ "+state.Owned.Count+" / "+m[0])+" · "+m[0]+"종 수집 → "+CollectionPolicy.Names[m[1]]+" 조각 +"+m[2]))+"\n\n합성 "+state.Syntheses+"회 · 성공 "+state.SynthesisSuccesses+"회 · 소품 제작 "+state.Crafts+"회";
            RefreshTargets();
        }
        private void RefreshTargets()
        {
            var state=SangmonCollection.State(book.Collection,book.Progress);targets=SangmonItems.All.Where(i=>SangmonCollection.Rarity(i.Id)==grade.SelectedIndex && !state.Owned.Contains(i.Id) && !SangmonCollection.Starters.Contains(i.Id)).ToArray();
            item.Items.Clear();item.Items.AddRange(targets.Select(i=>(object)i.Name));item.Enabled=targets.Length>0;if(targets.Length>0)item.SelectedIndex=0;else item.Text="이 등급을 모두 수집했어요";RefreshCraft();
        }
        private void RefreshCraft() {var s=SangmonCollection.State(book.Collection,book.Progress);CraftButton.Enabled=targets.Length>0 && s.Materials[grade.SelectedIndex]>=5 && (book.Collection==null || book.Collection.Actions.Count<SangmonCollection.MaxActions);}
    }
}
