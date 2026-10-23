package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.myday.diary.chat.*
import com.myday.diary.ui.design.AppPalette
import kotlinx.coroutines.*
import java.util.UUID

@Composable fun PhoneLounge() {
    var address by remember {mutableStateOf("http://10.0.2.2:8765/")}
    var nickname by remember {mutableStateOf("")};var code by remember {mutableStateOf("")}
    var text by remember {mutableStateOf("")};var status by remember {mutableStateOf("같은 Wi-Fi의 PC 서버 주소와 초대코드로 입장해요.")}
    var messages by remember {mutableStateOf(emptyList<LoungeMessage>())};var users by remember {mutableStateOf(emptyList<String>())}
    var client by remember {mutableStateOf<LoungeClient?>(null)};var connection by remember {mutableStateOf<Job?>(null)}
    var connected by remember {mutableStateOf(false)};var sending by remember {mutableStateOf(false)}
    var pendingText by remember {mutableStateOf("")};var pendingID by remember {mutableStateOf("")}
    val scope=rememberCoroutineScope();val lifecycle=LocalLifecycleOwner.current.lifecycle
    fun disconnect() {client?.stop();connection?.cancel()}
    DisposableEffect(lifecycle) {val observer=LifecycleEventObserver {_,event->if(event==Lifecycle.Event.ON_STOP)disconnect()};lifecycle.addObserver(observer)
        onDispose {lifecycle.removeObserver(observer);disconnect()}}
    Column(Modifier.fillMaxSize().padding(16.dp),verticalArrangement=Arrangement.spacedBy(10.dp)) {
        Text("함께 쓰는 작은 이야기",style=MaterialTheme.typography.headlineSmall)
        Text("Windows · Mac · Android 상몬 라운지",style=MaterialTheme.typography.labelSmall)
        if(!connected && connection==null) {
            OutlinedTextField(address,{address=it},label={Text("서버 주소")},modifier=Modifier.fillMaxWidth(),singleLine=true)
            OutlinedTextField(nickname,{nickname=it},label={Text("닉네임 2~20자")},modifier=Modifier.fillMaxWidth(),singleLine=true)
            OutlinedTextField(code,{code=it},label={Text("초대코드")},visualTransformation=PasswordVisualTransformation(),modifier=Modifier.fillMaxWidth(),singleLine=true)
            Button({
                val c=runCatching {LoungeClient(address.trim())}.getOrElse {status=it.message ?: "주소를 확인해 주세요.";return@Button};client=c
                connection=scope.launch {
                    try {
                        nickname=withContext(Dispatchers.IO) {c.join(nickname,code)};connected=true;messages=emptyList();code="";status="입장했어요.";var cursor=0L;var failures=0
                        while(isActive) {
                            try {val page=withContext(Dispatchers.IO) {c.poll(cursor)};cursor=page.cursor
                                messages=((if(page.reset) emptyList() else messages)+page.messages).distinctBy {it.id}.sortedBy {it.id}.takeLast(ChatContract.ChatVisibleHistory)
                                users=page.users;failures=0;status="연결됨 · ${users.size}명";delay(maxOf(250,page.delay))
                            } catch(e: Exception) {if(e is CancellationException)throw e;if(e is LoungeFailure && e.status in listOf(401,403,503) || ++failures>=3)throw e;status="연결 재시도 중…";delay(5000)}
                        }
                    } catch(e: Exception) {if(e !is CancellationException)status=e.message ?: "서버에 연결하지 못했어요."}
                    finally {withContext(NonCancellable+Dispatchers.IO) {c.leave()};connected=false;sending=false;client=null;connection=null;users=emptyList()}
                }
            },modifier=Modifier.fillMaxWidth()) {Text("입장하기")}
        } else TextButton({disconnect();status="채팅방에서 나왔어요."}) {Text("나가기")}
        Text(status,style=MaterialTheme.typography.bodySmall)
        LazyColumn(Modifier.weight(1f).fillMaxWidth(),verticalArrangement=Arrangement.spacedBy(8.dp)) {
            items(messages,key={it.id}) {m->Surface(color=if(m.nickname==nickname)AppPalette.chatMine else AppPalette.chatOther,shape=MaterialTheme.shapes.medium) {Column(Modifier.fillMaxWidth().padding(12.dp)) {Text(m.nickname,style=MaterialTheme.typography.labelSmall);Text(m.text)}}}
        }
        if(connected) {
            OutlinedTextField(text,{text=it},label={Text("메시지 · 최대 2,000자")},modifier=Modifier.fillMaxWidth(),maxLines=4)
            Button({val c=client ?: return@Button;val sent=text;if(pendingText!=sent || pendingID.isEmpty()){pendingText=sent;pendingID=UUID.randomUUID().toString().replace("-","")};sending=true
                scope.launch {try {withContext(Dispatchers.IO) {c.send(sent,pendingID)};if(text==sent)text="";pendingID="";status="전송했어요."}catch(e: Exception){if(e !is CancellationException)status=e.message ?: "전송 실패 · 다시 시도해 주세요."}finally{sending=false}}
            },enabled=!sending && text.isNotBlank() && text.codePointCount(0,text.length)<=ChatContract.ChatMaxText,modifier=Modifier.fillMaxWidth()) {Text(if(sending) "전송 중…" else "전송")}
        }
    }
}
