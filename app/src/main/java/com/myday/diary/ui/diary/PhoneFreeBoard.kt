package com.myday.diary.ui.diary

import androidx.compose.foundation.*
import androidx.compose.foundation.gestures.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.*
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.*
import androidx.compose.ui.unit.*
import com.myday.diary.data.*
import com.myday.diary.diary.NotebookRules
import com.myday.diary.platform.*
import kotlin.math.roundToInt

/** A bounded viewport preserves large desktop coordinates without giant mobile layouts. */
@Composable fun PhoneFreeBoard(entry: DiaryEntry,editing: Boolean,haptics: DiaryHaptics,onBounds: (DiaryBlock)->Unit,onOpen: (String)->Unit) {
    var zoom by remember {mutableFloatStateOf(.45f)}
    var originX by remember {mutableFloatStateOf(0f)};var originY by remember {mutableFloatStateOf(0f)}
    var viewportWidth by remember {mutableFloatStateOf(360f)}
    var chooseBlock by remember {mutableStateOf(false)}
    val density=LocalDensity.current
    val blocks=entry.blocks.map(NotebookRules::bound)
    val sceneWidth=maxOf(900,blocks.maxOfOrNull {it.x+it.width+24} ?: 900).toFloat()
    val sceneHeight=maxOf(700,blocks.maxOfOrNull {it.y+it.height+24} ?: 700).toFloat()
    fun constrain() {originX=originX.coerceIn(0f,maxOf(0f,sceneWidth-viewportWidth/zoom));originY=originY.coerceIn(0f,maxOf(0f,sceneHeight-470/zoom))}
    Column {
        Text(if(editing) "카드를 길게 눌러 이동 · ↘로 크기 조절 · 두 손가락으로 화면 이동" else "카드를 눌러 편집 · 빈 곳을 끌어 화면 이동 · 두 손가락으로 확대",style=MaterialTheme.typography.bodySmall)
        Row(verticalAlignment=Alignment.CenterVertically) {Text("확대 ${(zoom*100).roundToInt()}%",style=MaterialTheme.typography.labelSmall);Slider(zoom,{zoom=it;constrain()},valueRange=.3f..1.1f,modifier=Modifier.weight(1f))}
        Row {listOf("←" to Offset(-200f,0f),"→" to Offset(200f,0f),"↑" to Offset(0f,-200f),"↓" to Offset(0f,200f)).forEach {(title,delta)->TextButton({originX+=delta.x;originY+=delta.y;constrain()}) {Text(title)}}}
        Box {TextButton({chooseBlock=true}) {Text("블록으로 이동 ▾")};DropdownMenu(chooseBlock,{chooseBlock=false}) {
            blocks.forEachIndexed {index,b->DropdownMenuItem(text={Text(b.title.ifBlank {"기록 ${index+1}"},maxLines=1)},onClick={originX=maxOf(0f,b.x-12f);originY=maxOf(0f,b.y-12f);constrain();chooseBlock=false;haptics.perform(TactileEvent.SELECT)})}
        }}
        Box(Modifier.fillMaxWidth().height(470.dp).onSizeChanged {viewportWidth=it.width/density.density}.clip(RoundedCornerShape(16.dp)).background(Color(0xfff7f5f0)).border(1.dp,Color(0xffe9e4db),RoundedCornerShape(16.dp)).pointerInput(editing,sceneWidth,sceneHeight) {
            awaitEachGesture {
                awaitFirstDown(requireUnconsumed=false)
                var total=Offset.Zero
                do {
                    val event=awaitPointerEvent();val active=event.changes.count {it.pressed}
                    val pan=event.calculatePan();total+=pan
                    if(active>1 || !editing && total.getDistance()>viewConfiguration.touchSlop && event.changes.none {it.isConsumed}) {
                        val previous=zoom;val focal=event.calculateCentroid(useCurrent=false)/density.density
                        val world=Offset(originX,originY)+focal/previous
                        zoom=(zoom*event.calculateZoom()).coerceIn(.3f,1.1f)
                        originX=world.x-focal.x/zoom-pan.x/density.density/zoom;originY=world.y-focal.y/zoom-pan.y/density.density/zoom;constrain()
                        event.changes.forEach {if(it.positionChanged()) it.consume()}
                    }
                } while(event.changes.any {it.pressed})
            }
        }) {
            Canvas(Modifier.fillMaxSize()) {
                if(entry.pageStyle=="dots") for(x in 0..(size.width/22).toInt()) for(y in 0..(size.height/22).toInt()) drawCircle(Color(0xffe0d7ca),1f,Offset(x*22f,y*22f))
                if(entry.pageStyle=="paper") for(y in 1..(size.height/32).toInt()) drawLine(Color(0xffe9e4db),Offset(0f,y*32f),Offset(size.width,y*32f),1f)
            }
            blocks.filter {it.x+it.width>=originX && it.y+it.height>=originY && it.x<=originX+viewportWidth/zoom && it.y<=originY+470/zoom}.forEach {block->key(block.id) {
                val current by rememberUpdatedState(block);val commitBounds by rememberUpdatedState(onBounds)
                var x by remember(block.x) {mutableFloatStateOf(block.x.toFloat())};var y by remember(block.y) {mutableFloatStateOf(block.y.toFloat())}
                var w by remember(block.width) {mutableFloatStateOf(block.width.toFloat())};var h by remember(block.height) {mutableFloatStateOf(block.height.toFloat())}
                var moving by remember {mutableStateOf(false)}
                val deltaScale=zoom*density.density
                fun commit() {commitBounds(current.copy(x=x.roundToInt(),y=y.roundToInt(),width=w.roundToInt(),height=h.roundToInt()));haptics.perform(TactileEvent.DROP)}
                val gestures=if(editing) Modifier.pointerInput(block.id,zoom) {
                    detectDragGesturesAfterLongPress(onDragStart={moving=true;haptics.perform(TactileEvent.GRAB)},onDragEnd={moving=false;commit()},onDragCancel={moving=false;x=block.x.toFloat();y=block.y.toFloat()},
                        onDrag={change,delta->change.consume();x=(x+delta.x/deltaScale).coerceIn(0f,10000f);y=(y+delta.y/deltaScale).coerceIn(0f,10000f)})
                } else Modifier
                Surface(Modifier.testTag("free-block-${block.id}").absoluteOffset {IntOffset(((x-originX)*deltaScale).roundToInt(),((y-originY)*deltaScale).roundToInt())}
                    .wrapContentSize(Alignment.TopStart,unbounded=true).requiredSize((w*zoom).dp,(h*zoom).dp).then(gestures).clickable(enabled=!moving) {onOpen(block.id)},
                    shape=RoundedCornerShape(12.dp),color=if(block.type=="emotion") Color(0xfff3e6df) else Color.White,shadowElevation=if(moving) 8.dp else 1.dp) {
                    Box {
                        Column(Modifier.padding(10.dp)) {
                            Text(block.title.ifBlank {when(block.type) {"todo"->"할 일";"habit"->"습관";"photo","photo-slot"->"사진 기록";"emotion"->"감정";else->"글 일기"}},style=MaterialTheme.typography.labelLarge,maxLines=1)
                            if(block.photo!=null) PhonePhoto(block.photo,Modifier.fillMaxWidth().heightIn(max=(h*zoom*.55f).dp))
                            Text(block.text.ifBlank {block.prompt.ifBlank {"눌러서 기록하기"}},style=MaterialTheme.typography.bodySmall,maxLines=3)
                            if(block.type in setOf("todo","habit")) Text(if(block.checked) "☑ 완료" else "☐ 미완료",style=MaterialTheme.typography.labelSmall)
                        }
                        if(editing) Box(Modifier.align(Alignment.BottomEnd).size(36.dp).semantics {contentDescription="블록 크기 조절"}.background(Color(0xfff3e6df)).pointerInput(block.id,zoom) {
                            detectDragGestures(onDragStart={haptics.perform(TactileEvent.GRAB)},onDragEnd={commit()},onDragCancel={w=block.width.toFloat();h=block.height.toFloat()},onDrag={change,delta->change.consume();w=(w+delta.x/deltaScale).coerceIn(NotebookRules.MIN_WIDTH.toFloat(),NotebookRules.MAX_WIDTH.toFloat());h=(h+delta.y/deltaScale).coerceIn(NotebookRules.MIN_HEIGHT.toFloat(),NotebookRules.MAX_HEIGHT.toFloat())})
                        },contentAlignment=Alignment.Center) {Text("↘")}
                    }
                }
            }}
        }
    }
}
