package com.myday.diary.ui.diary
import androidx.compose.foundation.*
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.input.key.onPreviewKeyEvent
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.DiaryHaptics
import com.myday.diary.ui.components.SangmonAvatar
import kotlinx.coroutines.*
import java.time.LocalDate
import java.time.YearMonth
import java.util.Locale
import kotlin.math.*

@OptIn(ExperimentalLayoutApi::class)
@Composable fun PhoneStudio(controller:NotebookController,haptics:DiaryHaptics,privacy:PhonePrivacy,onClose:()->Unit,onOpen:(LocalDate)->Unit){
 val context=androidx.compose.ui.platform.LocalContext.current
 val roomArt=remember{StudioCatalog.furnitureKinds.associateWith{kind->requireNotNull(context.assets.open("studio/$kind.png").use{android.graphics.BitmapFactory.decodeStream(it)}).asImageBitmap()}}
 var tab by remember{mutableIntStateOf(0)};var error by remember{mutableStateOf("")};val scope=rememberCoroutineScope()
 var draft by remember(controller.book.studio){mutableStateOf(controller.book.studio ?: StudioData())}
 var photoId by remember{mutableStateOf<String?>(null)};var photoSource by remember{mutableStateOf("")};val key=controller.date.toString()
 fun save(next:StudioData){runCatching{next.validate();if(controller.configure{it.copy(studio=next,version=maxOf(9,it.version))})draft=next}.onFailure{error=it.message ?: "저장 실패"}}
 Dialog(onClose,properties=DialogProperties(usePlatformDefaultWidth=false,securePolicy=androidx.compose.ui.window.SecureFlagPolicy.Inherit)){Surface(Modifier.fillMaxSize().pointerInput(Unit){awaitPointerEventScope{while(true){awaitPointerEvent();privacy.touch()}}}.onPreviewKeyEvent{privacy.touch();false}){Column(Modifier.statusBarsPadding().navigationBarsPadding()){
  Row(Modifier.fillMaxWidth().padding(12.dp),horizontalArrangement=Arrangement.SpaceBetween){Text("하루 꾸미기",style=MaterialTheme.typography.headlineSmall);TextButton(onClose){Text("닫기")}}
  ScrollableTabRow(tab){listOf("1 사진·스티커","2 상몬의 방","3 감정 달력","4 태그·즐겨찾기","5 오늘의 질문","6 일기 잠금").forEachIndexed{i,title->Tab(tab==i,{tab=i},text={Text(title)})}}
  if(error.isNotEmpty()||controller.error!=null)Text(error.ifEmpty{controller.error ?: ""},Modifier.padding(12.dp),color=MaterialTheme.colorScheme.error)
  when(tab){
   0->LazyColumn(Modifier.fillMaxSize().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
    item{Text("사진을 골라 자르기·회전·프레임·스티커를 적용해요. 저장하기 전에는 기존 사진을 바꾸지 않아요.")}
    items(controller.entry.blocks.filter{it.type=="photo"&&it.photo!=null},key={it.id}){b->OutlinedButton({photoId=b.id;photoSource=b.photo!!},enabled=controller.saved){Text("사진 꾸미기 · "+b.text.ifBlank{"사진"}.take(35))}}
    item{Text("독립 스티커는 일기 블록으로 추가해 자유롭게 배치할 수 있어요.");FlowRow{StudioCatalog.stickers.forEachIndexed{i,title->TextButton({scope.launch{runCatching{val encoded=withContext(Dispatchers.Default){val image=PhotoDecor.sticker(i);try{PhotoDecor.encode(image)}finally{image.recycle()}};controller.edit(NotebookRules.append(controller.entry,listOf(DiaryBlock(type="photo",photo=encoded,title="$title 스티커",height=300))),true)}.onFailure{error=it.message ?: "스티커 추가 실패"}}},enabled=controller.saved){Text("$title +")}}}}
   }
   1->Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)){
    Text("가구를 손가락으로 움직여요. 최대 24개, 상몬은 옷장 소품을 입고 쉬어요.")
    var selected by remember{mutableStateOf<String?>(null)};var resting by remember{mutableStateOf(false)}
    BoxWithConstraints(Modifier.fillMaxWidth().height(330.dp).background(listOf(Color(0xfff3ebdc),Color(0xffe5deef),Color(0xffe0e9da))[draft.roomTheme],RoundedCornerShape(20.dp))){
     val density=LocalDensity.current;val w=with(density){maxWidth.toPx()};val h=with(density){maxHeight.toPx()};val pieceW=with(density){80.dp.toPx()};val pieceH=with(density){96.dp.toPx()}
     Box(Modifier.fillMaxWidth().fillMaxHeight(0.28f).align(Alignment.BottomCenter).background(Color(0xffd6c5ae)))
     SangmonAvatar(controller.book.characterStyle,controller.book.equipment,Modifier.size(145.dp).align(Alignment.Center),haptics,controller.reactionTick,sleepy=resting)
     draft.furniture.forEach{item->Column(Modifier.offset{IntOffset((item.x*(w-pieceW)/900).roundToInt(),(item.y*(h-pieceH)/900).roundToInt())}.width(80.dp).height(96.dp).background(if(selected==item.id)Color(0xfff5d4c1)else Color(0xfffbf8f2),RoundedCornerShape(20.dp)).pointerInput(item.id,w,h){detectDragGestures(onDragStart={selected=item.id},onDragEnd={save(draft)},onDrag={change,delta->change.consume();selected=item.id;draft=draft.copy(furniture=draft.furniture.map{f->if(f.id==item.id)f.copy(x=(f.x+delta.x*900/max(1f,w-pieceW)).roundToInt().coerceIn(0,900),y=(f.y+delta.y*900/max(1f,h-pieceH)).roundToInt().coerceIn(0,900))else f})})}.padding(8.dp),horizontalAlignment=Alignment.CenterHorizontally){val i=StudioCatalog.furnitureKinds.indexOf(item.kind);Image(roomArt.getValue(item.kind),StudioCatalog.furnitureNames[i],Modifier.size(58.dp));Text(StudioCatalog.furnitureNames[i],style=MaterialTheme.typography.labelSmall)}}
    }
    FlowRow{StudioCatalog.furnitureKinds.forEachIndexed{i,kind->TextButton({save(draft.copy(furniture=draft.furniture+RoomItem(kind=kind,x=100+draft.furniture.size%5*140,y=300)))},enabled=controller.saved&&draft.furniture.size<24){Text(StudioCatalog.furnitureNames[i]+" +")}};TextButton({save(draft.copy(furniture=draft.furniture.filter{it.id!=selected}));selected=null},enabled=controller.saved&&selected!=null){Text("선택 가구 치우기")};TextButton({resting=!resting}){Text("상몬 쉬기 / 깨우기")};TextButton({save(draft.copy(roomTheme=(draft.roomTheme+1)%3))},enabled=controller.saved){Text("배경 바꾸기")}}
   }
   2->Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)){
    var month by remember{mutableStateOf(YearMonth.from(controller.date))};Text("직접 선택한 감정을 날짜별로 돌아봐요.");Row{TextButton({if(month.year>1||month.monthValue>1)month=month.minusMonths(1)}){Text("‹")};Text(month.toString(),Modifier.padding(14.dp));TextButton({if(month.year<9999||month.monthValue<12)month=month.plusMonths(1)}){Text("›")}}
    val offset=month.atDay(1).dayOfWeek.value%7
    Row{listOf("일","월","화","수","목","금","토").forEach{Text(it,Modifier.weight(1f))}}
    repeat((offset+month.lengthOfMonth()+6)/7){row->Row{repeat(7){col->val n=row*7+col-offset+1;if(n !in 1..month.lengthOfMonth())Spacer(Modifier.weight(1f))else{val date=month.atDay(n);val mood=controller.book.days[date.toString()]?.mood;val index=StudioCatalog.moods.indexOf(mood);val colors=listOf(Color(0xffdfe8d5),Color(0xfffae4a4),Color(0xffdfd8ec),Color(0xffd5e0ed),Color(0xfff4d8dd));TextButton({onOpen(date)},Modifier.weight(1f).padding(1.dp).background(if(index<0)Color.Transparent else colors[index],RoundedCornerShape(8.dp)),contentPadding=PaddingValues(2.dp)){Text("$n\n"+(mood ?: ""),style=MaterialTheme.typography.labelSmall)}}}}}
   }
   3->Column(Modifier.fillMaxSize().padding(16.dp)){
    val note=draft.note(key);var tags by remember(key){mutableStateOf(note.tags.joinToString(", "))};var favourite by remember(key){mutableStateOf(note.favourite)};var query by remember{mutableStateOf("")};var only by remember{mutableStateOf(false)}
    Text(key+" · 태그는 쉼표로 구분해요. 최대 10개·각 20자");OutlinedTextField(tags,{tags=it},label={Text("태그")},modifier=Modifier.fillMaxWidth().testTag("studio-tags"));Row(verticalAlignment=Alignment.CenterVertically){Checkbox(favourite,{favourite=it});Text("이날을 즐겨찾기")};Button({runCatching{save(draft.setNote(StudioNote(key,tags.split(',').map{it.trim().trimStart('#')}.filter{it.isNotEmpty()}.distinctBy{it.lowercase(Locale.ROOT)},favourite)))}.onFailure{error=it.message ?: "태그를 확인해 주세요."}},enabled=controller.saved){Text("이날의 분류 저장")}
    OutlinedTextField(query,{query=it},label={Text("태그로 찾기")});Row(verticalAlignment=Alignment.CenterVertically){Checkbox(only,{only=it});Text("즐겨찾기만")}
    LazyColumn{items(draft.pages.filter{(!only||it.favourite)&&(query.isBlank()||it.tags.any{t->t.contains(query.trim().trimStart('#'),true)})}.sortedByDescending{it.date}.take(100),key={it.date}){n->TextButton({onOpen(LocalDate.parse(n.date))}){Text((if(n.favourite)"★ "else "")+n.date+"  "+n.tags.joinToString(" "){"#$it"})}}}
   }
   4->Column(Modifier.padding(22.dp),verticalArrangement=Arrangement.spacedBy(20.dp)){
    val question=StudioData.question(controller.date);Text("$key 의 작은 질문");Text(question,style=MaterialTheme.typography.headlineSmall);Button({if(controller.entry.blocks.any{it.prompt==question})error="이미 이 질문이 일기에 있어요." else{controller.edit(NotebookRules.append(controller.entry,listOf(DiaryBlock(type="text",title="오늘의 질문",prompt=question))),true);if(controller.error==null)onClose()}},enabled=controller.saved){Text("이 질문으로 일기 쓰기")};Text("날짜별 질문은 기기 안의 공통 목록에서 골라요. 답변을 외부로 보내지 않아요.")
   }
   else->Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
    var old by remember{mutableStateOf("")};var pin by remember{mutableStateOf("")};var confirmation by remember{mutableStateOf("")};var working by remember{mutableStateOf(false)}
    Text("이 기기의 화면 잠금",style=MaterialTheme.typography.titleLarge);Text("앱을 열 때·5분 미사용·배경 전환 시 잠가요. PIN은 백업에 넣지 않아요. 화면을 가리는 기능이며 저장 파일 자체를 암호화하지 않아요. PIN을 잊지 않도록 보관해 주세요.");Text(if(privacy.enabled)"현재 켜짐" else "현재 꺼짐")
    listOf(Triple(old,{v:String->old=v},"현재 PIN (처음에는 비워 두세요)"),Triple(pin,{v:String->pin=v},"새 PIN · 숫자 6~12자리"),Triple(confirmation,{v:String->confirmation=v},"새 PIN 확인")).forEach{(value,set,title)->OutlinedTextField(value,{set(it.take(12))},label={Text(title)},visualTransformation=androidx.compose.ui.text.input.PasswordVisualTransformation(),singleLine=true)}
    fun update(disable:Boolean){working=true;scope.launch{runCatching{withContext(Dispatchers.Default){if(disable)privacy.vault.disable(old)else{require(pin==confirmation){"두 PIN 입력이 달라요."};privacy.vault.set(old,pin)}}}.onSuccess{old="";pin="";confirmation="";privacy.refresh();com.myday.diary.DiaryWidgetProvider.refreshAll(context);error=""}.onFailure{error=it.message ?: "잠금 설정 실패"};working=false}}
    Button({update(false)},enabled=!working){Text("PIN 설정·변경")};OutlinedButton({update(true)},enabled=!working&&privacy.enabled){Text("잠금 끄기")};TextButton({privacy.locked=true},enabled=privacy.enabled){Text("지금 잠그기")}
   }
  }
 }}}
 if(photoId!=null)PhonePhotoDecor(photoSource,onClose={photoId=null},onApply={encoded->val target=controller.entry.blocks.find{it.id==photoId};if(target!=null){controller.updateBlock(target.copy(photo=encoded));if(controller.error==null)photoId=null}})
}

@OptIn(ExperimentalLayoutApi::class)
@Composable private fun PhonePhotoDecor(source:String,onClose:()->Unit,onApply:(String)->Unit) {
    var image by remember { mutableStateOf<android.graphics.Bitmap?>(null) }
    var sticker by remember { mutableIntStateOf(-1) }
    var error by remember { mutableStateOf("") }
    var busy by remember { mutableStateOf(false) }
    val scope=rememberCoroutineScope()
    LaunchedEffect(source) {
        runCatching { withContext(Dispatchers.Default) { PhotoDecor.decode(source) } }
            .onSuccess { image=it }.onFailure { error="사진을 읽지 못했어요." }
    }
    // Keep preview images GC-owned while Compose may still reference an earlier frame.
    fun replace(next:android.graphics.Bitmap) { image=next }
    Dialog(onClose,properties=DialogProperties(usePlatformDefaultWidth=false)) {
        Surface(Modifier.fillMaxSize()) {
            Column(Modifier.statusBarsPadding().navigationBarsPadding().padding(16.dp)
                .verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)) {
                Text("사진 꾸미기",style=MaterialTheme.typography.headlineSmall)
                Text("스티커를 고른 뒤 사진에서 붙일 위치를 눌러요.")
                Text(error)
                val bitmap=image
                if(bitmap!=null) {
                    BoxWithConstraints(Modifier.fillMaxWidth().height(300.dp)) {
                        val density=LocalDensity.current
                        val width=with(density) { maxWidth.toPx() }
                        val height=with(density) { maxHeight.toPx() }
                        val touch=Modifier.fillMaxSize().pointerInput(bitmap,sticker) {
                            detectTapGestures { point ->
                                if(sticker>=0) {
                                    val scale=min(width/bitmap.width,height/bitmap.height)
                                    val x=(point.x-(width-bitmap.width*scale)/2)/scale
                                    val y=(point.y-(height-bitmap.height*scale)/2)/scale
                                    if(x>=0&&y>=0&&x<bitmap.width&&y<bitmap.height) {
                                        val next=bitmap.copy(android.graphics.Bitmap.Config.ARGB_8888,true)
                                        PhotoDecor.stamp(android.graphics.Canvas(next),sticker,x,y,min(bitmap.width,bitmap.height)*0.22f)
                                        replace(next)
                                    }
                                }
                            }
                        }
                        Image(bitmap.asImageBitmap(),"편집 미리보기",touch,contentScale=ContentScale.Fit)
                    }
                    FlowRow {
                        TextButton({replace(PhotoDecor.rotate(bitmap,-90f))}) {Text("왼쪽 회전")}
                        TextButton({replace(PhotoDecor.rotate(bitmap,90f))}) {Text("오른쪽 회전")}
                        TextButton({replace(PhotoDecor.crop(bitmap,1.0))}) {Text("정사각 자르기")}
                        TextButton({replace(PhotoDecor.crop(bitmap,4.0/3))}) {Text("4:3 자르기")}
                        listOf("크림","라벤더","숲").forEachIndexed { i,name ->
                            TextButton({replace(PhotoDecor.frame(bitmap,i))}) {Text("$name 테두리")}
                        }
                    }
                    FlowRow {
                        StudioCatalog.stickers.forEachIndexed { i,name ->
                            FilterChip(sticker==i,{sticker=i},label={Text(name)})
                        }
                    }
                    Button({
                        busy=true
                        scope.launch {
                            runCatching {withContext(Dispatchers.Default) {PhotoDecor.encode(bitmap)}}
                                .onSuccess(onApply).onFailure {error=it.message ?: "사진 적용 실패"}
                            busy=false
                        }
                    },enabled=!busy) {Text("이 사진 적용")}
                }
                TextButton({runCatching {replace(PhotoDecor.decode(source))}
                    .onFailure {error="사진을 읽지 못했어요."}},enabled=!busy) {Text("처음으로")}
                OutlinedButton(onClose,enabled=!busy) {Text("취소")}
            }
        }
    }
}
