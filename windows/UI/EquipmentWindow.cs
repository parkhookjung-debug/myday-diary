using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class EquipmentWindow : Form
    {
        private const int PageSize=12;
        private static readonly Color Backdrop=Color.FromArgb(249,245,238),CardFill=Color.FromArgb(255,252,247),Accent=Color.FromArgb(130,96,78),Muted=Color.FromArgb(150,128,112);
        private readonly DiaryBook book;
        private readonly Func<string,string,bool> equip;
        private readonly FlowLayoutPanel list=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,Padding=Design.Pad(12),BackColor=Backdrop};
        private readonly MonsterView avatar=new MonsterView {BackColor=CardFill};
        private readonly Label summary=Design.Label("",9),pageLabel=Design.Label("",9);
        internal readonly ChoiceButton Category=Design.Choice(132),Element=Design.Choice(92);
        internal readonly TextBox Search=new TextBox();
        internal readonly Button ClearWeapon=Design.Button("손 소품 해제"),ClearCharm=Design.Button("작은 소품 해제");
        internal readonly Button Previous=Design.Button("이전"),Next=Design.Button("다음");
        internal readonly Dictionary<string,Button> EquipButtons=new Dictionary<string,Button>();
        private int page;
        public EquipmentWindow(DiaryBook book,Func<string,string,bool> equip,Action openCollection=null)
        {
            this.book=book;this.equip=equip;
            Text="MyDay · 300가지 상몬 소품";BackColor=Backdrop;ForeColor=Accent;Font=Design.Font(10);AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;ClientSize=new Size(Math.Min(Design.P(1040),work.Width-64),Math.Min(Design.P(860),work.Height-80));MinimumSize=Design.Size(760,600);
            var hero=new Panel {Dock=DockStyle.Top,Height=Design.P(180),BackColor=CardFill};
            avatar.Location=Design.Point(12,8);avatar.Size=Design.Size(166,158);hero.Controls.Add(avatar);
            var title=Design.Label("상몬의 작은 옷장",22,true);title.ForeColor=Design.Ink;title.Location=Design.Point(192,20);hero.Controls.Add(title);
            var hint=Design.Label("포근한 소품 300가지 · 나만의 작은 친구 꾸미기",9,true);hint.ForeColor=Accent;hint.Location=Design.Point(196,58);hero.Controls.Add(hint);
            summary.Location=Design.Point(196,85);summary.ForeColor=Muted;summary.AutoSize=false;summary.Size=Design.Size(560,76);hero.Controls.Add(summary);
            hero.Resize+=delegate {summary.Width=Math.Max(100,hero.Width-Design.P(218));};
            var filters=new FlowLayoutPanel {Dock=DockStyle.Top,Height=Design.P(80),Padding=Design.Pad(16,8,8,4),WrapContents=true};
            Category.Items.AddRange(SangmonItems.SlotNames.Cast<object>().Concat(new object[]{"전체 300종"}).ToArray());Category.SelectedIndex=0;Category.AccessibleName="소품 종류 선택";
            Element.Items.AddRange(new object[]{"모든 색감","크림","살구","하늘","민트","라벤더","딸기"});Element.SelectedIndex=0;Element.AccessibleName="소품 색감 선택";
            Search.Width=Design.P(190);Search.AccessibleName="소품 이름 검색";
            var clear=Design.Button("이 소품 벗기기");clear.Click+=delegate {if(Category.SelectedIndex<8 && equip(SangmonItems.Slots[Category.SelectedIndex],"none"))RefreshSelection();};
            Category.SelectedIndexChanged+=delegate {page=0;RenderItems();};Element.SelectedIndexChanged+=delegate {page=0;RenderItems();};Search.TextChanged+=delegate {page=0;RenderItems();};
            ClearWeapon.Click+=delegate {if(equip("weapon","none"))RefreshSelection();};ClearCharm.Click+=delegate {if(equip("charm","none"))RefreshSelection();};
            filters.Controls.AddRange(new Control[]{Category,Element,Search,clear,ClearWeapon,ClearCharm});
            var footer=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=Design.P(48),Padding=Design.Pad(16,8,8,0)};
            Previous.Click+=delegate {page--;RenderItems();};Next.Click+=delegate {page++;RenderItems();};
            var close=Design.Button("닫기");close.DialogResult=DialogResult.Cancel;CancelButton=close;
            pageLabel.ForeColor=Muted;pageLabel.Margin=Design.Pad(12,6,12,0);
            var gifts=Design.Button("소품 뽑기");gifts.Enabled=openCollection!=null;gifts.Click+=delegate {if(openCollection!=null)openCollection();};
            footer.Controls.AddRange(new Control[]{Previous,pageLabel,Next,gifts,close});
            Controls.Add(list);Controls.Add(filters);Controls.Add(hero);Controls.Add(footer);
            list.Resize+=delegate {LayoutItems();};RenderItems();
        }
        private void RenderItems()
        {
            string query=Search.Text.Trim();
            var matches=SangmonItems.All.Where(i=>(Category.SelectedIndex==8 || i.Slot==SangmonItems.Slots[Math.Max(0,Category.SelectedIndex)]) && (Element.SelectedIndex==0 || (int)i.Element==Element.SelectedIndex-1) && (query.Length==0 || (i.Name+" "+i.Description+" "+i.Id).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            int pages=Math.Max(1,(matches.Length+PageSize-1)/PageSize);page=Math.Max(0,Math.Min(page,pages-1));
            list.SuspendLayout();foreach(Control c in list.Controls.Cast<Control>().ToArray())c.Dispose();EquipButtons.Clear();
            foreach(var item in matches.Skip(page*PageSize).Take(PageSize)) {
                var selected=item;var card=new CardPanel {Size=Design.Size(270,254),Margin=Design.Pad(6),Fill=CardFill};
                var badge=Design.Label(SangmonItems.SlotName(item.Slot)+" · "+item.Description.Split('·')[0],8,true);badge.ForeColor=Accent;badge.Location=Design.Point(16,10);card.Controls.Add(badge);
                var art=new ItemView {Item=item,BackColor=CardFill,Location=Design.Point(16,32),Size=Design.Size(230,138),AccessibleName=item.Name+" 미리보기"};card.Controls.Add(art);
                var name=Design.Label(item.Name,11,true);name.ForeColor=Design.Ink;name.Location=Design.Point(16,176);card.Controls.Add(name);
                var detail=Design.Label("오늘의 상몬에게 어울리는 작은 소품",8);detail.ForeColor=Muted;detail.Location=Design.Point(16,201);card.Controls.Add(detail);
                var button=Design.Button("입혀 보기");button.Location=Design.Point(16,222);button.Size=Design.Size(230,26);button.Font=Design.Font(8);button.AccessibleName=item.Name+" 장착";
                button.Click+=delegate {if(equip(selected.Slot,selected.Id))RefreshSelection();};EquipButtons.Add(item.Id,button);card.Controls.Add(button);
                card.Resize+=delegate {art.Width=button.Width=card.ClientSize.Width-Design.P(32);};list.Controls.Add(card);
            }
            if(matches.Length==0){var empty=Design.Label("맞는 소품이 없어요. 이름이나 색감을 바꿔보세요.",10);empty.ForeColor=Muted;list.Controls.Add(empty);}
            list.ResumeLayout();list.AutoScrollPosition=Point.Empty;pageLabel.Text=matches.Length+"종 · "+(page+1)+" / "+pages;Previous.Enabled=page>0;Next.Enabled=page+1<pages;
            LayoutItems();RefreshSelection();
        }
        private void LayoutItems()
        {
            int width=list.ClientSize.Width-Design.P(30)-SystemInformation.VerticalScrollBarWidth,columns=width>=Design.P(800)?3:2;
            foreach(var card in list.Controls.OfType<CardPanel>())card.Width=Math.Max(Design.P(210),width/columns-Design.P(12));
        }
        private void RefreshSelection()
        {
            var state=book.Equipment??new SangmonEquipment();avatar.Variant=MonsterVariants.FromId(book.CharacterStyle);avatar.Equipment=state;
            summary.Text=string.Join("\n",Enumerable.Range(0,4).Select(row=>string.Join("    ·    ",SangmonItems.Slots.Skip(row*2).Take(2).Select(s=>SangmonItems.SlotName(s)+": "+SangmonItems.Name(SangmonItems.Get(state,s))))));
            ClearWeapon.Enabled=state.WeaponId!="none";ClearCharm.Enabled=state.CharmId!="none";
            foreach(var pair in EquipButtons){var item=SangmonItems.Find(pair.Key);bool chosen=SangmonItems.Get(state,item.Slot)==item.Id;pair.Value.Text=chosen?"함께하는 중":"입혀 보기";pair.Value.Enabled=!chosen;}
        }
    }
}
