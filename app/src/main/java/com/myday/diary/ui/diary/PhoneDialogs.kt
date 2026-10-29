package com.myday.diary.ui.diary

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.myday.diary.data.*
import com.myday.diary.diary.NotebookController

@Composable fun PhoneFullDialog(title: String,onDismiss: ()->Unit,error: String?=null,content: @Composable ColumnScope.()->Unit) {
    Dialog(onDismiss,properties=DialogProperties(usePlatformDefaultWidth=false)) {Surface(Modifier.fillMaxSize(),color=Color(0xfff7f5f0)) {Column(Modifier.fillMaxSize().statusBarsPadding().navigationBarsPadding().imePadding()) {
        Row(Modifier.fillMaxWidth().padding(horizontal=16.dp),horizontalArrangement=Arrangement.SpaceBetween) {Text(title,Modifier.padding(top=14.dp),style=MaterialTheme.typography.titleLarge);TextButton(onDismiss) {Text("닫기")}}
        if(error!=null) Text(error,Modifier.padding(16.dp),color=MaterialTheme.colorScheme.error,style=MaterialTheme.typography.bodySmall)
        content()
    }}}
}
@Composable fun LayoutDiagram(blocks: List<DiaryBlock>) {
    Canvas(Modifier.fillMaxWidth().height(130.dp)) {
        val width=maxOf(900,blocks.maxOfOrNull {it.x+it.width} ?: 900).toFloat();val height=maxOf(500,blocks.maxOfOrNull {it.y+it.height} ?: 500).toFloat()
        val scale=minOf(size.width/width,size.height/height)
        blocks.forEach {drawRoundRect(if(it.type in setOf("photo","photo-slot")) Color(0xffb4c3d2) else if(it.type=="emotion") Color(0xffdcc6bd) else Color(0xffc6b69f),
            topLeft=androidx.compose.ui.geometry.Offset(it.x*scale,it.y*scale),size=androidx.compose.ui.geometry.Size(it.width*scale,it.height*scale),cornerRadius=androidx.compose.ui.geometry.CornerRadius(6f))}
    }
}
@OptIn(ExperimentalLayoutApi::class)
@Composable fun TemplatePicker(catalog: NotebookCatalog,onApply: (DiaryTemplate)->Unit,onAddBlock: (String)->Unit,onDismiss: ()->Unit,error: String?=null) {
    var query by remember {mutableStateOf("")};var selected by remember {mutableStateOf<DiaryTemplate?>(null)}
    var featuredOnly by remember {mutableStateOf(false)}
    val keyboard=LocalSoftwareKeyboardController.current
    PhoneFullDialog("일기 형식 · 블록 추가",onDismiss,error) {
        Text("블록 바로 추가",Modifier.padding(horizontal=16.dp,vertical=8.dp),style=MaterialTheme.typography.titleMedium)
        FlowRow(Modifier.fillMaxWidth().padding(horizontal=16.dp),horizontalArrangement=Arrangement.spacedBy(8.dp)) {
            listOf("text" to "글","todo" to "할 일","habit" to "습관","emotion" to "감정","photo" to "사진").forEach {(kind,title)->OutlinedButton({onAddBlock(kind)}) {Text(title)}}
        }
        OutlinedTextField(query,{query=it},label={Text("분야·이름·질문 검색")},modifier=Modifier.fillMaxWidth().padding(16.dp),singleLine=true)
        Row(Modifier.padding(horizontal=16.dp),horizontalArrangement=Arrangement.spacedBy(8.dp)){FilterChip(featuredOnly,{featuredOnly=true},label={Text("추천 20종")});FilterChip(!featuredOnly,{featuredOnly=false},label={Text("전체 100종")})}
        selected?.let {template->Card(Modifier.fillMaxWidth().padding(horizontal=16.dp)) {LazyColumn(Modifier.heightIn(max=330.dp).padding(16.dp)) {
            item {Text(template.name,style=MaterialTheme.typography.titleLarge);Text(template.description);LayoutDiagram(template.blocks)}
            items(template.blocks) {Text("${it.title} · ${it.prompt}",style=MaterialTheme.typography.bodySmall)}
            item {Button({onApply(template)},modifier=Modifier.fillMaxWidth()) {Text("이 형식으로 기록하기")}}
        }}}
        LazyColumn(Modifier.weight(1f).padding(horizontal=16.dp),verticalArrangement=Arrangement.spacedBy(8.dp),contentPadding=PaddingValues(bottom=24.dp)) {
            items(catalog.templates.filter {(!featuredOnly || it.id in catalog.featuredTemplateIds) && (query.isBlank() || "${it.categoryName} ${it.layoutName} ${it.name} ${it.description} ${it.blocks.joinToString {b->b.prompt}}".contains(query,true))},key={it.id}) {template->Card(Modifier.fillMaxWidth().clickable {selected=template;keyboard?.hide()},colors=CardDefaults.cardColors(containerColor=Color.White)) {
                Column(Modifier.padding(16.dp)) {Text(template.name,style=MaterialTheme.typography.titleMedium);Text("${template.categoryName} · 약 ${template.minutes}분 · ${template.layoutName}",style=MaterialTheme.typography.labelSmall);Text(template.description,maxLines=2)}
            }}
        }
    }
}
@Composable fun NameDialog(title: String,initial: String="",onSave: (String)->Unit,onDismiss: ()->Unit,error: String?=null) {
    var name by remember {mutableStateOf(initial)}
    AlertDialog(onDismissRequest=onDismiss,title={Text(title)},text={Column {OutlinedTextField(name,{if(it.length<=40) name=it},label={Text("이름 (1~40자)")},singleLine=true);if(error!=null) Text(error,color=MaterialTheme.colorScheme.error)}},
        confirmButton={TextButton({onSave(name)},enabled=name.isNotBlank()) {Text("저장")}},dismissButton={TextButton(onDismiss) {Text("취소")}})
}
@Composable fun SavedLayoutPicker(controller: NotebookController,onApply: (SavedLayout)->Unit,onSave: ()->Unit,onRename: (SavedLayout)->Unit,onDelete: (String)->Unit,onDismiss: ()->Unit) {
    var query by remember {mutableStateOf("")};var deleting by remember {mutableStateOf<String?>(null)}
    PhoneFullDialog("내 레이아웃",onDismiss,controller.error) {
        Button(onSave,modifier=Modifier.fillMaxWidth().padding(horizontal=16.dp)) {Text("현재 배치 저장")}
        OutlinedTextField(query,{query=it},label={Text("저장한 배치 검색")},modifier=Modifier.fillMaxWidth().padding(16.dp))
        LazyColumn(Modifier.weight(1f).padding(horizontal=16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)) {
            if(controller.book.layouts.isEmpty()) item {Text("직접 꾸민 블록을 저장하면 다른 날짜에 다시 사용할 수 있어요. 본문과 실제 사진은 복사하지 않아요.")}
            items(controller.book.layouts.filter {it.name.contains(query,true)},key={it.id}) {layout->Card {Column(Modifier.padding(16.dp)) {Text(layout.name,style=MaterialTheme.typography.titleLarge);LayoutDiagram(layout.blocks)
                Row {TextButton({onApply(layout)}) {Text("불러오기")};TextButton({onRename(layout)}) {Text("이름 변경")};TextButton({deleting=layout.id}) {Text("삭제")}}
            }}}
        }
    }
    deleting?.let {id->AlertDialog(onDismissRequest={deleting=null},title={Text("저장한 배치를 삭제할까요?")},text={Text("이미 작성한 일기는 그대로 유지돼요.")},confirmButton={TextButton({onDelete(id);deleting=null}) {Text("삭제")}},dismissButton={TextButton({deleting=null}) {Text("취소")}})}
}
