package com.myday.diary.ui.diary

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.colorResource
import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import com.myday.diary.ui.components.DiaryActionButton
import com.myday.diary.ui.design.DiaryDimensions
import com.myday.diary.ui.design.DiaryOptions
import java.time.LocalDate

data class DiaryScreenActions(
    val onPreviousDay: () -> Unit,
    val onNextDay: () -> Unit,
    val onChooseDate: () -> Unit,
    val onRetrySave: () -> Unit,
    val onThemeChange: (Int) -> Unit,
    val onCharacterChange: (String) -> Unit,
    val onAddBlock: (String) -> Unit,
    val onEditBlock: (DiaryBlock) -> Unit,
    val onMoveBlock: (Int, Int) -> Unit,
    val onDeleteBlock: (String) -> Unit,
    val onPinWidget: () -> Unit,
    val onSetWallpaper: () -> Unit
)

/** Arrange sections here. No database, launcher or wallpaper side effects are used. */
@Composable
fun DiaryScreen(date: LocalDate, entry: DiaryEntry, saved: Boolean, actions: DiaryScreenActions) {
    var showAdd by remember { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<String?>(null) }
    Scaffold(containerColor = colorResource(DiaryOptions.backgroundResource(entry.theme))) { padding ->
        LazyColumn(Modifier.fillMaxSize().padding(padding).imePadding().padding(horizontal = DiaryDimensions.screenPadding),
            verticalArrangement = Arrangement.spacedBy(DiaryDimensions.sectionGap),
            contentPadding = PaddingValues(vertical = DiaryDimensions.screenVerticalPadding)) {
            item { DiaryHeader() }
            item { DiaryDateBar(date, saved, actions) }
            item { DiaryAppearancePicker(entry, actions) }
            item { HomeScreenControls(actions) }
            if (entry.blocks.isEmpty()) item { DiaryEmptyState(entry.character) }
            itemsIndexed(entry.blocks, key = { _, block -> block.id }) { index, block ->
                DiaryBlockCard(block, entry.character, canMoveUp = index > 0, canMoveDown = index < entry.blocks.lastIndex,
                    onEdit = actions.onEditBlock, onMoveUp = { actions.onMoveBlock(index, -1) },
                    onMoveDown = { actions.onMoveBlock(index, 1) }, onDelete = { deleting = block.id })
            }
            item { DiaryActionButton("+ 블록 추가", { showAdd = true }) }
            item { Text("감정처리반은 감정을 적어 보관하는 공간입니다. AI 대화는 아직 제공하지 않습니다.", style = MaterialTheme.typography.bodySmall) }
        }
    }
    if (showAdd) AddDiaryBlockDialog(entry.character, onAdd = { actions.onAddBlock(it); showAdd = false }, onDismiss = { showAdd = false })
    deleting?.let { id -> DeleteDiaryBlockDialog(onDelete = { actions.onDeleteBlock(id); deleting = null }, onDismiss = { deleting = null }) }
}
