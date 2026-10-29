package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.design.DiaryOptions
import com.myday.diary.ui.design.AppShell
import com.myday.diary.ui.design.AppPalette
import com.myday.diary.ui.components.SangmonAvatar
import java.time.LocalDate

@OptIn(ExperimentalLayoutApi::class)
@Composable fun PhoneNotebookScreen(controller: NotebookController,catalog: NotebookCatalog,haptics: DiaryHaptics,home: HomeScreenActions,busy: Boolean,
    onPhoto: (String?)->Unit,onExport: ()->Unit,onImport: ()->Unit) {
    val privacyContext=androidx.compose.ui.platform.LocalContext.current
    val privacy=remember {PhonePrivacy(PinVault(privacyContext.filesDir))}
    if(PhonePrivacyGate(privacy))return
    LaunchedEffect(controller.entry){privacy.touch()}
    val welcomePrefs=remember(privacyContext){privacyContext.getSharedPreferences("myday-welcome",android.content.Context.MODE_PRIVATE)}
    var welcome by remember{mutableStateOf(controller.book.days.isEmpty() && !welcomePrefs.getBoolean("complete",false))}
    fun closeWelcome(){welcomePrefs.edit().putBoolean("complete",true).apply();welcome=false}
    var studio by remember {mutableStateOf(false)}
    var tab by rememberSaveable {mutableIntStateOf(0)}
    var journalTools by remember {mutableStateOf(false)}
    var templates by remember {mutableStateOf(false)}
    var layouts by remember {mutableStateOf(false)}
    var editing by rememberSaveable {mutableStateOf(false)}
    var opening by remember {mutableStateOf<String?>(null)}
    var deleting by remember {mutableStateOf<String?>(null)}
    var nameFor by remember {mutableStateOf<SavedLayout?>(null)}
    var naming by remember {mutableStateOf(false)}
    var options by remember {mutableStateOf("")}
    val entry=controller.entry
    val date=controller.date
    val attempt: (TactileEvent?,()->Unit)->Unit = {event,work->controller.clearError();runCatching(work).onFailure {controller.report(it.message ?: "처리하지 못했어요.")}
        if(event!=null) haptics.perform(if(controller.error==null && (controller.saved || controller.saving)) event else TactileEvent.REJECT)
    }
    LaunchedEffect(date) {opening=null;deleting=null;editing=false}
    Scaffold(modifier=Modifier.pointerInput(Unit){awaitPointerEventScope{while(true){awaitPointerEvent();privacy.touch()}}},containerColor=AppPalette.backgrounds[entry.theme],
        topBar={Surface(color=Color(0xfff7f5f0)) {Row(Modifier.fillMaxWidth().statusBarsPadding().padding(horizontal=20.dp,vertical=12.dp),horizontalArrangement=Arrangement.SpaceBetween) {
            Column {Text(AppShell.brand,style=MaterialTheme.typography.titleLarge,color=AppPalette.olive);Text(AppShell.tagline,style=MaterialTheme.typography.labelSmall)}
            TextButton({attempt(TactileEvent.SELECT) {controller.selectDate(LocalDate.now());tab=0}},enabled=controller.saved && !busy) {Text("오늘로 ↗")}
        }}},
        bottomBar={NavigationBar(containerColor=AppPalette.sidebar) {AppShell.titles.forEachIndexed {i,title->
            NavigationBarItem(modifier=Modifier.testTag("nav-$i"),selected=tab==i,onClick={tab=i;haptics.perform(TactileEvent.SELECT)},icon={Text(AppShell.symbols[i])},label={Text(title,maxLines=1)})
        }}},
        floatingActionButton={}) {padding->
        Column(Modifier.fillMaxSize().padding(padding).imePadding()) {
            controller.error?.let {message->Surface(color=Color(0xfff3e6df)) {Row(Modifier.fillMaxWidth().padding(horizontal=16.dp),horizontalArrangement=Arrangement.SpaceBetween) {
                Text(message,Modifier.weight(1f).padding(vertical=10.dp),style=MaterialTheme.typography.bodySmall)
                if(!controller.saved) TextButton({attempt(TactileEvent.CHECK) {controller.retry()}}) {Text("다시 저장")}
                else TextButton(controller::clearError) {Text("닫기")}
            }}}
            when(tab) {
                0->LazyColumn(Modifier.fillMaxSize().padding(horizontal=20.dp).testTag("diary-list"),verticalArrangement=Arrangement.spacedBy(14.dp),contentPadding=PaddingValues(top=14.dp,bottom=110.dp)) {
                    item {
                        Text("A LITTLE SPACE FOR YOUR DAY",style=MaterialTheme.typography.labelSmall,color=MaterialTheme.colorScheme.primary)
                        Text("${date.monthValue}월 ${date.dayOfMonth}일",style=MaterialTheme.typography.headlineLarge)
                        Text(AppShell.greeting,style=MaterialTheme.typography.titleLarge)
                        Text(AppShell.subtitle,style=MaterialTheme.typography.bodySmall)
                        Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {
                            TextButton({attempt(TactileEvent.SELECT) {controller.selectDate(date.minusDays(1))}},enabled=controller.saved && date>LocalDate.of(1,1,1)) {Text("‹ 이전")}
                            TextButton({tab=1;haptics.perform(TactileEvent.SELECT)}) {Text(date.toString())}
                            TextButton({attempt(TactileEvent.SELECT) {controller.selectDate(date.plusDays(1))}},enabled=controller.saved && date<LocalDate.of(9999,12,31)) {Text("다음 ›")}
                        }
                        Text(if(controller.saving) "휴대폰에 저장 중…" else if(controller.saved) "휴대폰에 자동 저장됨" else "저장 실패 · 기록은 화면에 유지돼요",style=MaterialTheme.typography.labelSmall)
                    }
                    item {FlowRow(horizontalArrangement=Arrangement.spacedBy(6.dp)) {
                        AssistChip(onClick={options="mood"},label={Text(entry.mood)})
                        AssistChip(onClick={options="style"},label={Text(when(entry.pageStyle) {"paper"->"노트";"dots"->"도트";else->"카드"})})
                        AssistChip(modifier=Modifier.testTag("add-block"),onClick={templates=true},label={Text("일기 형식 · 블록 추가")})
                        AssistChip(onClick={layouts=true},label={Text("내 레이아웃")})
                        AssistChip(onClick={studio=true},label={Text("하루 꾸미기")})
                    }}
                    item {Card(colors=CardDefaults.cardColors(containerColor=AppPalette.friendCard)) {Column(Modifier.padding(14.dp)) {
                        Text("YOUR LITTLE FRIEND",style=MaterialTheme.typography.labelSmall)
                        Row(verticalAlignment=androidx.compose.ui.Alignment.CenterVertically) {
                            SangmonAvatar(controller.book.characterStyle,controller.book.equipment,Modifier.size(92.dp),haptics,controller.reactionTick)
                            Column(Modifier.weight(1f)) {Text(AppShell.friendTitle,style=MaterialTheme.typography.titleMedium);Text("Lv.${NotebookRules.level(controller.book)} · ${NotebookRules.xp(controller.book)} XP",style=MaterialTheme.typography.labelSmall);TextButton({tab=2}) {Text("상몬·공방 열기")}}
                        }
                        Text("차곡차곡, 오늘의 실천",style=MaterialTheme.typography.titleSmall)
                        if(controller.reaction.isNotEmpty()) Text(controller.reaction,style=MaterialTheme.typography.bodySmall,color=AppPalette.olive)
                        listOf("todo" to "할 일","habit" to "습관").forEach {(kind,title)->val tasks=entry.blocks.filter {it.type==kind}
                            Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {Text(title);Text("${tasks.count {it.checked}} / ${tasks.size}")}
                            LinearProgressIndicator(progress={if(tasks.isEmpty()) 0f else tasks.count {it.checked}.toFloat()/tasks.size},modifier=Modifier.fillMaxWidth().padding(top=6.dp,bottom=10.dp))
                        }
                    }}}
                    item {FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                        FilterChip(entry.layoutMode=="cards",{attempt(TactileEvent.SELECT) {controller.edit(entry.copy(layoutMode="cards"));editing=false}},label={Text("자동 정렬")})
                        FilterChip(entry.layoutMode=="free",{attempt(TactileEvent.SELECT) {controller.edit(NotebookRules.free(entry))}},label={Text("자유 배치")})
                        if(entry.layoutMode=="free") {FilterChip(editing,{editing=!editing;haptics.perform(TactileEvent.GRAB)},label={Text(if(editing) "편집 완료" else "배치 편집")})
                            TextButton({attempt(TactileEvent.DROP) {controller.edit(NotebookRules.arrange(entry))}}) {Text("자동 정리")}}
                    }}
                    if(entry.blocks.isEmpty()) item {Card(Modifier.fillMaxWidth(),colors=CardDefaults.cardColors(containerColor=Color.White)) {Column(Modifier.padding(24.dp)) {Text("아직 비어 있는 하루",style=MaterialTheme.typography.titleLarge);Text("하루 조각을 추가하거나 일기 형식을 골라 보세요.")}}}
                    if(entry.layoutMode=="free" && entry.blocks.isNotEmpty()) item {PhoneFreeBoard(entry,editing,haptics,
                        onBounds={b->attempt(TactileEvent.DROP) {controller.updateBlock(b,false)}},onOpen={opening=it;haptics.perform(TactileEvent.SELECT)})}
                    else itemsIndexed(entry.blocks,key={_,b->b.id}) {index,block->PhoneBlockEditor(block,entry.pageStyle,haptics,
                        onEdit={b->attempt(null) {controller.updateBlock(b)}},onMove={delta->attempt(TactileEvent.DROP) {controller.move(block.id,delta)}},
                        onDelete={deleting=block.id},onPhoto={onPhoto(block.id)},canMoveUp=index>0,canMoveDown=index<entry.blocks.lastIndex)}
                    item {Text("감정처리반은 마음을 적어 보관하는 공간이에요.",style=MaterialTheme.typography.bodySmall)}
                }
                1->PhoneCalendar(controller.book,date) {next->attempt(TactileEvent.SELECT) {controller.selectDate(next);if(controller.saved) tab=0}}
                2->PhoneCompanion(controller,catalog,haptics,attempt)
                3->PhoneLounge()
                else->LazyColumn(Modifier.fillMaxSize().padding(horizontal=20.dp),verticalArrangement=Arrangement.spacedBy(14.dp),contentPadding=PaddingValues(top=14.dp,bottom=24.dp)) {
                    item {OutlinedButton({welcome=true},Modifier.fillMaxWidth().testTag("guide-open")){Text("처음 시작하기 · 사용 안내")}}
                    item {Button({studio=true},modifier=Modifier.fillMaxWidth()) {Text("하루 꾸미기 · 사진 / 방 / 태그 / 잠금")}}
                    item {Button({journalTools=true},modifier=Modifier.fillMaxWidth().testTag("journal-tools-open")) {Text("기록 도구 · 복구 / 리포트 / 알림")}}
                    item {Text("오늘의 분위기",style=MaterialTheme.typography.headlineSmall);FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)) {DiaryOptions.themes.forEach {theme->FilterChip(entry.theme==theme.id,{attempt(TactileEvent.SELECT) {controller.edit(entry.copy(theme=theme.id))}},label={Text(theme.name)})}}}
                    item {Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {Row(Modifier.fillMaxWidth().padding(16.dp),horizontalArrangement=Arrangement.SpaceBetween) {
                        Column(Modifier.weight(1f)) {Text("햅틱 반응",style=MaterialTheme.typography.titleMedium);Text("체크·블록 이동·장착에 짧게 반응해요. 휴대폰의 터치 피드백 설정을 따라요.",style=MaterialTheme.typography.bodySmall)}
                        Switch(controller.book.haptics,{enabled->attempt(null) {controller.configure {it.copy(haptics=enabled)}}})
                    }}}
                    item {Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {Column(Modifier.padding(16.dp),verticalArrangement=Arrangement.spacedBy(10.dp)) {
                        Text("소중한 하루 보관하기",style=MaterialTheme.typography.titleMedium)
                        Text("사진·배치·상몬 성장·장비를 함께 보관하고 Windows에서도 가져올 수 있어요.",style=MaterialTheme.typography.bodySmall)
                        Button(onExport,modifier=Modifier.fillMaxWidth()) {Text("↓ 백업 다운로드")}
                        OutlinedButton(onImport,modifier=Modifier.fillMaxWidth()) {Text("↥ 백업 복원")}
                    }}}
                    item {Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {Column(Modifier.padding(16.dp),verticalArrangement=Arrangement.spacedBy(10.dp)) {
                        Text("홈 화면의 작은 친구",style=MaterialTheme.typography.titleMedium)
                        OutlinedButton(home::pinWidget,modifier=Modifier.fillMaxWidth()) {Text("홈 화면에 일기 위젯 추가")}
                        Button(home::setWallpaper,modifier=Modifier.fillMaxWidth()) {Text("상몬 라이브 배경화면 설정")}
                        Text("선택한 상몬과 장비가 홈 화면에도 함께해요.",style=MaterialTheme.typography.bodySmall)
                    }}}
                }
            }
        }
    }
    if(welcome)PhoneWelcome(controller.book,onClose={closeWelcome()},onAction={i->
        if(controller.saved && !busy){closeWelcome();tab=0;when(i){0->attempt(TactileEvent.SELECT){controller.add("text")};1->onPhoto(null);else->studio=true}}
        else controller.report("저장이 끝나면 시작할 수 있어요.")
    })
    if(studio)PhoneStudio(controller,haptics,privacy,onClose={studio=false},onOpen={day->controller.selectDate(day);if(controller.saved){studio=false;tab=0}})
    if(journalTools) PhoneJournalTools(controller,onExport,onDismiss={journalTools=false},onOpenDay={day->controller.clearError();controller.selectDate(day);if(controller.saved&&controller.error==null){journalTools=false;tab=0}})
    if(templates) TemplatePicker(catalog,onApply={template->attempt(TactileEvent.DROP) {controller.applyTemplate(template)};if(controller.error==null) templates=false},onAddBlock={kind->if(kind=="photo") onPhoto(null) else attempt(TactileEvent.SELECT) {controller.add(kind)};if(controller.error==null) templates=false},onDismiss={templates=false},error=controller.error)
    if(layouts) SavedLayoutPicker(controller,onApply={layout->attempt(TactileEvent.DROP) {controller.applyLayout(layout)};if(controller.error==null) layouts=false},onSave={nameFor=null;naming=true},onRename={nameFor=it;naming=true},onDelete={id->attempt(TactileEvent.DROP) {controller.deleteLayout(id)}},onDismiss={layouts=false})
    if(naming) NameDialog(if(nameFor==null) "현재 배치 저장" else "레이아웃 이름 변경",nameFor?.name ?: "",onSave={name->attempt(TactileEvent.CHECK) {if(nameFor==null) controller.saveLayout(name) else controller.renameLayout(nameFor!!.id,name)};if(controller.error==null) naming=false},onDismiss={naming=false},error=controller.error)
    deleting?.let {id->AlertDialog(onDismissRequest={deleting=null},title={Text("이 블록을 삭제할까요?")},text={Text("이전 글과 사진은 기록 도구의 휴지통·이력에 보관해요.")},confirmButton={TextButton({attempt(TactileEvent.DROP) {controller.delete(id)};deleting=null;opening=null}) {Text("삭제")}},dismissButton={TextButton({deleting=null}) {Text("취소")}})}
    opening?.let {id->entry.blocks.find {it.id==id}?.let {block->PhoneFullDialog("하루 조각 편집",{opening=null},controller.error) {LazyColumn(Modifier.fillMaxSize().padding(16.dp)) {item {PhoneBlockEditor(block,entry.pageStyle,haptics,
        onEdit={b->attempt(null) {controller.updateBlock(b)}},onMove={delta->attempt(TactileEvent.DROP) {controller.move(id,delta)}},onDelete={deleting=id},onPhoto={onPhoto(id)},canMoveUp=entry.blocks.first().id!=id,canMoveDown=entry.blocks.last().id!=id)}}}}}
    if(options.isNotEmpty()) {val isMood=options=="mood";val choices=if(isMood) listOf("평온해요","행복해요","피곤해요","속상해요","설레요") else listOf("노트","카드","도트")
        AlertDialog(onDismissRequest={options=""},title={Text(if(isMood) "오늘의 마음" else "일기 종이")},text={Column {choices.forEachIndexed {i,name->TextButton({attempt(TactileEvent.SELECT) {controller.edit(if(isMood) entry.copy(mood=name) else entry.copy(pageStyle=listOf("paper","plain","dots")[i]))};options=""}) {Text(name)}}}},confirmButton={TextButton({options=""}) {Text("닫기")}})
    }
}
