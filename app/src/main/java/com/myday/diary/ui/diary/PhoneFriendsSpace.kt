package com.myday.diary.ui.diary

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.myday.diary.social.*
import com.myday.diary.ui.design.AppPalette
import kotlinx.coroutines.launch
import java.time.LocalDate

@Composable internal fun PhoneFriends(onPublish: ()->Unit, canPublish: Boolean) {
    val context=LocalContext.current; val scope=rememberCoroutineScope(); val client=remember{FriendsClient()}
    var profile by remember{mutableStateOf<FriendProfile?>(null)}
    var friends by remember{mutableStateOf(emptyList<FriendProfile>())}
    var requests by remember{mutableStateOf(emptyList<FriendRequest>())}
    var posts by remember{mutableStateOf(emptyList<SharedPost>())}
    var mine by remember{mutableStateOf(false)}; var filter by remember{mutableStateOf<String?>(null)}
    var busy by remember{mutableStateOf(false)}; var status by remember{mutableStateOf("")}
    var detail by remember{mutableStateOf<SharedPostDetail?>(null)}
    var invite by remember{mutableStateOf<FriendInvite?>(null)}; var entering by remember{mutableStateOf(false)}
    var code by remember{mutableStateOf("")}; var managing by remember{mutableStateOf(false)}
    var removing by remember{mutableStateOf<FriendProfile?>(null)}
    var clearing by remember{mutableStateOf(false)}; var logout by remember{mutableStateOf(false)}
    suspend fun refresh() {
        if(!CommunityConnection.hasToken()){profile=null;posts=emptyList();return}
        profile=client.me(); friends=client.friends(); requests=client.requests()
        if(filter!=null && friends.none{it.id==filter})filter=null
        posts=client.posts(mine,if(mine)null else filter)
    }
    fun action(work: suspend ()->Unit) { scope.launch{busy=true;status="";try{work()}catch(e: Exception){status=message(e);if(e is FriendsFailure && e.status==401)profile=null}finally{busy=false}} }
    LaunchedEffect(mine,filter,CommunityConnection.revision){busy=true;try{refresh()}catch(e: Exception){posts=emptyList();status=message(e)}finally{busy=false}}
    LazyColumn(Modifier.fillMaxSize().padding(horizontal=16.dp).testTag("friends-space"),verticalArrangement=Arrangement.spacedBy(10.dp),contentPadding=PaddingValues(vertical=16.dp)) {
        item {
            Text("친구의 하루에 놀러 가요",style=MaterialTheme.typography.headlineSmall)
            Spacer(Modifier.height(8.dp))
            if(!CommunityConnection.configured) {
                ConnectionNotice()
                Button(onPublish,enabled=canPublish,modifier=Modifier.fillMaxWidth()){Text("공개할 내용 미리 골라 보기")}
            } else if(profile==null) {
                Text("카카오로 로그인한 뒤 초대코드를 주고받으세요. 서로 수락한 친구만 공개한 일기를 볼 수 있어요.")
                Button({busy=true;CommunityConnection.login(context){error->busy=false;if(error!=null)status=message(error)}},enabled=!busy,
                    modifier=Modifier.fillMaxWidth().testTag("kakao-login"),colors=ButtonDefaults.buttonColors(containerColor=Color(0xfffee500),contentColor=Color(0xff191919))){Text("카카오로 시작하기")}
            } else {
                Row(verticalAlignment=Alignment.CenterVertically){Text("${profile!!.nickname}님의 친구 공간",Modifier.weight(1f),style=MaterialTheme.typography.titleMedium);TextButton({logout=true},enabled=!busy){Text("로그아웃")}}
                Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                    Button({action{invite=client.createInvite()}},enabled=!busy,modifier=Modifier.weight(1f).testTag("create-invite")){Text("친구 초대")}
                    OutlinedButton({entering=true},enabled=!busy,modifier=Modifier.weight(1f)){Text("코드 입력")}
                }
                TextButton({managing=true},enabled=!busy){Text("친구 관리 · 받은 요청 ${requests.count{it.incoming}}개")}
                Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                    FilterChip(!mine,{mine=false},label={Text("친구 일기")},enabled=!busy)
                    FilterChip(mine,{mine=true},label={Text("내 공개 일기")},enabled=!busy)
                    TextButton({action{refresh()}},enabled=!busy){Text("새로고침")}
                }
                if(!mine && friends.isNotEmpty())LazyRow(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                    item{FilterChip(filter==null,{filter=null},label={Text("전체")},enabled=!busy)}
                    items(friends,key={it.id}){friend->FilterChip(filter==friend.id,{filter=friend.id},label={Text(friend.nickname)},enabled=!busy)}
                }
                OutlinedButton(onPublish,enabled=canPublish && !busy,modifier=Modifier.fillMaxWidth()){Text("현재 날짜의 일기 공개하기")}
            }
            if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
            if(CommunityConnection.authMessage.isNotBlank())Text(CommunityConnection.authMessage,style=MaterialTheme.typography.bodySmall)
            if(status.isNotBlank())Text(status,color=MaterialTheme.colorScheme.primary,modifier=Modifier.padding(vertical=8.dp))
        }
        if(profile!=null) {
            if(posts.isEmpty() && !busy)item{Text(if(mine)"아직 공개한 일기가 없어요. 보여줄 기록만 골라 공개해 보세요." else "아직 볼 수 있는 일기가 없어요. 친구를 초대하고 서로 공개한 하루를 나눠 보세요.",Modifier.padding(vertical=16.dp))}
            items(posts,key={it.ownerId+"/"+it.date}){post->Card(colors=CardDefaults.cardColors(containerColor=Color.White)){
                Column(Modifier.fillMaxWidth().padding(16.dp),verticalArrangement=Arrangement.spacedBy(7.dp)){
                    Text("${post.nickname} · ${post.date}",style=MaterialTheme.typography.labelMedium,color=AppPalette.olive)
                    Text(post.title.ifBlank{"${post.date}의 하루"},style=MaterialTheme.typography.titleMedium)
                    if(post.mood.isNotBlank())Text(post.mood)
                    Text("기록 ${post.blockCount}개 · 사진 ${post.photoCount}장",style=MaterialTheme.typography.bodySmall)
                    TextButton({action{detail=client.detail(post)}},enabled=!busy){Text("일기 보러 가기")}
                    if(mine)TextButton({action{client.unpublish(LocalDate.parse(post.date));refresh();status="친구 공개를 내렸어요."}},enabled=!busy){Text("친구 공개 내리기")}
                }
            }}
            if(mine)item{TextButton({clearing=true},enabled=!busy){Text("공개 기록과 친구 연결 모두 지우기")}}
        }
    }
    detail?.let{PhonePublicDetail(it,{detail=null})}
    invite?.let{value->AlertDialog(onDismissRequest={invite=null},title={Text("친구를 초대해요")},text={Column(verticalArrangement=Arrangement.spacedBy(12.dp)){
        Text(FriendsClient.formatCode(value.code),style=MaterialTheme.typography.headlineSmall)
        Text("생성 후 24시간 동안 한 번 사용할 수 있어요. 친구가 코드를 입력하면 이곳의 ‘친구 관리’에서 요청을 수락해 주세요.")
        Text("친구도 마이데이 친구 앱을 설치해야 해요. 새 코드를 만들면 이전 코드는 만료돼요.",style=MaterialTheme.typography.bodySmall)
        OutlinedButton({(context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager).setPrimaryClip(ClipData.newPlainText("친구 초대코드",FriendsClient.formatCode(value.code)));status="초대코드를 복사했어요."}){Text("코드 복사")}
    }},confirmButton={TextButton({
        val share=Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT,"마이데이 친구에서 하루를 나눠요!\n초대코드: ${FriendsClient.formatCode(value.code)}\n앱의 친구 일기 → 코드 입력에 넣어 주세요. 요청을 확인하고 수락할게요.\n코드는 생성 후 24시간 동안 한 번 사용할 수 있어요.")
        runCatching{context.startActivity(Intent(share).setPackage("com.kakao.talk"))}.onFailure{context.startActivity(Intent.createChooser(share,"초대코드 보내기"))}
    }){Text("카톡으로 보내기")}},dismissButton={TextButton({invite=null}){Text("닫기")}})}
    if(entering)AlertDialog(onDismissRequest={entering=false},title={Text("친구 초대코드 입력")},text={Column{
        OutlinedTextField(code,{code=it.take(32)},label={Text("초대코드")},singleLine=true)
        Text("코드를 입력한 뒤 상대가 요청을 수락하면 친구가 돼요.",style=MaterialTheme.typography.bodySmall)
    }},confirmButton={TextButton({entering=false;action{client.redeem(code);code="";refresh();status="친구 요청을 보냈어요. 상대가 수락하면 일기를 볼 수 있어요."}},enabled=code.isNotBlank() && !busy){Text("친구 요청")}},dismissButton={TextButton({entering=false}){Text("취소")}})
    if(managing)AlertDialog(onDismissRequest={managing=false},title={Text("친구와 받은 요청")},text={Column(Modifier.heightIn(max=420.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
        requests.forEach{request->Column{
            Text(request.nickname,style=MaterialTheme.typography.titleMedium)
            if(request.incoming)Row{TextButton({action{client.answer(request.id,true);refresh();status="친구로 연결됐어요."}},enabled=!busy){Text("수락")};TextButton({action{client.answer(request.id,false);refresh()}},enabled=!busy){Text("거절")}}
            else Text("상대의 수락을 기다리고 있어요.",style=MaterialTheme.typography.bodySmall)
        }}
        if(requests.isEmpty())Text("새 친구 요청이 없어요.")
        HorizontalDivider()
        friends.forEach{friend->Row(verticalAlignment=Alignment.CenterVertically){Text(friend.nickname,Modifier.weight(1f));TextButton({removing=friend},enabled=!busy){Text("친구 끊기")}}}
        if(friends.isEmpty())Text("아직 연결한 친구가 없어요.")
    }},confirmButton={TextButton({action{refresh()}},enabled=!busy){Text("새로고침")}},dismissButton={TextButton({managing=false}){Text("닫기")}})
    removing?.let{friend->AlertDialog(onDismissRequest={removing=null},title={Text("${friend.nickname}님과 친구를 끊을까요?")},text={Text("이후 서로의 공개 일기를 볼 수 없어요. 다시 연결하려면 초대와 수락을 다시 해야 해요.")},confirmButton={TextButton({removing=null;detail=null;action{client.removeFriend(friend.id);filter=null;refresh()}}){Text("친구 끊기")}},dismissButton={TextButton({removing=null}){Text("취소")}})}
    if(logout)AlertDialog(onDismissRequest={logout=false},title={Text("로그아웃할까요?")},text={Text("공개한 일기는 남아 있어요. 공개를 내리려면 내 공개 일기에서 먼저 내려 주세요.")},confirmButton={TextButton({logout=false;CommunityConnection.logout{profile=null;posts=emptyList();friends=emptyList();requests=emptyList();detail=null}}){Text("로그아웃")}},dismissButton={TextButton({logout=false}){Text("취소")}})
    if(clearing)AlertDialog(onDismissRequest={clearing=false},title={Text("공개 기록과 친구 연결을 지울까요?")},text={Text("공개한 글·사진과 친구·초대 연결을 모두 지워요. 휴대폰의 개인 일기는 유지되고 카카오 계정으로 다시 로그인할 수 있어요.")},confirmButton={TextButton({clearing=false;detail=null;action{client.clearShared();filter=null;refresh();status="공개 기록과 친구 연결을 지웠어요."}}){Text("모두 지우기")}},dismissButton={TextButton({clearing=false}){Text("취소")}})
}
