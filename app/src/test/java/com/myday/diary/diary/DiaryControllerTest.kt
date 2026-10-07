package com.myday.diary.diary

import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import com.myday.diary.data.DiaryRepository
import org.junit.Assert.*
import org.junit.Test
import java.time.LocalDate

class DiaryControllerTest {
    private val firstDay = LocalDate.of(2026, 10, 8)
    private val secondDay = firstDay.plusDays(1)

    private class MemoryRepository : DiaryRepository {
        val entries = mutableMapOf<String, DiaryEntry>()
        var failWrites = false
        override fun read(date: String) = entries[date] ?: DiaryEntry()
        override fun write(date: String, entry: DiaryEntry): Boolean {
            if (failWrites) return false
            entries[date] = entry
            return true
        }
    }

    @Test
    fun failedSaveKeepsWorkingEntryUntilRetryBeforeDateNavigation() {
        val repository = MemoryRepository()
        val controller = DiaryController(repository, firstDay)
        repository.failWrites = true
        controller.addBlock("text")
        val unsavedBlock = controller.entry.blocks.single().copy(text = "잃으면 안 되는 기록")
        controller.editBlock(unsavedBlock)
        controller.selectDate(secondDay)
        assertEquals(firstDay, controller.date)
        assertEquals(unsavedBlock, controller.entry.blocks.single())
        assertFalse(controller.saved)
        assertTrue(repository.entries.isEmpty())

        repository.failWrites = false
        controller.retrySave()
        controller.selectDate(secondDay)
        assertTrue(controller.entry.blocks.isEmpty())
        controller.selectDate(firstDay)
        assertEquals(unsavedBlock, controller.entry.blocks.single())
        assertTrue(controller.saved)
    }

    @Test
    fun reorderingEditingAndRestartPreserveBlockIdentityAndCompletion() {
        val repository = MemoryRepository()
        val text = DiaryBlock("text-id", "text", "원래 기록")
        val todo = DiaryBlock("todo-id", "todo", "오늘 할 일", checked = true)
        repository.entries[firstDay.toString()] = DiaryEntry(blocks = listOf(text, todo))
        val controller = DiaryController(repository, firstDay)
        controller.moveBlock(1, -1)
        controller.editBlock(text.copy(text = "수정한 기록"))
        controller.selectTheme(2)
        controller.selectCharacter("🐱")

        val reopened = DiaryController(repository, firstDay)
        assertEquals(listOf("todo-id", "text-id"), reopened.entry.blocks.map { it.id })
        assertEquals(todo, reopened.entry.blocks.first())
        assertEquals("수정한 기록", reopened.entry.blocks.last().text)
        assertEquals(2, reopened.entry.theme)
        assertEquals("🐱", reopened.entry.character)
    }

    @Test
    fun deletingOneDayDoesNotChangeAnotherDayAndBoundaryMovesAreSafe() {
        val repository = MemoryRepository()
        val first = DiaryBlock("first-id", "habit", "산책", checked = true)
        val second = DiaryBlock("second-id", "emotion", "오늘의 마음")
        repository.entries[firstDay.toString()] = DiaryEntry(blocks = listOf(first))
        repository.entries[secondDay.toString()] = DiaryEntry(blocks = listOf(second))
        val controller = DiaryController(repository, firstDay)
        controller.moveBlock(0, -1)
        controller.moveBlock(0, 1)
        assertEquals(listOf(first), controller.entry.blocks)
        controller.deleteBlock(first.id)
        controller.selectDate(secondDay)
        assertEquals(listOf(second), controller.entry.blocks)
        controller.selectDate(firstDay)
        assertTrue(controller.entry.blocks.isEmpty())
    }
}
