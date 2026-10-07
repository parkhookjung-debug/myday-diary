package com.myday.diary.ui.preview

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.tooling.preview.Preview
import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import com.myday.diary.data.DiaryRepository
import com.myday.diary.diary.DiaryController
import com.myday.diary.ui.design.MyDayTheme
import com.myday.diary.ui.diary.DiaryBlockCard
import com.myday.diary.ui.diary.DiaryScreen
import com.myday.diary.ui.diary.DiaryScreenActions
import java.time.LocalDate

private val exampleDate = LocalDate.of(2026, 10, 8)
private fun exampleEntry(theme: Int) = DiaryEntry(theme = theme, blocks = listOf(
    DiaryBlock(id = "preview-text", type = "text", text = "오늘은 일기 앱의 첫 화면을 만들었다."),
    DiaryBlock(id = "preview-todo", type = "todo", text = "앱 실행해 보기", checked = true),
    DiaryBlock(id = "preview-habit", type = "habit", text = "책 10쪽 읽기"),
    DiaryBlock(id = "preview-emotion", type = "emotion", text = "새로운 시작이라 설렌다.")
))

/** Preview edits are held only in memory. No phone storage or system setup is used. */
private class PreviewRepository(initial: DiaryEntry) : DiaryRepository {
    private val entries = mutableMapOf(exampleDate.toString() to initial)
    override fun read(date: String) = entries[date] ?: DiaryEntry()
    override fun write(date: String, entry: DiaryEntry): Boolean { entries[date] = entry; return true }
}

@Composable
private fun DiaryExample(theme: Int = 0, empty: Boolean = false) {
    val controller = remember { DiaryController(PreviewRepository(if (empty) DiaryEntry() else exampleEntry(theme)), exampleDate) }
    MyDayTheme {
        DiaryScreen(controller.date, controller.entry, controller.saved, DiaryScreenActions(
            onPreviousDay = { controller.selectDate(controller.date.minusDays(1)) },
            onNextDay = { controller.selectDate(controller.date.plusDays(1)) },
            onChooseDate = {},
            onRetrySave = controller::retrySave,
            onThemeChange = controller::selectTheme,
            onCharacterChange = controller::selectCharacter,
            onAddBlock = controller::addBlock,
            onEditBlock = controller::editBlock,
            onMoveBlock = controller::moveBlock,
            onDeleteBlock = controller::deleteBlock,
            onPinWidget = {},
            onSetWallpaper = {}
        ))
    }
}

@Preview(name = "일기 화면 · 크림", showBackground = true, widthDp = 390, heightDp = 850)
@Composable
fun CreamDiaryPreview() = DiaryExample()

@Preview(name = "일기 화면 · 숲", showBackground = true, widthDp = 390, heightDp = 850)
@Composable
fun ForestDiaryPreview() = DiaryExample(theme = 1)

@Preview(name = "일기 화면 · 라벤더", showBackground = true, widthDp = 390, heightDp = 850)
@Composable
fun LavenderDiaryPreview() = DiaryExample(theme = 2)

@Preview(name = "빈 하루", showBackground = true, widthDp = 390, heightDp = 850)
@Composable
fun EmptyDiaryPreview() = DiaryExample(empty = true)

@Preview(name = "일기 카드", showBackground = true, widthDp = 350)
@Composable
fun DiaryBlockPreview() {
    var block by remember { mutableStateOf(exampleEntry(0).blocks.first()) }
    MyDayTheme {
        DiaryBlockCard(block, "🐰", false, false, onEdit = { block = it }, onMoveUp = {}, onMoveDown = {}, onDelete = {})
    }
}
