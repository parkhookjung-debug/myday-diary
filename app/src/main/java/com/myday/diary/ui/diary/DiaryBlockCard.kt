package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import com.myday.diary.data.DiaryBlock
import com.myday.diary.ui.components.DiaryBlockSurface
import com.myday.diary.ui.design.DiaryDimensions

@Composable
fun DiaryBlockCard(block: DiaryBlock, character: String, canMoveUp: Boolean, canMoveDown: Boolean,
    onEdit: (DiaryBlock) -> Unit, onMoveUp: () -> Unit, onMoveDown: () -> Unit, onDelete: () -> Unit) {
    DiaryBlockSurface {
        Column(Modifier.padding(DiaryDimensions.cardPadding), verticalArrangement = Arrangement.spacedBy(DiaryDimensions.contentGap)) {
            Text(when (block.type) { "todo" -> "☑ 할 일"; "habit" -> "🌱 습관 체크"; "emotion" -> "$character 감정처리반"; else -> "✍ 오늘의 일기" }, style = MaterialTheme.typography.titleMedium)
            if (block.type == "emotion") Text("판단 없이 들어줄게. 지금 마음을 들려줘.", style = MaterialTheme.typography.bodyMedium)
            OutlinedTextField(value = block.text, onValueChange = { onEdit(block.copy(text = it)) }, modifier = Modifier.fillMaxWidth(),
                minLines = if (block.type in listOf("text", "emotion")) DiaryDimensions.diaryMinimumLines else DiaryDimensions.taskMinimumLines,
                label = { Text(when (block.type) { "todo" -> "해야 할 일"; "habit" -> "오늘 실천할 습관"; "emotion" -> "내 마음 기록"; else -> "오늘은 어떤 하루였나요?" }) })
            if (block.type in listOf("todo", "habit")) {
                Row {
                    Checkbox(checked = block.checked, onCheckedChange = { onEdit(block.copy(checked = it)) })
                    Text(if (block.checked) "오늘 완료했어요" else "완료하면 체크해 주세요", modifier = Modifier.padding(top = DiaryDimensions.checkboxTextTop))
                }
            }
            Row {
                TextButton(onClick = onMoveUp, enabled = canMoveUp) { Text("↑ 위로") }
                TextButton(onClick = onMoveDown, enabled = canMoveDown) { Text("↓ 아래로") }
                Spacer(Modifier.weight(1f))
                TextButton(onClick = onDelete) { Text("삭제") }
            }
        }
    }
}
