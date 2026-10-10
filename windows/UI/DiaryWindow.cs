using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MyDay.Windows.Core;
using MyDay.Windows.Character;

namespace MyDay.Windows.UI
{
    public sealed partial class DiaryWindow : Form
    {
        private readonly DiaryStore store;
        private DiaryBook book;
        private DiaryEntry entry;
        private System.Collections.Generic.List<DiaryBlock> savedRecord;
        private DateTime date = DateTime.Today;
        private int historyPage;
        private bool dirty, entryDirty, binding, exitRequested,editingLayout;
        private readonly Timer saveTimer = new Timer { Interval = 350 };
        private readonly DiaryBoard board;
        private readonly ChoiceButton layoutChoice=Design.Choice(86);
        private readonly ChoiceButton pageStyle=Design.Choice(86);
        private readonly Button templates=Design.Button("일기 형식",true);
        private readonly Button myLayouts=Design.Button("내 레이아웃");
        private readonly Button browse=Design.Button("달력 · 일기 검색");
        private readonly Button growth=Design.Button("상몬 성장");
        private readonly Button items=Design.Button("아이템");
        private readonly Button editLayout=Design.Button("배치 편집"),arrangeLayout=Design.Button("자동 정리");
        private readonly Label layoutHint=Design.Label("",9);
        private readonly Label status = Design.Label("내 컴퓨터에 자동 저장", 9);
        private readonly Label dayTitle = Design.Label("", 22, true);
        private readonly FlowLayoutPanel history = new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false, AutoScroll=true, BackColor=Color.White, Padding=Design.Pad(12,8,4,0) };
        private readonly Label summary = Design.Label("", 9);
        private readonly DateTimePicker picker = new DateTimePicker { Format=DateTimePickerFormat.Custom, CustomFormat="yyyy. MM. dd", Width=Design.P(135) };
        private readonly ChoiceButton theme = Design.Choice(92);
        private readonly ChoiceButton mood = Design.Choice(108);
        private readonly ChoiceButton characterChoice=Design.Choice(160);
        private readonly Button petToggle = Design.Button("캐릭터 숨기기");
        private readonly Panel content = new Panel { Dock=DockStyle.Fill, Padding=Design.Pad(24,18,20,12) };
        private readonly MonsterView avatar = new MonsterView();
        private DesktopPet pet;
        private readonly NotifyIcon tray;
        private readonly bool testMode;
        public DiaryWindow(DiaryStore store, DiaryBook book, bool testMode = false)
        {
            this.store=store; this.book=book; this.testMode=testMode;
            board=new DiaryBoard(QueueSave) { Dock=DockStyle.Fill };
            Text="MyDay · 나의 작은 일기장";
            var work=Screen.PrimaryScreen.WorkingArea;
            Size=new Size(Math.Min(Design.P(1220),work.Width-32),Math.Min(Design.P(860),work.Height-32));
            MinimumSize=new Size(Math.Min(Design.P(860),work.Width-32),Math.Min(Design.P(570),work.Height-32));
            StartPosition=FormStartPosition.CenterScreen; Font=Design.Font(10); ForeColor=Design.Ink;
            AutoScaleMode=AutoScaleMode.None; DoubleBuffered=true;
            KeyPreview=true; KeyDown+=delegate(object sender,KeyEventArgs e) {
                if(editingLayout && e.KeyCode==Keys.Escape) { board.CancelPlacement(); e.Handled=true; }
                if(e.Control && e.KeyCode==Keys.F) { e.Handled=true; e.SuppressKeyPress=true; BeginInvoke((MethodInvoker)delegate { BrowseDiary(); }); }
            };
            var side = new Panel { Dock=DockStyle.Left, Width=Design.P(260), BackColor=Color.White };
            var sideHeader=new Panel { Dock=DockStyle.Top, Height=Design.P(204), BackColor=Color.White };
            var brand=Design.Label("myday",20,true); brand.Location=Design.Point(24,20); sideHeader.Controls.Add(brand);
            var tagline=Design.Label("나의 작은 일기장",9); tagline.ForeColor=Design.Muted; tagline.Location=Design.Point(25,64); sideHeader.Controls.Add(tagline);
            var newDay=Design.Button("＋ 오늘 기록하기",true); newDay.Location=Design.Point(24,96); newDay.Width=Design.P(212);
            newDay.Click+=delegate { ChangeDate(DateTime.Today); }; sideHeader.Controls.Add(newDay);
            browse.Location=Design.Point(24,140); browse.Size=Design.Size(212,30); browse.AccessibleName="달력과 일기 검색 열기"; browse.Click+=delegate { BrowseDiary(); }; sideHeader.Controls.Add(browse);
            var historyTitle=Design.Label("기록함",9,true); historyTitle.Location=Design.Point(26,182); sideHeader.Controls.Add(historyTitle);
            var companion=new Panel { Dock=DockStyle.Bottom, Height=Design.P(252), BackColor=Color.White };
            var sideLine=new Panel { BackColor=Design.Soft, Dock=DockStyle.Top, Height=1 }; companion.Controls.Add(sideLine);
            avatar.Location=Design.Point(16,16); avatar.Size=Design.Size(64,58); companion.Controls.Add(avatar);
            var petHint=Design.Label("내 친구 상몬",10,true); petHint.Location=Design.Point(88,20); companion.Controls.Add(petHint);
            characterChoice.Location=Design.Point(88,46); characterChoice.Width=Design.P(148); characterChoice.AccessibleName="캐릭터 버전";
            characterChoice.Items.AddRange(MonsterVariants.All.Select(v=>(object)MonsterVariants.Name(v)));
            characterChoice.ItemEnabled=i=>SangmonGrowth.CanUse(book.Progress,(MonsterVariant)i);
            characterChoice.ItemLabel=i=>MonsterVariants.Name((MonsterVariant)i)+(characterChoice.ItemEnabled(i)?"":" · Lv."+SangmonGrowth.RequiredLevel((MonsterVariant)i));
            characterChoice.SelectedIndexChanged+=delegate { if(!binding) SelectVariant((MonsterVariant)characterChoice.SelectedIndex); }; companion.Controls.Add(characterChoice);
            growth.Location=Design.Point(24,86); growth.Size=Design.Size(140,28); growth.Font=Design.Font(8); growth.AccessibleName="상몬 성장과 보상 외형 보기"; growth.Click+=delegate { OpenGrowth(); }; companion.Controls.Add(growth);
            items.Location=Design.Point(172,86); items.Size=Design.Size(64,28); items.Font=Design.Font(8); items.AccessibleName="상몬 아이템 장착 열기"; items.Click+=delegate { OpenItems(); }; companion.Controls.Add(items);
            petToggle.Location=Design.Point(24,124); petToggle.Width=Design.P(103); petToggle.Font=Design.Font(8); petToggle.Click+=delegate { TogglePet(); }; companion.Controls.Add(petToggle);
            var hide=Design.Button("창 숨기기"); hide.Location=Design.Point(133,124); hide.Width=Design.P(103); hide.Click+=delegate { if (FlushSave()) Hide(); }; companion.Controls.Add(hide);
            var export=Design.Button("기록 백업"); export.Location=Design.Point(24,166); export.Width=Design.P(103); export.Click+=delegate { ExportBook(); }; companion.Controls.Add(export);
            var import=Design.Button("가져오기"); import.Location=Design.Point(133,166); import.Width=Design.P(103); import.Click+=delegate { ImportBook(); }; companion.Controls.Add(import);
            status.MaximumSize=Design.Size(216,0); status.Font=Design.Font(8); status.ForeColor=Design.Muted; status.Location=Design.Point(26,214); companion.Controls.Add(status);
            side.Controls.Add(history); side.Controls.Add(sideHeader); side.Controls.Add(companion);
            var divider=new Panel { Dock=DockStyle.Right, Width=1, BackColor=Design.Soft }; side.Controls.Add(divider);
            var grid=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=5, Margin=Design.Pad(0), BackColor=Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(126))); grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(44)));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(48)));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent,100)); grid.RowStyles.Add(new RowStyle(SizeType.Absolute,Design.P(62)));
            var header=new JournalCover { Dock=DockStyle.Fill, Margin=Design.Pad(0,0,0,12) }; dayTitle.Location=Design.Point(24,34); header.Controls.Add(dayTitle);
            var eyebrow=Design.Label("MY PERSONAL JOURNAL",8,true); eyebrow.ForeColor=Design.Accent; eyebrow.Location=Design.Point(25,14); header.Controls.Add(eyebrow);
            var subtitle=Design.Label("조용히 쌓이는, 나만의 시간",9); subtitle.ForeColor=Design.Muted; subtitle.Location=Design.Point(25,82); header.Controls.Add(subtitle);
            var dates=new FlowLayoutPanel { FlowDirection=FlowDirection.LeftToRight, Top=Design.P(49), Width=Design.P(270), Height=Design.P(32), WrapContents=false, Anchor=AnchorStyles.Top|AnchorStyles.Right, BackColor=header.BackColor };
            header.Resize+=delegate { dates.Left=header.ClientSize.Width-dates.Width-Design.P(12); dayTitle.MaximumSize=new Size(Math.Max(Design.P(100),dates.Left-Design.P(36)),Design.P(42)); };
            var prev=Design.Button("‹"); prev.Size=Design.Size(30,26); prev.AccessibleName="이전 날짜"; prev.Click+=delegate { ChangeDate(date.AddDays(-1)); };
            var next=Design.Button("›"); next.Size=Design.Size(30,26); next.AccessibleName="다음 날짜"; next.Click+=delegate { ChangeDate(date.AddDays(1)); };
            var today=Design.Button("오늘"); today.Size=Design.Size(58,26); today.Click+=delegate { ChangeDate(DateTime.Today); };
            picker.Font=Design.Font(9); picker.AccessibleName="일기 날짜";
            picker.ValueChanged+=delegate { if (!binding) ChangeDate(picker.Value.Date); };
            dates.Controls.AddRange(new Control[] { prev,picker,next,today }); header.Controls.Add(dates); grid.Controls.Add(header,0,0);
            var appearance=new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=Design.Pad(0,10,0,0), WrapContents=false };
            var moodLabel=Design.Label("오늘의 마음",9); moodLabel.Margin=Design.Pad(0,4,10,0);
            mood.Items.AddRange(new object[] { "평온해요","행복해요","피곤해요","속상해요","설레요" }); mood.AccessibleName="오늘의 마음";
            mood.SelectedIndexChanged+=delegate { if (!binding) { entry.Mood=(string)mood.SelectedItem; QueueSave(); } };
            var themeLabel=Design.Label("배경",9); themeLabel.Margin=Design.Pad(26,4,10,0);
            theme.Items.AddRange(new object[] { "아이보리","화이트","미스트" }); theme.AccessibleName="배경 테마";
            theme.SelectedIndexChanged+=delegate { if (!binding) { entry.Theme=theme.SelectedIndex; ApplyTheme(); QueueSave(); } };
            pageStyle.Items.AddRange(new object[] {"노트","카드","도트"}); pageStyle.AccessibleName="일기 종이 스타일";
            pageStyle.Margin=Design.Pad(12,0,0,0);
            pageStyle.SelectedIndexChanged+=delegate { if(!binding) { entry.PageStyle=new[] {"paper","plain","dots"}[pageStyle.SelectedIndex]; QueueSave(); RenderCards(); } };
            appearance.Controls.AddRange(new Control[] { moodLabel,mood,themeLabel,theme,pageStyle }); grid.Controls.Add(appearance,0,1);
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
            layoutHint.ForeColor=Design.Muted; layoutHint.Font=Design.Font(8); layoutHint.Margin=Design.Pad(2,9,0,0);
            editLayout.Width=Design.P(72); arrangeLayout.Width=Design.P(72); templates.Width=Design.P(84); myLayouts.Width=Design.P(92);
            templates.Click+=delegate { using(var gallery=new TemplateGallery()) if(gallery.ShowDialog(this)==DialogResult.OK) ApplyTemplate(gallery.SelectedTemplate); };
            myLayouts.Click+=delegate { ManageLayouts(); };
            layoutBar.Controls.AddRange(new Control[] {layoutLabel,layoutChoice,editLayout,arrangeLayout,templates,myLayouts,layoutHint}); grid.Controls.Add(layoutBar,0,2);
            layoutBar.Resize+=delegate { layoutHint.Visible=layoutBar.ClientSize.Width>=Design.P(740); };
            var toolbar=new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false, Padding=Design.Pad(0,14,0,0) };
            string[] kinds={"text","todo","habit","emotion","photo"}; string[] names={"＋ 글 일기","＋ 할 일","＋ 습관","＋ 감정","＋ 사진"};
            for(int i=0;i<kinds.Length;i++) { string kind=kinds[i]; var add=Design.Button(names[i],i==0); add.Width=Design.P(88); add.Click+=delegate { if(kind=="photo") SelectPhoto(null); else AddBlock(kind); }; toolbar.Controls.Add(add); }
            summary.Margin=Design.Pad(4,9,0,0); summary.ForeColor=Design.Muted; toolbar.Controls.Add(summary); grid.Controls.Add(toolbar,0,4);
            toolbar.Resize+=delegate { summary.Visible=toolbar.ClientSize.Width>=Design.P(630); };
            grid.Controls.Add(board,0,3); content.Controls.Add(grid); Controls.Add(content); Controls.Add(side);
            board.SizeChanged+=delegate { ArrangeCards(); };
            saveTimer.Tick+=delegate { FlushSave(); };
            tray=new NotifyIcon { Icon=SystemIcons.Application, Text="MyDay · 일기와 불꽃 몬스터", Visible=!testMode };
            var trayMenu=new ContextMenuStrip(); trayMenu.Items.Add("일기 열기",null,delegate { OpenDiary(); });
            trayMenu.Items.Add("캐릭터 표시 / 숨기기",null,delegate { TogglePet(); });
            trayMenu.Items.Add("상몬 성장",null,delegate { OpenGrowth(); });
            trayMenu.Items.Add("아이템 장착",null,delegate { OpenItems(); });
            trayMenu.Items.Add("모두 종료",null,delegate { ExitApp(); }); tray.ContextMenuStrip=trayMenu;
            tray.DoubleClick+=delegate { OpenDiary(); };
            avatar.Fired+=delegate { if (pet!=null) pet.Fire(); };
            FormClosing+=delegate(object sender,FormClosingEventArgs e) {
                board.CancelPlacement();
                if(!FlushSave()) { e.Cancel=true; return; }
                if(!exitRequested && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); }
            };
            LoadDate(); UpdateGrowth();
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
            pet=new DesktopPet(OpenDiary,delegate { OpenGrowth(); },delegate { OpenItems(); });
            pet.SetAvailability(v=>SangmonGrowth.CanUse(book.Progress,v));
            pet.Variant=MonsterVariants.FromId(book.CharacterStyle);
            pet.Equipment=book.Equipment;
            pet.VariantChanged+=delegate { if(!binding) SelectVariant(pet.Variant); };
            pet.PetHidden+=delegate { petToggle.Text="캐릭터 띄우기"; };
        }
        public void OpenDiary() { Show(); if(WindowState==FormWindowState.Minimized) WindowState=FormWindowState.Normal; Activate(); }
        public void SelectVariant(MonsterVariant variant)
        {
            if(!SangmonGrowth.CanUse(book.Progress,variant)) { ApplyVariant(); return; }
            book.CharacterStyle=MonsterVariants.Id(variant); ApplyVariant(); QueueBookSave();
        }
        private void ApplyVariant()
        {
            bool previous=binding; binding=true;
            var variant=MonsterVariants.FromId(book.CharacterStyle);
            characterChoice.SelectedIndex=(int)variant; avatar.Variant=variant;
            ApplyEquipment();
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
        private void BrowseDiary(Action<JournalBrowser> shown=null)
        {
            board.CancelPlacement(); if(!FlushSave()) return;
            using(var browser=new JournalBrowser(book,date,picker.MinDate,picker.MaxDate)) {
                if(testMode) { browser.StartPosition=FormStartPosition.Manual; browser.Location=new Point(-30000,-30000); }
                if(shown!=null) browser.Shown+=delegate { browser.BeginInvoke((MethodInvoker)delegate { shown(browser); }); };
                if(browser.ShowDialog(this)==DialogResult.OK && browser.SelectedDate.HasValue) ChangeDate(browser.SelectedDate.Value);
            }
        }
        private void ManageLayouts(Action<MyLayoutsWindow> shown=null)
        {
            board.CancelPlacement(); if(!FlushSave()) return;
            using(var window=new MyLayoutsWindow(book,SaveCurrentLayout,delegate(SavedLayout layout,string name) { CommitLayouts(SavedLayouts.Rename(book.Layouts,layout.Id,name)); },
                delegate(SavedLayout layout) { CommitLayouts(book.Layouts.Where(l=>l.Id!=layout.Id).ToList()); })) {
                if(testMode) { window.StartPosition=FormStartPosition.Manual; window.Location=new Point(-30000,-30000); }
                if(shown!=null) window.Shown+=delegate { window.BeginInvoke((MethodInvoker)delegate { shown(window); }); };
                if(window.ShowDialog(this)==DialogResult.OK && window.SelectedLayout!=null)
                    try { ApplySavedLayout(window.SelectedLayout); } catch(Exception ex) { if(testMode) throw; MessageBox.Show(this,"배치를 불러오지 못했어요.\n"+ex.Message); }
            }
        }
        private SavedLayout SaveCurrentLayout(string name)
        {
            var saved=SavedLayouts.Capture(entry,name,board.LogicalWidth); CommitLayouts(SavedLayouts.Add(book.Layouts,saved)); return saved;
        }
        private void CommitLayouts(System.Collections.Generic.List<SavedLayout> layouts)
        {
            SavedLayouts.Validate(layouts); var previous=book.Layouts; int version=book.Version; book.Layouts=layouts;
            try { store.Save(book); status.Text="레이아웃 저장됨"; status.ForeColor=Design.Accent; }
            catch { book.Layouts=previous; book.Version=version; throw; }
        }
        private void ApplySavedLayout(SavedLayout layout)
        {
            board.CancelPlacement(); var previousIds=entry.Blocks.Select(b=>b.Id).ToArray(); SavedLayouts.Apply(entry,layout); editingLayout=false;
            bool previous=binding; binding=true;
            theme.SelectedIndex=entry.Theme; pageStyle.SelectedIndex=entry.PageStyle=="paper"?0:entry.PageStyle=="dots"?2:1; layoutChoice.SelectedIndex=entry.LayoutMode=="free"?1:0;
            binding=previous; RefreshLayoutControls(); ApplyTheme(); QueueSave(); RenderCards();
            var added=board.Controls.OfType<BlockCard>().First(c=>!previousIds.Contains(c.Block.Id)); board.ScrollControlIntoView(added); added.Editor.Focus();
        }
        private void LoadDate()
        {
            if(!book.Days.TryGetValue(DiaryStore.Key(date),out entry)) entry=DiaryTemplates.NewPage();
            savedRecord=SangmonGrowth.CaptureRecord(entry); entryDirty=false;
            editingLayout=false;
            if(entry.LayoutMode=="free") DiaryLayout.EnableFree(entry,board.LogicalWidth);
            binding=true; picker.Value=date;
            dayTitle.Text=date.ToString("M월 d일",new System.Globalization.CultureInfo("ko-KR"));
            mood.SelectedIndex=Math.Max(0,mood.Items.IndexOf(entry.Mood)); theme.SelectedIndex=entry.Theme;
            layoutChoice.SelectedIndex=entry.LayoutMode=="free"?1:0;
            pageStyle.SelectedIndex=entry.PageStyle=="paper"?0:entry.PageStyle=="dots"?2:1;
            binding=false; RefreshLayoutControls(); ApplyTheme(); ApplyVariant(); RenderCards(); RefreshHistory(true);
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
            layoutHint.Text=editingLayout?"제목 드래그 · ↘ 크기":entry.LayoutMode=="free"?"내가 꾸민 페이지":"창에 맞춰 정렬";
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
                var card=new BlockCard(item,QueueSave,delegate(int step) { MoveBlock(item,step); },delegate { RemoveBlock(item); },delegate { item.Wide=!item.Wide; QueueSave(); RenderCards(); },entry.PageStyle,delegate { SelectPhoto(item); });
                card.SetEditing(editingLayout,entry.LayoutMode=="free"); board.AddCard(card);
            }
            if(entry.Blocks.Count==0) { var empty=Design.Label("빈 페이지예요. 아래에서 원하는 블록을 추가해보세요.",11); empty.Margin=Design.Pad(20); board.Controls.Add(empty); }
            board.ResumeLayout(false); ArrangeCards(); UpdateSummary();
        }
        private void ArrangeCards()
        {
            if(entry!=null) board.Arrange(entry);
        }
        public void AddBlock(string kind)
        {
            if(kind=="photo") { SelectPhoto(null); return; }
            if(entry.Blocks.Count>=200) { MessageBox.Show(this,"한 날짜에는 최대 200개 블록을 넣을 수 있어요."); return; }
            var block=DiaryBlock.Create(kind); entry.Blocks.Add(block);
            if(entry.LayoutMode=="free") DiaryLayout.PlaceNew(entry,block);
            QueueSave(); RenderCards();
            var last=board.Controls.OfType<BlockCard>().First(c=>c.Block==block); board.ScrollControlIntoView(last); if(!editingLayout) last.Editor.Focus();
        }
        private void SelectPhoto(DiaryBlock existing)
        {
            using(var dialog=new OpenFileDialog { Title=existing==null?"일기에 사진 추가":"사진 바꾸기",Filter="사진 (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",CheckFileExists=true }) {
                if(dialog.ShowDialog(this)!=DialogResult.OK) return;
                try { SetPhotoFromFile(dialog.FileName,existing); }
                catch(Exception ex) { MessageBox.Show(this,"사진을 추가하지 못했어요.\n"+ex.Message,"사진 추가",MessageBoxButtons.OK,MessageBoxIcon.Error); }
            }
        }
        private DiaryBlock SetPhotoFromFile(string path,DiaryBlock existing=null)
        {
            if(existing==null && entry.Blocks.Count>=200) throw new InvalidDataException("한 날짜에는 최대 200개 블록을 넣을 수 있어요.");
            var candidate=DiaryBlock.Create("photo"); candidate.Photo=DiaryPhoto.FromFile(path); DiaryPhoto.Validate(candidate);
            long total=book.Days.Where(p=>p.Key!=DiaryStore.Key(date)).Sum(p=>p.Value.Blocks.Sum(b=>b.Photo==null?0L:b.Photo.Length))+
                entry.Blocks.Where(b=>b!=existing).Sum(b=>b.Photo==null?0L:b.Photo.Length)+candidate.Photo.Length;
            if(total>DiaryPhoto.MaxBookCharacters) throw new InvalidDataException("전체 사진 저장 공간이 가득 찼어요. 백업 후 사용하지 않는 사진 블록을 정리해주세요.");
            var block=existing??candidate;
            if(existing==null) { entry.Blocks.Add(block); if(entry.LayoutMode=="free") DiaryLayout.PlaceNew(entry,block); }
            else { existing.Kind="photo"; existing.Photo=candidate.Photo; existing.ValidatedPhoto=candidate.ValidatedPhoto; }
            QueueSave(); RenderCards();
            var card=board.Controls.OfType<BlockCard>().First(c=>c.Block==block); board.ScrollControlIntoView(card); if(!editingLayout) card.Editor.Focus();
            return block;
        }
        private void ApplyTemplate(JournalTemplate template)
        {
            board.CancelPlacement();
            bool untouched=!book.Days.ContainsKey(DiaryStore.Key(date)) && entry.LayoutMode=="cards" && entry.Blocks.Count==1 &&
                entry.Blocks[0].Text.Length==0 && entry.Blocks[0].Title==DiaryTemplates.All[0].Sections[0].Title && entry.Blocks[0].Width==0;
            bool fresh=untouched || entry.Blocks.Count==0;
            if(!DiaryTemplates.Append(entry,template,board.LogicalWidth)) { MessageBox.Show(this,"블록은 한 날짜에 최대 200개까지 추가할 수 있어요."); return; }
            if(untouched) entry.Blocks.RemoveAt(0);
            if(fresh) {
                entry.LayoutMode="free";
                var positions=JournalLayouts.Arrange(template.Layout,entry.Blocks.Count,board.LogicalWidth);
                for(int i=0;i<entry.Blocks.Count;i++) DiaryLayout.SetBounds(entry.Blocks[i],positions[i]);
                bool wasBinding=binding; binding=true; layoutChoice.SelectedIndex=1; binding=wasBinding; RefreshLayoutControls();
            }
            entry.PageStyle=template.Style;
            bool previous=binding; binding=true; pageStyle.SelectedIndex=entry.PageStyle=="paper"?0:entry.PageStyle=="dots"?2:1; binding=previous;
            QueueSave(); RenderCards();
            var firstAdded=entry.Blocks[entry.Blocks.Count-template.Sections.Length];
            var card=board.Controls.OfType<BlockCard>().First(c=>c.Block==firstAdded); board.ScrollControlIntoView(card); if(!editingLayout) card.Editor.Focus();
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
            entryDirty=!SangmonGrowth.SameRecord(savedRecord,entry); book.Days[DiaryStore.Key(date)]=entry; QueueBookSave();
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
            return FlushSaveAt(DateTime.Today);
        }
        private bool FlushSaveAt(DateTime actualDay)
        {
            saveTimer.Stop(); if(!dirty) return true;
            var previous=book.Progress; int previousVersion=book.Version,previousLevel=SangmonGrowth.Level(previous);
            if(entryDirty) book.Progress=SangmonGrowth.Award(previous,entry,actualDay);
            bool awarded=!ReferenceEquals(previous,book.Progress);
            try {
                store.Save(book); dirty=false; entryDirty=false; savedRecord=SangmonGrowth.CaptureRecord(entry); UpdateGrowth();
                status.Text=awarded?"저장됨 · 상몬 +10 XP":"저장됨 · "+DateTime.Now.ToString("HH:mm");
                if(SangmonGrowth.Level(book.Progress)>previousLevel) { status.Text="저장됨 · Lv."+SangmonGrowth.Level(book.Progress)+" 달성!"; if(pet!=null) pet.Celebrate(); }
                status.ForeColor=Design.Accent; RefreshHistory(); return true;
            }
            catch(Exception ex)
            {
                book.Progress=previous; book.Version=previousVersion;
                if(testMode) throw new IOException("Native smoke-test save failed.",ex);
                status.Text="저장 실패 · 다시 시도 필요"; status.ForeColor=Color.Firebrick;
                MessageBox.Show(this,"기록을 저장하지 못했어요. 창을 닫기 전에 디스크 공간과 폴더 권한을 확인해주세요.\n\n"+ex.Message,"저장 오류",MessageBoxButtons.OK,MessageBoxIcon.Error); return false;
            }
        }
        private void RefreshHistory(bool reveal=false)
        {
            var scroll=history.AutoScrollPosition;
            history.SuspendLayout();
            foreach(Control row in history.Controls.Cast<Control>().ToArray()) row.Dispose();
            var dates=book.Days.Keys.Select(key=>DateTime.ParseExact(key,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture))
                .Concat(new[] {date,DateTime.Today}).Distinct().OrderByDescending(value=>value).ToList();
            if(reveal) historyPage=dates.IndexOf(date)/60;
            historyPage=Math.Max(0,Math.Min(historyPage,(dates.Count-1)/60));
            foreach(var value in dates.Skip(historyPage*60).Take(60)) {
                DiaryEntry saved;
                string preview="새로운 하루를 기록해보세요";
                if(book.Days.TryGetValue(DiaryStore.Key(value),out saved)) {
                    var text=saved.Blocks.FirstOrDefault(block=>block.Kind=="text" && !string.IsNullOrWhiteSpace(block.Text))
                        ??saved.Blocks.FirstOrDefault(block=>!string.IsNullOrWhiteSpace(block.Text));
                    preview=text==null?"기록 "+saved.Blocks.Count+"개":text.Text.Substring(0,Math.Min(120,text.Text.Length)).Replace("\r"," ").Replace("\n"," ");
                }
                var target=value;
                DiaryEntry historyEntry;
                var row=new HistoryRow(target,preview,target==date,book.Days.TryGetValue(DiaryStore.Key(target),out historyEntry) && DiaryBrowse.HasPhoto(historyEntry)); row.Click+=delegate { ChangeDate(target); };
                history.Controls.Add(row);
            }
            if(dates.Count>60) {
                var pages=new FlowLayoutPanel { Width=Design.P(232), Height=Design.P(38), WrapContents=false };
                var prev=Design.Button("‹"); prev.Width=Design.P(36); prev.Enabled=historyPage>0;
                var next=Design.Button("›"); next.Width=Design.P(36); next.Enabled=(historyPage+1)*60<dates.Count;
                prev.AccessibleName="최근 기록 목록"; next.AccessibleName="이전 기록 목록";
                prev.Click+=delegate { historyPage--; RefreshHistory(); history.AutoScrollPosition=Point.Empty; };
                next.Click+=delegate { historyPage++; RefreshHistory(); history.AutoScrollPosition=Point.Empty; };
                var count=Design.Label((historyPage+1)+" / "+((dates.Count+59)/60),8); count.Margin=Design.Pad(8,10,8,0);
                pages.Controls.AddRange(new Control[] {prev,count,next}); history.Controls.Add(pages);
            }
            history.ResumeLayout(); history.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);
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
                    using(var stream=File.OpenRead(dialog.FileName)) incoming=DiaryStore.Read(stream);
                    var conflicts=incoming.Days.Keys.Count(book.Days.ContainsKey);
                    var layouts=SavedLayouts.Merge(book.Layouts,incoming.Layouts);
                    if(MessageBox.Show(this,incoming.Days.Count+"일의 기록과 내 레이아웃 "+incoming.Layouts.Count+"개를 가져올까요?\n같은 날짜의 기록 "+conflicts+"개와 같은 ID의 레이아웃은 백업 내용으로 바뀝니다. 이름이 겹치면 번호를 붙입니다. 기존 파일은 diary.json.bak으로 남습니다.","백업 가져오기",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
                    var merged=new DiaryBook(); foreach(var pair in book.Days) merged.Days[pair.Key]=pair.Value;
                    merged.CharacterStyle=incoming.CharacterStyle;
                    merged.Progress=SangmonGrowth.Merge(book.Progress,incoming.Progress);
                    merged.Equipment=SangmonItems.Import(book.Equipment,incoming.Equipment);
                    merged.Layouts=layouts; merged.Version=Math.Max(book.Version,incoming.Version);
                    foreach(var pair in incoming.Days) merged.Days[pair.Key]=pair.Value;
                    store.Save(merged); book=merged; dirty=false; entryDirty=false; UpdateGrowth(); LoadDate(); status.Text="가져오기 완료";
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
            var recordedDate=date;
            ChangeDate(date.AddDays(1));
            history.Controls.OfType<HistoryRow>().First(row=>row.AccessibleName==recordedDate.ToString("yyyy년 M월 d일")+" 일기").PerformClick();
            if(date!=recordedDate) throw new Exception("History click did not select saved date");
            if(!entry.Blocks.First(b=>b.Id==id).Text.Contains("윈도우")) throw new Exception("Date navigation lost content");
            if(!store.Load().Days[DiaryStore.Key(date)].Blocks.Any(b=>b.Checked)) throw new Exception("Check did not persist");
            if(entry.Mood!="속상해요" || entry.Theme!=1 || mood.SelectedIndex!=3 || theme.SelectedIndex!=1) throw new Exception("Appearance did not persist");
            characterChoice.SelectedIndex=(int)MonsterVariant.Winged;
            if(avatar.Variant!=MonsterVariant.Winged || pet==null || pet.Variant!=MonsterVariant.Winged) throw new Exception("Diary variant choice did not update desktop pet");
            var menu=pet.ContextMenuStrip.Items.OfType<ToolStripMenuItem>().First(item=>item.Text=="캐릭터 버전");
            ((ToolStripMenuItem)menu.DropDownItems[(int)MonsterVariant.Mini]).PerformClick();
            if(characterChoice.SelectedIndex!=(int)MonsterVariant.Mini || avatar.Variant!=MonsterVariant.Mini) throw new Exception("Desktop variant menu did not update diary");
            foreach(var variant in MonsterVariants.All.Take(56)) {
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
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-dm-compact.png"),System.Drawing.Imaging.ImageFormat.Png); }
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
            var savedIds=entry.Blocks.Select(b=>b.Id).ToArray();
            var savedText=entry.Blocks.Select(b=>b.Text).ToArray();
            var savedPositions=entry.Blocks.Select(DiaryLayout.Bounds).ToArray();
            ApplyTemplate(DiaryTemplates.All[1]);
            if(!entry.Blocks.Take(savedIds.Length).Select(b=>b.Id).SequenceEqual(savedIds) ||
                !entry.Blocks.Take(savedIds.Length).Select(b=>b.Text).SequenceEqual(savedText) ||
                !entry.Blocks.Take(savedIds.Length).Select(DiaryLayout.Bounds).SequenceEqual(savedPositions)) throw new Exception("Template overwrote existing diary content or placement");
            FlushSave(); ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            if(entry.PageStyle!="paper" || !entry.Blocks.Any(b=>b.Title=="기억에 남는 장면" && b.Prompt!=null)) throw new Exception("Template metadata did not survive date reload");
            entry.Blocks.RemoveAll(b=>!savedIds.Contains(b.Id)); RenderCards();
            var historyDates=Enumerable.Range(0,121).Select(i=>new DateTime(2024,1,1).AddDays(i)).ToArray();
            foreach(var value in historyDates) book.Days[DiaryStore.Key(value)]=DiaryEntry.Empty();
            RefreshHistory(true);
            if(history.Controls.OfType<HistoryRow>().Count()>60) throw new Exception("History created an unbounded number of native rows");
            var pageControls=history.Controls.OfType<FlowLayoutPanel>().Single();
            pageControls.Controls.OfType<Button>().First(button=>button.AccessibleName=="이전 기록 목록").PerformClick();
            if(historyPage!=1 || history.Controls.OfType<HistoryRow>().Count()>60) throw new Exception("History pagination failed");
            var returnDate=date; ChangeDate(historyDates[0]);
            if(historyPage==0 || !history.Controls.OfType<HistoryRow>().Any(row=>row.AccessibleName==historyDates[0].ToString("yyyy년 M월 d일")+" 일기")) throw new Exception("Selecting old date did not reveal its history page");
            foreach(var value in historyDates) book.Days.Remove(DiaryStore.Key(value)); ChangeDate(returnDate);
            // Recompose this example freely to demonstrate positions independent of the card grid.
            var textBlock=entry.Blocks.First(b=>b.Id==id); DiaryLayout.SetBounds(textBlock,new Rectangle(18,12,450,250));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="todo"),new Rectangle(492,34,300,210));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="habit"),new Rectangle(480,274,310,200));
            DiaryLayout.SetBounds(entry.Blocks.First(b=>b.Kind=="emotion"),new Rectangle(34,284,434,200));
            entry.Blocks.First(b=>b.Kind=="todo").Text="산책 20분 하기\r\n책상 정리하기";
            entry.Blocks.First(b=>b.Kind=="todo").Checked=true;
            for(int i=1;i<=2;i++) {
                var previous=DiaryEntry.Empty(); var message=DiaryBlock.Create("text");
                message.Text=i==1?"바쁜 하루 끝에, 잠깐의 여유.":"시작이 조금 서툴러도 괜찮아.";
                previous.Blocks.Add(message); book.Days[DiaryStore.Key(date.AddDays(-i))]=previous;
            }
            theme.SelectedIndex=0; RenderCards();
            ArrangeCards(); board.AutoScrollPosition=Point.Empty; QueueSave(); FlushSave();
            avatar.Fire(); PerformLayout(); ArrangeCards(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(destination,System.Drawing.Imaging.ImageFormat.Png); }
            editLayout.PerformClick(); PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-free-layout.png"),System.Drawing.Imaging.ImageFormat.Png); }
            using(var gallery=new TemplateGallery()) {
                gallery.StartPosition=FormStartPosition.Manual; gallery.Location=new Point(-30000,-30000); gallery.Show(); Application.DoEvents();
                gallery.VerifyAndRender(Path.Combine(Path.GetDirectoryName(destination),"windows-journal-templates.png"));
            }
            ChangeDate(date.AddDays(1)); ApplyTemplate(DiaryTemplates.All[1]);
            if(entry.Blocks.Count!=3) throw new Exception("Fresh page retained a redundant empty starter when selecting format");
            entry.Blocks[0].Text="오랜만에 창문을 열고,\r\n좋아하는 음악을 들으며 하루를 시작했다.";
            entry.Blocks[1].Text="작게 시작해도 충분하다는 것.\r\n끝낸 일 하나가 다음 일을 할 힘이 되어줬다.";
            entry.Blocks[2].Text="점심 먹고 10분 산책하기";
            SelectLayout(true);
            DiaryLayout.SetBounds(entry.Blocks[0],new Rectangle(18,12,450,210));
            DiaryLayout.SetBounds(entry.Blocks[1],new Rectangle(34,244,434,230));
            DiaryLayout.SetBounds(entry.Blocks[2],new Rectangle(492,46,300,220));
            RenderCards(); QueueSave(); FlushSave(); board.AutoScrollPosition=Point.Empty; PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-reflection.png"),System.Drawing.Imaging.ImageFormat.Png); }
            ChangeDate(date.AddDays(1));
            var cornell=DiaryTemplates.All.First(t=>t.Id=="cornell-notes"); ApplyTemplate(cornell);
            var preset=JournalLayouts.Arrange(cornell.Layout,3,board.LogicalWidth);
            if(entry.LayoutMode!="free" || entry.PageStyle!="dots" || !entry.Blocks.Select(DiaryLayout.Bounds).SequenceEqual(preset)) throw new Exception("Fresh template did not apply its previewed layout");
            entry.Blocks[0].Text="핵심 개념은 무엇일까?\r\n다른 사례에도 적용할 수 있을까?";
            entry.Blocks[1].Text="오늘 배운 내용을 내 말로 설명해본다.\r\n\r\n정의와 예시를 연결하니 이해하기 쉬웠다.";
            entry.Blocks[2].Text="질문하고, 설명하고, 짧게 요약하기.\r\n헷갈린 개념은 예제를 바꿔 다시 확인해보자.";
            RenderCards(); QueueSave(); FlushSave(); ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            if(entry.PageStyle!="dots" || !entry.Blocks.Select(DiaryLayout.Bounds).SequenceEqual(preset)) throw new Exception("Preset layout did not persist independently for its date");
            board.AutoScrollPosition=Point.Empty; PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-cornell.png"),System.Drawing.Imaging.ImageFormat.Png); }
            ChangeDate(date.AddDays(1));
            string fixture=Path.Combine(Path.GetDirectoryName(destination),"sample-photo.png"); Tests.WritePhotoFixture(fixture);
            var photo=SetPhotoFromFile(fixture); var photoCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==photo);
            photoCard.Editor.Text="오늘의 산책. 잠깐 멈춰서 바라본 풍경.";
            SelectLayout(true); DiaryLayout.SetBounds(photo,new Rectangle(18,12,450,420));
            var note=entry.Blocks.First(b=>b.Kind=="text"); note.Title="사진 속 하루"; note.Text="기억하고 싶은 순간을 사진으로 남겼다.\r\n\r\n사진도 자유롭게 배치하고, 아래에 짧은 이야기를 적을 수 있다.";
            DiaryLayout.SetBounds(note,new Rectangle(530,34,300,270)); RenderCards();
            photoCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==photo);
            editLayout.PerformClick(); var photoStart=photoCard.DragHandle.PointToScreen(Design.Point(10,10));
            photoCard.DragHandle.DragForTest(photoStart,new Point(photoStart.X+Design.P(20),photoStart.Y+Design.P(15)),false);
            photoStart=photoCard.ResizeHandle.PointToScreen(Design.Point(5,5));
            photoCard.ResizeHandle.DragForTest(photoStart,new Point(photoStart.X+Design.P(20),photoStart.Y+Design.P(20)),false); editLayout.PerformClick();
            if(DiaryLayout.Bounds(photo)!=new Rectangle(38,27,470,440)) throw new Exception("Photo drag or resize failed");
            var photoBounds=DiaryLayout.Bounds(photo); string caption=photo.Text;
            SetPhotoFromFile(fixture,photo);
            if(photo.Text!=caption || DiaryLayout.Bounds(photo)!=photoBounds || entry.Blocks.Count!=2) throw new Exception("Replacing photo lost caption, geometry or duplicated block");
            string bad=Path.Combine(Path.GetDirectoryName(destination),"bad-photo.png"); File.WriteAllText(bad,"invalid"); string retainedPhoto=photo.Photo;
            try { SetPhotoFromFile(bad,photo); throw new Exception("Invalid image accepted"); } catch(InvalidDataException) { }
            if(photo.Photo!=retainedPhoto) throw new Exception("Invalid replacement changed existing photo");
            try { SetPhotoFromFile(bad); throw new Exception("Invalid image accepted"); } catch(InvalidDataException) { }
            if(entry.Blocks.Count!=2) throw new Exception("Invalid image added an empty block");
            QueueSave(); FlushSave(); File.Delete(fixture);
            ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1));
            photo=entry.Blocks.First(b=>b.Kind=="photo"); photoCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==photo);
            if(photo.Text!=caption || DiaryLayout.Bounds(photo)!=photoBounds || photoCard.Photo==null || !photoCard.ReplacePhoto.Enabled) throw new Exception("Photo date navigation lost data or preview");
            board.AutoScrollPosition=Point.Empty; PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-photos.png"),System.Drawing.Imaging.ImageFormat.Png); }
            Size fullSize=Size; Size=MinimumSize; PerformLayout(); Application.DoEvents();
            var photoButton=AllControls(this).OfType<Button>().First(b=>b.Text=="＋ 사진");
            if(photoButton.Right>photoButton.Parent.ClientSize.Width || !photoButton.Visible) throw new Exception("Photo action clipped at minimum window width");
            DiaryLayout.SetBounds(photo,new Rectangle(12,12,300,180)); RenderCards(); photoCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==photo);
            if(photoCard.Photo.Bottom>=photoCard.Editor.Top || photoCard.Editor.Bottom>photoCard.ReplacePhoto.Top) throw new Exception("Small photo card overlaps caption or action");
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-photos-compact.png"),System.Drawing.Imaging.ImageFormat.Png); }
            DiaryLayout.SetBounds(photo,photoBounds); Size=fullSize; RenderCards(); QueueSave(); FlushSave();
            var photoDate=date; var savedPhoto=photo.Photo;
            photoCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==photo);
            photoCard.Editor.Text="바닷가를 걷던 하루. 잠깐 멈춰서 바라본 풍경.";
            BrowseDiary(delegate(JournalBrowser browser) { browser.VerifyAndRender(Path.GetDirectoryName(destination),photoDate); });
            if(date!=photoDate || entry.Blocks.First(b=>b.Kind=="photo").Photo!=savedPhoto || DiaryLayout.Bounds(entry.Blocks.First(b=>b.Kind=="photo"))!=photoBounds ||
                !store.Load().Days[DiaryStore.Key(date)].Blocks.Any(b=>b.Text.Contains("바닷가"))) throw new Exception("Opening browser did not save pending text or retained the wrong diary");
            BrowseDiary(delegate(JournalBrowser browser) { browser.SelectDayForTest(photoDate.AddDays(1)); });
            if(date!=photoDate.AddDays(1) || book.Days.ContainsKey(DiaryStore.Key(date))) throw new Exception("Opening an empty day unexpectedly stored a record");
            BrowseDiary(delegate(JournalBrowser browser) { browser.SelectDayForTest(photoDate); });
            if(date!=photoDate || !entry.Blocks.Any(b=>b.Kind=="photo" && b.Photo==savedPhoto)) throw new Exception("Calendar navigation lost the photo diary");
            BrowseDiary(delegate(JournalBrowser browser) { browser.DialogResult=DialogResult.Cancel; browser.Close(); });
            if(date!=photoDate) throw new Exception("Closing browser changed the selected diary");
            string layoutId=null; var originalBlocks=entry.Blocks.Select(b=>b.Id).ToArray();
            ManageLayouts(delegate(MyLayoutsWindow window) { layoutId=window.VerifyAndRender(Path.GetDirectoryName(destination)); });
            if(entry.Blocks.Count!=4 || !entry.Blocks.Take(2).Select(b=>b.Id).SequenceEqual(originalBlocks) || !entry.Blocks.Any(b=>b.Photo==savedPhoto) ||
                store.Load().Layouts.Count!=3 || !entry.Blocks.Skip(2).All(b=>b.Text=="")) throw new Exception("Applying to an existing day lost data or layouts did not persist");
            var oldLayouts=book.Layouts; int oldVersion=book.Version; bool blocked=false;
            using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                try { SaveCurrentLayout("저장 실패 테스트"); } catch(IOException) { blocked=true; }
            }
            if(!blocked || book.Layouts!=oldLayouts || book.Version!=oldVersion) throw new Exception("Save failure changed the in-memory layout collection");
            ChangeDate(photoDate.AddDays(1));
            ManageLayouts(delegate(MyLayoutsWindow window) { window.ApplyForTest(layoutId); });
            var savedLayout=book.Layouts.First(l=>l.Id==layoutId);
            if(entry.Blocks.Count!=2 || entry.Blocks.Any(b=>b.Text!="" || b.Checked || b.Photo!=null) || !entry.Blocks.Select(DiaryLayout.Bounds).SequenceEqual(savedLayout.Blocks.Select(b=>b.Bounds))) throw new Exception("Fresh page did not restore empty, exact saved layout");
            var slot=entry.Blocks.First(b=>b.Kind=="photo-slot"); var slotCard=board.Controls.OfType<BlockCard>().First(c=>c.Block==slot);
            if(slotCard.Photo==null || slotCard.ReplacePhoto.Text!="사진 선택") throw new Exception("Photo slot lacks an image picker");
            QueueSave(); FlushSave(); board.AutoScrollPosition=Point.Empty; PerformLayout(); Update();
            using(var image=new Bitmap(Width,Height)) { DrawToBitmap(image,new Rectangle(Point.Empty,Size)); image.Save(Path.Combine(Path.GetDirectoryName(destination),"windows-saved-layout-applied.png"),System.Drawing.Imaging.ImageFormat.Png); }
            slotCard.Editor.Text="새 날짜의 사진 설명"; var slotBounds=DiaryLayout.Bounds(slot);
            try { SetPhotoFromFile(bad,slot); throw new Exception("Invalid photo was accepted into slot"); } catch(InvalidDataException) { }
            if(slot.Kind!="photo-slot" || slot.Photo!=null || slot.Text!="새 날짜의 사진 설명") throw new Exception("Invalid photo changed the empty slot");
            Tests.WritePhotoFixture(fixture); SetPhotoFromFile(fixture,slot); File.Delete(fixture); QueueSave(); FlushSave();
            ChangeDate(date.AddDays(1)); ChangeDate(date.AddDays(-1)); slot=entry.Blocks.First(b=>b.Kind=="photo");
            if(slot.Text!="새 날짜의 사진 설명" || DiaryLayout.Bounds(slot)!=slotBounds || slot.Photo==null || !DiaryBrowse.HasPhoto(entry)) throw new Exception("Filling a photo slot lost caption, placement or calendar marker");
            Size=MinimumSize; PerformLayout(); Application.DoEvents();
            if(myLayouts.Right>myLayouts.Parent.ClientSize.Width || !myLayouts.Visible) throw new Exception("My layouts action clipped at minimum diary width"); Size=fullSize;
        }
        private static System.Collections.Generic.IEnumerable<Control> AllControls(Control parent)
        {
            foreach(Control child in parent.Controls) { yield return child; foreach(var nested in AllControls(child)) yield return nested; }
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing) { saveTimer.Dispose(); tray.Visible=false; if(tray.ContextMenuStrip!=null) tray.ContextMenuStrip.Dispose(); tray.Dispose(); if(pet!=null) pet.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
