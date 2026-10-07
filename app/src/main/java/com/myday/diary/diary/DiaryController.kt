package com.myday.diary.diary

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.myday.diary.data.DiaryBlock
import com.myday.diary.data.DiaryEntry
import com.myday.diary.data.DiaryRepository
import java.time.LocalDate

/** Handles edits and saving; never depends on colors, layout or Android views. */
class DiaryController(private val repository: DiaryRepository, initialDate: LocalDate = LocalDate.now()) {
    var date by mutableStateOf(initialDate)
        private set
    var entry by mutableStateOf(repository.read(initialDate.toString()))
        private set
    var saved by mutableStateOf(true)
        private set

    fun selectDate(nextDate: LocalDate) {
        if (!saved || nextDate == date) return
        date = nextDate
        entry = repository.read(date.toString())
        saved = true
    }

    private fun update(next: DiaryEntry) {
        entry = next
        saved = repository.write(date.toString(), next)
    }

    fun retrySave() { saved = repository.write(date.toString(), entry) }
    fun selectTheme(theme: Int) = update(entry.copy(theme = theme))
    fun selectCharacter(character: String) = update(entry.copy(character = character))
    fun addBlock(type: String) = update(entry.copy(blocks = entry.blocks + DiaryBlock(type = type)))
    fun editBlock(block: DiaryBlock) = update(entry.copy(blocks = entry.blocks.map { if (it.id == block.id) block else it }))
    fun deleteBlock(id: String) = update(entry.copy(blocks = entry.blocks.filterNot { it.id == id }))

    fun moveBlock(index: Int, delta: Int) {
        val next = entry.blocks.toMutableList()
        val destination = index + delta
        if (index !in next.indices || destination !in next.indices) return
        next.add(destination, next.removeAt(index))
        update(entry.copy(blocks = next))
    }
}
