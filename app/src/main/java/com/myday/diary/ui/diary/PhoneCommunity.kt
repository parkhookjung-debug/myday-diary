package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.data.DiaryEntry
import com.myday.diary.diary.NotebookController
import com.myday.diary.social.*
import com.myday.diary.ui.design.AppPalette
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import java.time.LocalDate

internal fun message(error: Throwable): String {
    if(error is CancellationException) throw error
    return error.message ?: "연결을 확인하지 못했어요. 다시 시도해 주세요."
}

@Composable fun PhoneCommunity(controller: NotebookController) {
    var section by remember {mutableIntStateOf(0)}
    var sharing by remember {mutableStateOf(false)}
    Column(Modifier.fillMaxSize()) {
        TabRow(section) {
            Tab(section==0,{section=0},text={Text("친구 일기")})
            Tab(section==1,{section=1},text={Text("채팅 라운지")})
        }
        Box(Modifier.weight(1f)) {
            if(section==0)PhoneFriends(onPublish={sharing=true},canPublish=controller.saved)
            else PhoneLounge()
        }
    }
    if(sharing)PhoneShareDiary(controller.date,controller.entry,{sharing=false})
}

@Composable internal fun ConnectionNotice() {
    Column(Modifier.fillMaxWidth().padding(vertical=12.dp),verticalArrangement=Arrangement.spacedBy(8.dp)) {
        Text("친구 일기 공유 미리보기",style=MaterialTheme.typography.titleMedium)
        Text("현재 통합판은 외부 서버 연결이 꺼져 있어요. 선택한 기록의 미리보기만 사용할 수 있고 로그인·초대·전송은 하지 않아요. 채팅 라운지는 기존 로컬 서버를 사용해요.",style=MaterialTheme.typography.bodyMedium)
    }
}

@Composable internal fun PhonePublicDetail(value: SharedPostDetail,onClose: ()->Unit) {
    PhoneFullDialog("${value.summary.nickname}님의 공개 일기",onClose,null) {
        LazyColumn(Modifier.fillMaxSize().padding(16.dp),verticalArrangement=Arrangement.spacedBy(16.dp)) {
            item{Text(value.summary.date,style=MaterialTheme.typography.labelLarge);Text(value.summary.title,style=MaterialTheme.typography.headlineSmall);if(value.summary.mood.isNotBlank())Text(value.summary.mood)}
            items(value.blocks,key={it.id}){block->Card(colors=CardDefaults.cardColors(containerColor=Color.White)){
                Column(Modifier.fillMaxWidth().padding(16.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){
                    if(block.title.isNotBlank())Text(block.title,style=MaterialTheme.typography.titleMedium)
                    block.photo?.let{PhonePhoto(it,Modifier.fillMaxWidth().heightIn(max=420.dp))}
                    if(block.type in setOf("todo","habit"))Text(if(block.checked) "완료한 기록" else "진행 중인 기록",style=MaterialTheme.typography.labelMedium)
                    if(block.text.isNotBlank())Text(block.text)
                }
            }}
            item{Text("친구가 직접 골라 공개한 시점의 내용이에요.",style=MaterialTheme.typography.bodySmall)}
        }
    }
}

@Composable fun PhoneShareDiary(date: LocalDate,entry: DiaryEntry,onDismiss: ()->Unit) {
    val context=LocalContext.current
    val scope=rememberCoroutineScope()
    val client=remember{FriendsClient()}
    // Start with nothing selected, including the mood. Private edits never auto-publish.
    var selected by remember(date){mutableStateOf(emptySet<String>())}
    var title by remember(date){mutableStateOf("${date.monthValue}월 ${date.dayOfMonth}일의 하루")}
    var includeMood by remember{mutableStateOf(false)}
    var busy by remember{mutableStateOf(false)}
    var status by remember{mutableStateOf("")}
    var confirm by remember{mutableStateOf<SharedDiaryDraft?>(null)}
    var preview by remember{mutableStateOf<SharedDiaryDraft?>(null)}
    var retracting by remember{mutableStateOf(false)}
    LaunchedEffect(CommunityConnection.revision){if(CommunityConnection.hasToken())runCatching{client.me()}.onFailure{status=message(it)}}
    PhoneFullDialog("친구에게 공개할 내용",{if(!busy)onDismiss()},null){
        LazyColumn(Modifier.fillMaxSize().padding(16.dp).testTag("share-diary"),verticalArrangement=Arrangement.spacedBy(12.dp),contentPadding=PaddingValues(bottom=30.dp)){
            item{
                Text(date.toString(),style=MaterialTheme.typography.labelLarge)
                Text("체크한 기록만 친구에게 보여요",style=MaterialTheme.typography.headlineSmall)
                Text("공개한 시점의 글과 사진만 올라가요. 이후 수정한 내용은 다시 공개하기 전까지 기기에만 남아요.",style=MaterialTheme.typography.bodyMedium)
                if(!CommunityConnection.configured)ConnectionNotice()
                else if(!CommunityConnection.signedIn)Button({busy=true;CommunityConnection.login(context){error->busy=false;if(error!=null)status=message(error) else scope.launch{runCatching{client.me()}.onFailure{status=message(it)}}}},enabled=!busy,colors=ButtonDefaults.buttonColors(containerColor=Color(0xfffee500),contentColor=Color(0xff191919))){Text("카카오로 로그인")}
                OutlinedTextField(title,{if(it.length<=80)title=it},label={Text("친구에게 보여줄 제목")},modifier=Modifier.fillMaxWidth(),enabled=!busy)
                Row(verticalAlignment=Alignment.CenterVertically){Checkbox(includeMood,{includeMood=it},enabled=!busy);Text("오늘의 기분도 함께 공개 (${entry.mood})")}
                Text("${selected.size}개 선택 · 사진은 한 번에 6장까지 공개할 수 있어요.",style=MaterialTheme.typography.labelLarge)
                if(entry.blocks.isEmpty())Text("먼저 사진이나 글을 일기에 남겨 주세요.",Modifier.padding(vertical=16.dp))
            }
            items(entry.blocks,key={it.id}){block->
                val meaningful=SharedDiaryDraft.hasContent(block) && block.type!="photo-slot"
                Card(colors=CardDefaults.cardColors(containerColor=if(block.id in selected)AppPalette.selection else Color.White)){
                    Column(Modifier.fillMaxWidth().padding(12.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){
                        Row(verticalAlignment=Alignment.CenterVertically){Checkbox(block.id in selected,{yes->selected=if(yes)selected+block.id else selected-block.id},enabled=meaningful && !busy);Text(block.title.ifBlank{when(block.type){"photo","photo-slot"->"사진 기록";"emotion"->"감정 기록";"todo"->"할 일";"habit"->"습관";else->"글 일기"}},style=MaterialTheme.typography.titleMedium)}
                        if(block.id in selected)block.photo?.let{PhonePhoto(it,Modifier.fillMaxWidth().heightIn(max=200.dp))}
                        if(block.text.isNotBlank())Text(block.text.take(300)+(if(block.text.length>300) "…" else ""))
                        if(!meaningful)Text("내용을 적거나 사진을 추가한 뒤 공개할 수 있어요.",style=MaterialTheme.typography.bodySmall)
                    }
                }
            }
            item{
                if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
                if(status.isNotBlank())Text(status,color=MaterialTheme.colorScheme.primary,modifier=Modifier.padding(vertical=10.dp))
                OutlinedButton({runCatching{SharedDiaryDraft.select(entry,selected,title,includeMood)}.onSuccess{preview=it}.onFailure{status=message(it)}},enabled=selected.isNotEmpty() && !busy,modifier=Modifier.fillMaxWidth()){Text("선택한 내용 전체 미리보기")}
                Button({runCatching{SharedDiaryDraft.select(entry,selected,title,includeMood)}.onSuccess{confirm=it}.onFailure{status=message(it)}},enabled=selected.isNotEmpty() && !busy && CommunityConnection.configured && CommunityConnection.signedIn,modifier=Modifier.fillMaxWidth().testTag("publish-selected")){Text("선택한 ${selected.size}개 친구에게 공개")}
                TextButton({retracting=true},enabled=!busy && CommunityConnection.configured && CommunityConnection.signedIn){Text("이 날짜의 친구 공개 내리기")}
                Text("초대코드로 연결하고 서로 수락한 친구에게만 열려요. 친구를 끊으면 이후 조회가 차단돼요.",style=MaterialTheme.typography.bodySmall)
            }
        }
    }
    preview?.let{draft->PhonePublicDetail(SharedPostDetail(SharedPost("0","공개 미리보기",date.toString(),draft.title,draft.mood,draft.blocks.size,draft.blocks.count{it.photo!=null},""),draft.blocks),{preview=null})}
    confirm?.let{draft->AlertDialog(onDismissRequest={confirm=null},title={Text("이 내용으로 친구에게 공개할까요?")},text={Text("${date}의 기록 ${draft.blocks.size}개와 사진 ${draft.blocks.count{it.photo!=null}}장을 공개해요. 체크하지 않은 기록은 보내지 않아요.")},confirmButton={TextButton({confirm=null;scope.launch{busy=true;try{client.publish(date,draft);status="선택한 내용만 친구에게 공개했어요. 친구 탭에서 확인할 수 있어요."}catch(e: Exception){status=message(e)}finally{busy=false}}}){Text("친구에게 공개")}},dismissButton={TextButton({confirm=null}){Text("취소")}})}
    if(retracting)AlertDialog(onDismissRequest={retracting=false},title={Text("이 날짜의 공개를 내릴까요?")},text={Text("서버의 공개 글과 사진을 내려요. 휴대폰에 저장한 일기는 그대로 남아요.")},confirmButton={TextButton({retracting=false;scope.launch{busy=true;try{client.unpublish(date);status="친구 공개를 내렸어요."}catch(e: Exception){status=message(e)}finally{busy=false}}}){Text("공개 내리기")}},dismissButton={TextButton({retracting=false}){Text("취소")}})
}
