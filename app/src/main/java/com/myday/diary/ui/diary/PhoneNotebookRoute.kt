package com.myday.diary.ui.diary

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.myday.diary.data.*
import com.myday.diary.diary.*
import com.myday.diary.platform.*
import com.myday.diary.ui.design.MyDayTheme
import kotlinx.coroutines.*

@Composable fun PhoneNotebookRoute() {
    val context=LocalContext.current
    val model: NotebookViewModel=viewModel()
    val controller=model.controller
    val catalog=remember(context) {NotebookCatalog(context)}
    val home=remember(context) {HomeScreenActions(context)}
    val scope=rememberCoroutineScope()
    var photoId by rememberSaveable {mutableStateOf<String?>(null)}
    var photoDate by rememberSaveable {mutableStateOf("")}
    var incoming by remember {mutableStateOf<DiaryBook?>(null)}
    var busy by remember {mutableStateOf(false)}
    var recovering by remember {mutableStateOf(false)}
    val haptics=rememberDiaryHaptics(controller?.book?.haptics ?: false)
    fun failed(e: Throwable) {controller?.report(e.message ?: "처리하지 못했어요.");haptics.perform(TactileEvent.REJECT)}
    val photo=rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) {uri->if(uri!=null && controller!=null) scope.launch {
        busy=true
        try {
            val data=withContext(Dispatchers.IO) {NotebookPhoto.read(context,uri)}
            require(controller.date.toString()==photoDate) {"사진을 선택하던 날짜가 바뀌었어요. 원하는 날짜에서 다시 선택해 주세요."}
            val existing=controller.entry.blocks.find {it.id==photoId}
            if(photoId!=null) require(existing!=null) {"사진 블록을 찾지 못했어요."}
            if(existing!=null) controller.updateBlock(existing.copy(type="photo",photo=data))
            else controller.edit(NotebookRules.append(controller.entry,listOf(DiaryBlock(type="photo",photo=data,height=360))),true)
            haptics.perform(if(controller.saved || controller.saving) TactileEvent.CHECK else TactileEvent.REJECT)
        } catch(e: Exception) {failed(e)} finally {busy=false}
    }}
    val export=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/json")) {uri->if(uri!=null && controller!=null) scope.launch {
        busy=true
        try {require(controller.saved);val snapshot=controller.book
            withContext(Dispatchers.IO) {context.contentResolver.openOutputStream(uri,"wt")?.use {it.write(NotebookJson.encode(snapshot).toByteArray(Charsets.UTF_8))} ?: error("백업 파일을 쓰지 못했어요.")}
            controller.report("백업을 저장했어요.");haptics.perform(TactileEvent.CHECK)
        } catch(e: Exception) {failed(e)} finally {busy=false}
    }}
    val import=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) {uri->if(uri!=null) scope.launch {
        busy=true
        try {incoming=withContext(Dispatchers.IO) {
            val raw=context.contentResolver.openInputStream(uri)?.use {input->val output=java.io.ByteArrayOutputStream();val buffer=ByteArray(8192);var total=0
                while(true) {val count=input.read(buffer);if(count<0) break;total+=count;require(total<=NotebookJson.MAX_BYTES) {"백업은 128MB 이하로 선택해 주세요."};output.write(buffer,0,count)};output.toString("UTF-8")} ?: error("백업을 읽지 못했어요.")
            NotebookJson.decode(raw).also(NotebookPhoto::validate)
        }} catch(e: Exception) {failed(e)} finally {busy=false}
    }}
    MyDayTheme {
        if(controller==null) Box(Modifier.fillMaxSize().systemBarsPadding().padding(24.dp),contentAlignment=Alignment.Center) {
            if(model.loadError==null) CircularProgressIndicator() else Column {Text("기록을 읽지 못했어요. 기존 저장본을 바꾸지 않았어요.");Text(model.loadError!!);Button(model::reload) {Text("다시 읽기")};if(model.recoveryAvailable)OutlinedButton({recovering=true}){Text("직전 저장본으로 복구")}}
        } else {
            PhoneNotebookScreen(controller,catalog,haptics,home,busy,
                onPhoto={id->if(controller.saved) {photoId=id;photoDate=controller.date.toString();photo.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))} else controller.report("먼저 일기를 다시 저장해 주세요.")},
                onExport={if(controller.saved) export.launch("myday-${controller.date}.json") else controller.report("먼저 일기를 다시 저장해 주세요.")},
                onImport={if(controller.saved) import.launch(arrayOf("application/json","text/plain","application/octet-stream")) else controller.report("먼저 일기를 다시 저장해 주세요.")})
        }
        if(recovering)AlertDialog(onDismissRequest={recovering=false},title={Text("직전 저장본으로 복구할까요?")},text={Text("현재 파일은 앱 저장 폴더에 별도로 보존해요. 직전 저장 이후 변경은 되돌아가요.")},confirmButton={TextButton({recovering=false;model.recover()}){Text("복구하기")}},dismissButton={TextButton({recovering=false}){Text("취소")}})
        if(busy) AlertDialog(onDismissRequest={},title={Text("기록을 준비하고 있어요")},text={LinearProgressIndicator(Modifier.fillMaxWidth())},confirmButton={})
        incoming?.let {book->AlertDialog(onDismissRequest={incoming=null},title={Text("백업을 가져올까요?")},text={Text("${book.days.size}일의 기록과 배치 ${book.layouts.size}개를 가져와요. 같은 날짜의 기록은 백업 내용으로 바뀌어요. 성장 날짜는 합치고 장비도 복원해요.")},
            confirmButton={TextButton({runCatching {controller?.importBook(book)}.onFailure(::failed);incoming=null}) {Text("가져오기")}},dismissButton={TextButton({incoming=null}) {Text("취소")}})}
    }
}
