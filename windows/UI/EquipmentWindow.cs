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
        private static readonly Color Night=Color.FromArgb(21,28,45),PanelColor=Color.FromArgb(35,45,66),Gold=Color.FromArgb(242,213,159),Muted=Color.FromArgb(168,184,201);
        private readonly DiaryBook book;
        private readonly Func<string,string,bool> equip;
        private readonly FlowLayoutPanel list=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,Padding=Design.Pad(16,6,16,6),BackColor=Night};
        private readonly MonsterView avatar=new MonsterView {BackColor=PanelColor};
        private readonly Label weaponLabel=Design.Label("",9),charmLabel=Design.Label("",9);
        internal readonly ChoiceButton Category=Design.Choice(152);
        internal readonly Button ClearWeapon=Design.Button("무기 해제"),ClearCharm=Design.Button("보주 해제");
        internal readonly Dictionary<string,Button> EquipButtons=new Dictionary<string,Button>();
        public EquipmentWindow(DiaryBook book,Func<string,string,bool> equip)
        {
            this.book=book;this.equip=equip;
            Text="MyDay · 상몬 아이템";BackColor=Night;ForeColor=Gold;Font=Design.Font(10);AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterParent;
            var work=Screen.PrimaryScreen.WorkingArea;ClientSize=new Size(Math.Min(Design.P(960),work.Width-64),Math.Min(Design.P(800),work.Height-80));MinimumSize=Design.Size(720,540);
            var hero=new Panel {Dock=DockStyle.Top,Height=Design.P(160),BackColor=PanelColor};
            avatar.Location=Design.Point(16,8);avatar.Size=Design.Size(154,144);hero.Controls.Add(avatar);
            var eyebrow=Design.Label("SANGMON · EQUIPMENT",8,true);eyebrow.Location=Design.Point(188,18);eyebrow.ForeColor=Gold;hero.Controls.Add(eyebrow);
            var title=Design.Label("상몬의 장비함",23,true);title.Location=Design.Point(184,40);title.ForeColor=Color.White;hero.Controls.Add(title);
            weaponLabel.Location=Design.Point(188,94);charmLabel.Location=Design.Point(188,122);weaponLabel.ForeColor=Muted;charmLabel.ForeColor=Muted;hero.Controls.AddRange(new Control[]{weaponLabel,charmLabel});
            ClearWeapon.Size=Design.Size(98,30);ClearCharm.Size=Design.Size(98,30);ClearWeapon.BackColor=ClearCharm.BackColor=Night;ClearWeapon.ForeColor=ClearCharm.ForeColor=Gold;
            ((RoundedButton)ClearWeapon).HoverFill=((RoundedButton)ClearCharm).HoverFill=Color.FromArgb(51,65,91);
            ClearWeapon.Click+=delegate {if(equip("weapon","none"))RefreshSelection();};ClearCharm.Click+=delegate {if(equip("charm","none"))RefreshSelection();};hero.Controls.AddRange(new Control[]{ClearWeapon,ClearCharm});
            hero.Resize+=delegate {ClearWeapon.Location=new Point(hero.ClientSize.Width-Design.P(122),Design.P(88));ClearCharm.Location=new Point(hero.ClientSize.Width-Design.P(122),Design.P(122));weaponLabel.MaximumSize=charmLabel.MaximumSize=new Size(hero.ClientSize.Width-Design.P(330),Design.P(24));};
            var filters=new Panel {Dock=DockStyle.Top,Height=Design.P(40),BackColor=Night};
            var hint=Design.Label("속성을 골라 함께하기",9,true);hint.ForeColor=Gold;hint.Location=Design.Point(24,12);filters.Controls.Add(hint);
            Category.Items.AddRange(new object[]{"무기 6종","마법 보주 6종","전체 12종"});Category.BackColor=PanelColor;Category.ForeColor=Gold;Category.SelectedIndex=0;Category.AccessibleName="아이템 종류 선택";Category.SelectedIndexChanged+=delegate {RenderItems();};filters.Controls.Add(Category);
            filters.Resize+=delegate {Category.Location=new Point(filters.ClientSize.Width-Category.Width-Design.P(24),Design.P(6));};
            Category.HoverFill=Color.FromArgb(51,65,91);
            var footer=new Panel {Dock=DockStyle.Bottom,Height=Design.P(44),BackColor=Night};
            var rule=Design.Label("무기와 보주를 하나씩 · 모든 아이템 바로 장착 · 기록과 함께 저장",8);rule.ForeColor=Muted;rule.Location=Design.Point(24,13);footer.Controls.Add(rule);
            var close=Design.Button("닫기");close.BackColor=PanelColor;close.ForeColor=Gold;close.Size=Design.Size(74,28);close.DialogResult=DialogResult.Cancel;footer.Controls.Add(close);CancelButton=close;
            footer.Resize+=delegate {close.Location=new Point(footer.ClientSize.Width-close.Width-Design.P(20),Design.P(8));rule.MaximumSize=new Size(footer.ClientSize.Width-close.Width-Design.P(66),Design.P(30));};
            ((RoundedButton)close).HoverFill=Color.FromArgb(51,65,91);
            Controls.Add(list);Controls.Add(filters);Controls.Add(hero);Controls.Add(footer);list.Resize+=delegate {LayoutItems();};RenderItems();RefreshSelection();
        }
        private void RenderItems()
        {
            list.SuspendLayout();foreach(Control c in list.Controls.Cast<Control>().ToArray())c.Dispose();EquipButtons.Clear();
            foreach(var item in SangmonItems.All.Where(i=>Category.SelectedIndex==2 || i.Slot==(Category.SelectedIndex==0?"weapon":"charm"))) {
                var selected=item;
                var card=new CardPanel {Size=Design.Size(270,260),Margin=Design.Pad(6),Fill=PanelColor};
                var badge=Design.Label((item.Slot=="weapon"?"WEAPON":"MAGIC ORB")+"  ·  "+ElementName(item.Element),8,true);badge.ForeColor=ItemPainter.Tone(item.Element);badge.Location=Design.Point(18,12);card.Controls.Add(badge);
                var art=new ItemView {Item=item,BackColor=PanelColor,Location=Design.Point(18,30),Size=Design.Size(234,148),AccessibleName=item.Name+" 미리보기"};card.Controls.Add(art);
                var name=Design.Label(item.Name,11,true);name.ForeColor=Color.White;name.Location=Design.Point(18,180);card.Controls.Add(name);
                var detail=Design.Label(item.Description,8);detail.ForeColor=Muted;detail.Location=Design.Point(18,203);card.Controls.Add(detail);
                var button=Design.Button("장착하기");button.Location=Design.Point(18,226);button.Size=Design.Size(234,28);button.Font=Design.Font(8);button.BackColor=Night;button.ForeColor=Gold;button.AccessibleName=item.Name+" 장착";
                button.Click+=delegate {if(equip(selected.Slot,selected.Id))RefreshSelection();};EquipButtons.Add(item.Id,button);card.Controls.Add(button);
                ((RoundedButton)button).HoverFill=Color.FromArgb(51,65,91);
                card.Resize+=delegate {art.Width=button.Width=card.ClientSize.Width-Design.P(36);detail.MaximumSize=new Size(art.Width,Design.P(20));};list.Controls.Add(card);
            }
            list.ResumeLayout();list.AutoScrollPosition=Point.Empty;LayoutItems();RefreshSelection();
        }
        private void LayoutItems()
        {
            int width=list.ClientSize.Width-Design.P(32)-SystemInformation.VerticalScrollBarWidth;
            int columns=width>=Design.P(720)?3:2;
            foreach(Control card in list.Controls)card.Width=Math.Max(Design.P(200),width/columns-Design.P(12));
        }
        private void RefreshSelection()
        {
            var state=book.Equipment??new SangmonEquipment();avatar.Variant=MonsterVariants.FromId(book.CharacterStyle);avatar.Equipment=state;
            weaponLabel.Text="무기  ·  "+SangmonItems.Name(state.WeaponId);charmLabel.Text="보주  ·  "+SangmonItems.Name(state.CharmId);
            ClearWeapon.Enabled=state.WeaponId!="none";ClearCharm.Enabled=state.CharmId!="none";
            foreach(var pair in EquipButtons) {var item=SangmonItems.Find(pair.Key);bool chosen=(item.Slot=="weapon"?state.WeaponId:state.CharmId)==item.Id;pair.Value.Text=chosen?"장착 중":"장착하기";pair.Value.Enabled=!chosen;}
        }
        private static string ElementName(ItemElement e)
        {
            switch(e){case ItemElement.Solar:return "태양";case ItemElement.Ember:return "화염";case ItemElement.Frost:return "빙결";case ItemElement.Storm:return "번개";case ItemElement.Shadow:return "그림자";default:return "별빛";}
        }
    }
}
