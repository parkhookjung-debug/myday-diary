package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import com.myday.diary.data.*
import com.myday.diary.diary.TodoRules
import com.myday.diary.platform.*

@Composable fun PhoneTodoList(block: DiaryBlock,haptics: DiaryHaptics,onEdit: (DiaryBlock)->Unit) {
    var draft by remember(block.id) {mutableStateOf("")}
    var problem by remember(block.id) {mutableStateOf<String?>(null)}
    val items=TodoRules.items(block)
    fun commit(next: List<DiaryTodoItem>) {runCatching {TodoRules.update(block,next)}.onSuccess {onEdit(it);problem=null}.onFailure {problem=it.message ?: "목록을 저장하지 못했어요."}}
    fun add() {if(draft.trim().isNotEmpty() && items.size<100) {val next=items+DiaryTodoItem(draft.trim());if(next.sumOf {it.text.length}+next.size-1<=100000) {commit(next);draft="";haptics.perform(TactileEvent.CHECK)}else problem="할 일 내용은 전체 100,000자까지예요."}}
    Column(verticalArrangement=Arrangement.spacedBy(8.dp)) {
    Row(verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(8.dp)) {
        OutlinedTextField(draft,{draft=it.take(100000)},label={Text("새 할 일")},modifier=Modifier.weight(1f).testTag("todo-input"),singleLine=true,
            keyboardOptions=KeyboardOptions(imeAction=ImeAction.Done),keyboardActions=KeyboardActions(onDone={add()}))
        Button({add()},enabled=draft.isNotBlank() && items.size<100,modifier=Modifier.testTag("todo-add")) {Text("추가")}
    }
    Text("할 일 ${items.count {it.checked}} / ${items.size} · 중요도 상·중·하",style=MaterialTheme.typography.labelMedium)
    items.forEachIndexed {index,item ->
        Column {
            Row(verticalAlignment=Alignment.CenterVertically) {
                Checkbox(item.checked,{value->commit(items.mapIndexed {i,t->if(i==index)t.copy(checked=value) else t});haptics.perform(TactileEvent.CHECK)},modifier=Modifier.testTag("todo-check-$index"))
                OutlinedTextField(item.text,{text->commit(items.mapIndexed {i,t->if(i==index)t.copy(text=text) else t})},modifier=Modifier.weight(1f).testTag("todo-text-$index"),label={Text("할 일 ${index+1}")},singleLine=true)
                TextButton({commit(items.filterIndexed {i,_->i!=index})},modifier=Modifier.testTag("todo-delete-$index")) {Text("삭제")}
            }
            Row(Modifier.padding(start=40.dp),horizontalArrangement=Arrangement.spacedBy(6.dp)) {
                listOf(1 to "상",0 to "중",2 to "하").forEach {(value,title)->FilterChip(item.importance==value,{commit(items.mapIndexed {i,t->if(i==index)t.copy(importance=value) else t})},label={Text(title)},modifier=Modifier.testTag("todo-priority-$index-$value"))}
            }
        }
    }
    if(items.isNotEmpty()) Row(verticalAlignment=Alignment.CenterVertically) {
        Checkbox(items.all {it.checked},{value->commit(items.map {it.copy(checked=value)})},modifier=Modifier.testTag("todo-all"));Text("목록 모두 완료")
    }
    problem?.let {Text(it,color=MaterialTheme.colorScheme.error)}
    }
}
