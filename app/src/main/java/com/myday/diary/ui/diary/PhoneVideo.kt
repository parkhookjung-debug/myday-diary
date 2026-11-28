package com.myday.diary.ui.diary

import android.content.Intent
import android.net.Uri
import android.widget.MediaController
import android.widget.VideoView
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.myday.diary.data.DiaryBlock

@Composable fun PhoneVideo(block:DiaryBlock,onEdit:(DiaryBlock)->Unit) {
    val context=LocalContext.current
    var problem by remember(block.id) {mutableStateOf<String?>(null)}
    var playing by remember(block.videoPath) {mutableStateOf(false)}
    val lifecycle=androidx.lifecycle.compose.LocalLifecycleOwner.current.lifecycle
    DisposableEffect(lifecycle){val observer=androidx.lifecycle.LifecycleEventObserver {_,event->if(event==androidx.lifecycle.Lifecycle.Event.ON_PAUSE)playing=false};lifecycle.addObserver(observer);onDispose {lifecycle.removeObserver(observer)}}
    val picker=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()) {uri->if(uri!=null)runCatching {
        require(uri.scheme=="content" && context.contentResolver.getType(uri)?.startsWith("video/")==true){"동영상 파일을 선택해 주세요."}
        context.contentResolver.openFileDescriptor(uri,"r")?.use {require(it.statSize<=2000000000L){"영상은 2GB 이하로 선택해 주세요."}}?:error("영상 파일을 열지 못했어요.")
        context.contentResolver.takePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION)
        onEdit(block.copy(videoPath=uri.toString()));problem=null
    }.onFailure {problem=it.message?:"영상 파일을 열지 못했어요."}}
    Text("영상 원본은 이 기기에 보관해요. 백업을 다른 기기로 옮긴 뒤에는 영상을 다시 선택해 주세요.",style=MaterialTheme.typography.bodySmall)
    Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
        OutlinedButton({picker.launch(arrayOf("video/*"))},Modifier.testTag("video-choose-${block.id}")){Text(if(block.videoPath==null)"영상 선택" else "영상 바꾸기")}
        Button({runCatching {val uri=Uri.parse(block.videoPath);require(uri.scheme=="content");context.contentResolver.openFileDescriptor(uri,"r")?.close()?:error("영상 없음");playing=true;problem=null}.onFailure {problem="원본 영상을 찾지 못했어요. 이 기기의 영상을 다시 선택해 주세요."}},enabled=block.videoPath!=null,modifier=Modifier.testTag("video-play-${block.id}")){Text("재생")}
    }
    problem?.let {Text(it,color=MaterialTheme.colorScheme.error)}
    if(playing)Dialog(onDismissRequest={playing=false},properties=DialogProperties(usePlatformDefaultWidth=false)) {
        Surface(Modifier.fillMaxWidth().padding(8.dp)) {Column {
            Row(Modifier.fillMaxWidth()){Text("영상 재생",Modifier.weight(1f).padding(12.dp));TextButton({playing=false}){Text("닫기")}}
            AndroidView(modifier=Modifier.fillMaxWidth().height(300.dp),factory={ctx->VideoView(ctx).apply {val video=this;setMediaController(MediaController(ctx).apply {setAnchorView(video)});setOnErrorListener {_,_,_->problem="이 기기에서 재생하지 못하는 영상이에요. MP4(H.264)로 다시 선택해 주세요.";playing=false;true};setVideoURI(Uri.parse(block.videoPath));setOnPreparedListener {start()} }},onRelease={it.stopPlayback()})
        }}
    }
}
