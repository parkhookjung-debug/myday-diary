package com.myday.diary.ui.diary

import android.graphics.BitmapFactory
import android.util.Base64
import androidx.compose.foundation.Image
import androidx.compose.foundation.gestures.detectDragGesturesAfterLongPress
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.dp
import com.myday.diary.data.DiaryBlock
import com.myday.diary.platform.*
import kotlin.math.abs

@Composable fun PhonePhoto(data: String,modifier: Modifier=Modifier) {
    val bitmap=remember(data) {runCatching {
        require(data.length<=com.myday.diary.data.NotebookPhoto.MAX_CHARACTERS)
        val bytes=Base64.decode(data,Base64.DEFAULT)
        val bounds=BitmapFactory.Options().apply {inJustDecodeBounds=true}
        BitmapFactory.decodeByteArray(bytes,0,bytes.size,bounds)
        require(bounds.outWidth in 1..1600 && bounds.outHeight in 1..1600)
        BitmapFactory.decodeByteArray(bytes,0,bytes.size,BitmapFactory.Options().apply {inSampleSize=2})
    }.getOrNull()}
    DisposableEffect(bitmap) {onDispose {bitmap?.recycle()}}
    if(bitmap!=null) Image(bitmap.asImageBitmap(),"일기에 첨부한 사진",modifier,contentScale=ContentScale.Fit)
    else Text("사진을 표시하지 못했어요.")
}

@Composable fun PhoneBlockEditor(block: DiaryBlock,style: String,haptics: DiaryHaptics,onEdit: (DiaryBlock)->Unit,
    onMove: (Int)->Unit,onDelete: ()->Unit,onPhoto: ()->Unit,canMoveUp: Boolean=true,canMoveDown: Boolean=true) {
    var dragging by remember(block.id) {mutableFloatStateOf(0f)}
    val density=LocalDensity.current
    val title=block.title.ifBlank {when(block.type) {"todo"->"할 일";"habit"->"습관 체크";"emotion"->"감정처리반";"photo","photo-slot"->"사진 기록";else->"오늘의 일기"}}
    val ink=MaterialTheme.colorScheme.primary
    val fill=when {block.type=="emotion"->Color(0xfff3e6df);style=="paper"->Color(0xfffffcf8);else->Color.White}
    Card(Modifier.fillMaxWidth().graphicsLayer {translationY=dragging},shape=RoundedCornerShape(22.dp),colors=CardDefaults.cardColors(containerColor=fill)) {
        Column(Modifier.padding(18.dp),verticalArrangement=Arrangement.spacedBy(10.dp)) {
            Row(Modifier.fillMaxWidth().semantics {contentDescription="$title 블록 이동, 길게 누르고 위아래로 끌기"}.pointerInput(block.id,canMoveUp,canMoveDown) {
                detectDragGesturesAfterLongPress(onDragStart={haptics.perform(TactileEvent.GRAB)},onDragEnd={
                    if(abs(dragging)>with(density) {40.dp.toPx()}) {val direction=if(dragging>0) 1 else -1;if(direction>0 && canMoveDown || direction<0 && canMoveUp) onMove(direction)}
                    dragging=0f;haptics.perform(TactileEvent.DROP)
                },onDragCancel={dragging=0f},onDrag={change,delta->change.consume();dragging+=delta.y})
            },horizontalArrangement=Arrangement.SpaceBetween) {
                Column(Modifier.weight(1f)) {Text(title,style=MaterialTheme.typography.titleMedium);Text(if(style=="dots") "· · ·  나의 하루 조각" else "나의 하루 조각",style=MaterialTheme.typography.labelSmall,color=ink)}
                Text("⋮⋮",color=ink,modifier=Modifier.padding(10.dp))
            }
            if(block.photo!=null) PhonePhoto(block.photo,Modifier.fillMaxWidth().heightIn(max=280.dp))
            if(block.type in setOf("photo","photo-slot")) OutlinedButton(onClick=onPhoto,modifier=Modifier.fillMaxWidth()) {Text(if(block.photo==null) "사진 선택" else "사진 바꾸기")}
            if(block.type in setOf("photo","photo-slot")) OutlinedTextField(block.title,{if(it.length<=80)onEdit(block.copy(title=it))},
                modifier=Modifier.fillMaxWidth(),label={Text("사진 제목 · 장소")},placeholder={Text("예: 한강에서 보낸 오후")},singleLine=true,shape=RoundedCornerShape(14.dp))
            if(block.prompt.isNotBlank()) Text(block.prompt,style=MaterialTheme.typography.bodySmall,color=MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedTextField(block.text,{if(it.length<=100000) onEdit(block.copy(text=it))},modifier=Modifier.fillMaxWidth(),
                label={Text(if(block.type in setOf("photo","photo-slot")) "사진 속 순간을 자세히" else "내 기록")},
                supportingText=if(block.type in setOf("photo","photo-slot")) {{Text("언제, 누구와, 어떤 일이 있었나요? 사진마다 이야기를 남겨 보세요.")}} else null,
                minLines=if(block.type in setOf("text","emotion","photo","photo-slot")) 3 else 1,shape=RoundedCornerShape(14.dp))
            if(block.type in setOf("todo","habit")) Row {
                Checkbox(block.checked,{onEdit(block.copy(checked=it));haptics.perform(TactileEvent.CHECK)})
                Text(if(block.checked) "오늘 완료했어요" else "완료하면 체크해 주세요",Modifier.padding(top=14.dp))
            }
            Row(Modifier.fillMaxWidth()) {
                TextButton({onMove(-1);haptics.perform(TactileEvent.DROP)},enabled=canMoveUp) {Text("↑ 위로")}
                TextButton({onMove(1);haptics.perform(TactileEvent.DROP)},enabled=canMoveDown) {Text("↓ 아래로")}
                Spacer(Modifier.weight(1f));TextButton(onDelete) {Text("삭제")}
            }
        }
    }
}
