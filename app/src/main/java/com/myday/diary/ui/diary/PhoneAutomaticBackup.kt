package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.NotebookController
import kotlinx.coroutines.*

@Composable fun PhoneAutomaticBackup(controller: NotebookController,onExport: ()->Unit,onDismiss: ()->Unit) {
    val context=LocalContext.current;val backup=remember{AutomaticBackup(context.filesDir)};val scope=rememberCoroutineScope()
    var state by remember{mutableStateOf(backup.state())};var files by remember{mutableStateOf(backup.files())};var busy by remember{mutableStateOf(false)};var message by remember{mutableStateOf("")};var incoming by remember{mutableStateOf<DiaryBook?>(null)}
    fun refresh(){state=backup.state();files=backup.files()}
    PhoneFullDialog("자동 백업",onDismiss,controller.error){LazyColumn(Modifier.fillMaxSize().padding(16.dp),verticalArrangement=Arrangement.spacedBy(14.dp)){
        item{Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween){Text("저장한 날 하루 한 번 전체 백업");Switch(state.enabled,{enabled->runCatching{backup.setEnabled(enabled);refresh()}.onFailure{message=it.message ?: "설정 저장 실패"}})}
            Text("최근 3일분을 기기 안에 보관해요. 앱을 종료한 동안에는 실행하지 않아요. 백업 실패가 일기 저장을 막지 않아요.")
            Text("마지막 백업: ${state.lastSuccess.ifBlank{"아직 없음"}}\n보관 ${files.size} / 3일분")
            if(state.error.isNotBlank())Text("백업 실패: ${state.error}",color=MaterialTheme.colorScheme.error)
            if(message.isNotBlank())Text(message)
            Button({val snapshot=controller.book;busy=true;scope.launch{try{val success=withContext(Dispatchers.IO){backup.capture(NotebookJson.encode(snapshot).toByteArray(),true)};message=if(success)"전체 백업을 저장했어요." else "백업 상태를 확인해 주세요.";refresh()}finally{busy=false}}},enabled=!busy&&controller.saved){Text("지금 전체 백업")}
            OutlinedButton(onExport,enabled=controller.saved){Text("기기 밖에 백업 내보내기")}
        }
        items(files.size){index->val file=files[index];OutlinedCard{Column(Modifier.padding(12.dp)){Text(file.name.removePrefix("daily-").removeSuffix(".json"));Text("${file.length()/1024} KB",style=MaterialTheme.typography.labelSmall);TextButton({scope.launch{try{incoming=withContext(Dispatchers.IO){require(file.length()<=NotebookJson.MAX_BYTES);NotebookJson.decode(file.readText()).also(NotebookPhoto::validate)}}catch(e:Exception){message=e.message ?: "백업을 읽지 못했어요."}}},enabled=controller.saved){Text("이 백업 복원 검토")}}}}
    }}
    incoming?.let{book->AlertDialog(onDismissRequest={incoming=null},title={Text("자동 백업을 가져올까요?")},text={Text("${book.days.size}일의 기록을 가져와요. 같은 날짜는 백업 내용으로 바뀌고 성장·소품은 기존 병합 규칙으로 보존해요.")},confirmButton={TextButton({runCatching{controller.importBook(book)}.onFailure{message=it.message ?: "가져오기 실패"};incoming=null}){Text("가져오기")}},dismissButton={TextButton({incoming=null}){Text("취소")}})}
}
