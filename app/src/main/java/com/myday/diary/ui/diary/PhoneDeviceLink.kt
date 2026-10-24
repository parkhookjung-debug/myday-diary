package com.myday.diary.ui.diary

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.unit.dp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.myday.diary.data.*
import com.myday.diary.device.*
import com.myday.diary.diary.NotebookController
import com.google.zxing.*
import com.google.zxing.common.HybridBinarizer
import com.google.zxing.qrcode.QRCodeReader
import com.google.zxing.qrcode.QRCodeWriter
import kotlinx.coroutines.*

@Composable fun PhoneDeviceLink(controller: NotebookController) {
    val context=LocalContext.current;val scope=rememberCoroutineScope();val clipboard=LocalClipboardManager.current
    var server by remember {mutableStateOf<DeviceLinkServer?>(null)};var ips by remember {mutableStateOf(emptyList<String>())};var host by remember {mutableStateOf("")}
    var address by remember {mutableStateOf("")};var message by remember {mutableStateOf("연결할 때만 같은 Wi-Fi에 임시로 열려요.")}
    var busy by remember {mutableStateOf(false)};var remaining by remember {mutableIntStateOf(0)};var incoming by remember {mutableStateOf<DiaryBook?>(null)}
    val lifecycle=LocalLifecycleOwner.current.lifecycle
    fun stop(){server?.close();server=null;remaining=0}
    DisposableEffect(lifecycle){val observer=LifecycleEventObserver {_,event->if(event==Lifecycle.Event.ON_STOP)stop()};lifecycle.addObserver(observer);onDispose{lifecycle.removeObserver(observer);stop()}}
    LaunchedEffect(Unit){runCatching {withContext(Dispatchers.IO){DeviceAddress.addresses()}}.onSuccess {ips=it;host=it.firstOrNull() ?: ""}.onFailure {message="Wi-Fi 주소를 읽지 못했어요."}}
    LaunchedEffect(server){while(server!=null){remaining=server?.remaining ?: 0;if(server?.active!=true){stop();message="연결 시간이 끝났어요. 새 QR을 만들어 주세요.";break};delay(1000)}}
    val qr by produceState<Bitmap?>(null,server?.invitation){val text=server?.invitation
        value=if(text==null)null else withContext(Dispatchers.Default){val matrix=QRCodeWriter().encode(text,BarcodeFormat.QR_CODE,420,420);Bitmap.createBitmap(420,420,Bitmap.Config.ARGB_8888).apply {val pixels=IntArray(420*420){i->if(matrix[i%420,i/420]) android.graphics.Color.BLACK else android.graphics.Color.WHITE};setPixels(pixels,0,420,0,0,420,420)}}
    }
    val photo=rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()){uri->if(uri!=null)scope.launch{try{address=withContext(Dispatchers.IO){
        val bytes=context.contentResolver.openInputStream(uri)?.use{DeviceAddress.readBounded(it,20*1024*1024)} ?: error("QR 사진을 읽지 못했어요.")
        val info=BitmapFactory.Options().apply{inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,info);require(info.outWidth>0 && info.outHeight>0 && info.outWidth.toLong()*info.outHeight<=50000000)
        var sample=1;while(maxOf(info.outWidth,info.outHeight)/sample>2000)sample*=2
        val bitmap=BitmapFactory.decodeByteArray(bytes,0,bytes.size,BitmapFactory.Options().apply{inSampleSize=sample}) ?: error("QR 사진을 확인해 주세요.")
        try{val pixels=IntArray(bitmap.width*bitmap.height);bitmap.getPixels(pixels,0,bitmap.width,0,0,bitmap.width,bitmap.height);QRCodeReader().decode(BinaryBitmap(HybridBinarizer(RGBLuminanceSource(bitmap.width,bitmap.height,pixels)))).text.also{DeviceAddress.parse(it)}}finally{bitmap.recycle()}
    };message="QR의 연결 주소를 읽었어요. 기록 받기를 눌러 주세요."}catch(_:Exception){message="MyDay QR을 읽지 못했어요. 연결 주소를 복사해 입력해도 돼요."}}}
    Column(verticalArrangement=Arrangement.spacedBy(12.dp)) {
        Text("같은 Wi-Fi에서 기기 연결",style=MaterialTheme.typography.titleLarge)
        Text("QR 또는 주소로 연결하고, 받은 기록은 확인한 뒤 가져와요. Python 설치는 필요 없어요.")
        if(ips.isEmpty())Text("Wi-Fi의 사설 IPv4 주소를 찾지 못했어요. Wi-Fi 연결을 확인해 주세요.")
        ips.forEach {ip->FilterChip(host==ip,{host=ip},label={Text(ip)},enabled=server==null)}
        Button({val snapshot=controller.book;busy=true;scope.launch{try{stop();server=withContext(Dispatchers.IO){DeviceLinkServer(host,NotebookJson.encode(snapshot).toByteArray(),context.assets.open("device-link.html").use{it.readBytes()})};message="다른 기기에서 QR을 스캔하거나 주소를 입력해 주세요."}catch(e:Exception){message=e.message ?: "연결을 열지 못했어요."}finally{busy=false}}},enabled=controller.saved && !busy && host.isNotEmpty(),modifier=Modifier.testTag("device-start")){Text("이 기기의 연결 QR 만들기")}
        qr?.let{Image(it.asImageBitmap(),"기기 연결 QR",Modifier.size(230.dp))}
        server?.let{link->Text(link.invitation,style=MaterialTheme.typography.bodySmall);Text("남은 시간 ${remaining/60}:${(remaining%60).toString().padStart(2,'0')}")
            Row{TextButton({clipboard.setText(AnnotatedString(link.invitation))}){Text("주소 복사")};TextButton({stop();message="기기 연결을 닫았어요."}){Text("연결 닫기")}}
            OutlinedButton({val bytes=link.incoming;if(bytes==null){message="아직 도착한 기록이 없어요."}else scope.launch{try{incoming=withContext(Dispatchers.IO){DeviceAddress.validate(bytes)}}catch(e:Exception){message=e.message ?: "백업을 확인해 주세요."}}},enabled=!busy){Text("도착한 기록 확인")}}
        HorizontalDivider()
        OutlinedTextField(address,{address=it},label={Text("다른 기기의 연결 주소")},singleLine=true,modifier=Modifier.fillMaxWidth().testTag("device-address"))
        Row {TextButton({address=clipboard.getText()?.text ?: ""}){Text("주소 붙여넣기")};TextButton({photo.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))}){Text("QR 이미지 읽기")}}
        Button({val text=address;busy=true;scope.launch{try{incoming=withContext(Dispatchers.IO){DeviceAddress.validate(DeviceAddress.download(text))}}catch(e:Exception){message=e.message ?: "연결 주소를 확인해 주세요."}finally{busy=false}}},enabled=!busy && controller.saved,modifier=Modifier.testTag("device-receive")){Text("이 주소에서 기록 받기")}
        Text(message,style=MaterialTheme.typography.bodySmall)
        Text("연결 창을 닫거나 앱을 배경으로 보내면 연결이 닫혀요. 신뢰하는 같은 Wi-Fi에서 사용하세요.",style=MaterialTheme.typography.labelSmall)
    }
    incoming?.let{book->AlertDialog(onDismissRequest={incoming=null},title={Text("도착한 기록을 가져올까요?")},text={Text("${book.days.size}일 · 사진 ${book.days.values.sumOf{e->e.blocks.count{it.photo!=null}}}장\n겹치는 날짜 ${book.days.keys.count{it in controller.book.days}}일은 백업 내용으로 바뀌어요. 성장 기록과 소품은 기존 병합 규칙으로 확인해요.")},confirmButton={TextButton({runCatching{controller.importBook(book)}.onFailure{controller.report(it.message ?: "가져오기 실패")};incoming=null}){Text("가져오기")}},dismissButton={TextButton({incoming=null}){Text("취소")}})}
}
