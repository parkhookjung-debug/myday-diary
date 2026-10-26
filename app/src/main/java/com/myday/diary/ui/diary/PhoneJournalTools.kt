package com.myday.diary.ui.diary

import android.Manifest
import android.content.Intent
import android.net.Uri
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.NotebookController
import com.myday.diary.platform.JournalReminder
import kotlinx.coroutines.*
import java.net.HttpURLConnection
import java.net.URL
import java.time.YearMonth
import org.json.JSONObject

@Composable fun PhoneJournalTools(controller: NotebookController,onExport: ()->Unit,onDismiss: ()->Unit,onOpenDay: (java.time.LocalDate)->Unit={controller.selectDate(it)}) {
    val context=LocalContext.current;val scope=rememberCoroutineScope();val archive=remember {JournalHistory(context.filesDir)}
    var tab by remember {mutableIntStateOf(0)};var revisions by remember {mutableStateOf(emptyList<JournalRevision>())}
    var selected by remember {mutableStateOf<JournalRevision?>(null)};var confirm by remember {mutableStateOf("")}
    var message by remember {mutableStateOf("")};var busy by remember {mutableStateOf(false)}
    var album by remember {mutableStateOf(false)};var backups by remember {mutableStateOf(false)}
    var month by remember {mutableStateOf(YearMonth.from(controller.date))}
    val prefs=remember {JournalReminder.preferences(context)};var enabled by remember {mutableStateOf(prefs.getBoolean("enabled",false))}
    var hour by remember {mutableStateOf((prefs.getInt("minute",1200)/60).toString())};var minute by remember {mutableStateOf((prefs.getInt("minute",1200)%60).toString())}
    fun refresh() {scope.launch {try {revisions=withContext(Dispatchers.IO) {archive.list()}}catch(e: Exception){message=e.message ?: "이력을 읽지 못했어요."}}}
    fun saveReminder() {runCatching {val h=hour.toInt();val m=minute.toInt();require(h in 0..23 && m in 0..59);JournalReminder.configure(context,enabled,h*60+m);message="알림 설정을 저장했어요."}.onFailure {message=it.message ?: "시간을 확인해 주세요."}}
    val permission=rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) {granted->if(granted) saveReminder() else {enabled=false;message="알림 권한이 꺼져 있어요. 시스템 설정에서 허용할 수 있어요."}}
    LaunchedEffect(Unit) {refresh()}
    LaunchedEffect(controller.book,controller.saving) {if(!controller.saving) refresh()}
    PhoneFullDialog("나의 기록 도구 · ${JournalPolicy.appVersion}",onDismiss,controller.error) {
        Row(Modifier.fillMaxWidth().padding(horizontal=16.dp)){TextButton({album=true}){Text("사진 모아보기")};TextButton({backups=true}){Text("자동 백업")}}
        ScrollableTabRow(tab) {listOf("1. 복구","2. 리포트","3. 알림","4. Wi-Fi·QR","6. 업데이트").forEachIndexed {i,title->Tab(tab==i,{tab=i},text={Text(title)})}}
        if(message.isNotEmpty()) Text(message,Modifier.padding(12.dp),style=MaterialTheme.typography.bodySmall)
        LazyColumn(Modifier.weight(1f).padding(horizontal=16.dp),verticalArrangement=Arrangement.spacedBy(14.dp),contentPadding=PaddingValues(vertical=18.dp)) {
            when(tab) {
                0->{
                    item {Text("수정 전 기록을 5분 간격으로 보관하고 블록·사진 삭제 전에는 바로 보관해요. 이력은 이 기기에 남아요.");OutlinedButton({confirm="day"},enabled=controller.saved) {Text("현재 날짜 일기를 휴지통으로")}}
                    items(revisions,key={it.file.name}) {item->OutlinedCard {Column(Modifier.padding(12.dp)) {
                        Text("${item.date} · ${item.reason}",style=MaterialTheme.typography.titleMedium);Text(item.captured,style=MaterialTheme.typography.labelSmall)
                        Text("이 시점의 글·사진·배치를 복원할 수 있어요.",style=MaterialTheme.typography.bodySmall)
                        Row {TextButton({selected=item;confirm="restore"},enabled=controller.saved) {Text("이 버전 복원")};TextButton({selected=item;confirm="remove"}) {Text("이력 영구 삭제")}}
                    }}}
                    if(revisions.isEmpty()) item {Text("아직 보관된 이전 버전이 없어요.")}
                }
                1->{item {Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {TextButton({month=month.minusMonths(1)},enabled=month.year>1 || month.monthValue>1) {Text("‹")};Text(month.toString());TextButton({month=month.plusMonths(1)},enabled=month.year<9999 || month.monthValue<12) {Text("›")}};Text(JournalReport.forMonth(controller.book,month).summary(),style=MaterialTheme.typography.titleMedium)}}
                2->{item {
                    Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween) {Text("매일 일기 알림");Switch(enabled,{enabled=it})}
                    Row(horizontalArrangement=Arrangement.spacedBy(12.dp)) {OutlinedTextField(hour,{hour=it.take(2)},label={Text("시 (0~23)")},modifier=Modifier.weight(1f),singleLine=true);OutlinedTextField(minute,{minute=it.take(2)},label={Text("분 (0~59)")},modifier=Modifier.weight(1f),singleLine=true)}
                    Button({if(enabled && Build.VERSION.SDK_INT>=33 && !JournalReminder.allowed(context)) permission.launch(Manifest.permission.POST_NOTIFICATIONS) else saveReminder()}) {Text("알림 설정 적용")}
                    Text("휴대폰 절전 상태에 따라 시간이 늦어질 수 있어요. 매일 한 번만 예약하고 재부팅·시간대 변경 시 다시 예약해요. 알림을 끄면 예약도 취소해요.")
                }}
                3->{item {PhoneDeviceLink(controller)}}
                else->{item {Text("현재 버전 ${JournalPolicy.appVersion}",style=MaterialTheme.typography.titleLarge);Text("복구 보관함·월간 리포트·알림·Wi-Fi 전송 도구·상몬 반응을 추가했어요.")
                    Button({scope.launch {busy=true;try {message=withContext(Dispatchers.IO) {checkJournalRelease()}}catch(_: Exception){message="버전 확인 실패 · 연결을 확인하고 다시 시도해 주세요."}finally{busy=false}}},enabled=!busy) {Text("최신 정식 버전 확인")}
                    OutlinedButton(onExport,enabled=controller.saved) {Text("1. 업데이트 전 백업 저장")}
                    OutlinedButton({context.startActivity(Intent(Intent.ACTION_VIEW,Uri.parse("https://github.com/parkhookjung-debug/myday-diary/actions/workflows/android.yml")))}) {Text("2. Android 개발 빌드 받기 (Actions)")}
                    Text("같은 앱 ID와 서명의 APK를 설치하면 기록이 유지돼요. 서명이 다르다는 안내가 나오면 삭제하지 말고 먼저 백업하세요. 개발 빌드는 Actions에서 받아요.")
                }}
            }
        }
    }
    if(album)PhoneFullDialog("사진 모아보기",{album=false},controller.error){PhonePhotoAlbum(controller.book,controller.date){day->onOpenDay(day);if(controller.saved&&controller.error==null)album=false}}
    if(backups)PhoneAutomaticBackup(controller,onExport){backups=false}
    if(confirm.isNotEmpty()) AlertDialog(onDismissRequest={confirm=""},title={Text(when(confirm){"day"->"일기를 휴지통으로 옮길까요?";"remove"->"이 복구 사본을 영구 삭제할까요?";else->"이 버전으로 복원할까요?"})},text={Text(if(confirm=="remove") "이 사본은 다시 복원할 수 없어요." else "이전 글·사진을 보관해요. 성장 점수와 재화는 바뀌지 않아요.")},confirmButton={TextButton({val action=confirm;confirm="";scope.launch {try {
        when(action) {"day"->controller.deleteDay();"restore"->selected?.let {item->val restored=withContext(Dispatchers.IO) {controller.book.days[item.date]?.let {archive.capture(item.date,it,"복원 전",true)};item.entry};controller.restoreRevision(item.date,restored)};"remove"->selected?.let {withContext(Dispatchers.IO) {archive.remove(it)}}};refresh()
    }catch(e: Exception){message=e.message ?: "처리하지 못했어요."}}}) {Text("확인")}},dismissButton={TextButton({confirm=""}) {Text("취소")}})
}
internal fun checkJournalRelease(): String {
    val connection=URL("https://api.github.com/repos/parkhookjung-debug/myday-diary/releases/latest").openConnection() as HttpURLConnection
    try {connection.connectTimeout=15000;connection.readTimeout=15000;connection.instanceFollowRedirects=false;connection.setRequestProperty("User-Agent","MyDay/${JournalPolicy.appVersion}")
        if(connection.responseCode==404)return "아직 정식 릴리스가 없어요. 개발 빌드는 Actions에 있어요."
        require(connection.responseCode==200)
        val bytes=connection.inputStream.use {it.readBytesBounded(1048576)};val root=JSONObject(String(bytes,Charsets.UTF_8));val version=root.getString("tag_name").removePrefix("v")
        val latest=version.split('.').map {it.toInt()};val current=JournalPolicy.appVersion.split('.').map {it.toInt()};require(latest.size==3)
        return if(latest.zip(current).firstOrNull {it.first!=it.second}?.let {it.first>it.second}==true) "새 버전 $version\n${root.optString("body")}" else "현재 버전보다 새로운 정식 릴리스가 없어요."
    }finally {connection.disconnect()}
}
private fun java.io.InputStream.readBytesBounded(limit: Int): ByteArray {val output=java.io.ByteArrayOutputStream();val buffer=ByteArray(8192);while(true){val n=read(buffer);if(n<0)break;require(output.size()+n<=limit);output.write(buffer,0,n)};return output.toByteArray()}
