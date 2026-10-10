package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.myday.diary.ui.character.CharacterKind

@Composable
fun AddDiaryBlockDialog(character: String, onAdd: (String) -> Unit, onDismiss: () -> Unit) {
    AlertDialog(onDismissRequest = onDismiss, title = { Text("어떤 블록을 넣을까요?") },
        text = { Column {
            listOf("text" to "✍ 글 일기", "todo" to "☑ 할 일", "habit" to "🌱 습관 체크", "emotion" to "${CharacterKind.fromStoredValue(character).displayName}의 감정처리반").forEach { (type, title) ->
                TextButton(onClick = { onAdd(type) }, modifier = Modifier.fillMaxWidth()) { Text(title) }
            }
        } }, confirmButton = { TextButton(onClick = onDismiss) { Text("닫기") } })
}

@Composable
fun DeleteDiaryBlockDialog(onDelete: () -> Unit, onDismiss: () -> Unit) {
    AlertDialog(onDismissRequest = onDismiss, title = { Text("블록을 삭제할까요?") }, text = { Text("이 블록에 작성한 내용도 함께 삭제됩니다.") },
        confirmButton = { TextButton(onClick = onDelete) { Text("삭제") } }, dismissButton = { TextButton(onClick = onDismiss) { Text("취소") } })
}
