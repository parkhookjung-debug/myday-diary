package com.myday.diary.ui.diary

import android.annotation.SuppressLint
import android.graphics.BitmapFactory
import android.util.Base64
import android.webkit.*
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.myday.diary.diary.GameScoreStore
import org.json.JSONObject
import java.io.ByteArrayInputStream

@SuppressLint("SetJavaScriptEnabled")
@Composable fun PhonePlayroom(onDismiss:()->Unit) {
    val context=LocalContext.current
    val scores=remember {GameScoreStore(context.filesDir)}
    var web by remember {mutableStateOf<WebView?>(null)}
    var image by remember {mutableStateOf<Pair<String,ByteArray>?>(null)}
    var error by remember {mutableStateOf<String?>(null)}
    fun reply(token:String,ok:Boolean,message:String) {web?.evaluateJavascript("MyDayPlayroom.reply(${JSONObject.quote(token)},$ok,${JSONObject.quote(message)})",null)}
    val export=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("image/png")) {uri->
        val pending=image;image=null
        if(pending!=null) {if(uri==null)reply(pending.first,false,"이미지 저장을 취소했어요.") else runCatching {context.contentResolver.openOutputStream(uri)?.use {it.write(pending.second)}?:error("저장할 파일을 열지 못했어요.")}.fold({reply(pending.first,true,"결과 이미지를 저장했어요.")},{reply(pending.first,false,it.message?:"이미지 저장에 실패했어요.")})}
    }
    val lifecycle=LocalLifecycleOwner.current.lifecycle
    DisposableEffect(lifecycle,web) {val observer=LifecycleEventObserver {_,event->if(event==Lifecycle.Event.ON_PAUSE){web?.evaluateJavascript("MyDayPlayroom.pause()",null);web?.onPause()}else if(event==Lifecycle.Event.ON_RESUME)web?.onResume()};lifecycle.addObserver(observer);onDispose {lifecycle.removeObserver(observer)}}
    Dialog(onDismissRequest=onDismiss,properties=DialogProperties(usePlatformDefaultWidth=false)) {
        Surface(Modifier.fillMaxSize()) {Column {
            Row(Modifier.fillMaxWidth().padding(8.dp)) {Text("상몬 놀이터",Modifier.weight(1f));TextButton(onDismiss){Text("닫기")}}
            error?.let {Text(it,Modifier.padding(12.dp),color=MaterialTheme.colorScheme.error)}
            AndroidView(modifier=Modifier.fillMaxWidth().weight(1f),factory={ctx->WebView(ctx).apply {
                web=this;settings.javaScriptEnabled=true;settings.allowFileAccess=false;settings.allowContentAccess=false;settings.domStorageEnabled=false;settings.javaScriptCanOpenWindowsAutomatically=false;settings.setSupportMultipleWindows(false);settings.mixedContentMode=WebSettings.MIXED_CONTENT_NEVER_ALLOW
                webViewClient=object:WebViewClient() {
                    override fun shouldOverrideUrlLoading(view:WebView?,request:WebResourceRequest?)=true
                    override fun shouldInterceptRequest(view:WebView?,request:WebResourceRequest?)=WebResourceResponse("text/plain","UTF-8",ByteArrayInputStream(ByteArray(0)))
                }
                addJavascriptInterface(object {
                    @JavascriptInterface fun post(text:String) {if(text.length>2100000)return;post {runCatching {
                        val msg=JSONObject(text);val token=msg.optString("token");require(token.length<=150)
                        when(msg.getString("type")) {
                            "ready"->{val rows=JSONObject();scores.read().forEach {rows.put(it.id,it.best)};val atlas=context.assets.open("sangmon/original.png").use {Base64.encodeToString(it.readBytes(),Base64.NO_WRAP)};val bootstrap=JSONObject().put("records",rows).put("atlas","data:image/png;base64,$atlas");evaluateJavascript("MyDayPlayroom.initialize($bootstrap)",null)}
                            "score"->{scores.record(msg.getString("round"),msg.getString("id"),msg.getInt("score"));reply(token,true,"점수를 저장했어요.")}
                            "image"->{require(image==null){"이미지 저장이 진행 중이에요."};val raw=msg.getString("png");require(raw.length<=2000000);val bytes=Base64.decode(raw,Base64.DEFAULT);val bounds=BitmapFactory.Options().apply {inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,bounds);require(bounds.outWidth==960&&bounds.outHeight==720&&bounds.outMimeType=="image/png");image=token to bytes;export.launch("상몬-게임-결과.png")}
                            else->error("알 수 없는 게임 요청이에요.")
                        }
                    }.onFailure {problem->error=problem.message?:"게임 저장소를 열지 못했어요.";runCatching {reply(JSONObject(text).optString("token"),false,error!!)}}}}
                },"MyDayNative")
                loadDataWithBaseURL("https://myday.invalid/playroom/",context.assets.open("playroom.html").bufferedReader().use {it.readText()},"text/html","UTF-8",null)
            }},onRelease={it.removeJavascriptInterface("MyDayNative");it.stopLoading();it.destroy();web=null})
        }}
    }
}
