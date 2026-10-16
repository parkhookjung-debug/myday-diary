using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class CollectionWindow : Form
    {
        private readonly DiaryBook book;
        private readonly Func<string,bool> collect;
        private readonly FlowLayoutPanel grid=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,Padding=Design.Pad(12)};
        private readonly Label balance=Design.Label("",11,true),result=Design.Label("작은 선물 상자를 열어보세요",12,true),pageLabel=Design.Label("",9),pity=Design.Label("",9);
        private readonly ItemView gift=new ItemView {BackColor=Color.FromArgb(255,252,247)};
        private readonly TextBox search=new TextBox {AccessibleName="수집 소품 검색"};
        private readonly CheckBox ownedOnly=new CheckBox {Text="수집한 소품만",AutoSize=true};
        internal readonly Button DrawButton=Design.Button("선물 뽑기 · 클로버 20",true);
        private readonly Button previous=Design.Button("이전"),next=Design.Button("다음");
        private int page;
        internal CollectionWindow(DiaryBook book,Func<string,bool> collect)
        {
            this.book=book;this.collect=collect;
            Text="MyDay · 상몬의 선물 상자";Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Backgrounds[0];AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.CenterParent;ClientSize=Design.Size(880,740);MinimumSize=Design.Size(720,600);
            var hero=new Panel {Dock=DockStyle.Top,Height=Design.P(240),BackColor=gift.BackColor};
            gift.Location=Design.Point(22,14);gift.Size=Design.Size(145,155);hero.Controls.Add(gift);
            var title=Design.Label("상몬의 선물 상자",20,true);title.Location=Design.Point(184,18);hero.Controls.Add(title);
            balance.Location=Design.Point(186,60);hero.Controls.Add(balance);
            result.Location=Design.Point(186,91);result.AutoSize=false;result.Size=Design.Size(470,47);hero.Controls.Add(result);
            DrawButton.Location=Design.Point(186,143);DrawButton.Size=Design.Size(240,34);DrawButton.Click+=delegate {Act(null);};hero.Controls.Add(DrawButton);
            pity.Location=Design.Point(440,153);hero.Controls.Add(pity);
            var rules=Design.Label("환영 클로버 40 · 1 XP = 클로버 1 · 레벨 업 +5 · 연속 출석 7일마다 +10\n일반 뽑기는 300종 각각 1/300 · 10번째마다 미수집 소품 균등 보장\n중복은 꽃잎 3개 · 꽃잎 15개로 미수집 소품 선택 교환 · 자유 꾸미기는 그대로",8);
            rules.Location=Design.Point(24,185);rules.AutoSize=false;rules.Size=Design.Size(810,52);hero.Controls.Add(rules);
            hero.Resize+=delegate {rules.Width=Math.Max(100,hero.ClientSize.Width-Design.P(48));};
            var filters=new FlowLayoutPanel {Dock=DockStyle.Top,Height=Design.P(44),Padding=Design.Pad(20,7,12,0)};
            search.Width=Design.P(240);search.TextChanged+=delegate {page=0;RefreshItems();};ownedOnly.CheckedChanged+=delegate {page=0;RefreshItems();};filters.Controls.Add(search);filters.Controls.Add(ownedOnly);
            var footer=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=Design.P(46),Padding=Design.Pad(20,6,0,0)};
            previous.Click+=delegate {page--;RefreshItems();};next.Click+=delegate {page++;RefreshItems();};pageLabel.Margin=Design.Pad(12,6,12,0);
            var close=Design.Button("닫기");close.DialogResult=DialogResult.Cancel;CancelButton=close;footer.Controls.AddRange(new Control[]{previous,pageLabel,next,close});
            Controls.Add(grid);Controls.Add(filters);Controls.Add(hero);Controls.Add(footer);
            grid.Resize+=delegate {LayoutCards();};RefreshItems();
        }
        private void Act(string itemId)
        {
            var before=SangmonCollection.State(book.Collection,book.Progress);
            if(!collect(itemId))return;
            var action=book.Collection.Actions.Last();gift.Item=SangmonItems.Find(action.ItemId);
            result.Text=(action.Kind=="exchange"?"꽃잎으로 데려왔어요":before.Owned.Contains(action.ItemId)?"또 만났어요 · 꽃잎 +3":"새 소품을 만났어요!")+"\n"+gift.Item.Name;
            RefreshItems();
        }
        private void RefreshItems()
        {
            var state=SangmonCollection.State(book.Collection,book.Progress);
            balance.Text="클로버 "+state.Coins+" · 꽃잎 "+state.Petals+" · 수집 "+state.Owned.Count+"/300";
            DrawButton.Enabled=state.Coins>=20 && (book.Collection==null || book.Collection.Actions.Count<SangmonCollection.MaxActions);
            DrawButton.Text="클로버 20 · 선물 뽑기";pity.Text=state.Owned.Count==SangmonItems.All.Length?"모든 소품 수집 완료 · 일반 뽑기":(10-state.Draws%10)+"회 후 새 소품 보장";
            var matches=SangmonItems.All.Where(i=>(!ownedOnly.Checked || state.Owned.Contains(i.Id)) && (i.Name+" "+i.Id).IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            int pages=Math.Max(1,(matches.Length+11)/12);page=Math.Max(0,Math.Min(page,pages-1));
            grid.SuspendLayout();foreach(Control control in grid.Controls.Cast<Control>().ToArray())control.Dispose();
            foreach(var item in matches.Skip(page*12).Take(12)) {
                bool owned=state.Owned.Contains(item.Id);var selected=item;
                var card=new CardPanel {Size=Design.Size(250,210),Margin=Design.Pad(5),Fill=owned?Color.FromArgb(239,245,232):Color.FromArgb(255,252,247)};
                var art=new ItemView {Item=item,BackColor=card.Fill,Location=Design.Point(14,8),Size=Design.Size(218,130)};card.Controls.Add(art);
                var name=Design.Label(item.Name,10,true);name.Location=Design.Point(14,141);card.Controls.Add(name);
                var exchange=Design.Button(owned?"수집 완료":"꽃잎 15개로 교환");exchange.Location=Design.Point(14,172);exchange.Size=Design.Size(218,28);exchange.Enabled=!owned && state.Petals>=15 && (book.Collection==null || book.Collection.Actions.Count<SangmonCollection.MaxActions);
                exchange.Click+=delegate {Act(selected.Id);};card.Controls.Add(exchange);card.Resize+=delegate {art.Width=exchange.Width=card.ClientSize.Width-Design.P(28);};grid.Controls.Add(card);
            }
            if(matches.Length==0)grid.Controls.Add(Design.Label("아직 수집한 소품이 없거나 검색 결과가 없어요.",10));
            grid.ResumeLayout();grid.AutoScrollPosition=Point.Empty;LayoutCards();pageLabel.Text=matches.Length+"종 · "+(page+1)+"/"+pages;previous.Enabled=page>0;next.Enabled=page+1<pages;
        }
        private void LayoutCards()
        {
            int width=grid.ClientSize.Width-Design.P(30)-SystemInformation.VerticalScrollBarWidth,columns=width>=Design.P(780)?3:2;
            foreach(var card in grid.Controls.OfType<CardPanel>())card.Width=Math.Max(Design.P(210),width/columns-Design.P(12));
        }
    }
}
