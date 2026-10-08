using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;

namespace MyDay.Windows.UI
{
    public sealed class DiaryWindow : Form
    {
        private readonly DiaryStore store;
        private DiaryBook book;
        private DiaryEntry entry;
        private DateTime date = DateTime.Today;
        private bool dirty, binding, exitRequested,editingLayout;
        private readonly Timer saveTimer = new Timer { Interval = 350 };
        private readonly DiaryBoard board;
        private readonly ChoiceButton layoutChoice=Design.Choice(108);
        private readonly Button editLayout=Design.Button("배치 편집"),arrangeLayout=Design.Button("자동 정리");
        private readonly Label layoutHint=Design.Label("",9);
        private readonly Label status = Design.Label("내 컴퓨터에 자동 저장", 9);
        private readonly Label dayTitle = Design.Label("", 23, true);
        private readonly Label summary = Design.Label("", 9);
        private readonly DateTimePicker picker = new DateTimePicker { Format=DateTimePickerFormat.Custom, CustomFormat="yyyy. MM. dd", Width=Design.P(135) };
        private readonly ChoiceButton theme = Design.Choice(92);
        private readonly ChoiceButton mood = Design.Choice(108);
        private readonly ChoiceButton characterChoice=Design.Choice(160);
        private readonly Button petToggle = Design.Button("캐릭터 숨기기");
        private readonly Panel content = new Panel { Dock=DockStyle.Fill, Padding=Design.Pad(30,24,28,12) };
        private readonly MonsterView avatar = new MonsterView();
        private DesktopPet pet;
        private readonly NotifyIcon tray;
        private readonly bool testMode;
        public DiaryWindow(DiaryStore store, DiaryBook book, bool testMode = false)
        {
            this.store=store; this.book=book; this.testMode=testMode;
            board=new DiaryBoard(QueueSave) { Dock=DockStyle.Fill };
            Text="마이데이 · 나만의 하루";
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(1220),work.Width-32),Math.Min(Design.P(860),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(860),work.Width-32),Math.Min(Design.P(570),work.Height-32));
            StartPosition=FormStartPosition.CenterScreen; Font=Design.Font(10); ForeColor=Design.Ink;
            AutoScaleMode=AutoScaleMode.None; DoubleBuffered=true;
            KeyPreview=true; KeyDown+=delegate(object sender,KeyEventArgs e) { if(editingLayout && e.KeyCode==Keys.Escape) { board.CancelPlacement(); e.Handled=true; } };
            var side = new Panel { Dock=DockStyle.Left, AutoScroll=true, Width=Design.P(210), BackColor=Color.White, Padding=Design.Pad(24) };
            var brand=Design.Label("myday.",29,true); brand.Location=Design.Point(24,28); side.Controls.Add(brand);
            var tagline=Design.Label("하루를 담는 나만의 공간",9); tagline.ForeColor=Design.Muted; tagline.Location=Design.Point(24,90); side.Controls.Add(tagline);
            var sideLine=new Panel { BackColor=Design.Soft, Location=Design.Point(24,132), Size=Design.Size(158,1) }; side.Controls.Add(sideLine);
            var nav=Design.Label("나의 일기",11,true); nav.ForeColor=Design.Accent; nav.Location=Design.Point(26,158); side.Controls.Add(nav);
            var note=Design.Label("작은 기록이 모여\n나만의 하루가 돼요.",10); note.ForeColor=Design.Muted; note.Location=Design.Point(26,194); side.Controls.Add(note);
            avatar.Location=Design.Point(10,272); avatar.Size=Design.Size(186,155); side.Controls.Add(avatar);
            var petHint=Design.Label("눌러서 불꽃 인사하기",9); petHint.Location=Design.Point(34,430); petHint.ForeColor=Design.Muted; side.Controls.Add(petHint);
            characterChoice.Location=Design.Point(24,466); characterChoice.AccessibleName="캐릭터 버전";
            characterChoice.Items.AddRange(MonsterVariants.All.Select(v=>(object)MonsterVariants.Name(v)));
            characterChoice.SelectedIndexChanged+=delegate { if(!binding) SelectVariant((MonsterVariant)characterChoice.SelectedIndex); }; side.Controls.Add(characterChoice);
            petToggle.Location=Design.Point(24,518); petToggle.Width=Design.P(160); petToggle.Click+=delegate { TogglePet(); }; side.Controls.Add(petToggle);
            var hide=Design.Button("일기 창 숨기기"); hide.Location=Design.Point(24,562); hide.Width=Design.P(160); hide.Click+=delegate { if (FlushSave()) Hide(); }; side.Controls.Add(hide);
            var bottom = new FlowLayoutPanel { Location=Design.Point(24,636), Width=Design.P(160), Height=Design.P(148), FlowDirection=FlowDirection.TopDown, WrapContents=false };
            var export=Design.Button("기록 백업"); export.Width=Design.P(160); export.Margin=Design.Pad(0,0,0,8); export.Click+=delegate { ExportBook(); };
            var import=Design.Button("백업 가져오기"); import.Width=Design.P(160); import.Margin=Design.Pad(0,0,0,12); import.Click+=delegate { ImportBook(); };
            status.MaximumSize=Design.Size(160,0); status.ForeColor=Design.Muted;
            bottom.Controls.AddRange(new Control[] { export,import,status }); side.Controls.Add(bottom);
            var grid=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=5, Margin=Design.Pad(0), BackColor=Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(105))); grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(68)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(48)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(50))); grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var header=new Panel { Dock=DockStyle.Fill }; dayTitle.Location=Design.Point(0,0); header.Controls.Add(dayTitle);
            var subtitle=Design.Label("오늘을 원하는 조각으로 채워보세요.",10); subtitle.ForeColor=Design.Muted; subtitle.Location=Design.Point(2,46); header.Controls.Add(subtitle);
            var dates=new FlowLayoutPanel { AutoSize=true, FlowDirection=FlowDirection.LeftToRight, Top=Design.P(74), Left=Design.P(0), Height=Design.P(28), WrapContents=false };
            var prev=Design.Button("‹"); prev.Size=Design.Size(30,26); prev.AccessibleName="이전 날짜"; prev.Click+=delegate { ChangeDate(date.AddDays(-1)); };
            var next=Design.Button("›"); next.Size=Design.Size(30,26); next.AccessibleName="다음 날짜"; next.Click+=delegate { ChangeDate(date.AddDays(1)); };
            var today=Design.Button("오늘"); today.Size=Design.Size(58,26); today.Click+=delegate { ChangeDate(DateTime.Today); };
            picker.Font=Design.Font(9); picker.AccessibleName="일기 날짜";
            picker.ValueChanged+=delegate { if (!binding) ChangeDate(picker.Value.Date); };
            dates.Controls.AddRange(new Control[] { prev,picker,next,today }); header.Controls.Add(dates); grid.Controls.Add(header,0,0);
            var appearance=new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=Design.Pad(0,18,0,0), WrapContents=false };
            var moodLabel=Design.Label("오늘의 마음",9); moodLabel.Margin=Design.Pad(0,4,10,0);
            mood.Items.AddRange(new object[] { "평온해요","행복해요","피곤해요","속상해요","설레요" }); mood.AccessibleName="오늘의 마음";
            mood.SelectedIndexChanged+=delegate { if (!binding) { entry.Mood=(string)mood.SelectedItem; QueueSave(); } };
            var themeLabel=Design.Label("배경",9); themeLabel.Margin=Design.Pad(26,4,10,0);
            theme.Items.AddRange(new object[] { "크림","세이지","라벤더" }); theme.AccessibleName="배경 테마";
            theme.SelectedIndexChanged+=delegate { if (!binding) { entry.Theme=theme.SelectedIndex; ApplyTheme(); QueueSave(); } };
            appearance.Controls.AddRange(new Control[] { moodLabel,mood,themeLabel,theme }); grid.Controls.Add(appearance,0,1);
            var layoutBar=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false,Padding=Design.Pad(0,5,0,0) };
            var layoutLabel=Design.Label("배치",9); layoutLabel.Margin=Design.Pad(0,7,10,0);
            layoutChoice.Items.AddRange(new object[] {"자동 정렬","자유 배치"}); layoutChoice.AccessibleName="일기 블록 배치 방식";
            layoutChoice.SelectedIndexChanged+=delegate { if(!binding) SelectLayout(layoutChoice.SelectedIndex==1); };
            editLayout.Click+=delegate {
                board.CancelPlacement();
                if(entry.LayoutMode!="free") SelectLayout(true);
                editingLayout=!editingLayout; RefreshLayoutControls();
                foreach(var card in board.Controls.OfType<BlockCard>()) card.SetEditing(editingLayout,true);
            };
            arrangeLayout.Click+=delegate { board.CancelPlacement(); DiaryLayout.ArrangeFree(entry,board.LogicalWidth); QueueSave(); ArrangeCards(); };
            layoutHint.ForeColor=Design.Muted; layoutHint.Margin=Design.Pad(2,8,0,0);
            layoutBar.Controls.AddRange(new Control[] {layoutLabel,layoutChoice,editLayout,arrangeLayout,layoutHint}); grid.Controls.Add(layoutBar,0,2);
            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false, Padding=Design.Pad(0,5,0,0) };
            string[] kinds={"text","todo","habit","emotion"}; string[] names={"＋ 글 일기","＋ 할 일","＋ 습관","＋ 감정 기록"};
            for(int i=0;i<kinds.Length;i++) { string kind=kinds[i]; var add=Design.Button(names[i]); add.Click+=delegate { AddBlock(kind); }; toolbar.Controls.Add(add); }
            summary.Margin=Design.Pad(8,9,0,0); summary.ForeColor=Design.Muted; toolbar.Controls.Add(summary); grid.Controls.Add(toolbar,0,3);
            grid.Controls.Add(board,0,4); content.Controls.Add(grid); Controls.Add(content); Controls.Add(side);
            board.SizeChanged+=delegate { ArrangeCards(); };
            saveTimer.Tick+=delegate { FlushSave(); };
            tray=new NotifyIcon { Icon=SystemIcons.Application, Text="MyDay · 일기와 불꽃 몬스터", Visible=!testMode };
            var trayMenu=new ContextMenuStrip(); trayMenu.Items.Add("일기 열기",null,delegate { OpenDiary(); });
            trayMenu.Items.Add("캐릭터 표시 / 숨기기",null,delegate { TogglePet(); });
            trayMenu.Items.Add("모두 종료",null,delegate { ExitApp(); }); tray.ContextMenuStrip=trayMenu;
            tray.DoubleClick+=delegate { OpenDiary(); };
            avatar.Fired+=delegate { if (pet!=null) pet.Fire(); };
            FormClosing+=delegate(object sender,FormClosingEventArgs e) {
                board.CancelPlacement();
                if(!FlushSave()) { e.Cancel=true; return; }
                if(!exitRequested && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); }
            };
            LoadDate();
        }
        public void StartOnDesktop()
        {
            // Keep a hidden window handle so tray Exit raises FormClosed even before the first click.
            if(!IsHandleCreated) CreateHandle();
            EnsurePet(); if(!testMode) pet.Show(); petToggle.Text="캐릭터 숨기기";
        }
        public void ExitApp()
        {
            exitRequested=true; Close(); if(!IsDisposed) exitRequested=false;
        }
        private void EnsurePet()
        {
            if(pet!=null && !pet.IsDisposed) return;
            pet=new DesktopPet(OpenDiary);
            pet.Variant=MonsterVariants.FromId(book.CharacterStyle);
            pet.VariantChanged+=delegate { if(!binding) SelectVariant(pet.Variant); };
            pet.PetHidden+=delegate { petToggle.Text="캐릭터 띄우기"; };
        }
        public void OpenDiary() { Show(); if(WindowState==FormWindowState.Minimized) WindowState=FormWindowState.Normal; Activate(); }
        public void SelectVariant(MonsterVariant variant)
        {
            book.CharacterStyle=MonsterVariants.Id(variant); ApplyVariant(); QueueBookSave();
        }
        private void ApplyVariant()
        {
            bool previous=binding; binding=true;
            var variant=MonsterVariants.FromId(book.CharacterStyle);
            characterChoice.SelectedIndex=(int)variant; avatar.Variant=variant;
            if(pet!=null) pet.Variant=variant;
            binding=previous;
        }
        private void TogglePet()
        {
            EnsurePet(); if(pet.Visible) { pet.Hide(); petToggle.Text="캐릭터 띄우기"; }
            else { pet.Show(); petToggle.Text="캐릭터 숨기기"; }
        }
        public void ChangeDate(DateTime target)
        {
            if(target.Date<picker.MinDate.Date || target.Date>picker.MaxDate.Date) return;
            board.CancelPlacement();
            if(!FlushSave()) { binding=true; picker.Value=date; binding=false; return; }
            date=target.Date; LoadDate();
        }
        private void LoadDate()
        {
            if(!book.Days.TryGetValue(DiaryStore.Key(date),out entry)) entry=DiaryEntry.FirstPage();
            editingLayout=false;
            if(entry.LayoutMode=="free") DiaryLayout.EnableFree(entry,board.LogicalWidth);
            binding=true; picker.Value=date;
            dayTitle.Text=date.ToString("M월 d일, dddd",new System.Globalization.CultureInfo("ko-KR"));
            mood.SelectedIndex=Math.Max(0,mood.Items.IndexOf(entry.Mood)); theme.SelectedIndex=entry.Theme;
            layoutChoice.SelectedIndex=entry.LayoutMode=="free"?1:0;
            binding=false; RefreshLayoutControls(); ApplyTheme(); ApplyVariant(); RenderCards();
        }
        private void SelectLayout(bool free)
        {
            board.CancelPlacement(); editingLayout=false;
            if(free) DiaryLayout.EnableFree(entry,board.LogicalWidth); else entry.LayoutMode="cards";
            bool previous=binding; binding=true; layoutChoice.SelectedIndex=free?1:0; binding=previous;
            RefreshLayoutControls(); QueueSave(); RenderCards();
        }
        private void RefreshLayoutControls()
        {
            editLayout.Text=editingLayout?"편집 완료":"배치 편집";
            editLayout.BackColor=editingLayout?Design.Accent:Color.White; editLayout.ForeColor=editingLayout?Color.White:Design.Ink;
            arrangeLayout.Enabled=entry.LayoutMode=="free";
            layoutHint.Text=editingLayout?"제목으로 이동 · ↘로 크기 조절":entry.LayoutMode=="free"?"위치·크기 자동 저장":"창 크기에 맞춰 정렬";
        }
        private void ApplyTheme() { BackColor=Design.Backgrounds[entry.Theme]; content.BackColor=BackColor; board.BackColor=BackColor; }
        private void RenderCards()
        {
            board.CancelPlacement();
            board.SuspendLayout();
            foreach(Control control in board.Controls.Cast<Control>().ToArray()) control.Dispose();
            board.Controls.Clear();
            foreach(var block in entry.Blocks)
            {
                var item=block;
                var card=new BlockCard(item,QueueSave,delegate(int step) { MoveBlock(item,step); },delegate { RemoveBlock(item); },delegate { item.Wide=!item.Wide; QueueSave(); RenderCards(); });
                card.SetEditing(editingLayout,entry.LayoutMode=="free"); board.AddCard(card);
            }
            if(entry.Blocks.Count==0) { var empty=Design.Label("빈 페이지예요. 위에서 원하는 블록을 추가해보세요.",11); empty.Margin=Design.Pad(20); board.Controls.Add(empty); }
            board.ResumeLayout(false); ArrangeCards(); UpdateSummary();
        }
        private void ArrangeCards()
        {
            if(entry!=null) board.Arrange(entry);
        }
        public void AddBlock(string kind)
        {
            if(entry.Blocks.Count>=200) { MessageBox.Show(this,"한 날짜에는 최대 200개 블록을 넣을 수 있어요."); return; }
            var block=DiaryBlock.Create(kind); entry.Blocks.Add(block);
            if(entry.LayoutMode=="free") DiaryLayout.PlaceNew(entry,block);
            QueueSave(); RenderCards();
            var last=board.Controls.OfType<BlockCard>().First(c=>c.Block==block); board.ScrollControlIntoView(last); if(!editingLayout) last.Editor.Focus();
        }
        public void MoveBlock(DiaryBlock block,int step)
        {
            int index=entry.Blocks.IndexOf(block), target=index+step;
            if(index<0 || target<0 || target>=entry.Blocks.Count) return;
            entry.Blocks.RemoveAt(index); entry.Blocks.Insert(target,block); QueueSave(); RenderCards();
        }
        private void RemoveBlock(DiaryBlock block)
        {
            if(MessageBox.Show(this,"이 블록을 삭제할까요? 적은 내용도 함께 삭제됩니다.","블록 삭제",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
            entry.Blocks.Remove(block); QueueSave(); RenderCards();
        }
        private void QueueSave()
        {
            book.Days[DiaryStore.Key(date)]=entry; QueueBookSave();
        }
        private void QueueBookSave()
        {
            dirty=true; status.Text="저장 중…"; status.ForeColor=Design.Muted;
            saveTimer.Stop(); saveTimer.Start(); UpdateSummary();
        }
        private void UpdateSummary()
        {
            var checks=entry.Blocks.Where(b=>b.Kind=="todo" || b.Kind=="habit").ToList();
            summary.Text=checks.Count==0?"":checks.Count(b=>b.Checked)+" / "+checks.Count+" 완료";
        }
        public bool FlushSave()
        {
            saveTimer.Stop(); if(!dirty) return true;
            try { store.Save(book); dirty=false; status.Text="저장됨 · "+DateTime.Now.ToString("HH:mm"); status.ForeColor=Design.Accent; return true; }
            catch(Exception ex)
            {
                if(testMode) throw new IOException("Native smoke-test save failed.",ex);
                status.Text="저장 실패 · 다시 시도 필요"; status.ForeColor=Color.Firebrick;
                MessageBox.Show(this,"기록을 저장하지 못했어요. 창을 닫기 전에 디스크 공간과 폴더 권한을 확인해주세요.\n\n"+ex.Message,"저장 오류",MessageBoxButtons.OK,MessageBoxIcon.Error); return false;
            }
        }
        private void ExportBook()
        {
            if(!FlushSave()) return;
            using(var dialog=new SaveFileDialog { Filter="MyDay 백업 (*.json)|*.json", FileName="myday-"+DateTime.Today.ToString("yyyyMMdd")+".json" })
                if(dialog.ShowDialog(this)==DialogResult.OK)
                    try { store.Export(dialog.FileName,book); status.Text="백업 완료"; }
                    catch(Exception ex) { MessageBox.Show(this,"백업에 실패했어요.\n"+ex.Message); }
        }
        private void ImportBook()
        {
            if(!FlushSave()) return;
            using(var dialog=new OpenFileDialog { Filter="MyDay 백업 (*.json)|*.json", CheckFileExists=true })
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK) return;
                try
                {
                    DiaryBook incoming;
                    using(var stream=File.OpenRead(dialog.FileName)) { if(stream.Length>50*1024*1024) throw new InvalidDataException("파일이 너무 큽니다."); incoming=DiaryStore.Read(stream); }
                    var conflicts=incoming.Days.Keys.Count(book.Days.ContainsKey);
                    if(MessageBox.Show(this,incoming.Days.Count+"일의 기록을 가져올까요?\n같은 날짜의 기록 "+conflicts+"개는 백업 내용으로 바뀝니다. 기존 파일은 diary.json.bak으로 남습니다.","백업 가져오기",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
                    var merged=new DiaryBook(); foreach(var pair in book.Days) merged.Days[pair.Key]=pair.Value;
                    merged.CharacterStyle=incoming.CharacterStyle;
                    foreach(var pair in incoming.Days) merged.Days[pair.Key]=pair.Value;
                    store.Save(merged); book=merged; dirty=false; LoadDate(); status.Text="가져오기 완료";
                }
                catch(Exception ex) { MessageBox.Show(this,"기록을 가져오지 못했어요.\n"+ex.Message,"가져오기 오류",MessageBoxButtons.OK,MessageBoxIcon.Error); }
            }
        }
        public void SmokeTest(string destination)
        {
            mood.SelectedIndex=3; theme.SelectedIndex=1;
            var first=board.Controls.OfType<BlockCard>().First(c=>c.Block.Kind=="text");
            first.Editor.Text="윈도우에서도 나만의 하루를 기록해요.\r\n\r\n오늘은 작은 목표 하나를 끝냈어요. 창 옆에서는 불꽃 몬스터가 함께해요.";
            var check=board.Controls.OfType<BlockCard>().First(c=>c.Check!=null); check.Editor.Text="오늘의 일기 한 줄 쓰기"; check.Check.Checked=true;
            var emotion=board.Controls.OfType<BlockCard>().First(c=>c.Block.Kind=="emotion"); emotion.Editor.Text="처음이라 조금 떨리지만, 하나씩 만들어보려고 해.";
            string id=first.Block.Id; MoveBlock(first.Block,1); MoveBlock(entry.Blocks.First(b=>b.Id==id),-1);
            if(!FlushSave()) throw new Exception("UI save failed");
            ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            if(!entry.Blocks.First(b=>b.Id==id).Text.Contains("윈도우")) throw new Exception("Date navigation lost content");
            if(!store.Load().Days[DiaryStore.Key(date)].Blocks.Any(b=>b.Checked)) throw new Exception("Check did not persist");
            if(entry.Mood!="속상해요" || entry.Theme!=1 || mood.SelectedIndex!=3 || theme.SelectedIndex!=1) throw new Exception("Appearance did not persist");
            characterChoice.SelectedIndex=(int)MonsterVariant.Winged;
            if(avatar.Variant!=MonsterVariant.Winged || pet==null || pet.Variant!=MonsterVariant.Winged) throw new Exception("Diary variant choice did not update desktop pet");
            var menu=pet.ContextMenuStrip.Items.OfType<ToolStripMenuItem>().First(item=>item.Text=="캐릭터 버전");
            ((ToolStripMenuItem)menu.DropDownItems[(int)MonsterVariant.Mini]).PerformClick();
            if(characterChoice.SelectedIndex!=(int)MonsterVariant.Mini || avatar.Variant!=MonsterVariant.Mini) throw new Exception("Desktop variant menu did not update diary");
            foreach(var variant in MonsterVariants.All) {
                characterChoice.SelectedIndex=(int)variant;
                if(avatar.Variant!=variant || pet.Variant!=variant) throw new Exception("Diary choice missed "+variant);
                ((ToolStripMenuItem)menu.DropDownItems[(int)variant]).PerformClick();
                if(characterChoice.SelectedIndex!=(int)variant || avatar.Variant!=variant) throw new Exception("Desktop menu missed "+variant);
            }
            var games=pet.ContextMenuStrip.Items.OfType<ToolStripMenuItem>().First(item=>item.Text=="게임 스타일");
            var gameVariants=MonsterVariants.All.Where(GameSkins.IsGame).ToArray();
            if(games.DropDownItems.Count!=gameVariants.Length) throw new Exception("Game style shortcut count is wrong");
            for(int i=0;i<gameVariants.Length;i++) {
                ((ToolStripMenuItem)games.DropDownItems[i]).PerformClick();
                if(avatar.Variant!=gameVariants[i] || characterChoice.SelectedIndex!=(int)gameVariants[i]) throw new Exception("Game shortcut did not update diary");
                if(games.DropDownItems.OfType<ToolStripMenuItem>().Count(item=>item.Checked)!=1) throw new Exception("Game style checkmarks are out of sync");
            }
            characterChoice.SelectedIndex=(int)MonsterVariant.Mini;
            if(!FlushSave() || store.Load().CharacterStyle!="mini") throw new Exception("Variant did not save");
            characterChoice.SelectedIndex=(int)MonsterVariant.Frost;
            ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            if(characterChoice.SelectedIndex!=(int)MonsterVariant.Frost) throw new Exception("Date change reset global variant");
            var original=Size; Size=MinimumSize; PerformLayout(); ArrangeCards();
            foreach(var card in board.Controls.OfType<BlockCard>())
                if(card.Width>board.ClientSize.Width || card.Editor.Width<=0 || card.Editor.Height<=0) throw new Exception("Small window layout overflow");
            Size=original; PerformLayout(); ArrangeCards();
            editLayout.PerformClick();
            if(entry.LayoutMode!="free" || !editingLayout) throw new Exception("Placement editor did not enable free mode");
            var positioned=board.Controls.OfType<BlockCard>().First(c=>c.Block.Id==id);
            var bounds=DiaryLayout.Bounds(positioned.Block);
            Point start=positioned.DragHandle.PointToScreen(new Point(Design.P(8),Design.P(8)));
            positioned.DragHandle.DragForTest(start,new Point(start.X+Design.P(35),start.Y+Design.P(42)),false);
            if(positioned.Block.X!=bounds.X+35 || positioned.Block.Y!=bounds.Y+42 || !positioned.Editor.Text.Contains("윈도우")) throw new Exception("Native header drag did not retain text or move block");
            bounds=DiaryLayout.Bounds(positioned.Block);
            start=positioned.ResizeHandle.PointToScreen(new Point(Design.P(4),Design.P(4)));
            positioned.ResizeHandle.DragForTest(start,new Point(start.X+Design.P(50),start.Y+Design.P(30)),false);
            if(positioned.Block.Width!=bounds.Width+50 || positioned.Block.Height!=bounds.Height+30) throw new Exception("Native grip did not resize block");
            bounds=DiaryLayout.Bounds(positioned.Block);
            start=positioned.DragHandle.PointToScreen(new Point(Design.P(8),Design.P(8)));
            positioned.DragHandle.DragForTest(start,new Point(start.X+Design.P(24),start.Y+Design.P(24)),true);
            if(DiaryLayout.Bounds(positioned.Block)!=bounds) throw new Exception("Cancelled placement changed saved geometry");
            var scrolledBlock=entry.Blocks.First(b=>b.Kind=="emotion");
            DiaryLayout.SetBounds(scrolledBlock,new Rectangle(60,900,350,288)); RenderCards();
            var scrolledCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==scrolledBlock);
            board.ScrollControlIntoView(scrolledCard);
            if(board.AutoScrollPosition.Y>=0) throw new Exception("Free canvas did not scroll to distant block");
            start=scrolledCard.DragHandle.PointToScreen(new Point(Design.P(8),Design.P(8)));
            scrolledCard.DragHandle.DragForTest(start,new Point(start.X+Design.P(21),start.Y+Design.P(17)),false);
            if(scrolledBlock.X!=81 || scrolledBlock.Y!=917) throw new Exception("Scrolled drag used viewport coordinates instead of saved coordinates");
            foreach(var card in board.Controls.OfType<BlockCard>())
                if(!card.DeleteButton.Visible || card.DeleteButton.Right>card.DeleteButton.Parent.ClientSize.Width) throw new Exception("Free placement hid or clipped delete button");
            if(!FlushSave()) throw new Exception("Placement save failed");
            ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            if(entry.LayoutMode!="free" || DiaryLayout.Bounds(entry.Blocks.First(b=>b.Id==id))!=bounds || editingLayout) throw new Exception("Date change lost placement or retained edit lock");
            var diskEntry=store.Load().Days[DiaryStore.Key(date)];
            if(diskEntry.LayoutMode!="free" || DiaryLayout.Bounds(diskEntry.Blocks.First(b=>b.Id==id))!=bounds) throw new Exception("Disk reload lost placement");
            original=Size; Size=MinimumSize; PerformLayout(); ArrangeCards();
            if(DiaryLayout.Bounds(entry.Blocks.First(b=>b.Id==id))!=bounds || board.AutoScrollMinSize.Width<positioned.Block.Width) throw new Exception("Small window changed saved placement or lost scrolling");
            Size=original; PerformLayout(); ArrangeCards();
            // Recompose this example freely to demonstrate positions independent of the card grid.
            var textBlock=entry.Blocks.First(b=>b.Id==id); DiaryLayout.SetBounds(textBlock,new Rectangle(18,12,470,340));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="todo"),new Rectangle(510,34,300,220));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="habit"),new Rectangle(500,284,310,200));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="emotion"),new Rectangle(34,386,450,280));
            ArrangeCards(); board.AutoScrollPosition=Point.Empty; QueueSave(); FlushSave();
            avatar.Fire(); PerformLayout(); ArrangeCards(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(destination,System.Drawing.Imaging.ImageFormat.Png); }
            editLayout.PerformClick(); PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-free-layout.png"),System.Drawing.Imaging.ImageFormat.Png); }
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing) { saveTimer.Dispose(); tray.Visible=false; if(tray.ContextMenuStrip!=null) tray.ContextMenuStrip.Dispose(); tray.Dispose(); if(pet!=null) pet.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
