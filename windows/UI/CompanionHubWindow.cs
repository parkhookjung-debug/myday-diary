using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Character;
using MyDay.Windows.Core;

namespace MyDay.Windows.UI
{
    internal sealed class CompanionHubWindow : Form
    {
        internal int Destination;
        private readonly MonsterView avatar;
        private readonly FlowLayoutPanel cards;
        internal CompanionHubWindow(DiaryBook book,Func<MonsterVariant,bool> select)
        {
            Text=AppShell.Titles[2];Font=Design.Font(10);ForeColor=Design.Ink;BackColor=Design.Backgrounds[0];StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;
            Size=Design.Size(860,760);MinimumSize=Design.Size(640,550);
            var top=new Panel {Dock=DockStyle.Top,Height=Design.P(228),BackColor=Design.FriendCard};
            avatar=new MonsterView {Location=Design.Point(16,12),Size=Design.Size(150,128),Variant=MonsterVariants.FromId(book.CharacterStyle),Equipment=book.Equipment,BackColor=Design.FriendCard};top.Controls.Add(avatar);
            var title=Design.Label("Lv."+SangmonGrowth.Level(book.Progress)+" · 나의 작은 친구",20,true);title.Location=Design.Point(180,22);top.Controls.Add(title);
            var detail=Design.Label("총 "+SangmonGrowth.XP(book.Progress)+" XP · 만렙 100 · 3,590 XP",10);detail.Location=Design.Point(180,64);top.Controls.Add(detail);
            var meter=new JournalProgress {Location=Design.Point(180,100),Size=Design.Size(360,8),Maximum=SangmonGrowth.NeededXP(SangmonGrowth.Level(book.Progress)),Value=SangmonGrowth.IntoLevel(book.Progress),BackColor=Design.FriendCard};top.Controls.Add(meter);
            var navigation=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=Design.P(80),Padding=Design.Pad(12,0,8,0),WrapContents=true};
            string[] names={"상몬 64종","소품 300종","소품 뽑기","조각 합성·성취","오늘의 미션·출석"};
            for(int i=0;i<names.Length;i++){int index=i;var button=Design.Button(names[i],i==0);button.AutoSize=true;button.Click+=delegate {if(index!=0){Destination=index;DialogResult=DialogResult.OK;Close();}};navigation.Controls.Add(button);}
            top.Controls.Add(navigation);
            cards=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,Padding=Design.Pad(16),WrapContents=true};
            foreach(var variant in MonsterVariants.All){var choice=variant;bool available=SangmonGrowth.CanUse(book.Progress,choice);
                var card=new Button {Size=Design.Size(142,174),Margin=Design.Pad(6),FlatStyle=FlatStyle.Flat,BackColor=book.CharacterStyle==MonsterVariants.Id(choice)?Design.Selection:Design.Paper,ForeColor=Design.Ink,Font=Design.Font(9),TextImageRelation=TextImageRelation.ImageAboveText,Enabled=available,AccessibleName=MonsterVariants.Name(choice)};card.FlatAppearance.BorderColor=Design.Soft;
                using(var outline=Design.Rounded(new RectangleF(0,0,card.Width,card.Height),Design.P(16)))card.Region=new Region(outline);
                var image=new Bitmap(Design.P(116),Design.P(110));using(var g=Graphics.FromImage(image))MonsterPainter.Draw(g,new Rectangle(Point.Empty,image.Size),MonsterPose.ForActivity(.3,PetActivity.Rest,.3,0,0),true,choice);card.Image=image;
                card.Text=MonsterVariants.Name(choice)+"\n"+(available?"선택하기":"총 "+SangmonGrowth.RequiredXP(choice)+" XP에 해금");
                card.Click+=delegate {if(select(choice)){avatar.Variant=choice;foreach(Button other in cards.Controls)other.BackColor=Design.Paper;card.BackColor=Design.Selection;}};
                cards.Controls.Add(card);
            }
            Controls.Add(cards);Controls.Add(top);
        }
        internal void CaptureForTest(string path)
        {
            Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(path);}Close();
        }
        protected override void Dispose(bool disposing){if(disposing && cards!=null)foreach(Button card in cards.Controls)if(card.Image!=null)card.Image.Dispose();base.Dispose(disposing);}
    }
    public sealed partial class DiaryWindow
    {
        private void OpenCompanion()
        {
            if(!FlushSave())return;int destination;
            using(var hub=new CompanionHubWindow(book,EquipReward)){hub.ShowDialog(this);destination=hub.Destination;}
            if(destination==1)OpenItems();else if(destination==2)OpenCollection();else if(destination==3)OpenAtelier();else if(destination==4)OpenGrowth();
        }
        internal void VerifyCommonScreensForTest(string directory)
        {
            if(!testMode)throw new InvalidOperationException();
            using(var hub=new CompanionHubWindow(book,EquipReward))hub.CaptureForTest(Path.Combine(directory,"windows-companion-hub.png"));
            using(var settings=new PreferencesWindow(entry.Theme,SetThemeFromSettings,delegate{},delegate{},delegate{})){
                settings.Show(this);Application.DoEvents();using(var image=new Bitmap(settings.Width,settings.Height)){settings.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(directory,"windows-common-settings.png"));}settings.Close();
            }
        }
    }
}
